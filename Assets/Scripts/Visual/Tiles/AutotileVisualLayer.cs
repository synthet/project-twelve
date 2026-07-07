namespace ProjectTwelve.Visual.Tiles
{
    public enum AutotileVisualLayer
    {
        Ground,
        Cover,
    }

    public static class AutotileVisualLayerNames
    {
        public const string Ground = "ground";
        public const string Cover = "cover";

        public static string ToName(AutotileVisualLayer layer)
        {
            return layer == AutotileVisualLayer.Cover ? Cover : Ground;
        }

        public static AutotileVisualLayer Parse(string layerName)
        {
            if (string.Equals(layerName, Cover, System.StringComparison.OrdinalIgnoreCase))
            {
                return AutotileVisualLayer.Cover;
            }

            return AutotileVisualLayer.Ground;
        }
    }
}
