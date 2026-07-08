using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json.Linq;
using ProjectTwelve.Sandbox.Registry;
using ProjectTwelve.Visual.AutotileDebug;
using ProjectTwelve.Visual.Tiles;
using UnityEngine;

namespace ProjectTwelve.RuntimeMcp
{
    /// <summary>
    /// Builds tile and autotile debug payloads for runtime MCP tools.
    /// </summary>
    public static class McpTileDebug
    {
        public const int DefaultRadius = 2;
        public const int MaxAreaCells = 400;
        public const int MaxDiffScanCells = 10000;

        /// <summary>
        /// Resolves inclusive world-tile bounds from explicit min/max or center + radius arguments.
        /// </summary>
        public static bool TryResolveBounds(
            JObject args,
            SandboxWorld world,
            int maxCells,
            out int xMin,
            out int yMin,
            out int xMax,
            out int yMax,
            out string error)
        {
            xMin = yMin = xMax = yMax = 0;
            error = null;

            if (args == null)
            {
                error = "arguments object is required";
                return false;
            }

            if (args["xMin"] != null && args["yMin"] != null && args["xMax"] != null && args["yMax"] != null)
            {
                xMin = args["xMin"].Value<int>();
                yMin = args["yMin"].Value<int>();
                xMax = args["xMax"].Value<int>();
                yMax = args["yMax"].Value<int>();
                return TryResolveExplicitBounds(xMin, yMin, xMax, yMax, maxCells, out error);
            }

            int centerX;
            int centerY;
            if (args.Value<bool?>("aroundPlayer") == true)
            {
                if (world == null || !world.TryGetPlayerWorldPosition(out Vector2 position))
                {
                    error = "aroundPlayer requires a SandboxWorld with an assigned player target.";
                    return false;
                }

                Vector2Int tile = world.WorldPositionToTile(position);
                centerX = tile.x;
                centerY = tile.y;
            }
            else
            {
                centerX = args["centerX"]?.Value<int>() ?? args["x"]?.Value<int>() ?? 0;
                centerY = args["centerY"]?.Value<int>() ?? args["y"]?.Value<int>() ?? 0;
            }

            int radiusX = args["radiusX"]?.Value<int>() ?? args["radius"]?.Value<int>() ?? DefaultRadius;
            int radiusY = args["radiusY"]?.Value<int>() ?? args["radius"]?.Value<int>() ?? DefaultRadius;
            return TryResolveCenteredBounds(
                centerX,
                centerY,
                radiusX,
                radiusY,
                maxCells,
                out xMin,
                out yMin,
                out xMax,
                out yMax,
                out error);
        }

        /// <summary>
        /// Validates explicit inclusive world-tile bounds.
        /// </summary>
        public static bool TryResolveExplicitBounds(
            int xMin,
            int yMin,
            int xMax,
            int yMax,
            int maxCells,
            out string error)
        {
            return ValidateBounds(xMin, yMin, xMax, yMax, maxCells, out error);
        }

        /// <summary>
        /// Resolves inclusive bounds from a center tile and non-negative radii.
        /// </summary>
        public static bool TryResolveCenteredBounds(
            int centerX,
            int centerY,
            int radiusX,
            int radiusY,
            int maxCells,
            out int xMin,
            out int yMin,
            out int xMax,
            out int yMax,
            out string error)
        {
            xMin = yMin = xMax = yMax = 0;
            if (radiusX < 0 || radiusY < 0)
            {
                error = "radius values must be non-negative.";
                return false;
            }

            xMin = centerX - radiusX;
            xMax = centerX + radiusX;
            yMin = centerY - radiusY;
            yMax = centerY + radiusY;
            return ValidateBounds(xMin, yMin, xMax, yMax, maxCells, out error);
        }

