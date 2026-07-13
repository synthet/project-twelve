using ProjectTwelve.Sandbox.Registry;

namespace ProjectTwelve.Sandbox.Lighting
{
    /// <summary>Non-generating adapter from the live world to the pure light solver.</summary>
    public sealed class SandboxWorldLightGrid : ISandboxLightGrid
    {
        private readonly SandboxWorld world;

        public SandboxWorldLightGrid(SandboxWorld world)
        {
            this.world = world;
        }

        public bool IsLoaded(int x, int y)
        {
            return world.TryGetExistingTileForLighting(x, y, out _);
        }

        public SandboxTile GetTile(int x, int y)
        {
            return world.TryGetExistingTileForLighting(x, y, out SandboxTile tile) ? tile : default;
        }

        public byte GetSourceLight(int x, int y)
        {
            if (!world.TryGetExistingTileForLighting(x, y, out SandboxTile tile))
            {
                return 0;
            }

            TileDefinition definition = SandboxRegistries.Tiles.Get(tile.id);
            byte source = definition.LightEmission;
            if (!definition.Opaque && world.IsSkySourceForLighting(x, y))
            {
                source = SandboxLightSolver.MaxLight;
            }

            return source;
        }

        public byte GetAttenuation(int x, int y)
        {
            if (!world.TryGetExistingTileForLighting(x, y, out SandboxTile tile))
            {
                return SandboxLightSolver.AirAttenuation;
            }

            return SandboxRegistries.Tiles.Get(tile.id).LightAttenuation;
        }

        public void SetLight(int x, int y, byte light)
        {
            world.SetTileLightForLighting(x, y, light);
        }
    }
}
