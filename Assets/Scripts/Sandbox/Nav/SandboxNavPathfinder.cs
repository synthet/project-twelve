using System.Collections.Generic;
using UnityEngine;

namespace ProjectTwelve.Sandbox.Nav
{
    /// <summary>
    /// Grid A* for the P2 walker archetype per <c>docs/wiki/09-pathfinding.md</c> § "P2-AI-001
    /// specification". Nodes are supported foot cells (air with solid support below); edges are
    /// walk, jump, and fall moves. The walker body is a 1x1-tile AABB, so the foot cell is the
    /// body cell and jump clearance probes a rectilinear up-across-down approximation of the arc.
    /// Unloaded cells are impassable: the search never expands into unloaded chunks.
    /// </summary>
    public static class SandboxNavPathfinder
    {
        private const float OctileDiagonalWeight = 0.41421356f; // sqrt(2) - 1

        /// <summary>
        /// Finds a followable path between two supported foot cells. Returns
        /// <see cref="SandboxNavStatus.NoPath"/> when either endpoint is not standable or the
        /// reachable set is exhausted, and <see cref="SandboxNavStatus.BudgetExhausted"/> when
        /// <paramref name="maxExpansions"/> nodes were expanded without concluding.
        /// </summary>
        public static SandboxNavPath FindPath(
            ISandboxNavGrid grid,
            Vector2Int start,
            Vector2Int goal,
            ISandboxNavVersionSource versions = null,
            int maxExpansions = SandboxNavConstants.MaxExpansionsPerRequest)
        {
            if (!IsStandable(grid, start.x, start.y) || !IsStandable(grid, goal.x, goal.y))
            {
                return new SandboxNavPath(SandboxNavStatus.NoPath, start, null, versions);
            }

            var open = new MinHeap();
            var gScore = new Dictionary<Vector2Int, float>();
            var cameFrom = new Dictionary<Vector2Int, SandboxNavStep>();
            var closed = new HashSet<Vector2Int>();
            var neighborScratch = new List<SandboxNavStep>(32);

            gScore[start] = 0f;
            open.Push(new HeapEntry(Heuristic(start, goal), 0f, start));

            int expansions = 0;
            while (open.Count > 0)
            {
                HeapEntry current = open.Pop();
                if (closed.Contains(current.Cell))
                {
                    continue; // stale duplicate entry
                }

                if (current.Cell == goal)
                {
                    return new SandboxNavPath(SandboxNavStatus.Found, start, Reconstruct(cameFrom, start, goal), versions);
                }

                if (++expansions > maxExpansions)
                {
                    return new SandboxNavPath(SandboxNavStatus.BudgetExhausted, start, null, versions);
                }

                closed.Add(current.Cell);
                float currentG = gScore[current.Cell];

                neighborScratch.Clear();
                CollectNeighbors(grid, current.Cell, neighborScratch);
                foreach (SandboxNavStep neighbor in neighborScratch)
                {
                    if (closed.Contains(neighbor.Cell))
                    {
                        continue;
                    }

                    float tentative = currentG + MoveCost(current.Cell, neighbor.Cell);
                    if (gScore.TryGetValue(neighbor.Cell, out float known) && tentative >= known)
                    {
                        continue;
                    }

                    gScore[neighbor.Cell] = tentative;
                    cameFrom[neighbor.Cell] = new SandboxNavStep(current.Cell, neighbor.Move);
                    open.Push(new HeapEntry(tentative + Heuristic(neighbor.Cell, goal), tentative, neighbor.Cell));
                }
            }

            return new SandboxNavPath(SandboxNavStatus.NoPath, start, null, versions);
        }

        /// <summary>
        /// Whether a foot cell can be stood on: loaded air with loaded solid support directly
        /// below. This is the single walkability rule shared by pathfinding and spawn selection.
        /// </summary>
        public static bool IsStandable(ISandboxNavGrid grid, int x, int y)
        {
            return grid.IsLoaded(x, y) && !grid.IsSolid(x, y)
                && grid.IsLoaded(x, y - 1) && grid.IsSolid(x, y - 1);
        }

