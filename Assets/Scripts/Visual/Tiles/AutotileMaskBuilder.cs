using System;

namespace ProjectTwelve.Visual.Tiles
{
    /// <summary>
    /// Full ground-mask build result including visual/solid layers and normalization flags.
    /// </summary>
    public readonly struct GroundMaskBuildResult
    {
        public int[,] VisualMask { get; }
        public int[,] SolidMask { get; }
        public int[,] ConnectivityMask { get; }
        public int[,] FinalMask { get; }
        public bool StairInteriorRemap { get; }
        public bool CavityUndersideRemap { get; }
        public bool MaterialBoundaryRemap { get; }

        public GroundMaskBuildResult(
            int[,] visualMask,
            int[,] solidMask,
            int[,] connectivityMask,
            int[,] finalMask,
            bool stairInteriorRemap,
            bool cavityUndersideRemap,
            bool materialBoundaryRemap)
        {
            VisualMask = visualMask;
            SolidMask = solidMask;
            ConnectivityMask = connectivityMask;
            FinalMask = finalMask;
            StairInteriorRemap = stairInteriorRemap;
            CavityUndersideRemap = cavityUndersideRemap;
            MaterialBoundaryRemap = materialBoundaryRemap;
        }
    }

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
            return BuildGroundMask(sharesGroundGroup, sharesGroundGroup, worldX, worldY, null);
        }

        /// <summary>
        /// Builds a ground autotile mask using explicit solid checks for underside normalization.
        /// </summary>
        /// <param name="isSolid">Returns true for any solid tile, including foreign tilesets.</param>
        public static int[,] BuildGroundMask(
            Func<int, int, bool> sharesGroundGroup,
            Func<int, int, bool> isSolid,
            int worldX,
            int worldY)
        {
            return BuildGroundMask(sharesGroundGroup, isSolid, worldX, worldY, null);
        }

        /// <summary>
        /// Builds a ground autotile mask with an optional exposed surface predicate for stair-step normalization.
        /// </summary>
        public static int[,] BuildGroundMask(
            Func<int, int, bool> sharesGroundGroup,
            Func<int, int, bool> isSolid,
            int worldX,
            int worldY,
            Func<int, int, bool> isSurfaceTile)
        {
            return BuildGroundMaskDetailed(sharesGroundGroup, isSolid, worldX, worldY, isSurfaceTile).FinalMask;
        }

        /// <summary>
        /// Builds visual, solid, connectivity, and final masks plus normalization flags.
        /// </summary>
        public static GroundMaskBuildResult BuildGroundMaskDetailed(
            Func<int, int, bool> sharesGroundGroup,
            Func<int, int, bool> isSolid,
            int worldX,
            int worldY,
            Func<int, int, bool> isSurfaceTile)
        {
            int[,] visualMask = BuildVisualGroundMask(sharesGroundGroup, worldX, worldY);
            int[,] solidMask = isSolid != null ? BuildSolidGroundMask(isSolid, worldX, worldY) : null;
            int[,] connectivityMask = BuildConnectivityGroundMask(sharesGroundGroup, worldX, worldY, isSolid);
            int[,] finalMask = NormalizeGroundMask(
                connectivityMask,
                sharesGroundGroup,
                isSolid,
                worldX,
                worldY,
                isSurfaceTile,
                out bool stairInteriorRemap,
                out bool cavityUndersideRemap,
                out bool materialBoundaryRemap);

            return new GroundMaskBuildResult(
                visualMask,
                solidMask,
                connectivityMask,
                finalMask,
                stairInteriorRemap,
                cavityUndersideRemap,
                materialBoundaryRemap);
        }

        /// <summary>
        /// Same-material / same-ground visual connectivity only.
        /// </summary>
        internal static int[,] BuildVisualGroundMask(Func<int, int, bool> sharesGroundGroup, int worldX, int worldY)
        {
            return BuildGroundMaskFromPredicate(
                (dx, dy) => sharesGroundGroup(worldX + dx, worldY + dy));
        }

        /// <summary>
        /// Any-solid connectivity for physical support and exposure context.
        /// </summary>
        internal static int[,] BuildSolidGroundMask(Func<int, int, bool> isSolid, int worldX, int worldY)
        {
            return BuildGroundMaskFromPredicate(
                (dx, dy) => isSolid(worldX + dx, worldY + dy));
        }

        /// <summary>
        /// Builds a cover autotile mask for grass overlays at the given world coordinate.
        /// Grass cover runs horizontally along the surface, so connectivity is decided purely by
        /// the west/east neighbors on mask row y = 1 (mask[0,1], mask[1,1], mask[2,1]). Vertical and
        /// diagonal cells stay empty so the cover rule table matches cleanly.
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
            mask[0, 1] = ResolveCoverNeighbor(sharesCoverGroup, hasGroundBody, worldX - 1, worldY);
            mask[2, 1] = ResolveCoverNeighbor(sharesCoverGroup, hasGroundBody, worldX + 1, worldY);
            return mask;
        }

        /// <summary>
        /// Raw 3×3 connectivity mask before underside / material-boundary normalization.
        /// </summary>
        internal static int[,] BuildConnectivityGroundMask(Func<int, int, bool> sharesGroundGroup, int worldX, int worldY)
        {
            return BuildConnectivityGroundMask(sharesGroundGroup, worldX, worldY, null);
        }

        /// <summary>
        /// Connectivity mask: visualMask with south-row foreign-solid support blended in.
        /// Side and north neighbors never blend across materials.
        /// </summary>
        internal static int[,] BuildConnectivityGroundMask(
            Func<int, int, bool> sharesGroundGroup,
            int worldX,
            int worldY,
            Func<int, int, bool> isSolid)
        {
            if (isSolid == null)
            {
                return BuildVisualGroundMask(sharesGroundGroup, worldX, worldY);
            }

            bool Has(int dx, int dy) => sharesGroundGroup(worldX + dx, worldY + dy);
            bool CheckSupport(int dx, int dy) => Has(dx, dy) || (dy <= 0 && isSolid(worldX + dx, worldY + dy));

            return new[,]
            {
                {
                    CheckSupport(-1, 1) && CheckSupport(-1, 0) && CheckSupport(0, 1) ? 1 : 0,
                    CheckSupport(-1, 0) ? 1 : 0,
                    CheckSupport(-1, -1) && CheckSupport(-1, 0) && CheckSupport(0, -1) ? 1 : 0
                },
                {
                    CheckSupport(0, 1) ? 1 : 0,
                    1,
                    CheckSupport(0, -1) ? 1 : 0
                },
                {
                    CheckSupport(1, 1) && CheckSupport(1, 0) && CheckSupport(0, 1) ? 1 : 0,
                    CheckSupport(1, 0) ? 1 : 0,
                    CheckSupport(1, -1) && CheckSupport(1, 0) && CheckSupport(0, -1) ? 1 : 0
                }
            };
        }

        /// <summary>
        /// Applies stair-interior and material-boundary remaps so rule selection matches exposed
        /// tile faces. Undersides and vertical faces resolve through the authored vendor rules
        /// (14–17/31 underside family, 8/22 side faces) without mask remapping.
        /// </summary>
        internal static int[,] NormalizeGroundMask(
            int[,] mask,
            Func<int, int, bool> sharesGroundGroup,
            Func<int, int, bool> isSolid,
            int worldX,
            int worldY,
            Func<int, int, bool> isSurfaceTile)
        {
            return NormalizeGroundMask(
                mask,
                sharesGroundGroup,
                isSolid,
                worldX,
                worldY,
                isSurfaceTile,
                out _,
                out _,
                out _);
        }

        internal static int[,] NormalizeGroundMask(
            int[,] mask,
            Func<int, int, bool> sharesGroundGroup,
            Func<int, int, bool> isSolid,
            int worldX,
            int worldY,
            Func<int, int, bool> isSurfaceTile,
            out bool stairInteriorRemap,
            out bool cavityUndersideRemap,
            out bool materialBoundaryRemap)
        {
            stairInteriorRemap = false;
            cavityUndersideRemap = false;
            materialBoundaryRemap = false;

            if (TryRemapStairInteriorDiagonalMask(mask, isSurfaceTile, worldX, worldY, out int[,] stairInterior))
            {
                stairInteriorRemap = true;
                return stairInterior;
            }

            if (TryRemapCavityBridgeToUnderside(
                    mask,
                    sharesGroundGroup,
                    isSolid,
                    worldX,
                    worldY,
                    out int[,] cavityUnderside))
            {
                cavityUndersideRemap = true;
                return cavityUnderside;
            }

            if (TryRemapMaterialBoundaryCornerMask(
                    mask,
                    sharesGroundGroup,
                    isSolid,
                    worldX,
                    worldY,
                    out int[,] boundary))
            {
                materialBoundaryRemap = true;
                return boundary;
            }

            return mask;
        }

        /// <summary>
        /// Remaps stair-step support cells that have full cardinal support and only one upper
        /// diagonal open. When the adjacent same-row cell is an exposed grass surface, these read
        /// better as interior fill than as repeated diagonal corner sprites down a slope.
        /// </summary>
        internal static bool TryRemapStairInteriorDiagonalMask(
            int[,] mask,
            Func<int, int, bool> isSurfaceTile,
            int worldX,
            int worldY,
            out int[,] remapped)
        {
            remapped = null;
            if (isSurfaceTile == null)
            {
                return false;
            }

            if (mask[1, 0] != 1
                || mask[0, 1] != 1
                || mask[2, 1] != 1
                || mask[1, 2] != 1
                || mask[0, 2] != 1
                || mask[2, 2] != 1)
            {
                return false;
            }

            bool missingNorthWest = mask[0, 0] == 0 && mask[2, 0] == 1 && isSurfaceTile(worldX - 1, worldY);
            bool missingNorthEast = mask[2, 0] == 0 && mask[0, 0] == 1 && isSurfaceTile(worldX + 1, worldY);
            if (!missingNorthWest && !missingNorthEast)
            {
                return false;
            }

            remapped = new[,]
            {
                { 1, 1, 1 },
                { 1, 1, 1 },
                { 1, 1, 1 }
            };
            return true;
        }

        /// <summary>
        /// One-tile-wide cavity lintels/floors often match rule 25 (bridge) even though they should
        /// read as continuous ceiling/underside (rule 17) when corner cells continue on both sides.
        /// </summary>
        internal static bool TryRemapCavityBridgeToUnderside(
            int[,] mask,
            Func<int, int, bool> sharesGroundGroup,
            Func<int, int, bool> isSolid,
            int worldX,
            int worldY,
            out int[,] remapped)
        {
            remapped = null;
            if (sharesGroundGroup == null || isSolid == null || !IsBridgeMask(mask))
            {
                return false;
            }

            if (isSolid(worldX, worldY - 1))
            {
                return false;
            }

            if (!sharesGroundGroup(worldX - 1, worldY) || !sharesGroundGroup(worldX + 1, worldY))
            {
                return false;
            }

            // Top lintel row: open north (sky/cavity) with same-group west/east arms.
            if (!isSolid(worldX, worldY + 1))
            {
                remapped = FullUndersideMask();
                return true;
            }

            // Floor lintel row: open north into the cavity with corner support below.
            if (sharesGroundGroup(worldX - 1, worldY - 1) && sharesGroundGroup(worldX + 1, worldY - 1))
            {
                remapped = FullUndersideMask();
                return true;
            }

            return false;
        }

        internal static bool IsBridgeMask(int[,] mask)
        {
            for (int x = 0; x < 3; x++)
            {
                if (mask[x, 0] != 0 || mask[x, 2] != 0 || mask[x, 1] != 1)
                {
                    return false;
                }
            }

            return true;
        }

        internal static int[,] FullUndersideMask()
        {
            return new[,]
            {
                { 1, 1, 0 },
                { 1, 1, 0 },
                { 1, 1, 0 }
            };
        }

        /// <summary>
        /// Clears south-row connectivity at foreign ground boundaries so lip tiles reuse corner caps
        /// (rules 16/24) instead of horizontal run ends (rule 0) or west-open strips (rule 8).
        /// </summary>
        internal static bool TryRemapMaterialBoundaryCornerMask(
            int[,] mask,
            Func<int, int, bool> sharesGroundGroup,
            Func<int, int, bool> isSolid,
            int worldX,
            int worldY,
            out int[,] remapped)
        {
            remapped = null;
            if (sharesGroundGroup == null || isSolid == null)
            {
                return false;
            }

            bool foreignWest = isSolid(worldX - 1, worldY) && !sharesGroundGroup(worldX - 1, worldY);
            bool foreignEast = isSolid(worldX + 1, worldY) && !sharesGroundGroup(worldX + 1, worldY);
            if (!foreignWest && !foreignEast)
            {
                return false;
            }

            if (!HasFilledSouthRow(mask))
            {
                return false;
            }

            if (foreignWest && IsWestColumnOpen(mask))
            {
                remapped = ClearSouthRow(mask);
                return true;
            }

            if (foreignEast && IsEastColumnOpen(mask))
            {
                remapped = ClearSouthRow(mask);
                return true;
            }

            return false;
        }

        internal static bool IsWestColumnOpen(int[,] mask)
        {
            return mask[0, 0] == 0 && mask[0, 1] == 0 && mask[0, 2] == 0;
        }

        internal static bool IsEastColumnOpen(int[,] mask)
        {
            return mask[2, 0] == 0 && mask[2, 1] == 0 && mask[2, 2] == 0;
        }

        internal static bool HasFilledSouthRow(int[,] mask)
        {
            return mask[0, 2] == 1 || mask[1, 2] == 1 || mask[2, 2] == 1;
        }

        internal static int[,] ClearSouthRow(int[,] mask)
        {
            int[,] cleared = new int[3, 3];
            for (int x = 0; x < 3; x++)
            {
                for (int y = 0; y < 3; y++)
                {
                    cleared[x, y] = y == 2 ? 0 : mask[x, y];
                }
            }

            return cleared;
        }

        private static int[,] BuildGroundMaskFromPredicate(
            Func<int, int, bool> has,
            Func<int, bool> hasSouth = null)
        {
            bool Has(int dx, int dy) => has(dx, dy);
            bool HasSouth(int dx) => hasSouth != null ? hasSouth(dx) : Has(dx, -1);

            // mask[x, y]: x = west..east, y = north..south (matches AutotileRule / rule-table layout).
            return new[,]
            {
                {
                    Has(-1, 1) && Has(-1, 0) && Has(0, 1) ? 1 : 0,
                    Has(-1, 0) ? 1 : 0,
                    HasSouth(-1) && Has(-1, 0) && HasSouth(0) ? 1 : 0
                },
                {
                    Has(0, 1) ? 1 : 0,
                    1,
                    HasSouth(0) ? 1 : 0
                },
                {
                    Has(1, 1) && Has(1, 0) && Has(0, 1) ? 1 : 0,
                    Has(1, 0) ? 1 : 0,
                    HasSouth(1) && Has(1, 0) && HasSouth(0) ? 1 : 0
                }
            };
        }

        private static int ResolveCoverNeighbor(
            Func<int, int, bool> sharesCoverGroup,
            Func<int, int, bool> hasGroundBody,
            int neighborX,
            int neighborY)
        {
            if (sharesCoverGroup(neighborX, neighborY))
            {
                return 1;
            }

            // Solid ground body on the same row (e.g. dirt beside grass) reads as a cliff-side edge.
            if (hasGroundBody(neighborX, neighborY))
            {
                return 2;
            }

            // A solid column rising beside the tile reads as a cliff-side edge.
            if (hasGroundBody(neighborX, neighborY + 1))
            {
                return 2;
            }

            // Open air beside the tile reads as an end cap.
            return 0;
        }
    }
}
