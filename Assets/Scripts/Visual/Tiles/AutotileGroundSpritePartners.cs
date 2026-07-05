using System.Collections.Generic;

namespace ProjectTwelve.Visual.Tiles
{
    /// <summary>
    /// Reserved for fixture-validated authored opposite-side sprite substitutions.
    /// The vendor PixelFantasy resolver returns the matched rule sprite with <c>flipX = true</c>
    /// for row/column flip passes; do not enable partner substitution here without mask-topology proof.
    /// See docs/wiki/ground-autotile-32-rules.md § Mirroring policy.
    /// </summary>
    public static class AutotileGroundSpritePartners
    {
        private static readonly Dictionary<string, string> ColumnFlipPartners = new Dictionary<string, string>();

        private static readonly Dictionary<string, string> RowFlipPartners = new Dictionary<string, string>();

        /// <summary>
        /// Returns the dedicated east-side (column-mirrored) partner when fixture-validated.
        /// </summary>
        public static bool TryGetColumnFlipPartner(string spriteId, out string partnerSpriteId)
        {
            return ColumnFlipPartners.TryGetValue(spriteId, out partnerSpriteId);
        }

        /// <summary>
        /// Returns the dedicated row-mirrored partner when fixture-validated.
        /// </summary>
        public static bool TryGetRowFlipPartner(string spriteId, out string partnerSpriteId)
        {
            return RowFlipPartners.TryGetValue(spriteId, out partnerSpriteId);
        }

        /// <summary>
        /// True when the tileset uses the 32-sprite ground sheet.
        /// </summary>
        public static bool UsesAuthoredGroundPartners(int spriteCount)
        {
            return spriteCount == AutotileRuleTables.GroundSpriteCount;
        }
    }
}