        /// <summary>
        /// Octile heuristic: max(dx, dy) + (sqrt(2) - 1) * min(dx, dy) on the tile grid.
        /// </summary>
        public static float Heuristic(Vector2Int from, Vector2Int to)
        {
            int dx = Mathf.Abs(to.x - from.x);
            int dy = Mathf.Abs(to.y - from.y);
            return Mathf.Max(dx, dy) + OctileDiagonalWeight * Mathf.Min(dx, dy);
        }

        private static float MoveCost(Vector2Int from, Vector2Int to)
        {
            // Manhattan distance for every edge type: walk = 1, jump = gap + rise, fall = 1 + drop.
            return Mathf.Abs(to.x - from.x) + Mathf.Abs(to.y - from.y);
        }

        /// <summary>
        /// Emits neighbors in a fixed order (walk left/right, fall left/right, jumps by landing
        /// column left-to-right then rise low-to-high) so searches are deterministic.
        /// </summary>
        private static void CollectNeighbors(ISandboxNavGrid grid, Vector2Int node, List<SandboxNavStep> results)
        {
            for (int direction = -1; direction <= 1; direction += 2)
            {
                int x = node.x + direction;
                if (IsStandable(grid, x, node.y))
                {
                    results.Add(new SandboxNavStep(new Vector2Int(x, node.y), SandboxNavMove.Walk));
                }
            }

            for (int direction = -1; direction <= 1; direction += 2)
            {
                if (TryGetFallLanding(grid, node, direction, out Vector2Int landing))
                {
                    results.Add(new SandboxNavStep(landing, SandboxNavMove.Fall));
                }
            }

            int maxReach = SandboxNavConstants.MaxJumpGap + 1;
            for (int lx = node.x - maxReach; lx <= node.x + maxReach; lx++)
            {
                if (lx == node.x)
                {
                    continue;
                }

                for (int rise = 0; rise <= SandboxNavConstants.MaxJumpHeight; rise++)
                {
                    if (rise == 0 && Mathf.Abs(lx - node.x) == 1)
                    {
                        continue; // covered by the walk edge
                    }

                    Vector2Int landing = new Vector2Int(lx, node.y + rise);
                    if (IsStandable(grid, landing.x, landing.y) && HasJumpClearance(grid, node, landing))
                    {
                        results.Add(new SandboxNavStep(landing, SandboxNavMove.Jump));
                    }
                }
            }
        }