        /// <summary>
        /// Builds a rectangular tile dump with optional ASCII grid and autotile resolution per cell.
        /// </summary>
        public static JObject BuildAreaDump(
            SandboxWorld world,
            SandboxTileVisualCatalog catalog,
            int xMin,
            int yMin,
            int xMax,
            int yMax,
            bool includeAir,
            bool includeAutotile)
        {
            JArray tiles = new JArray();
            for (int y = yMax; y >= yMin; y--)
            {
                for (int x = xMin; x <= xMax; x++)
                {
                    SandboxTile tile = world.GetTile(x, y);
                    if (!includeAir && !tile.IsSolid)
                    {
                        continue;
                    }

                    JObject entry = BuildTileSummary(x, y, tile);
                    if (includeAutotile && tile.IsSolid)
                    {
                        entry["autotile"] = BuildAutotilePayload(world, catalog, x, y, tile);
                    }

                    tiles.Add(entry);
                }
            }

            return new JObject
            {
                ["xMin"] = xMin,
                ["yMin"] = yMin,
                ["xMax"] = xMax,
                ["yMax"] = yMax,
                ["width"] = xMax - xMin + 1,
                ["height"] = yMax - yMin + 1,
                ["includeAir"] = includeAir,
                ["includeAutotile"] = includeAutotile,
                ["ascii"] = BuildAsciiGrid(world, xMin, yMin, xMax, yMax),
                ["tiles"] = tiles
            };
        }

        /// <summary>
        /// Builds full autotile connectivity and sprite resolution for one world tile.
        /// </summary>
        public static JObject BuildAutotileAt(
            SandboxWorld world,
            SandboxTileVisualCatalog catalog,
            int x,
            int y)
        {
            SandboxTile tile = world.GetTile(x, y);
            JObject result = BuildTileSummary(x, y, tile);
            result["autotile"] = BuildAutotilePayload(world, catalog, x, y, tile);
            return result;
        }

        private static JObject BuildTileSummary(int x, int y, SandboxTile tile)
        {
            return new JObject
            {
                ["x"] = x,
                ["y"] = y,
                ["tileId"] = tile.id,
                ["tileName"] = GetTileName(tile.id),
                ["solid"] = tile.IsSolid,
                ["light"] = tile.light,
                ["fluid"] = tile.fluid,
                ["metadata"] = tile.metadata,
                ["chunk"] = BuildChunkCoord(x, y)
            };
        }

        private static JObject BuildAutotilePayload(
            SandboxWorld world,
            SandboxTileVisualCatalog catalog,
            int x,
            int y,
            SandboxTile tile)
        {
            JObject payload = new JObject
            {
                ["neighbors"] = BuildNeighborConnectivity(world, catalog, x, y, tile)
            };

            if (catalog == null || !catalog.HasAutotileSources)
            {
                payload["ground"] = new JObject { ["resolved"] = false, ["reason"] = "No autotile catalog configured." };
                payload["cover"] = new JObject { ["rendered"] = false, ["reason"] = "No autotile catalog configured." };
                return payload;
            }

            AppendGroundAutotile(payload, world, catalog, x, y, tile);
            AppendCoverAutotile(payload, world, catalog, x, y, tile);
            return payload;
        }

