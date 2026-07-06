using System;
using System.IO;
using Newtonsoft.Json.Linq;
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
                "Read the resolved autotile payload and any active ground visual override for a world tile.",
                TileCoordSchema(),
                args =>
                {
                    SandboxWorld world = RequireWorld();
                    int x = args["x"]?.Value<int>() ?? 0;
                    int y = args["y"]?.Value<int>() ?? 0;
                    return BuildEntry(world, x, y);
                }));

            dispatcher.RegisterTool(new McpTool(
                "visual_override_set",
                "Debug-only: override the ground sprite id and flipX for a world tile until cleared.",
                new JObject
                {
                    ["type"] = "object",
                    ["properties"] = new JObject
                    {
                        ["x"] = new JObject { ["type"] = "integer" },
                        ["y"] = new JObject { ["type"] = "integer" },
                        ["spriteId"] = new JObject { ["type"] = "string" },
                        ["flipX"] = new JObject { ["type"] = "boolean" }
                    },
                    ["required"] = new JArray("x", "y", "spriteId")
                },
                args =>
                {
                    SandboxWorld world = RequireWorld();
                    int x = args["x"]?.Value<int>() ?? 0;
                    int y = args["y"]?.Value<int>() ?? 0;
                    string spriteId = args["spriteId"]?.ToString();
                    if (string.IsNullOrWhiteSpace(spriteId))
                    {
                        throw new InvalidOperationException("visual_override_set requires a non-empty spriteId.");
                    }

                    world.SetVisualOverride(x, y, spriteId, args["flipX"]?.Value<bool>() ?? false);
                    return BuildEntry(world, x, y);
                }));

            dispatcher.RegisterTool(new McpTool(
                "visual_override_clear",
                "Debug-only: clear a ground visual override from a world tile.",
                TileCoordSchema(),
                args =>
                {
                    SandboxWorld world = RequireWorld();
                    int x = args["x"]?.Value<int>() ?? 0;
                    int y = args["y"]?.Value<int>() ?? 0;
                    bool removed = world.ClearVisualOverride(x, y);
                    JObject result = BuildEntry(world, x, y);
                    result["removed"] = removed;
                    return result;
                }));

            dispatcher.RegisterTool(new McpTool(
                "visual_override_list",
                "List active runtime ground visual overrides.",
                new JObject { ["type"] = "object", ["properties"] = new JObject() },
                _ =>
                {
                    SandboxWorld world = RequireWorld();
                    JArray overrides = new JArray();
                    foreach (System.Collections.Generic.KeyValuePair<Vector2Int, SandboxVisualOverride> pair in world.VisualOverrides)
                    {
                        overrides.Add(BuildOverride(pair.Key.x, pair.Key.y, pair.Value));
                    }

                    return new JObject { ["count"] = overrides.Count, ["overrides"] = overrides };
                }));

            dispatcher.RegisterTool(new McpTool(
                "visual_override_save",
                "Debug-only: save active runtime ground visual overrides to JSON.",
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
                    SandboxWorld world = RequireWorld();
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
                    return new JObject { ["path"] = written, ["count"] = world.VisualOverrides.Count };
                }));
        }

        private static SandboxWorld RequireWorld()
        {
            SandboxWorld world = UnityEngine.Object.FindAnyObjectByType<SandboxWorld>();
            if (world == null)
            {
                throw new InvalidOperationException("SandboxWorld not found in scene; visual override MCP tools require an active sandbox world.");
            }

            if (world.VisualOverrides == null)
            {
                throw new InvalidOperationException("SandboxWorld visual override map is not active.");
            }

            return world;
        }

        private static JObject BuildEntry(SandboxWorld world, int x, int y)
        {
            JObject result = McpTileDebug.BuildAutotileAt(world, world.TileVisualCatalog, x, y);
            result["override"] = world.TryGetVisualOverride(x, y, out SandboxVisualOverride visualOverride)
                ? BuildOverride(x, y, visualOverride)
                : null;
            return result;
        }

        private static JObject BuildOverride(int x, int y, SandboxVisualOverride visualOverride)
        {
            return new JObject
            {
                ["x"] = x,
                ["y"] = y,
                ["layer"] = "ground",
                ["spriteId"] = visualOverride.SpriteId,
                ["flipX"] = visualOverride.FlipX
            };
        }

        private static JObject TileCoordSchema()
        {
            return new JObject
            {
                ["type"] = "object",
                ["properties"] = new JObject
                {
                    ["x"] = new JObject { ["type"] = "integer" },
                    ["y"] = new JObject { ["type"] = "integer" }
                },
                ["required"] = new JArray("x", "y")
            };
        }
    }
}
