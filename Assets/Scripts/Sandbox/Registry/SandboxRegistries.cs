namespace ProjectTwelve.Sandbox.Registry
{
    /// <summary>
    /// Process-wide access point for the frozen core content registries (P2-DATA-002). Built
    /// lazily on first touch — there is no common Awake/execution-order relationship between the
    /// consumers (world, renderer, ScriptableObject catalog, static MCP tools), so a static
    /// holder keeps Play Mode and EditMode tests on the same single frozen instance without any
    /// scene bootstrapping. Cross-registry references are validated once at build time.
    /// </summary>
    public static class SandboxRegistries
    {
        private static ContentRegistry<TileDefinition> tiles;
        private static ContentRegistry<ItemDefinition> items;
        private static ContentRegistry<EntityDefinition> entities;
        private static int airIndex = -1;
        private static int grassIndex = -1;

        /// <summary>Frozen tile registry; validated against <see cref="Items"/> on first build.</summary>
        public static ContentRegistry<TileDefinition> Tiles => tiles ?? (tiles = BuildTiles());

        /// <summary>Frozen item registry (contracts firm up under P2-INV-001).</summary>
        public static ContentRegistry<ItemDefinition> Items => items ?? (items = SandboxCoreContent.CreateItemRegistry());

        /// <summary>Frozen entity registry (contracts firm up under P2-AI-001).</summary>
        public static ContentRegistry<EntityDefinition> Entities => entities ?? (entities = SandboxCoreContent.CreateEntityRegistry());

        /// <summary>Cached runtime index of <see cref="SandboxCoreContent.AirTileId"/> (the frozen empty slot, always 0).</summary>
        public static int AirIndex => airIndex >= 0 ? airIndex : (airIndex = Tiles.GetIndex(SandboxCoreContent.AirTileId));

        /// <summary>Cached runtime index of <see cref="SandboxCoreContent.GrassTileId"/>.</summary>
        public static int GrassIndex => grassIndex >= 0 ? grassIndex : (grassIndex = Tiles.GetIndex(SandboxCoreContent.GrassTileId));

        /// <summary>
        /// Test-only: swaps the live registries (pass null to rebuild core content on next touch),
        /// e.g. to simulate a mod-induced registry reordering for save palette round-trips.
        /// Values already snapshotted from the previous registry (for example
        /// <see cref="SandboxTileIds"/> fields) are not re-resolved.
        /// </summary>
        internal static void ResetForTests(
            ContentRegistry<TileDefinition> newTiles = null,
            ContentRegistry<ItemDefinition> newItems = null,
            ContentRegistry<EntityDefinition> newEntities = null)
        {
            tiles = newTiles;
            items = newItems;
            entities = newEntities;
            airIndex = -1;
            grassIndex = -1;
        }

        private static ContentRegistry<TileDefinition> BuildTiles()
        {
            ContentRegistry<TileDefinition> built = SandboxCoreContent.CreateTileRegistry();
            SandboxCoreContent.ValidateTileReferences(built, Items);
            return built;
        }
    }
}