        private static void AppendGroundAutotile(
            JObject payload,
            SandboxWorld world,
            SandboxTileVisualCatalog catalog,
            int x,
            int y,
            SandboxTile tile)
        {
            if (catalog == null || !catalog.TryGetGroundTileset(tile.id, out AutotileTileset tileset))
            {
                payload["ground"] = new JObject
                {
                    ["resolved"] = false,
                    ["reason"] = $"Tile '{GetTileName(tile.id)}' has no ground tileset."
                };
                return;
            }

            bool SharesGround(int nx, int ny)
            {
                SandboxTile neighbor = world.GetTile(nx, ny);
                return catalog.SharesGroundAutotileGroup(tile.id, neighbor.id);
            }

            bool IsSolid(int nx, int ny) =>
                AutotileExposure.CreateIsSolid(world.GetTile, world.AutotileExposureFloorY)(nx, ny);

            bool IsSurfaceTile(int nx, int ny)
            {
                SandboxTile surface = world.GetTile(nx, ny);
                return surface.id == SandboxRegistries.GrassIndex && !world.GetTile(nx, ny + 1).IsSolid;
            }

            GroundMaskBuildResult maskBuild = AutotileMaskBuilder.BuildGroundMaskDetailed(
                SharesGround,
                IsSolid,
                x,
                y,
                IsSurfaceTile);
            Sprite sprite = AutotileResolver.ResolveSprite(tileset, maskBuild.FinalMask, out bool flipX);

            string spriteId = sprite != null ? sprite.name : null;
            payload["ground"] = new JObject
            {
                ["tileset"] = tileset.Name,
                ["materialGroup"] = tileset.Name,
                ["visualMask"] = MaskToJson(maskBuild.VisualMask),
                ["solidMask"] = maskBuild.SolidMask != null ? MaskToJson(maskBuild.SolidMask) : null,
                ["connectivityMask"] = MaskToJson(maskBuild.ConnectivityMask),
                ["rawMask"] = MaskToJson(maskBuild.ConnectivityMask),
                ["mask"] = MaskToJson(maskBuild.FinalMask),
                ["normalizedMask"] = MaskToJson(maskBuild.FinalMask),
                ["maskLayout"] = "mask[x][y]; x=west..east, y=north..south",
                ["normalization"] = new JObject
                {
                    ["stairInterior"] = maskBuild.StairInteriorRemap,
                    ["cavityUnderside"] = maskBuild.CavityUndersideRemap,
                    ["materialBoundary"] = maskBuild.MaterialBoundaryRemap,
                    ["innerCavity"] = maskBuild.InnerCavityRemap
                },
                ["normalizationTrace"] = new JArray(maskBuild.NormalizationTrace),
                ["matchingSpriteIds"] = FindMatchingSpriteIds(tileset.Rules, maskBuild.FinalMask),
                ["matchedRuleId"] = spriteId,
                ["spriteId"] = spriteId,
                ["flipX"] = flipX,
                ["finalSpriteId"] = spriteId,
                ["partnerSubstitution"] = false,
                ["resolved"] = sprite != null
            };
        }

        private static void AppendCoverAutotile(
            JObject payload,
            SandboxWorld world,
            SandboxTileVisualCatalog catalog,
            int x,
            int y,
            SandboxTile tile)
        {
            SandboxTile tileAbove = world.GetTile(x, y + 1);
            bool shouldRender = catalog.ShouldRenderGrassCover(tile.id, tileAbove);
            if (!shouldRender)
            {
                payload["cover"] = new JObject
                {
                    ["rendered"] = false,
                    ["reason"] = tile.id == SandboxRegistries.AirIndex
                        ? "Cover applies to solid ground tiles only."
                        : "Cover requires air directly above the tile."
                };
                return;
            }

            if (!catalog.TryGetCoverTileset(tile.id, out AutotileTileset tileset))
            {
                payload["cover"] = new JObject
                {
                    ["rendered"] = false,
                    ["reason"] = "Grass cover tileset is not configured."
                };
                return;
            }

            int[,] mask = BuildCoverMask(world, catalog, x, y);
            Sprite sprite = AutotileResolver.ResolveSprite(tileset, mask, out bool flipX);
            payload["cover"] = new JObject
            {
                ["rendered"] = true,
                ["tileset"] = tileset.Name,
                ["mask"] = MaskToJson(mask),
                ["maskLayout"] = "row y=1 encodes west/center/east at mask[0][1], mask[1][1], mask[2][1]",
                ["matchingSpriteIds"] = FindMatchingSpriteIds(tileset.Rules, mask),
                ["spriteId"] = sprite != null ? sprite.name : null,
                ["flipX"] = flipX,
                ["resolved"] = sprite != null
            };
        }

        private static JObject BuildNeighborConnectivity(
            SandboxWorld world,
            SandboxTileVisualCatalog catalog,
            int x,
            int y,
            SandboxTile center)
        {
            JObject neighbors = new JObject();
            AddNeighbor(neighbors, "NW", world, catalog, center, x - 1, y + 1);
            AddNeighbor(neighbors, "N", world, catalog, center, x, y + 1);
            AddNeighbor(neighbors, "NE", world, catalog, center, x + 1, y + 1);
            AddNeighbor(neighbors, "W", world, catalog, center, x - 1, y);
            AddNeighbor(neighbors, "E", world, catalog, center, x + 1, y);
            AddNeighbor(neighbors, "SW", world, catalog, center, x - 1, y - 1);
            AddNeighbor(neighbors, "S", world, catalog, center, x, y - 1);
            AddNeighbor(neighbors, "SE", world, catalog, center, x + 1, y - 1);
            return neighbors;
        }

