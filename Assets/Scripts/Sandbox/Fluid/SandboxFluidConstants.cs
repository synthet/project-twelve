namespace ProjectTwelve.Sandbox.Fluid
{
    /// <summary>
    /// Named constants for the P2 water simulation, mirroring the table in
    /// <c>docs/wiki/08-liquids.md</c> § "P2-FLUID-001 specification". Call sites must use these
    /// constants instead of magic numbers so the spec and code cannot drift silently (guarded by
    /// <c>SandboxFluidSimulatorTests.Constants_MatchP2Fluid001SpecTable</c>).
    /// </summary>
    public static class SandboxFluidConstants
    {
        /// <summary>Nominal full cell; equilibrium target for an uncompressed cell.</summary>
        public const float MaxFill = 1.0f;

        /// <summary>
        /// Extra mass a cell holds per stacked cell of head above it. This overfill is what lets
        /// water rise back to its source level in U-shaped channels (pressure).
        /// </summary>
        public const float MaxCompress = 0.02f;

        /// <summary>
        /// Below this a transfer skips the 0.5 damping factor, so thin films still fully drain
        /// instead of creeping toward the threshold forever.
        /// </summary>
        public const float MinFlow = 0.01f;

        /// <summary>Cap on a single down/up transfer per cell per tick (flow rate).</summary>
        public const float MaxTransferPerTick = 1.0f;

        /// <summary>A cell whose net movement this tick is below this sleeps (leaves the active set).</summary>
        public const float SettleEpsilon = 1e-4f;

        /// <summary>Render-only threshold for drawing a partial-fill cell; unused by simulation math.</summary>
        public const float MinVisibleFill = 0.02f;

        /// <summary>Simulation ticks advanced per rendered frame for smoother motion.</summary>
        public const int IterationsPerFrame = 2;

        /// <summary>
        /// Per-tick active-cell budget. Cells beyond the cap stay awake and are processed next
        /// tick rather than blowing the frame; mass is still conserved (deferred cells keep fluid).
        /// </summary>
        public const int MaxActiveCellsPerTick = 4096;
    }
}
