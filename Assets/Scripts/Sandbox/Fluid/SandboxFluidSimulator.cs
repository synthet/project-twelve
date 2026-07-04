using System.Collections.Generic;
using UnityEngine;

namespace ProjectTwelve.Sandbox.Fluid
{
    /// <summary>
    /// Compressible-water cellular automaton for the P2 liquid pass per
    /// <c>docs/wiki/08-liquids.md</c> § "P2-FLUID-001 specification". Flow runs over an
    /// <b>active set</b> so still water costs nothing, and one tick reads a pre-tick snapshot and
    /// accumulates every transfer into a delta buffer applied at the end — so the tick is
    /// <b>order-independent and exactly mass-conserving</b> (each transfer subtracts from the source
    /// and adds the identical amount to the destination) with no directional scan bias. All world
    /// access goes through <see cref="ISandboxFluidGrid"/>, so the core is EditMode-testable without
    /// a scene. Cells that report unloaded are dropped from the active set (distant water is free)
    /// and flow never crosses into them (unloaded reads as solid).
    /// </summary>
    public sealed class SandboxFluidSimulator
    {
        private HashSet<Vector2Int> active = new HashSet<Vector2Int>();
        private HashSet<Vector2Int> nextActive = new HashSet<Vector2Int>();

        // Transfers accumulate in double and are stored back to the float fluid field only once per
        // cell per tick. Each transfer contributes +flow and -flow (the same widened float), so their
        // double contributions cancel exactly — total mass drift is bounded to the final per-cell
        // float rounding, keeping the conservation invariant tight (< 1e-3 over long runs).
        private readonly Dictionary<Vector2Int, double> delta = new Dictionary<Vector2Int, double>();
        private readonly List<Vector2Int> scanOrder = new List<Vector2Int>();

        /// <summary>Cells scheduled for simulation next tick. Zero once fluid has settled.</summary>
        public int ActiveCount => active.Count;

        /// <summary>
        /// Equilibrium mass of the lower of two vertically stacked cells given their combined
        /// <paramref name="total"/> mass. At or below <see cref="SandboxFluidConstants.MaxFill"/>
        /// all of it sits below; above that, compression lets the lower cell hold the overfill that
        /// drives pressure (U-tube rise).
        /// </summary>
        public static float StableStateBelow(float total)
        {
            const float maxFill = SandboxFluidConstants.MaxFill;
            const float maxCompress = SandboxFluidConstants.MaxCompress;
            if (total <= maxFill)
            {
                return maxFill;
            }

            if (total < 2f * maxFill + maxCompress)
            {
                return (maxFill * maxFill + total * maxCompress) / (maxFill + maxCompress);
            }

            return (total + maxCompress) / 2f;
        }

        /// <summary>Wakes a cell for simulation (no-op for solid or unloaded cells).</summary>
        public void Wake(ISandboxFluidGrid grid, int x, int y)
        {
            if (!grid.IsLoaded(x, y) || grid.IsSolid(x, y))
            {
                return;
            }

            active.Add(new Vector2Int(x, y));
        }

        /// <summary>
        /// Wakes a cell and its four face neighbours. Used by the tile-edit flow: an edit at or
        /// adjacent to fluid must re-wake it so surrounding fluid re-flows.
        /// </summary>
        public void WakeWithNeighbors(ISandboxFluidGrid grid, int x, int y)
        {
            Wake(grid, x, y);
            Wake(grid, x - 1, y);
            Wake(grid, x + 1, y);
            Wake(grid, x, y - 1);
            Wake(grid, x, y + 1);
        }

        /// <summary>
        /// Adds (or, with a negative amount, removes) fluid at a cell as an explicit source/sink and
        /// wakes it. This is the only sanctioned way total mass changes between edits.
        /// </summary>
        public void AddFluid(ISandboxFluidGrid grid, int x, int y, float amount)
        {
            grid.SetFluid(x, y, grid.GetFluid(x, y) + amount);
            WakeWithNeighbors(grid, x, y);
        }

