using System.Collections.Generic;
using UnityEngine;

namespace ProjectTwelve.Sandbox.Fluid
{
    /// <summary>
    /// Active-set cellular-automaton water simulation for P2 per
    /// <c>docs/wiki/08-liquids.md</c> § "P2-FLUID-001 specification". Pure C# over an
    /// <see cref="ISandboxFluidGrid"/> (EditMode-testable without a scene). Each tick processes only
    /// awake cells bottom-up — flow down (gravity) → equalize sideways → push excess up (pressure) —
    /// using the finite-water stable-state transfer so total mass is conserved by construction.
    /// Cells sleep once their net transfer falls below <see cref="SandboxFluidConstants.SettleEpsilon"/>,
    /// so still water costs nothing; tile edits re-wake cells through <see cref="Wake"/>.
    /// </summary>
    public sealed class SandboxFluidSimulator
    {
        private readonly ISandboxFluidGrid grid;
        private readonly int seed;

        // Cells scheduled for the next tick. A HashSet dedupes repeated wakes of the same cell. Each
        // tick it is drained into a sorted list (deterministic order) and then cleared so it can
        // re-accumulate the cells that still need work — transfer partners, wakes, and any cells
        // deferred by the budget.
        private readonly HashSet<Vector2Int> active = new HashSet<Vector2Int>();
        private readonly List<Vector2Int> processingOrder = new List<Vector2Int>();

        private long tick;

        public SandboxFluidSimulator(ISandboxFluidGrid grid, int seed = 0)
        {
            this.grid = grid;
            this.seed = seed;
        }

        /// <summary>Number of cells awake and scheduled for the next tick. Zero means fully settled.</summary>
        public int ActiveCount => active.Count;

        /// <summary>Ticks elapsed. Feeds the seeded per-row scan direction so runs are reproducible.</summary>
        public long Tick => tick;

        /// <summary>
        /// Wakes a cell and its four orthogonal neighbours, e.g. after a tile edit under or beside a
        /// pool. Solid/unloaded cells are skipped as sources but are still safe to pass in.
        /// </summary>
        public void Wake(int x, int y)
        {
            WakeSingle(x, y);
            WakeSingle(x - 1, y);
            WakeSingle(x + 1, y);
            WakeSingle(x, y - 1);
            WakeSingle(x, y + 1);
        }

        /// <summary>
        /// Adds fluid at a cell (a source) and wakes it. Clamps the write to non-negative; solid or
        /// unloaded targets are ignored. Returns the amount actually added.
        /// </summary>
        public float AddFluid(int x, int y, float amount)
        {
            if (amount <= 0f || !IsFlowable(x, y))
            {
                return 0f;
            }

            grid.SetFluid(x, y, grid.GetFluid(x, y) + amount);
            Wake(x, y);
            return amount;
        }

        /// <summary>
        /// Advances the simulation one tick and returns the number of cells processed. Cells beyond
        /// <paramref name="maxCells"/> are deferred to the next tick (kept awake) so a large lake
        /// drains over several frames instead of stalling one.
        /// </summary>
        public int ProcessTick(int maxCells = SandboxFluidConstants.MaxActiveCellsPerTick)
        {
            if (active.Count == 0)
            {
                tick++;
                return 0;
            }

            // Snapshot this tick's cells into a deterministic order, then clear the set so it can
            // re-accumulate the cells that still need work as we process (transfer partners, wakes,
            // and any budget-deferred cells). Iteration walks the snapshot list, so mutating the
            // set during processing is safe.
            BuildProcessingOrder();
            active.Clear();

            int budget = Mathf.Max(0, maxCells);
            int processed = 0;
            for (int i = 0; i < processingOrder.Count; i++)
            {
                Vector2Int cell = processingOrder[i];
                if (processed >= budget)
                {
                    // Over budget: defer the rest unchanged to the next tick.
                    WakeSingle(cell.x, cell.y);
                    continue;
                }

                ProcessCell(cell.x, cell.y);
                processed++;
            }

            tick++;
            return processed;
        }

        /// <summary>
        /// Builds the deterministic bottom-up processing order: rows ascending by y, and within a
        /// row the left↔right direction is chosen from a seeded PRNG so no directional bias builds up
        /// while the result stays reproducible for identical seed + tick.
        /// </summary>
        private void BuildProcessingOrder()
        {
            processingOrder.Clear();
            foreach (Vector2Int cell in active)
            {
                processingOrder.Add(cell);
            }

            processingOrder.Sort(CompareRowMajor);

            int i = 0;
            while (i < processingOrder.Count)
            {
                int rowY = processingOrder[i].y;
                int j = i;
                while (j < processingOrder.Count && processingOrder[j].y == rowY)
                {
                    j++;
                }

                if (!RowScansLeftToRight(rowY))
                {
                    processingOrder.Reverse(i, j - i);
                }

                i = j;
            }
        }

        private static int CompareRowMajor(Vector2Int a, Vector2Int b)
        {
            if (a.y != b.y)
            {
                return a.y.CompareTo(b.y);
            }

            return a.x.CompareTo(b.x);
        }

        /// <summary>Seeded per-row scan direction: reproducible for a given seed, tick, and row.</summary>
        private bool RowScansLeftToRight(int rowY)
        {
            uint h = Hash((uint)seed, (uint)(tick & 0xffffffff), (uint)rowY);
            return (h & 1u) == 0u;
        }

