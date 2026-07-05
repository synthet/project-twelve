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
        /// <summary>Nominal full cell (uncompressed). A cell may transiently exceed this under pressure.</summary>
        public const float MaxFill = 1.0f;

        /// <summary>
        /// Extra fill a cell accepts per unit of pressure from the column above. This is what lets
        /// water rise back to its source level in a U-tube; larger values equalize taller columns.
        /// </summary>
        public const float MaxCompression = 0.02f;

        /// <summary>Maximum fluid moved across a single edge in one tick (flow rate cap).</summary>
        public const float MaxTransferPerTick = 1.0f;

        /// <summary>
        /// Net change threshold below which a cell sleeps. Transfers at or above this value re-wake
        /// both endpoints; a settled pool falls below it everywhere and the active set empties.
        /// </summary>
        public const float SettleEpsilon = 0.0001f;

        /// <summary>Below this amount a cell renders as empty (render-only threshold, never a mass sink).</summary>
        public const float MinVisibleFill = 0.05f;

        /// <summary>
        /// Per-tick active-cell budget. When the active set is larger, the overflow (in deterministic
        /// sorted order) is deferred to the next tick rather than blowing the frame budget.
        /// </summary>
        public const int MaxActiveCellsPerTick = 4096;
    }
}
