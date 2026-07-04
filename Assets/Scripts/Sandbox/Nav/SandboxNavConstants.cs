namespace ProjectTwelve.Sandbox.Nav
{
    /// <summary>
    /// Named constants for the P2 walker enemy archetype, mirroring the tables in
    /// <c>docs/wiki/09-pathfinding.md</c> § "P2-AI-001 specification". Call sites must use these
    /// constants instead of magic numbers so the spec and code cannot drift silently
    /// (guarded by <c>SandboxNavPathfinderTests.Constants_MatchP2Ai001SpecTable</c>).
    /// </summary>
    public static class SandboxNavConstants
    {
        /// <summary>Max tiles the walker can jump up from standing.</summary>
        public const int MaxJumpHeight = 3;

        /// <summary>
        /// Max horizontal gap (open tiles crossed) the walker can jump across. The landing
        /// column offset is therefore <c>MaxJumpGap + 1</c>.
        /// </summary>
        public const int MaxJumpGap = 4;

        /// <summary>Max ledge drop (tiles) before the search refuses the fall edge.</summary>
        public const int MaxFallDistance = 12;

        /// <summary>A* node expansion cap per path request.</summary>
        public const int MaxExpansionsPerRequest = 2048;

        /// <summary>Path requests processed per simulation tick across all agents.</summary>
        public const int MaxRequestsPerTick = 4;

        /// <summary>Min Chebyshev distance (tiles) from the player foot tile for spawn candidates.</summary>
        public const int MinSpawnDistance = 24;

        /// <summary>Max Chebyshev distance (tiles) from the player foot tile for spawn candidates.</summary>
        public const int MaxSpawnDistance = 64;

        /// <summary>Underground spawns require light at or below this value (0-15 scale).</summary>
        public const int SpawnLightThreshold = 3;

        /// <summary>Max live walker enemies in the loaded-chunk set.</summary>
        public const int PopulationCap = 8;

        /// <summary>Seconds between spawn attempts per area controller.</summary>
        public const float SpawnInterval = 6f;

        /// <summary>Seconds an idle agent survives after its target leaves the loaded set.</summary>
        public const float DespawnGraceSeconds = 10f;

        /// <summary>Padding (tiles) added around the camera view rectangle for spawn exclusion.</summary>
        public const int CameraPaddingTiles = 1;

        /// <summary>
        /// Cells more than this many tiles below the column surface are "underground" and gated
        /// by <see cref="SpawnLightThreshold"/>.
        /// </summary>
        public const int SurfaceBandTiles = 2;
    }
}
