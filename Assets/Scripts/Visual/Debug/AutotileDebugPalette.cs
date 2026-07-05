using UnityEngine;

namespace ProjectTwelve.Visual.AutotileDebug
{
    /// <summary>
    /// Deterministic debug overlay colors for autotile sprite and tile ids.
    /// </summary>
    public static class AutotileDebugPalette
    {
        public const float OverlayAlpha = 0.55f;

        private const float GoldenRatioConjugate = 0.61803398875f;

        /// <summary>
        /// Returns a stable color keyed to a ground sprite id string (0–31).
        /// </summary>
        public static Color ColorForSpriteId(string spriteId)
        {
            int index = 0;
            if (!string.IsNullOrEmpty(spriteId) && int.TryParse(spriteId, out int parsed))
            {
                index = Mathf.Clamp(parsed, 0, 31);
            }

            return ColorFromIndex(index, saturation: 0.72f, value: 0.95f);
        }

        /// <summary>
        /// Returns a stable color keyed to a registry runtime tile index.
        /// </summary>
        public static Color ColorForTileId(int tileId)
        {
            int index = Mathf.Max(0, tileId);
            float hue = Mathf.Repeat(index * GoldenRatioConjugate + 0.33f, 1f);
            Color rgb = Color.HSVToRGB(hue, 0.65f, 0.9f);
            rgb.a = OverlayAlpha;
            return rgb;
        }

        /// <summary>
        /// High-contrast label tint for digit quads.
        /// </summary>
        public static Color LabelColor => new Color(1f, 1f, 1f, 0.95f);

        /// <summary>
        /// Flip indicator accent.
        /// </summary>
        public static Color FlipMarkerColor => new Color(1f, 0.35f, 0.2f, 0.95f);

        private static Color ColorFromIndex(int index, float saturation, float value)
        {
            float hue = Mathf.Repeat(index * GoldenRatioConjugate, 1f);
            Color rgb = Color.HSVToRGB(hue, saturation, value);
            rgb.a = OverlayAlpha;
            return rgb;
        }
    }
}
