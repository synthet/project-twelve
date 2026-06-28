using System.Collections.Generic;

namespace ProjectTwelve.Visual.Tiles
{
    /// <summary>
    /// Default ground and cover autotile rule tables (see docs/VISUAL_BEHAVIOR_SPEC.md).
    /// </summary>
    public static class AutotileRuleTables
    {
        public const int GroundSpriteCount = 32;
        public const string FallbackSpriteId = "20";

        /// <summary>Ground tilesets (32 sprites) use this rule set.</summary>
        public static IReadOnlyList<AutotileRule> Ground { get; } = BuildGroundRules();

        /// <summary>Cover tilesets use this rule set.</summary>
        public static IReadOnlyList<AutotileRule> Cover { get; } = BuildCoverRules();

        /// <summary>
        /// Returns the rule set for a tileset based on sprite count.
        /// </summary>
        public static IReadOnlyList<AutotileRule> GetRulesForSpriteCount(int spriteCount)
        {
            return spriteCount == GroundSpriteCount ? Ground : Cover;
        }

        private static List<AutotileRule> BuildGroundRules()
        {
            return new List<AutotileRule>
            {
                new AutotileRule("0", M(0,0,0, 0,1,1, 0,1,1)),
                new AutotileRule("1", M(0,0,0, 1,1,1, 1,1,1)),
                new AutotileRule("2", M(0,0,0, 1,1,1, 1,1,1)),
                new AutotileRule("3", M(1,1,0, 1,1,1, 0,1,1)),
                new AutotileRule("4", M(0,1,0, 1,1,1, 1,1,0)),
                new AutotileRule("5", M(0,1,0, 1,1,1, 1,1,1)),
                new AutotileRule("6", M(0,0,0, 1,1,1, 0,1,0)),
                new AutotileRule("7", M(0,0,0, 1,1,0, 0,1,0)),
                new AutotileRule("8", M(0,1,1, 0,1,1, 0,1,1)),
                new AutotileRule("9", M(1,1,1, 1,1,1, 1,1,1), weight: 4),
                new AutotileRule("10", M(1,1,1, 1,1,1, 1,1,1), weight: 1),
                new AutotileRule("11", M(0,1,1, 1,1,1, 1,1,1)),
                new AutotileRule("12", M(1,1,0, 1,1,1, 0,1,0)),
                new AutotileRule("13", M(1,1,1, 1,1,1, 0,1,0)),
                new AutotileRule("14", M(0,1,0, 1,1,1, 0,0,0)),
                new AutotileRule("15", M(0,1,0, 1,1,0, 0,0,0)),
                new AutotileRule("16", M(0,1,1, 0,1,1, 0,0,0)),
                new AutotileRule("17", M(1,1,1, 1,1,1, 0,0,0)),
                new AutotileRule("18", M(1,1,1, 1,1,1, 0,1,1)),
                new AutotileRule("19", M(1,1,0, 1,1,1, 1,1,0)),
                new AutotileRule("20", M(0,0,0, 0,1,0, 0,0,0)),
                new AutotileRule("21", M(0,1,0, 0,1,0, 0,1,0)),
                new AutotileRule("22", M(0,1,0, 0,1,1, 0,1,1)),
                new AutotileRule("23", M(0,0,0, 1,1,1, 1,1,0)),
                new AutotileRule("24", M(0,0,0, 0,1,1, 0,0,0)),
                new AutotileRule("25", M(0,0,0, 1,1,1, 0,0,0)),
                new AutotileRule("26", M(0,1,0, 1,1,1, 0,1,0)),
                new AutotileRule("27", M(0,1,0, 1,1,0, 0,1,0)),
                new AutotileRule("28", M(0,0,0, 0,1,0, 0,1,0)),
                new AutotileRule("29", M(0,1,0, 0,1,0, 0,0,0)),
                new AutotileRule("30", M(0,1,1, 0,1,1, 0,1,0)),
                new AutotileRule("31", M(1,1,0, 1,1,1, 0,0,0)),
            };
        }

        private static List<AutotileRule> BuildCoverRules()
        {
            return new List<AutotileRule>
            {
                new AutotileRule("2", M(0,0,0, 2,1,2, 0,0,0)),
                new AutotileRule("1", M(0,0,0, 0,1,2, 0,0,0)),
                new AutotileRule("5", M(0,0,0, 1,1,2, 0,0,0)),
                new AutotileRule("3", M(0,0,0, 0,1,1, 0,0,0)),
                new AutotileRule("4", M(0,0,0, 1,1,1, 0,0,0)),
                new AutotileRule("0", M(0,0,0, 0,1,0, 0,0,0)),
            };
        }

        private static int[,] M(params int[] values)
        {
            int[,] mask = new int[3, 3];
            for (int i = 0; i < values.Length; i++)
            {
                mask[i % 3, i / 3] = values[i];
            }

            return mask;
        }
    }
}
