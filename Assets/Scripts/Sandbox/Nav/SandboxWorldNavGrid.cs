using UnityEngine;

namespace ProjectTwelve.Sandbox.Nav
{
    /// <summary>
    /// Adapts a live <see cref="SandboxWorld"/> to the nav interfaces. Solidity reads the same
    /// tile data collision uses; "loaded" means the chunk currently has a renderer (the streamed
    /// window around the player), so navigation and spawning never reach into unloaded chunks.
    /// Callers must check <see cref="IsLoaded"/> before <see cref="IsSolid"/> — the pathfinder
    /// and spawn rules do — because tile reads on unloaded cells would generate chunks on demand.
    /// </summary>
    public sealed class SandboxWorldNavGrid : ISandboxNavGrid, ISandboxNavVersionSource
    {
        private readonly SandboxWorld world;

        public SandboxWorldNavGrid(SandboxWorld world)
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

        public int GetNavVersion(Vector2Int chunkCoord)
        {
            return world.GetNavVersion(chunkCoord);
        }
    }
}
