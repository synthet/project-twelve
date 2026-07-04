using UnityEngine;

namespace ProjectTwelve.Sandbox.Nav
{
    /// <summary>
    /// Read-only solidity view the pathfinder and spawn rules operate on. Implementations must
    /// derive solidity from the same data collision uses (<see cref="SandboxTile.IsSolid"/>) —
    /// never a second, divergent nav mask — and must not generate chunks on query: cells outside
    /// the loaded set report <see cref="IsLoaded"/> false and are treated as impassable.
    /// </summary>
    public interface ISandboxNavGrid
    {
        /// <summary>Whether the tile at the world coordinate is solid. Only meaningful for loaded cells.</summary>
        bool IsSolid(int x, int y);

        /// <summary>Whether the chunk owning the world coordinate is in the loaded set.</summary>
        bool IsLoaded(int x, int y);
    }

    /// <summary>
    /// Source of per-chunk navigation versions. The version is a monotonic counter bumped on every
    /// tile mutation (a chunk is "nav-dirty" relative to a snapshot when its version has moved on);
    /// a counter replaces a clearable dirty bool so multiple agents can observe the same edit
    /// without racing to clear a shared flag.
    /// </summary>
    public interface ISandboxNavVersionSource
    {
        /// <summary>Current navigation version of the chunk, or 0 when the chunk does not exist.</summary>
        int GetNavVersion(Vector2Int chunkCoord);
    }
}
