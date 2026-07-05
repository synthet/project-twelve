namespace ProjectTwelve.Sandbox.Fluid
{
    /// <summary>
    /// Adapts a live <see cref="SandboxWorld"/> to <see cref="ISandboxFluidGrid"/>. Solidity and the
    /// fluid amount read the same tile data collision and rendering use; "loaded" means the chunk
    /// currently has a renderer (the streamed window around the player), so the simulation never
    /// flows into unloaded chunks. Callers (the simulator) must gate reads on <see cref="IsLoaded"/>
    /// because tile access on an unloaded cell would generate the chunk on demand.
    /// </summary>
    public sealed class SandboxWorldFluidGrid : ISandboxFluidGrid
    {
        private readonly SandboxWorld world;

        public SandboxWorldFluidGrid(SandboxWorld world)
        {
            this.world = world;
        }

        public bool IsSolid(int x, int y)
        {
            return world.GetTile(x, y).IsSolid;
        }

        public bool IsLoaded(int x, int y)
        {
            return world.IsChunkLoaded(SandboxWorld.WorldToChunkCoord(x, y));
        }

        public float GetFluid(int x, int y)
        {
            return world.GetTileFluid(x, y);
        }

        public void SetFluid(int x, int y, float amount)
        {
            world.SetTileFluid(x, y, amount);
        }
    }
}