        /// <summary>
        /// Fall edge: step sideways off a ledge (the side cell is air without support) and drop
        /// through air until the first support, refusing drops beyond
        /// <see cref="SandboxNavConstants.MaxFallDistance"/> (unlimited fall is out of scope for
        /// P2 — no fall damage yet). Any solid or unloaded cell in the falling column blocks the edge.
        /// </summary>
        private static bool TryGetFallLanding(ISandboxNavGrid grid, Vector2Int node, int direction, out Vector2Int landing)
        {
            landing = default;
            int x = node.x + direction;
            if (!grid.IsLoaded(x, node.y) || grid.IsSolid(x, node.y))
            {
                return false;
            }

            if (grid.IsLoaded(x, node.y - 1) && grid.IsSolid(x, node.y - 1))
            {
                return false; // supported side cell: that is the walk edge
            }

            for (int drop = 1; drop <= SandboxNavConstants.MaxFallDistance; drop++)
            {
                int y = node.y - drop;
                if (!grid.IsLoaded(x, y))
                {
                    return false;
                }

                if (grid.IsSolid(x, y))
                {
                    return false; // falling corridor is blocked before support was found
                }

                if (grid.IsLoaded(x, y - 1) && grid.IsSolid(x, y - 1))
                {
                    landing = new Vector2Int(x, y);
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Rectilinear clearance probe approximating the jump arc for the 1x1 walker body:
        /// ascend in the start column to the apex row, traverse the apex row to the landing
        /// column, then descend to the landing cell. Every probed cell must be loaded air.
        /// The apex is one tile above the rise (capped at <see cref="SandboxNavConstants.MaxJumpHeight"/>),
        /// so max-height jumps have no headroom margin and flat gap jumps arc one tile up.
        /// </summary>
        private static bool HasJumpClearance(ISandboxNavGrid grid, Vector2Int from, Vector2Int landing)
        {
            int rise = landing.y - from.y;
            int reach = Mathf.Abs(landing.x - from.x);
            if (rise < 0 || rise > SandboxNavConstants.MaxJumpHeight
                || reach < 1 || reach > SandboxNavConstants.MaxJumpGap + 1)
            {
                return false;
            }

            int apex = Mathf.Min(rise + 1, SandboxNavConstants.MaxJumpHeight);
            int apexY = from.y + apex;

            for (int y = from.y + 1; y <= apexY; y++)
            {
                if (!IsClearAir(grid, from.x, y))
                {
                    return false;
                }
            }

            int step = landing.x > from.x ? 1 : -1;
            for (int x = from.x + step; x != landing.x + step; x += step)
            {
                if (!IsClearAir(grid, x, apexY))
                {
                    return false;
                }
            }

            for (int y = apexY - 1; y > landing.y; y--)
            {
                if (!IsClearAir(grid, landing.x, y))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool IsClearAir(ISandboxNavGrid grid, int x, int y)
        {
            return grid.IsLoaded(x, y) && !grid.IsSolid(x, y);
        }

        private static List<SandboxNavStep> Reconstruct(
            Dictionary<Vector2Int, SandboxNavStep> cameFrom, Vector2Int start, Vector2Int goal)
        {
            var steps = new List<SandboxNavStep>();
            Vector2Int cell = goal;
            while (cell != start)
            {
                SandboxNavStep parent = cameFrom[cell];
                steps.Add(new SandboxNavStep(cell, parent.Move));
                cell = parent.Cell;
            }

            steps.Reverse();
            return steps;
        }

        private readonly struct HeapEntry
        {
            public readonly float F;
            public readonly float G;
            public readonly Vector2Int Cell;

            public HeapEntry(float f, float g, Vector2Int cell)
            {
                F = f;
                G = g;
                Cell = cell;
            }

            /// <summary>
            /// Deterministic ordering: lower f, then lower g, then lexicographic (x, y). Since
            /// f = g + h, comparing g second also orders lower h implicitly.
            /// </summary>
            public int CompareTo(in HeapEntry other)
            {
                int compare = F.CompareTo(other.F);
                if (compare != 0) return compare;
                compare = G.CompareTo(other.G);
                if (compare != 0) return compare;
                compare = Cell.x.CompareTo(other.Cell.x);
                if (compare != 0) return compare;
                return Cell.y.CompareTo(other.Cell.y);
            }
        }

        /// <summary>Binary min-heap over <see cref="HeapEntry"/> with a total, deterministic order.</summary>
        private sealed class MinHeap
        {
            private readonly List<HeapEntry> items = new List<HeapEntry>();

            public int Count => items.Count;

            public void Push(HeapEntry entry)
            {
                items.Add(entry);
                int child = items.Count - 1;
                while (child > 0)
                {
                    int parent = (child - 1) / 2;
                    if (items[child].CompareTo(items[parent]) >= 0)
                    {
                        break;
                    }

                    (items[parent], items[child]) = (items[child], items[parent]);
                    child = parent;
                }
            }

            public HeapEntry Pop()
            {
                HeapEntry root = items[0];
                int last = items.Count - 1;
                items[0] = items[last];
                items.RemoveAt(last);

                int parent = 0;
                while (true)
                {
                    int left = parent * 2 + 1;
                    if (left >= items.Count)
                    {
                        break;
                    }

                    int right = left + 1;
                    int smallest = right < items.Count && items[right].CompareTo(items[left]) < 0 ? right : left;
                    if (items[parent].CompareTo(items[smallest]) <= 0)
                    {
                        break;
                    }

                    (items[parent], items[smallest]) = (items[smallest], items[parent]);
                    parent = smallest;
                }

                return root;
            }
        }
    }
}
