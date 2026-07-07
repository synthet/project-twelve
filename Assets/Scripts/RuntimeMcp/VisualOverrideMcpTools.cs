using System;
using System.IO;
using Newtonsoft.Json.Linq;
using ProjectTwelve.Visual.Tiles;
using UnityEngine;

namespace ProjectTwelve.RuntimeMcp
{
    /// <summary>Registers debug-only runtime MCP tools for inspecting and overriding tile visuals.</summary>
    public static class VisualOverrideMcpTools
    {
        private const string DefaultSavePath = "Saves/visual-overrides.json";

        public static void Register(McpDispatcher dispatcher)
        {
            dispatcher.RegisterTool(new McpTool(
                "visual_override_get",
                "Read the resolved autotile payload and any active visual override for a world tile.",
                LayerCoordSchema(),
                args =>
                {
                    SandboxWorld world = RequireWorld();
                    int x = args["x"]?.Value<int>() ?? 0;
                    int y = args["y"]?.Value<int>() ?? 0;
                    AutotileVisualLayer layer = ParseLayer(args["layer"]?.ToString());
                    return BuildEntry(world, x, y, layer);
                }));

            dispatcher.RegisterTool(new McpTool(
                "visual_override_set",
                "Debug-only: override sprite id and transforms for a world tile until cleared.",
                SetSchema(),
                args =>
                {
                    SandboxWorld world = RequireDebugOverrideMode();
                    int x = args["x"]?.Value<int>() ?? 0;
                    int y = args["y"]?.Value<int>() ?? 0;
                    AutotileVisualLayer layer = ParseLayer(args["layer"]?.ToString());
                    string spriteId = args["spriteId"]?.ToString() ?? args["overrideSpriteId"]?.ToString();
                    if (string.IsNullOrWhiteSpace(spriteId))
                    {
                        throw new InvalidOperationException("visual_override_set requires a non-empty spriteId.");
                    }

                    if (!TryResolveTileset(world, x, y, layer, out string tilesetName))
                    {
                        throw new InvalidOperationException($"No {AutotileVisualLayerNames.ToName(layer)} tileset at ({x}, {y}).");
                    }

                    world.SetVisualOverride(
                        x,
                        y,
                        layer,
                        tilesetName,
                        spriteId,
                        args["flipX"]?.Value<bool>() ?? args["overrideFlipX"]?.Value<bool>() ?? false,
                        args["flipY"]?.Value<bool>() ?? args["overrideFlipY"]?.Value<bool>() ?? false,
                        args["rotation"]?.Value<int>() ?? args["rotationDegrees"]?.Value<int>() ?? 0,
                        args["note"]?.ToString());
                    return BuildEntry(world, x, y, layer);
                }));

            dispatcher.RegisterTool(new McpTool(
                "visual_override_clear",
                "Debug-only: clear a visual override from a world tile.",
                LayerCoordSchema(),
                args =>
                {
                    SandboxWorld world = RequireDebugOverrideMode();
                    int x = args["x"]?.Value<int>() ?? 0;
                    int y = args["y"]?.Value<int>() ?? 0;
                    AutotileVisualLayer layer = ParseLayer(args["layer"]?.ToString());
                    bool removed = TryResolveTileset(world, x, y, layer, out string tilesetName)
                        && world.ClearVisualOverride(x, y, layer, tilesetName);
                    JObject result = BuildEntry(world, x, y, layer);
                    result["removed"] = removed;
                    return result;
                }));

            dispatcher.RegisterTool(new McpTool(
                "visual_override_list",
                "List active visual overrides in a world-tile bounding box.",
                ListSchema(),
                args =>
                {
                    SandboxWorld world = RequireWorld();
                    int xMin = args["xMin"]?.Value<int>() ?? int.MinValue;
                    int yMin = args["yMin"]?.Value<int>() ?? int.MinValue;
                    int xMax = args["xMax"]?.Value<int>() ?? int.MaxValue;
                    int yMax = args["yMax"]?.Value<int>() ?? int.MaxValue;

                    JArray overrides = new JArray();
                    foreach (AutotileVisualOverride entry in world.AutotileVisualOverrides.GetAll())
                    {
                        if (entry.x < xMin || entry.x > xMax || entry.y < yMin || entry.y > yMax)
                        {
                            continue;
                        }

                        overrides.Add(BuildOverrideEntry(world, entry));
                    }

                    return new JObject { ["count"] = overrides.Count, ["overrides"] = overrides };
                }));

            dispatcher.RegisterTool(new McpTool(
                "visual_override_save",
                "Debug-only: save active visual overrides to JSON.",
                new JObject
                {
                    ["type"] = "object",
                    ["properties"] = new JObject
                    {
                        ["path"] = new JObject { ["type"] = "string", ["description"] = $"Project-relative or absolute path (default {DefaultSavePath})." }
                    }
                },
                args =>
                {
                    SandboxWorld world = RequireDebugOverrideMode();
                    string path = args["path"]?.ToString();
                    if (string.IsNullOrWhiteSpace(path))
                    {
                        path = DefaultSavePath;
                    }

                    if (!Path.IsPathRooted(path))
                    {
                        path = Path.Combine(Directory.GetCurrentDirectory(), path);
                    }

                    string written = world.SaveVisualOverridesToPath(path);
                    return new JObject { ["path"] = written, ["count"] = world.AutotileVisualOverrides.Count };
                }));
        }

