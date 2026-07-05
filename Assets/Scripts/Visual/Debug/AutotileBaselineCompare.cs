using ProjectTwelve.Sandbox.Registry;
using ProjectTwelve.Visual.Tiles;

namespace ProjectTwelve.Visual.AutotileDebug
{
    /// <summary>
    /// Compares live autotile resolution against a loaded baseline cell.
    /// </summary>
    public static class AutotileBaselineCompare
    {
        /// <summary>
        /// Maps registry runtime tile index to legacy tile-viz id (0–7).
        /// </summary>
        public static int ToLegacyTileId(int runtimeIndex)
        {
            ContentRegistry<TileDefinition> tiles = SandboxRegistries.Tiles;
            if (runtimeIndex < 0 || runtimeIndex >= tiles.Count)
            {
                return 0;
            }

            string stringId = tiles.Get(runtimeIndex).Id;
            for (int i = 0; i < SandboxCoreContent.LegacyTileIdToStringId.Count; i++)
            {
                if (SandboxCoreContent.LegacyTileIdToStringId[i] == stringId)
                {
                    return i;
                }
            }

            return runtimeIndex;
        }

        /// <summary>
        /// Returns true when world tile id and ground sprite id/flip match the baseline entry.
        /// </summary>
        public static bool GroundMatches(
            int legacyTileId,
            AutotileGroundResolveResult resolve,
            BaselineCell baseline)
        {
            if (legacyTileId != baseline.TileId)
            {
                return false;
            }

            if (!resolve.Resolved)
            {
                return string.IsNullOrEmpty(baseline.GroundSpriteId);
            }

            return resolve.SpriteId == baseline.GroundSpriteId && resolve.FlipX == baseline.GroundFlipX;
        }

        /// <summary>
        /// Returns true when cover render state and sprite id/flip match the baseline entry.
        /// </summary>
        public static bool CoverMatches(
            bool coverRendered,
            string coverSpriteId,
            bool coverFlipX,
            BaselineCell baseline)
        {
            if (coverRendered != baseline.CoverRendered)
            {
                return false;
            }

            if (!coverRendered)
            {
                return true;
            }

            return coverSpriteId == baseline.CoverSpriteId && coverFlipX == baseline.CoverFlipX;
        }
    }
}
