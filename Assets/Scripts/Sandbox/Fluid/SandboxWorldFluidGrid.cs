namespace ProjectTwelve.Sandbox.Fluid
{
    /// <summary>
    /// Adapts a live <see cref="SandboxWorld"/> to <see cref="ISandboxFluidGrid"/>. Solidity and
    /// fluid reads come from the same tile data collision and rendering use; "loaded" means the
    /// chunk currently has a renderer (the streamed window around the player), so the simulation
    /// never reaches into unloaded chunks. Callers (the simulator) check <see cref="IsLoaded"/>
    /// before writing; reads on unloaded cells return solid/zero without generating chunks.
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
            // Unloaded cells read solid so flow never crosses the loaded-set boundary.
            return !IsLoaded(x, y) || world.GetTile(x, y).IsSolid;
        }

        public bool IsLoaded(int x, int y)
        {
            return world.IsChunkLoaded(SandboxWorld.WorldToChunkCoord(x, y));
        }

        public float GetFluid(int x, int y)
        {
            return world.GetFluid(x, y);
        }

        public void SetFluid(int x, int y, float amount)
        {
            world.SetFluid(x, y, amount);
        }
    }
}
