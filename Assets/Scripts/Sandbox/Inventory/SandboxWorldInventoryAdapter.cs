using System;

namespace ProjectTwelve.Sandbox.Inventory
{
    /// <summary>Routes inventory transactions through SandboxWorld's single SetTile choke point.</summary>
    public sealed class SandboxWorldInventoryAdapter : ISandboxInventoryWorld
    {
        private readonly SandboxWorld world;

        public SandboxWorldInventoryAdapter(SandboxWorld world)
        {
            this.world = world ?? throw new ArgumentNullException(nameof(world));
        }

        public int GetTileId(int x, int y) => world.GetTile(x, y).id;

        public bool TrySetTile(int x, int y, int tileId)
        {
            world.SetTile(x, y, tileId);
            return true;
        }
    }
}