        private static void AddNeighbor(
            JObject neighbors,
            string label,
            SandboxWorld world,
            SandboxTileVisualCatalog catalog,
            SandboxTile center,
            int x,
            int y)
        {
            SandboxTile tile = world.GetTile(x, y);
            JObject entry = new JObject
            {
                ["x"] = x,
                ["y"] = y,
                ["tileId"] = tile.id,
                ["tileName"] = GetTileName(tile.id),
                ["solid"] = tile.IsSolid
            };

            if (catalog != null)
            {
                entry["sharesGroundGroup"] = catalog.SharesGroundAutotileGroup(center.id, tile.id);
                entry["sharesCoverGroup"] = catalog.SharesCoverAutotileGroup(center.id, tile.id);
            }

            neighbors[label] = entry;
        }

        private static int[,] BuildCoverMask(
            SandboxWorld world,
            SandboxTileVisualCatalog catalog,
            int worldX,
            int worldY)
        {
            return AutotileMaskBuilder.BuildCoverMask(
                (x, y) => world.GetTile(x, y).IsSolid,
                worldX,
                worldY);
        }

        private static JArray FindMatchingSpriteIds(IReadOnlyList<AutotileRule> rules, int[,] mask)
        {
            JArray matches = new JArray();
            if (rules == null || mask == null)
            {
                return matches;
            }

            for (int i = 0; i < rules.Count; i++)
            {
                AutotileRule rule = rules[i];
                if (rule.Matches(mask, flipInput: false)
                    || rule.Matches(mask, flipInput: true)
                    || rule.MatchesColumns(mask, flipColumns: true))
                {
                    matches.Add(rule.SpriteId);
                }
            }

            return matches;
        }

        private static JArray MaskToJson(int[,] mask)
        {
            JArray rows = new JArray();
            if (mask == null)
            {
                return rows;
            }

            for (int x = 0; x < mask.GetLength(0); x++)
            {
                JArray column = new JArray();
                for (int y = 0; y < mask.GetLength(1); y++)
                {
                    column.Add(mask[x, y]);
                }

                rows.Add(column);
            }

            return rows;
        }

        private static JArray BuildAsciiGrid(
            SandboxWorld world,
            int xMin,
            int yMin,
            int xMax,
            int yMax)
        {
            JArray rows = new JArray();
            StringBuilder line = new StringBuilder();
            for (int y = yMax; y >= yMin; y--)
            {
                line.Clear();
                for (int x = xMin; x <= xMax; x++)
                {
                    line.Append(GetTileGlyph(world.GetTile(x, y).id));
                }

                rows.Add(line.ToString());
            }

            return rows;
        }

        private static JObject BuildChunkCoord(int x, int y)
        {
            Vector2Int chunk = SandboxWorld.WorldToChunkCoord(x, y);
            Vector2Int local = SandboxWorld.WorldToLocalCoord(x, y);
            return new JObject
            {
                ["x"] = chunk.x,
                ["y"] = chunk.y,
                ["localX"] = local.x,
                ["localY"] = local.y
            };
        }

        private static bool ValidateBounds(
            int xMin,
            int yMin,
            int xMax,
            int yMax,
            int maxCells,
            out string error)
        {
            if (xMax < xMin || yMax < yMin)
            {
                error = "xMax must be >= xMin and yMax must be >= yMin.";
                return false;
            }

            long width = xMax - xMin + 1L;
            long height = yMax - yMin + 1L;
            long cells = width * height;
            if (cells > maxCells)
            {
                error = $"Requested area has {cells} cells; maximum is {maxCells}.";
                return false;
            }

            error = null;
            return true;
        }

        /// <summary>
        /// Display name for a registry runtime tile index, derived from the definition's string
        /// ID (segment after the namespace, PascalCased — "core:gold_ore" → "GoldOre").
        /// </summary>
        public static string GetTileName(int tileId)
        {
            ContentRegistry<TileDefinition> tiles = SandboxRegistries.Tiles;
            if (tileId < 0 || tileId >= tiles.Count)
            {
                return $"Unknown({tileId})";
            }

            return PascalCaseFromStringId(tiles.Get(tileId).Id);
        }

