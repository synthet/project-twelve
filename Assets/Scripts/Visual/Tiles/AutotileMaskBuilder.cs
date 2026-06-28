using System;

namespace ProjectTwelve.Visual.Tiles
{
    /// <summary>
    /// Builds autotile neighbor masks from world tile connectivity.
    /// </summary>
    public static class AutotileMaskBuilder
    {
        /// <summary>
        /// Builds a ground autotile mask for the tile at the given world coordinate.
        /// </summary>
        public static int[,] BuildGroundMask(Func<int, int, bool> sharesGroundGroup, int worldX, int worldY)
        {
            bool Has(int dx, int dy) => sharesGroundGroup(worldX + dx, worldY + dy);

            return new[,]
            {
                {
                    Has(-1, 1) && Has(-1, 0) && Has(0, 1) ? 1 : 0,
                    Has(0, 1) ? 1 : 0,
                    Has(1, 1) && Has(1, 0) && Has(0, 1) ? 1 : 0
                },
                {
                    Has(-1, 0) ? 1 : 0,
                    1,
                    Has(1, 0) ? 1 : 0
                },
                {
                    Has(-1, -1) && Has(-1, 0) && Has(0, -1) ? 1 : 0,
                    Has(0, -1) ? 1 : 0,
                    Has(1, -1) && Has(1, 0) && Has(0, -1) ? 1 : 0
                }
            };
        }

        /// <summary>
        /// Builds a cover autotile mask for grass overlays at the given world coordinate.
        /// </summary>
        public static int[,] BuildCoverMask(
            Func<int, int, bool> sharesCoverGroup,
            Func<int, int, bool> hasGroundBody,
            int worldX,
            int worldY)
        {
            int[,] mask = BuildGroundMask(sharesCoverGroup, worldX, worldY);

            if (hasGroundBody(worldX - 1, worldY + 1) && hasGroundBody(worldX - 1, worldY))
            {
                mask[1, 0] = 2;
            }

            if (hasGroundBody(worldX + 1, worldY + 1) && hasGroundBody(worldX + 1, worldY))
            {
                mask[1, 2] = 2;
            }

            return mask;
        }
    }
}
