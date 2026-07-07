namespace ProjectTwelve.Visual.Tiles
{
    /// <summary>
    /// Resolved sprite and transforms after optional visual override lookup.
    /// </summary>
    public readonly struct VisualOverrideResult
    {
        public VisualOverrideResult(
            string spriteId,
            bool flipX,
            bool flipY,
            int rotationDegrees,
            bool overrideApplied,
            AutotileVisualOverride sourceOverride = null)
        {
            SpriteId = spriteId;
            FlipX = flipX;
            FlipY = flipY;
            RotationDegrees = rotationDegrees;
            OverrideApplied = overrideApplied;
            SourceOverride = sourceOverride;
        }

        public string SpriteId { get; }
        public bool FlipX { get; }
        public bool FlipY { get; }
        public int RotationDegrees { get; }
        public bool OverrideApplied { get; }
        public AutotileVisualOverride SourceOverride { get; }
    }

    public static class VisualOverrideDecision
    {
        public static VisualOverrideResult Apply(
            string autoSpriteId,
            bool autoFlipX,
            AutotileVisualOverride visualOverride,
            AutotileTileset tileset)
        {
            if (visualOverride == null
                || string.IsNullOrEmpty(visualOverride.overrideSpriteId)
                || tileset == null
                || !AutotileResolver.TryGetSpriteById(tileset, visualOverride.overrideSpriteId, out _))
            {
                return new VisualOverrideResult(autoSpriteId, autoFlipX, flipY: false, rotationDegrees: 0, overrideApplied: false);
            }

            return new VisualOverrideResult(
                visualOverride.overrideSpriteId,
                visualOverride.overrideFlipX,
                visualOverride.overrideFlipY,
                visualOverride.rotation,
                overrideApplied: true,
                sourceOverride: visualOverride);
        }
    }
}
