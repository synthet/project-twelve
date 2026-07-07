using ProjectTwelve.Visual.Tiles;

namespace ProjectTwelve.Sandbox.Debug
{
    /// <summary>
    /// Tileset-aware sprite id stepping for Visual Override Mode (F8).
    /// </summary>
    public static class VisualOverrideSpriteStep
    {
        public static string Step(string spriteId, int delta, int spriteCount)
        {
            if (spriteCount <= 0)
            {
                spriteCount = AutotileRuleTables.GroundSpriteCount;
            }

            int current = int.TryParse(spriteId, out int parsed) ? parsed : 0;
            int next = (current + delta) % spriteCount;
            if (next < 0)
            {
                next += spriteCount;
            }

            return next.ToString();
        }

        public static int GetShiftStride(int spriteCount)
        {
            if (spriteCount <= 0)
            {
                return 8;
            }

            return spriteCount < 8 ? spriteCount : 8;
        }
    }
}
