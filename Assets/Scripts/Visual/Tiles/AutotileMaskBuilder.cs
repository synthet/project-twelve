using System;
using System.Collections.Generic;

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
        public bool InnerCavityRemap { get; }

        /// <summary>
        /// Ordered applied/skipped decision trace for the normalizers (see the Autotile Next
        /// Actions Plan, Phase 0C). Mirrors the tile-viz trace field-for-field.
        /// </summary>
        public string[] NormalizationTrace { get; }

        public GroundMaskBuildResult(
            int[,] visualMask,
            int[,] solidMask,
            int[,] connectivityMask,
            int[,] finalMask,
            bool stairInteriorRemap,
            bool cavityUndersideRemap,
            bool materialBoundaryRemap,
            bool innerCavityRemap = false)
        {
            VisualMask = visualMask;
            SolidMask = solidMask;
            ConnectivityMask = connectivityMask;
            FinalMask = finalMask;
            StairInteriorRemap = stairInteriorRemap;
            CavityUndersideRemap = cavityUndersideRemap;
            MaterialBoundaryRemap = materialBoundaryRemap;
            InnerCavityRemap = innerCavityRemap;
            NormalizationTrace = AutotileMaskBuilder.BuildNormalizationTrace(
                stairInteriorRemap,
                innerCavityRemap,
                cavityUndersideRemap,
                materialBoundaryRemap);
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
                out bool materialBoundaryRemap,
                out bool innerCavityRemap);

            return new GroundMaskBuildResult(
                visualMask,
                solidMask,
                connectivityMask,
                finalMask,
                stairInteriorRemap,
                cavityUndersideRemap,
                materialBoundaryRemap,
                innerCavityRemap);
        }

        /// <summary>
        /// Ordered normalizers, evaluated first-match-wins. The trace and the tile-viz side
        /// (<c>NORMALIZATION_ORDER</c> in maskBuilder.js) must stay in lockstep.
        /// </summary>
        private static readonly (string Key, string AppliedReason)[] NormalizationOrder =
        {
            ("stairInterior", "diagonal step -> interior fill"),
            ("innerCavity", "flat lintel span -> underside"),
            ("cavityUnderside", "bridge -> underside"),
            ("materialBoundary", "south row cleared"),
        };

        /// <summary>
        /// Builds the ordered applied/skipped decision trace from the normalization flags.
        /// Because the normalizers short-circuit on first match, at most one flag is set: every
        /// normalizer before it reads "skipped", the matching one reads "applied", and normalizers
        /// after it are never evaluated (and so are omitted). Mirrors <c>buildNormalizationTrace</c>
        /// in maskBuilder.js.
        /// </summary>
        public static string[] BuildNormalizationTrace(
            bool stairInterior,
            bool innerCavity,
            bool cavityUnderside,
            bool materialBoundary)
        {
            bool[] flags = { stairInterior, innerCavity, cavityUnderside, materialBoundary };
            List<string> trace = new List<string>(NormalizationOrder.Length);
            for (int i = 0; i < NormalizationOrder.Length; i++)
            {
                (string key, string appliedReason) = NormalizationOrder[i];
                if (flags[i])
                {
                    trace.Add($"{key}: applied: {appliedReason}");
                    return trace.ToArray();
                }

                trace.Add($"{key}: skipped");
            }

            return trace.ToArray();
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
        /// Builds a cover autotile mask for the surface overlay at the given world coordinate.
        /// Cover runs horizontally along the surface, so connectivity is decided purely by the
        /// west/east neighbors on mask row y = 1 (mask[0,1], mask[1,1], mask[2,1]). Vertical and
        /// diagonal cells stay empty so the cover rule table matches cleanly.
        ///
        /// Vendor model (PixelTileEngine <c>LevelBuilder.SetCover</c>/<c>GetMask</c>): cover is an
        /// overlay on any exposed-top ground cell, independent of ground material. A side neighbor
        /// therefore reads by solidity alone — open air is an end cap, an exposed-top ground cell
        /// continues the run, and a ground cell with more ground stacked above it is a cliff step.
        /// </summary>
        /// <param name="isSolid">Returns true when the coordinate holds any solid ground tile.</param>
        public static int[,] BuildCoverMask(
            Func<int, int, bool> isSolid,
            int worldX,
            int worldY)
        {
            int[,] mask = new int[3, 3];
            mask[1, 1] = 1;
            mask[0, 1] = ResolveCoverNeighbor(isSolid, worldX - 1, worldY);
            mask[2, 1] = ResolveCoverNeighbor(isSolid, worldX + 1, worldY);
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
        /// Vendor-aligned connectivity mask: same-material blob only (PixelTileEngine <c>GetMask</c>).
        /// <paramref name="isSolid"/> is ignored here; use <see cref="BuildSolidGroundMask"/> for
        /// physical-support debug context.
        /// </summary>
        internal static int[,] BuildConnectivityGroundMask(
            Func<int, int, bool> sharesGroundGroup,
            int worldX,
            int worldY,
            Func<int, int, bool> isSolid)
        {
            _ = isSolid;
            return BuildVisualGroundMask(sharesGroundGroup, worldX, worldY);
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
            out bool materialBoundaryRemap,
            out bool innerCavityRemap)
        {
            stairInteriorRemap = false;
            cavityUndersideRemap = false;
            materialBoundaryRemap = false;
            innerCavityRemap = false;

            // Vendor alignment: the base PixelTileEngine autotiler has no normalization layer — it
            // resolves the raw blob mask directly (exact match -> mirror -> fallback). The project
            // normalization remaps (stairInterior / innerCavity / cavityUnderside / materialBoundary)
            // are intentionally disabled so ground resolution matches vendor behavior exactly. The
            // TryRemap* helpers below are retained for reference/tests but are no longer invoked.
            _ = sharesGroundGroup;
            _ = isSolid;
            _ = isSurfaceTile;
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
        /// Window/hole lintels and inner vertical strips inside cavities often match outside-body
        /// rules (18, 0) even though they should read as underside (17) or inner face (8).
        /// </summary>
        internal static bool TryRemapCavityInnerEdgeMask(
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

            if (!sharesGroundGroup(worldX - 1, worldY) || !sharesGroundGroup(worldX + 1, worldY))
            {
                return false;
            }

            if (!isSolid(worldX, worldY + 1))
            {
                return false;
            }

            if (mask[1, 0] != 1 || mask[0, 1] != 1 || mask[2, 1] != 1)
            {
                return false;
            }

            if (TryRemapCavityLintelToUnderside(mask, isSolid, worldX, worldY, out remapped))
            {
                return true;
            }

            if (!isSolid(worldX, worldY - 1)
                && TryRemapCavityInnerVerticalMask(mask, sharesGroundGroup, worldX, worldY, out remapped))
            {
                return true;
            }

            return false;
        }

        internal static bool TryRemapCavityLintelToUnderside(
            int[,] mask,
            Func<int, int, bool> isSolid,
            int worldX,
            int worldY,
            out int[,] remapped)
        {
            remapped = null;
            if (!HasFilledSouthRow(mask))
            {
                return false;
            }

            if (IsCavityInnerCornerMask(mask))
            {
                return false;
            }

            if (mask[0, 2] != 0 && mask[2, 2] != 0)
            {
                return false;
            }

            bool cavityBelow = !isSolid(worldX, worldY - 1)
                || !isSolid(worldX - 1, worldY - 1)
                || !isSolid(worldX + 1, worldY - 1);
            if (!cavityBelow)
            {
                return false;
            }

            remapped = FullUndersideMask();
            return true;
        }

        internal static bool TryRemapCavityInnerVerticalMask(
            int[,] mask,
            Func<int, int, bool> sharesGroundGroup,
            int worldX,
            int worldY,
            out int[,] remapped)
        {
            remapped = null;
            bool westOpenOutsideCorner = mask[0, 0] == 0 && mask[0, 1] == 1 && mask[1, 1] == 1
                && mask[1, 2] == 1 && mask[2, 2] == 1;
            bool eastOpenOutsideCorner = mask[2, 0] == 0 && mask[2, 1] == 1 && mask[1, 1] == 1
                && mask[1, 2] == 1 && mask[0, 2] == 1;
            if (!westOpenOutsideCorner && !eastOpenOutsideCorner)
            {
                return false;
            }

            if (westOpenOutsideCorner && !sharesGroundGroup(worldX + 1, worldY))
            {
                return false;
            }

            if (eastOpenOutsideCorner && !sharesGroundGroup(worldX - 1, worldY))
            {
                return false;
            }

            remapped = westOpenOutsideCorner
                ? WestOpenVerticalMask()
                : EastOpenVerticalMask();
            return true;
        }

        internal static int[,] WestOpenVerticalMask()
        {
            return new[,]
            {
                { 0, 1, 1 },
                { 0, 1, 1 },
                { 0, 1, 1 }
            };
        }

        internal static int[,] EastOpenVerticalMask()
        {
            return new[,]
            {
                { 1, 1, 0 },
                { 1, 1, 0 },
                { 1, 1, 0 }
            };
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

        /// <summary>
        /// Concave inner-corner topologies that should resolve to vendor sprite 18 (+flipX), not flat underside 17.
        /// </summary>
        internal static bool IsCavityInnerCornerMask(int[,] mask)
        {
            bool southWestNotch = mask[0, 2] == 1 && mask[1, 2] == 1 && mask[2, 2] == 0;
            bool southEastNotch = mask[0, 2] == 0 && mask[1, 2] == 1 && mask[2, 2] == 1;
            if (southWestNotch || southEastNotch)
            {
                return true;
            }

            bool eastReentrant = mask[2, 0] == 0 && mask[1, 0] == 1 && mask[0, 0] == 1
                && mask[0, 2] == 1 && mask[1, 2] == 1 && mask[2, 2] == 1;
            bool westReentrant = mask[0, 0] == 0 && mask[1, 0] == 1 && mask[2, 0] == 1
                && mask[0, 2] == 1 && mask[1, 2] == 1 && mask[2, 2] == 1;
            return eastReentrant || westReentrant;
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
            Func<int, int, bool> isSolid,
            int neighborX,
            int neighborY)
        {
            // Open air beside the tile reads as an end cap.
            if (!isSolid(neighborX, neighborY))
            {
                return 0;
            }

            // Ground stacked above the side neighbor means it is a buried cliff wall, not an exposed
            // top — the cover run ends here in a rising step (vendor SetCover mask[1,0]/[1,2] = 2).
            if (isSolid(neighborX, neighborY + 1))
            {
                return 2;
            }

            // Exposed-top ground beside the tile carries its own cover, so the run continues flat.
            return 1;
        }
    }
}
