using ProjectTwelve.Sandbox;

namespace ProjectTwelve.Visual.AutotileDebug
{
    /// <summary>
    /// Formats world tile coordinates for F3 debug HUD readouts.
    /// </summary>
    public static class GroundAutotileDebugCoordinates
    {
        /// <summary>
        /// World tile plus owning chunk and in-chunk local coordinates.
        /// </summary>
        public static string FormatHoverCoordinates(int worldX, int worldY)
        {
            UnityEngine.Vector2Int chunk = SandboxWorld.WorldToChunkCoord(worldX, worldY);
            UnityEngine.Vector2Int local = SandboxWorld.WorldToLocalCoord(worldX, worldY);
            return $"World ({worldX}, {worldY})\nChunk ({chunk.x}, {chunk.y}) local ({local.x}, {local.y})";
        }
    }
}
