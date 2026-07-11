using System.Collections.Generic;
using UnityEngine;

namespace ProjectTwelve.Sandbox.Grass
{
    /// <summary>
    /// Adapts a live <see cref="SandboxWorld"/> to <see cref="IGrassWorld"/>. Tile reads/writes go
    /// through the same API collision and rendering use; "loaded" means the chunk currently has a
    /// renderer (the streamed window around the player). The simulator gates reads on
    /// <see cref="IsLoaded"/>, so grass never grows into — or generates — unloaded chunks.
    /// </summary>
    public sealed class SandboxWorldGrassAdapter : IGrassWorld
    {
        private readonly SandboxWorld world;

        public SandboxWorldGrassAdapter(SandboxWorld world)
        {
            this.world = world;
        }

        public SandboxTile GetTile(int x, int y)
        {
            return world.GetTile(x, y);
        }

        public void SetTile(int x, int y, int tileId)
        {
            world.SetTile(x, y, tileId);
        }

        public bool IsLoaded(int x, int y)
        {
            return world.IsChunkLoaded(SandboxWorld.WorldToChunkCoord(x, y));
        }

        public IEnumerable<Vector2Int> LoadedChunkCoords => world.LoadedChunkCoords;
    }
}
