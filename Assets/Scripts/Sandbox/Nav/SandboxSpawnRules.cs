using System;
using UnityEngine;

namespace ProjectTwelve.Sandbox.Nav
{
    /// <summary>
    /// Pure spawn-candidate rules for the P2 walker archetype per
    /// <c>docs/wiki/09-pathfinding.md</c> § "Spawn rules (walker)". All world access goes through
    /// <see cref="ISandboxNavGrid"/> and injected delegates so the rules are EditMode-testable
    /// without a scene.
    /// </summary>
    public static class SandboxSpawnRules
    {
        /// <summary>Whether the live-enemy population allows another spawn.</summary>
        public static bool CanSpawn(int liveEnemyCount)
        {
            return liveEnemyCount < SandboxNavConstants.PopulationCap;
        }

        /// <summary>
        /// Validates one candidate foot cell against every spawn rule: standable (loaded air with
        /// solid support), outside the camera rectangle expanded by
        /// <see cref="SandboxNavConstants.CameraPaddingTiles"/>, Chebyshev distance from the
        /// player inside the spawn band, and — for underground cells — light at or below
        /// <see cref="SandboxNavConstants.SpawnLightThreshold"/>.
        /// </summary>
        public static bool IsValidSpawnCell(
            ISandboxNavGrid grid,
            Vector2Int cell,
            Vector2Int playerFootTile,
            RectInt cameraTileRect,
            Func<Vector2Int, int> lightAt,
            Func<int, int> surfaceHeightAt)
        {
            if (!SandboxNavPathfinder.IsStandable(grid, cell.x, cell.y))
            {
                return false;
            }

            RectInt exclusion = cameraTileRect;
            exclusion.min -= Vector2Int.one * SandboxNavConstants.CameraPaddingTiles;
            exclusion.max += Vector2Int.one * SandboxNavConstants.CameraPaddingTiles;
            if (exclusion.Contains(cell))
            {
                return false;
            }

            int distance = ChebyshevDistance(cell, playerFootTile);
            if (distance < SandboxNavConstants.MinSpawnDistance || distance > SandboxNavConstants.MaxSpawnDistance)
            {
                return false;
            }

            if (IsUnderground(cell, surfaceHeightAt) && lightAt(cell) > SandboxNavConstants.SpawnLightThreshold)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Whether the cell sits below the surface band: more than
        /// <see cref="SandboxNavConstants.SurfaceBandTiles"/> tiles under its column's surface.
        /// </summary>
        public static bool IsUnderground(Vector2Int cell, Func<int, int> surfaceHeightAt)
        {
            return cell.y < surfaceHeightAt(cell.x) - SandboxNavConstants.SurfaceBandTiles;
        }

        /// <summary>
        /// Interim light model until P2-LIGHT-001 lands: unlit (underground) cells read 0 and
        /// surface-band cells read 15, so dark-cave spawns are allowed and daylight spawns are
        /// not blocked by the light gate. Replace with real lightmap reads when available.
        /// </summary>
        public static int InterimLightAt(Vector2Int cell, Func<int, int> surfaceHeightAt)
        {
            return IsUnderground(cell, surfaceHeightAt) ? 0 : 15;
        }

        /// <summary>Chebyshev (square) distance in tiles, matching the chunk-window metric.</summary>
        public static int ChebyshevDistance(Vector2Int a, Vector2Int b)
        {
            return Mathf.Max(Mathf.Abs(a.x - b.x), Mathf.Abs(a.y - b.y));
        }

        /// <summary>
        /// Converts a world-space camera rectangle to the tile rectangle it covers (inclusive of
        /// partially covered edge tiles).
        /// </summary>
        public static RectInt CameraWorldRectToTileRect(Rect worldRect, float tileSize)
        {
            int minX = Mathf.FloorToInt(worldRect.xMin / tileSize);
            int minY = Mathf.FloorToInt(worldRect.yMin / tileSize);
            int maxX = Mathf.CeilToInt(worldRect.xMax / tileSize);
            int maxY = Mathf.CeilToInt(worldRect.yMax / tileSize);
            return new RectInt(minX, minY, maxX - minX, maxY - minY);
        }
    }
}
