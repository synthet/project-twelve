using System.Collections.Generic;
using UnityEngine;

namespace ProjectTwelve.Sandbox.Grass
{
    /// <summary>
    /// Read/write tile view the grass-growth simulation operates on. Solidity, fluid, and tile id
    /// come from the same tile data collision and rendering use (never a second grid). "Loaded" means
    /// the chunk owning the coordinate is in the streamed window; the simulator gates reads on
    /// <see cref="IsLoaded"/> so a query never generates a chunk (matching the fluid-grid contract),
    /// and grass never grows outside the loaded set. Writes go through <see cref="SetTile"/> so the
    /// change persists (marks the chunk edited), repaints, and dirties border neighbors.
    /// </summary>
    public interface IGrassWorld
    {
        /// <summary>Tile at the world coordinate. Only called for loaded cells.</summary>
        SandboxTile GetTile(int x, int y);

        /// <summary>Replaces the tile id at the world coordinate (grass ⇄ dirt conversions).</summary>
        void SetTile(int x, int y, int tileId);

        /// <summary>Whether the chunk owning the world coordinate is in the loaded (renderer-backed) set.</summary>
        bool IsLoaded(int x, int y);

        /// <summary>Coordinates of the currently loaded chunks the simulation scans.</summary>
        IEnumerable<Vector2Int> LoadedChunkCoords { get; }
    }
}