        private static char[] glyphsByRuntimeIndex;

        private static char GetTileGlyph(int tileId)
        {
            ContentRegistry<TileDefinition> tiles = SandboxRegistries.Tiles;
            if (glyphsByRuntimeIndex == null || glyphsByRuntimeIndex.Length != tiles.Count)
            {
                char[] glyphs = new char[tiles.Count];
                for (int i = 0; i < glyphs.Length; i++)
                {
                    glyphs[i] = ResolveGlyph(tiles.Get(i).Id);
                }

                glyphsByRuntimeIndex = glyphs;
            }

            return tileId >= 0 && tileId < glyphsByRuntimeIndex.Length ? glyphsByRuntimeIndex[tileId] : '?';
        }

        private static char ResolveGlyph(string stringId)
        {
            switch (stringId)
            {
                case "core:air":
                    return '.';
                case "core:dirt":
                    return 'd';
                case "core:grass":
                    return 'g';
                case "core:stone":
                    return 's';
                case "core:copper_ore":
                    return 'c';
                case "core:iron_ore":
                    return 'i';
                case "core:silver_ore":
                    return 'v';
                case "core:gold_ore":
                    return 'o';
                default:
                    return '?';
            }
        }

        private static string PascalCaseFromStringId(string stringId)
        {
            int nameStart = stringId.IndexOf(':') + 1;
            StringBuilder builder = new StringBuilder(stringId.Length - nameStart);
            bool upperNext = true;
            for (int i = nameStart; i < stringId.Length; i++)
            {
                char c = stringId[i];
                if (c == '_')
                {
                    upperNext = true;
                    continue;
                }

                builder.Append(upperNext ? char.ToUpperInvariant(c) : c);
                upperNext = false;
            }

            return builder.ToString();
        }

        /// <summary>
        /// Exports a rectangular world region as project-twelve/tile-space/v1 space JSON.
        /// </summary>
        public static JObject BuildTileSpaceExport(
            SandboxWorld world,
            int xMin,
            int yMin,
            int xMax,
            int yMax,
            bool writeFile,
            string name)
        {
            JArray tiles = new JArray();
            for (int y = yMin; y <= yMax; y++)
            {
                for (int x = xMin; x <= xMax; x++)
                {
                    SandboxTile tile = world.GetTile(x, y);
                    if (!tile.IsSolid)
                    {
                        continue;
                    }

                    tiles.Add(new JObject
                    {
                        ["x"] = x,
                        ["y"] = y,
                        ["id"] = RuntimeIndexToLegacyTileId(tile.id),
                        ["light"] = tile.light,
                    });
                }
            }

            JObject space = new JObject
            {
                ["format"] = "project-twelve/tile-space/v1",
                ["kind"] = "space",
                ["name"] = string.IsNullOrWhiteSpace(name) ? "runtime-export" : name,
                ["xMin"] = xMin,
                ["yMin"] = yMin,
                ["xMax"] = xMax,
                ["yMax"] = yMax,
                ["tiles"] = tiles,
            };

            if (writeFile)
            {
                string fileName = $"tile-space-{DateTime.UtcNow:yyyyMMdd-HHmmss}.json";
                string path = Path.Combine(Application.persistentDataPath, fileName);
                File.WriteAllText(path, space.ToString(Newtonsoft.Json.Formatting.Indented));
                space["filePath"] = path;
            }

            return space;
        }