        /// <summary>
        /// Advances one simulation tick over the active set, capped at <paramref name="budget"/>
        /// processed cells (excess cells stay awake for the next tick). Returns the number of cells
        /// processed. Total fluid mass is unchanged by a tick.
        /// </summary>
        public int Step(ISandboxFluidGrid grid, int budget = SandboxFluidConstants.MaxActiveCellsPerTick)
        {
            // Fixed order so the budget truncates deterministically; the math itself is
            // order-independent (all reads are pre-tick, all writes deferred to `delta`).
            scanOrder.Clear();
            scanOrder.AddRange(active);
            scanOrder.Sort(CompareBottomUp);

            delta.Clear();
            nextActive.Clear();

            int processed = 0;
            foreach (Vector2Int cell in scanOrder)
            {
                int x = cell.x;
                int y = cell.y;

                // Distant water is free: cells whose chunk unloaded drop out of the active set.
                if (!grid.IsLoaded(x, y))
                {
                    continue;
                }

                if (processed >= budget)
                {
                    nextActive.Add(cell); // defer to next tick, still awake
                    continue;
                }

                processed++;
                if (grid.IsSolid(x, y))
                {
                    continue;
                }

                float remaining = grid.GetFluid(x, y);
                if (remaining <= 0f)
                {
                    continue;
                }

                float movedHere = 0f;

                // 1. down (gravity)
                if (!grid.IsSolid(x, y - 1))
                {
                    float below = grid.GetFluid(x, y - 1);
                    float flow = StableStateBelow(remaining + below) - below;
                    flow = Damp(flow);
                    Transfer(grid, cell, new Vector2Int(x, y - 1), flow, SandboxFluidConstants.MaxTransferPerTick, ref remaining, ref movedHere);
                }

                // 2. sideways (symmetric split toward equal fill; both read the pre-tick snapshot)
                float here = grid.GetFluid(x, y);
                if (remaining > 0f && !grid.IsSolid(x - 1, y))
                {
                    float flow = Damp((here - grid.GetFluid(x - 1, y)) / 4f);
                    Transfer(grid, cell, new Vector2Int(x - 1, y), flow, remaining, ref remaining, ref movedHere);
                }

                if (remaining > 0f && !grid.IsSolid(x + 1, y))
                {
                    float flow = Damp((here - grid.GetFluid(x + 1, y)) / 4f);
                    Transfer(grid, cell, new Vector2Int(x + 1, y), flow, remaining, ref remaining, ref movedHere);
                }

                // 3. up (pressure) — pushes overfill into the cell above so water rises to its source level
                if (remaining > 0f && !grid.IsSolid(x, y + 1))
                {
                    float flow = Damp(remaining - StableStateBelow(remaining + grid.GetFluid(x, y + 1)));
                    Transfer(grid, cell, new Vector2Int(x, y + 1), flow, SandboxFluidConstants.MaxTransferPerTick, ref remaining, ref movedHere);
                }

                if (movedHere >= SandboxFluidConstants.SettleEpsilon)
                {
                    nextActive.Add(cell);
                }
            }

            foreach (KeyValuePair<Vector2Int, double> change in delta)
            {
                Vector2Int cell = change.Key;
                grid.SetFluid(cell.x, cell.y, (float)(grid.GetFluid(cell.x, cell.y) + change.Value));
            }

            (active, nextActive) = (nextActive, active);
            return processed;
        }

        private void Transfer(
            ISandboxFluidGrid grid,
            Vector2Int src,
            Vector2Int dst,
            float flow,
            float cap,
            ref float remaining,
            ref float movedHere)
        {
            flow = Mathf.Min(flow, Mathf.Min(cap, remaining));
            if (flow <= 0f)
            {
                return;
            }

            AddDelta(src, -flow);
            AddDelta(dst, flow);
            remaining -= flow;
            movedHere += flow;

            if (flow >= SandboxFluidConstants.SettleEpsilon)
            {
                nextActive.Add(src);
                WakeInto(grid, nextActive, dst.x, dst.y);
            }
        }

        private void AddDelta(Vector2Int cell, double amount)
        {
            delta.TryGetValue(cell, out double current);
            delta[cell] = current + amount;
        }

        private static void WakeInto(ISandboxFluidGrid grid, HashSet<Vector2Int> set, int x, int y)
        {
            if (!grid.IsLoaded(x, y) || grid.IsSolid(x, y))
            {
                return;
            }

            set.Add(new Vector2Int(x, y));
        }

        private static float Damp(float flow)
        {
            return flow > SandboxFluidConstants.MinFlow ? flow * 0.5f : flow;
        }

        private static int CompareBottomUp(Vector2Int a, Vector2Int b)
        {
            return a.y != b.y ? a.y - b.y : a.x - b.x;
        }
    }
}
