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
        /// Grass cover runs horizontally along the surface, so connectivity is decided purely by
        /// the west/east neighbors. The middle row encodes (west, center, east); vertical and
        /// diagonal cells stay empty so the cover rule table (which constrains only that row)
        /// matches cleanly. Cell values: 1 = cover continues, 2 = cliff-side edge, 0 = open air.
        /// </summary>
        /// <param name="hasGroundBody">Returns true when the coordinate holds any solid ground tile.</param>
        public static int[,] BuildCoverMask(
            Func<int, int, bool> sharesCoverGroup,
            Func<int, int, bool> hasGroundBody,
            int worldX,
            int worldY)
        {
            int[,] mask = new int[3, 3];
            mask[1, 1] = 1;
            mask[1, 0] = ResolveCoverNeighbor(sharesCoverGroup, hasGroundBody, worldX - 1, worldY);
            mask[1, 2] = ResolveCoverNeighbor(sharesCoverGroup, hasGroundBody, worldX + 1, worldY);
            return mask;
        }

        private static int ResolveCoverNeighbor(
            Func<int, int, bool> sharesCoverGroup,
            Func<int, int, bool> hasGroundBody,
            int neighborX,
            int neighborY)
        {
            // Grass cover continues across the edge.
            if (sharesCoverGroup(neighborX, neighborY))
            {
                return 1;
            }

            // A solid ground wall rising beside the tile reads as a cliff-side edge.
            if (hasGroundBody(neighborX, neighborY) && hasGroundBody(neighborX, neighborY + 1))
            {
                return 2;
            }

            // Open air beside the tile reads as an end cap.
            return 0;
        }
    }
}