        /// <summary>
        /// Compares live autotile resolution against a committed baseline; returns mismatches only.
        /// </summary>
        public static JObject BuildAutotileDiffBaseline(
            SandboxWorld world,
            SandboxTileVisualCatalog catalog,
            int xMin,
            int yMin,
            int xMax,
            int yMax,
            string baselineName,
            int maxDiffs,
            string compareLayer)
        {
            IReadOnlyDictionary<Vector2Int, BaselineCell> baseline = AutotileBaselineStore.TryLoad(
                string.IsNullOrWhiteSpace(baselineName) ? "sandbox-scene-mountain" : baselineName);
            if (baseline == null)
            {
                throw new InvalidOperationException(
                    $"Autotile baseline '{baselineName}' not found under StreamingAssets/AutotileBaselines.");
            }

            bool compareGround = compareLayer != "cover";
            bool compareCover = compareLayer != "ground";
            JArray diffs = new JArray();
            int compared = 0;
            int matched = 0;
            int missingBaseline = 0;

            for (int y = yMin; y <= yMax; y++)
            {
                for (int x = xMin; x <= xMax; x++)
                {
                    SandboxTile tile = world.GetTile(x, y);
                    if (!tile.IsSolid)
                    {
                        continue;
                    }

                    compared++;
                    Vector2Int key = new Vector2Int(x, y);
                    if (!baseline.TryGetValue(key, out BaselineCell expected))
                    {
                        missingBaseline++;
                        if (diffs.Count < maxDiffs)
                        {
                            diffs.Add(new JObject
                            {
                                ["x"] = x,
                                ["y"] = y,
                                ["errors"] = new JArray("missing in baseline"),
                            });
                        }

                        continue;
                    }

                    JObject autotile = BuildAutotilePayload(world, catalog, x, y, tile);
                    List<string> errors = new List<string>();

                    if (compareGround)
                    {
                        if (AutotileBaselineCompare.ToLegacyTileId(tile.id) != expected.TileId)
                        {
                            errors.Add($"tileId: expected {expected.TileId}, got {AutotileBaselineCompare.ToLegacyTileId(tile.id)}");
                        }

                        JObject ground = autotile["ground"] as JObject;
                        string actualSprite = ground?["spriteId"]?.Value<string>();
                        bool actualFlip = ground?["flipX"]?.Value<bool>() ?? false;
                        if (actualSprite != expected.GroundSpriteId)
                        {
                            errors.Add($"ground.spriteId: expected {expected.GroundSpriteId}, got {actualSprite}");
                        }

                        if (actualFlip != expected.GroundFlipX)
                        {
                            errors.Add($"ground.flipX: expected {expected.GroundFlipX}, got {actualFlip}");
                        }
                    }

                    if (compareCover)
                    {
                        JObject cover = autotile["cover"] as JObject;
                        bool actualRendered = cover?["rendered"]?.Value<bool>() ?? false;
                        if (actualRendered != expected.CoverRendered)
                        {
                            errors.Add($"cover.rendered: expected {expected.CoverRendered}, got {actualRendered}");
                        }
                        else if (actualRendered)
                        {
                            string actualCoverSprite = cover?["spriteId"]?.Value<string>();
                            bool actualCoverFlip = cover?["flipX"]?.Value<bool>() ?? false;
                            if (actualCoverSprite != expected.CoverSpriteId)
                            {
                                errors.Add($"cover.spriteId: expected {expected.CoverSpriteId}, got {actualCoverSprite}");
                            }

                            if (actualCoverFlip != expected.CoverFlipX)
                            {
                                errors.Add($"cover.flipX: expected {expected.CoverFlipX}, got {actualCoverFlip}");
                            }
                        }
                    }

                    if (errors.Count == 0)
                    {
                        matched++;
                        continue;
                    }

                    if (diffs.Count < maxDiffs)
                    {
                        diffs.Add(new JObject
                        {
                            ["x"] = x,
                            ["y"] = y,
                            ["errors"] = new JArray(errors),
                        });
                    }
                }
            }

            return new JObject
            {
                ["baselineName"] = baselineName,
                ["compareLayer"] = compareLayer ?? "all",
                ["xMin"] = xMin,
                ["yMin"] = yMin,
                ["xMax"] = xMax,
                ["yMax"] = yMax,
                ["summary"] = new JObject
                {
                    ["compared"] = compared,
                    ["matched"] = matched,
                    ["mismatched"] = compared - matched - missingBaseline,
                    ["missingBaseline"] = missingBaseline,
                    ["truncated"] = diffs.Count >= maxDiffs,
                },
                ["diffs"] = diffs,
            };
        }

        /// <summary>
        /// Maps a registry runtime tile index to legacy P1 tile-viz id (0–7).
        /// </summary>
        public static int RuntimeIndexToLegacyTileId(int runtimeIndex) =>
            AutotileBaselineCompare.ToLegacyTileId(runtimeIndex);
    }
}