        /// <summary>Applies the down → sideways → up rules to one cell, in place.</summary>
        private void ProcessCell(int x, int y)
        {
            if (!IsFlowable(x, y))
            {
                return;
            }

            float remaining = grid.GetFluid(x, y);
            if (remaining <= 0f)
            {
                return;
            }

            // 1. Down (gravity).
            remaining -= FlowDown(x, y, remaining);

            // 2. Sideways (equalize with both open neighbours; a seeded scan orders which first).
            if (remaining > SandboxFluidConstants.SettleEpsilon)
            {
                bool leftFirst = RowScansLeftToRight(y);
                int first = leftFirst ? x - 1 : x + 1;
                int second = leftFirst ? x + 1 : x - 1;
                remaining -= FlowSideways(x, y, first, remaining);
                remaining -= FlowSideways(x, y, second, remaining);
            }

            // 3. Up (pressure): push any excess above the stable share into the cell above.
            if (remaining > SandboxFluidConstants.MaxFill)
            {
                FlowUp(x, y, remaining);
            }
        }

        /// <summary>Moves fluid into the cell below toward the vertical stable state. Returns the amount moved.</summary>
        private float FlowDown(int x, int y, float amount)
        {
            int by = y - 1;
            if (!IsFlowable(x, by))
            {
                return 0f;
            }

            float below = grid.GetFluid(x, by);
            float flow = StableLower(amount + below) - below;
            flow = Clamp(flow, amount);
            if (flow <= 0f)
            {
                return 0f;
            }

            Transfer(x, y, x, by, flow);
            return flow;
        }

        /// <summary>Equalizes toward one horizontal neighbour (dampened). Returns the amount moved.</summary>
        private float FlowSideways(int x, int y, int nx, float amount)
        {
            if (!IsFlowable(nx, y))
            {
                return 0f;
            }

            float neighbour = grid.GetFluid(nx, y);
            // Quarter-step dampening keeps a single Gauss-Seidel pass from overshooting/oscillating.
            float flow = (amount - neighbour) * 0.25f;
            flow = Clamp(flow, amount);
            if (flow <= 0f)
            {
                return 0f;
            }

            Transfer(x, y, nx, y, flow);
            return flow;
        }

        /// <summary>Pushes pressurized excess up into the cell above. Returns the amount moved.</summary>
        private float FlowUp(int x, int y, float amount)
        {
            int ay = y + 1;
            if (!IsFlowable(x, ay))
            {
                return 0f;
            }

            float above = grid.GetFluid(x, ay);
            // This cell is the lower of the (cell, above) pair; anything over its stable share rises.
            float flow = amount - StableLower(amount + above);
            flow = Clamp(flow, amount);
            if (flow <= 0f)
            {
                return 0f;
            }

            Transfer(x, y, x, ay, flow);
            return flow;
        }

        /// <summary>
        /// Moves <paramref name="flow"/> from source to destination and, when the move is at least
        /// <see cref="SandboxFluidConstants.SettleEpsilon"/>, keeps both endpoints awake so flow
        /// continues to propagate next tick. Sub-epsilon moves still transfer mass (no leak) but let
        /// the cells sleep.
        /// </summary>
        private void Transfer(int sx, int sy, int dx, int dy, float flow)
        {
            grid.SetFluid(sx, sy, grid.GetFluid(sx, sy) - flow);
            grid.SetFluid(dx, dy, grid.GetFluid(dx, dy) + flow);

            if (flow >= SandboxFluidConstants.SettleEpsilon)
            {
                WakeSingle(sx, sy);
                WakeSingle(dx, dy);
            }
        }

        /// <summary>
        /// Finite-water stable amount for the <em>lower</em> cell of a vertical pair holding
        /// <paramref name="total"/> combined fluid. Continuous at <c>total = MaxFill</c> and the
        /// source of both gravity settling and pressure equalization.
        /// </summary>
        public static float StableLower(float total)
        {
            const float maxFill = SandboxFluidConstants.MaxFill;
            const float maxCompress = SandboxFluidConstants.MaxCompression;

            if (total <= maxFill)
            {
                return total;
            }

            if (total < 2f * maxFill + maxCompress)
            {
                return (maxFill * maxFill + total * maxCompress) / (maxFill + maxCompress);
            }

            return (total + maxCompress) / 2f;
        }

        private static float Clamp(float flow, float available)
        {
            if (flow < 0f)
            {
                return 0f;
            }

            float cap = Mathf.Min(available, SandboxFluidConstants.MaxTransferPerTick);
            return flow > cap ? cap : flow;
        }

        /// <summary>A cell can hold/receive fluid when it is loaded and not solid.</summary>
        private bool IsFlowable(int x, int y)
        {
            return grid.IsLoaded(x, y) && !grid.IsSolid(x, y);
        }

        private void WakeSingle(int x, int y)
        {
            if (IsFlowable(x, y))
            {
                active.Add(new Vector2Int(x, y));
            }
        }

        /// <summary>Deterministic 32-bit integer hash (Wang/xorshift mix) for the seeded scan direction.</summary>
        private static uint Hash(uint a, uint b, uint c)
        {
            unchecked
            {
                uint h = 2166136261u;
                h = (h ^ a) * 16777619u;
                h = (h ^ b) * 16777619u;
                h = (h ^ c) * 16777619u;
                h ^= h >> 15;
                h *= 2246822519u;
                h ^= h >> 13;
                return h;
            }
        }
    }
}