        private static SandboxWorld RequireWorld()
        {
            SandboxWorld world = UnityEngine.Object.FindAnyObjectByType<SandboxWorld>();
            if (world == null)
            {
                throw new InvalidOperationException("SandboxWorld not found in scene; visual override MCP tools require an active sandbox world.");
            }

            return world;
        }

        private static SandboxWorld RequireDebugOverrideMode()
        {
            SandboxWorld world = RequireWorld();
            if (!world.IsDebugOverrideModeEnabled)
            {
                throw new InvalidOperationException(
                    "MCP write tools are disabled until SandboxWorld debug override mode is explicitly enabled in an Editor or development build.");
            }

            return world;
        }

        private static JObject BuildEntry(SandboxWorld world, int x, int y, AutotileVisualLayer layer)
        {
            JObject result = McpTileDebug.BuildAutotileAt(world, world.TileVisualCatalog, x, y);
            if (TryResolveTileset(world, x, y, layer, out string tilesetName)
                && world.TryGetVisualOverride(x, y, layer, tilesetName, out AutotileVisualOverride visualOverride))
            {
                result["override"] = BuildOverridePayload(world, visualOverride);
            }
            else
            {
                result["override"] = null;
            }

            return result;
        }

        private static JObject BuildOverrideEntry(SandboxWorld world, AutotileVisualOverride entry)
        {
            return BuildOverridePayload(world, entry);
        }

        private static JObject BuildOverridePayload(SandboxWorld world, AutotileVisualOverride entry)
        {
            AutotileVisualLayer layer = AutotileVisualLayerNames.Parse(entry.layer);
            world.TryResolveAutoVisual(entry.x, entry.y, layer, out string autoSpriteId, out bool autoFlipX, out _);
            return new JObject
            {
                ["x"] = entry.x,
                ["y"] = entry.y,
                ["layer"] = entry.layer,
                ["tileset"] = entry.tileset,
                ["auto"] = new JObject
                {
                    ["spriteId"] = string.IsNullOrEmpty(entry.autoSpriteId) ? autoSpriteId : entry.autoSpriteId,
                    ["flipX"] = entry.autoSpriteId != null ? entry.autoFlipX : autoFlipX,
                },
                ["override"] = new JObject
                {
                    ["spriteId"] = entry.overrideSpriteId,
                    ["flipX"] = entry.overrideFlipX,
                    ["flipY"] = entry.overrideFlipY,
                    ["rotation"] = entry.rotation,
                    ["note"] = entry.note,
                },
            };
        }

        private static bool TryResolveTileset(SandboxWorld world, int x, int y, AutotileVisualLayer layer, out string tilesetName)
        {
            tilesetName = null;
            SandboxTile tile = world.GetTile(x, y);
            if (!tile.IsSolid || world.TileVisualCatalog == null)
            {
                return false;
            }

            if (layer == AutotileVisualLayer.Cover)
            {
                if (world.TileVisualCatalog.CanEditCoverAt(world.GetTile, x, y, out AutotileTileset coverTileset))
                {
                    tilesetName = coverTileset.Name;
                    return true;
                }

                return false;
            }

            if (world.TileVisualCatalog.TryGetGroundTileset(tile.id, out AutotileTileset groundTileset))
            {
                tilesetName = groundTileset.Name;
                return true;
            }

            return false;
        }

        private static AutotileVisualLayer ParseLayer(string layer)
        {
            return string.IsNullOrWhiteSpace(layer)
                ? AutotileVisualLayer.Ground
                : AutotileVisualLayerNames.Parse(layer);
        }

        private static JObject LayerCoordSchema()
        {
            return new JObject
            {
                ["type"] = "object",
                ["properties"] = new JObject
                {
                    ["x"] = new JObject { ["type"] = "integer" },
                    ["y"] = new JObject { ["type"] = "integer" },
                    ["layer"] = new JObject { ["type"] = "string", ["description"] = "ground or cover (default ground)" },
                },
                ["required"] = new JArray("x", "y"),
            };
        }

        private static JObject SetSchema()
        {
            return new JObject
            {
                ["type"] = "object",
                ["properties"] = new JObject
                {
                    ["x"] = new JObject { ["type"] = "integer" },
                    ["y"] = new JObject { ["type"] = "integer" },
                    ["layer"] = new JObject { ["type"] = "string" },
                    ["spriteId"] = new JObject { ["type"] = "string" },
                    ["flipX"] = new JObject { ["type"] = "boolean" },
                    ["flipY"] = new JObject { ["type"] = "boolean" },
                    ["rotation"] = new JObject { ["type"] = "integer" },
                    ["note"] = new JObject { ["type"] = "string" },
                },
                ["required"] = new JArray("x", "y", "spriteId"),
            };
        }

        private static JObject ListSchema()
        {
            return new JObject
            {
                ["type"] = "object",
                ["properties"] = new JObject
                {
                    ["xMin"] = new JObject { ["type"] = "integer" },
                    ["yMin"] = new JObject { ["type"] = "integer" },
                    ["xMax"] = new JObject { ["type"] = "integer" },
                    ["yMax"] = new JObject { ["type"] = "integer" },
                },
            };
        }
    }
}
