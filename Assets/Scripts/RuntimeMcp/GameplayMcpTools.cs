using Newtonsoft.Json.Linq;
using ProjectTwelve.Sandbox.Registry;
using UnityEngine;

namespace ProjectTwelve.RuntimeMcp
{
    /// <summary>
    /// Registers gameplay MCP tools that control the player and read sandbox debug state.
    /// </summary>
    public static class GameplayMcpTools
    {
        /// <summary>Registers all gameplay tools on the supplied dispatcher.</summary>
        public static void Register(McpDispatcher dispatcher, RuntimeMcpServer server)
        {
            dispatcher.RegisterTool(new McpTool(
                "player_move",
                "Set horizontal movement input for the player character.",
                new JObject
                {
                    ["type"] = "object",
                    ["properties"] = new JObject
                    {
                        ["direction"] = new JObject
                        {
                            ["type"] = "string",
                            ["enum"] = new JArray("left", "right", "none"),
                            ["description"] = "Movement direction."
                        },
                        ["durationSeconds"] = new JObject
                        {
                            ["type"] = "number",
                            ["description"] = "Optional duration in seconds before external input clears."
                        }
                    },
                    ["required"] = new JArray("direction")
                },
                args =>
                {
                    SandboxPlayerController controller = FindPlayerController();
                    if (controller == null)
                    {
                        throw new System.InvalidOperationException("SandboxPlayerController not found in scene.");
                    }

                    string direction = args["direction"]?.ToString() ?? "none";
                    float moveInput = direction switch
                    {
                        "left" => -1f,
                        "right" => 1f,
                        _ => 0f
                    };

                    float duration = args["durationSeconds"]?.Value<float>() ?? 0f;
                    controller.SetExternalMoveInput(moveInput, duration);

                    return new JObject
                    {
                        ["direction"] = direction,
                        ["moveInput"] = moveInput,
                        ["durationSeconds"] = duration
                    };
                }));

            dispatcher.RegisterTool(new McpTool(
                "player_jump",
                "Request a jump for the player character. Honored only when grounded.",
                new JObject
                {
                    ["type"] = "object",
                    ["properties"] = new JObject()
                },
                _ =>
                {
                    SandboxPlayerController controller = FindPlayerController();
                    if (controller == null)
                    {
                        throw new System.InvalidOperationException("SandboxPlayerController not found in scene.");
                    }

                    controller.RequestJump();

                    return new JObject
                    {
                        ["requested"] = true,
                        ["grounded"] = controller.IsGrounded
                    };
                }));

            dispatcher.RegisterTool(new McpTool(
                "player_teleport",
                "Teleport the player to a world-space position.",
                new JObject
                {
                    ["type"] = "object",
                    ["properties"] = new JObject
                    {
                        ["x"] = new JObject { ["type"] = "number" },
                        ["y"] = new JObject { ["type"] = "number" }
                    },
                    ["required"] = new JArray("x", "y")
                },
                args =>
                {
                    SandboxPlayerController controller = FindPlayerController();
                    if (controller == null)
                    {
                        throw new System.InvalidOperationException("SandboxPlayerController not found in scene.");
                    }

                    float x = args["x"]?.Value<float>() ?? 0f;
                    float y = args["y"]?.Value<float>() ?? 0f;
                    controller.TeleportTo(new Vector2(x, y));

                    return new JObject
                    {
                        ["x"] = x,
                        ["y"] = y
                    };
                }));

            dispatcher.RegisterTool(new McpTool(
                "world_set_tile",
                "Place or remove a tile at world tile coordinates.",
                new JObject
                {
                    ["type"] = "object",
                    ["properties"] = new JObject
                    {
                        ["x"] = new JObject { ["type"] = "integer" },
                        ["y"] = new JObject { ["type"] = "integer" },
                        ["tileId"] = new JObject
                        {
                            ["type"] = "integer",
                            ["description"] = "Registry runtime tile index to place (discover via tile_at/tiles_area). Use 0 (Air) to remove."
                        }
                    },
                    ["required"] = new JArray("x", "y", "tileId")
                },
                args =>
                {
                    SandboxWorld world = FindWorld();
                    if (world == null)
                    {
                        throw new System.InvalidOperationException("SandboxWorld not found in scene.");
                    }

                    int x = args["x"]?.Value<int>() ?? 0;
                    int y = args["y"]?.Value<int>() ?? 0;
                    int tileId = args["tileId"]?.Value<int>() ?? SandboxRegistries.AirIndex;
                    world.SetTile(x, y, tileId);

                    SandboxTile tile = world.GetTile(x, y);
                    return new JObject
                    {
                        ["x"] = x,
                        ["y"] = y,
                        ["tileId"] = tile.id,
                        ["solid"] = tile.IsSolid
                    };
                }));

            dispatcher.RegisterTool(new McpTool(
                "player_state",
                "Read the player position, velocity, grounded state, and current tile coordinate.",
                new JObject
                {
                    ["type"] = "object",
                    ["properties"] = new JObject()
                },
                _ => BuildPlayerState(FindPlayerController(), FindWorld())));

            dispatcher.RegisterTool(new McpTool(
                "world_info",
                "Read world seed, tile size, player position, player chunk, and loaded chunk count.",
                new JObject
                {
                    ["type"] = "object",
                    ["properties"] = new JObject()
                },
                _ => BuildWorldInfo(FindWorld())));

            RegisterTileDebugTools(dispatcher);

            dispatcher.RegisterTool(new McpTool(
                "tile_at",
                "Read tile data at world tile coordinates.",
                new JObject
                {
                    ["type"] = "object",
                    ["properties"] = new JObject
                    {
                        ["x"] = new JObject { ["type"] = "integer" },
                        ["y"] = new JObject { ["type"] = "integer" }
                    },
                    ["required"] = new JArray("x", "y")
                },
                args =>
                {
                    SandboxWorld world = FindWorld();
                    if (world == null)
                    {
                        throw new System.InvalidOperationException("SandboxWorld not found in scene.");
                    }

                    int x = args["x"]?.Value<int>() ?? 0;
                    int y = args["y"]?.Value<int>() ?? 0;
                    SandboxTile tile = world.GetTile(x, y);

                    return new JObject
                    {
                        ["x"] = x,
                        ["y"] = y,
                        ["tileId"] = tile.id,
                        ["solid"] = tile.IsSolid,
                        ["light"] = tile.light,
                        ["fluid"] = tile.fluid,
                        ["metadata"] = tile.metadata
                    };
                }));

            dispatcher.RegisterTool(new McpTool(
                "perf",
                "Read smoothed FPS and frame time.",
                new JObject
                {
                    ["type"] = "object",
                    ["properties"] = new JObject()
                },
                _ =>
                {
                    float delta = server != null ? server.SmoothedDeltaTime : Time.unscaledDeltaTime;
                    float fps = delta > 0f ? 1f / delta : 0f;

                    return new JObject
                    {
                        ["fps"] = fps,
                        ["frameTimeMs"] = delta * 1000f
                    };
                }));
        }

        private static void RegisterTileDebugTools(McpDispatcher dispatcher)
        {
            JObject areaProperties = new JObject
            {
                ["xMin"] = new JObject { ["type"] = "integer", ["description"] = "Inclusive west bound (use with xMax/yMin/yMax)." },
                ["yMin"] = new JObject { ["type"] = "integer" },
                ["xMax"] = new JObject { ["type"] = "integer" },
                ["yMax"] = new JObject { ["type"] = "integer" },
                ["centerX"] = new JObject { ["type"] = "integer", ["description"] = "Center X when using radius mode." },
                ["centerY"] = new JObject { ["type"] = "integer", ["description"] = "Center Y when using radius mode." },
                ["x"] = new JObject { ["type"] = "integer", ["description"] = "Alias for centerX." },
                ["y"] = new JObject { ["type"] = "integer", ["description"] = "Alias for centerY." },
                ["radius"] = new JObject { ["type"] = "integer", ["description"] = "Square radius from center (default 2)." },
                ["radiusX"] = new JObject { ["type"] = "integer", ["description"] = "Horizontal radius from center." },
                ["radiusY"] = new JObject { ["type"] = "integer", ["description"] = "Vertical radius from center." },
                ["aroundPlayer"] = new JObject
                {
                    ["type"] = "boolean",
                    ["description"] = "When true, center the area on the current player tile."
                },
                ["includeAir"] = new JObject
                {
                    ["type"] = "boolean",
                    ["description"] = "Include air cells in the tiles array (default true for tiles_area)."
                },
                ["maxCells"] = new JObject
                {
                    ["type"] = "integer",
                    ["description"] = $"Maximum cells in the requested area (default {McpTileDebug.MaxAreaCells})."
                }
            };

            dispatcher.RegisterTool(new McpTool(
                "tiles_area",
                "Dump a rectangular world-tile region with ids, names, chunk coords, and an ASCII grid.",
                new JObject
                {
                    ["type"] = "object",
                    ["properties"] = areaProperties
                },
                args =>
                {
                    SandboxWorld world = RequireWorld();
                    if (!McpTileDebug.TryResolveBounds(
                            args,
                            world,
                            McpTileDebug.MaxAreaCells,
                            out int xMin,
                            out int yMin,
                            out int xMax,
                            out int yMax,
                            out string boundsError))
                    {
                        throw new System.InvalidOperationException(boundsError);
                    }

                    bool includeAir = args.Value<bool?>("includeAir") ?? true;
                    return McpTileDebug.BuildAreaDump(
                        world,
                        world.TileVisualCatalog,
                        xMin,
                        yMin,
                        xMax,
                        yMax,
                        includeAir,
                        includeAutotile: false);
                }));

            dispatcher.RegisterTool(new McpTool(
                "tile_autotile",
                "Inspect autotile neighbor connectivity, masks, matching rule ids, and resolved sprite ids for one tile.",
                new JObject
                {
                    ["type"] = "object",
                    ["properties"] = new JObject
                    {
                        ["x"] = new JObject { ["type"] = "integer" },
                        ["y"] = new JObject { ["type"] = "integer" }
                    },
                    ["required"] = new JArray("x", "y")
                },
                args =>
                {
                    SandboxWorld world = RequireWorld();
                    int x = args["x"]?.Value<int>() ?? 0;
                    int y = args["y"]?.Value<int>() ?? 0;
                    return McpTileDebug.BuildAutotileAt(world, world.TileVisualCatalog, x, y);
                }));

            dispatcher.RegisterTool(new McpTool(
                "tiles_autotile_area",
                "Dump autotile connectivity and resolved ground/cover sprites for each solid tile in a region.",
                new JObject
                {
                    ["type"] = "object",
                    ["properties"] = areaProperties
                },
                args =>
                {
                    SandboxWorld world = RequireWorld();
                    int maxCells = args["maxCells"]?.Value<int>() ?? McpTileDebug.MaxAreaCells;
                    if (maxCells < 1)
                    {
                        maxCells = 1;
                    }

                    if (!McpTileDebug.TryResolveBounds(args, world, maxCells, out int xMin, out int yMin, out int xMax, out int yMax, out string boundsError))
                    {
                        throw new System.InvalidOperationException(boundsError);
                    }

                    bool includeAir = args.Value<bool?>("includeAir") ?? false;
                    return McpTileDebug.BuildAreaDump(
                        world,
                        world.TileVisualCatalog,
                        xMin,
                        yMin,
                        xMax,
                        yMax,
                        includeAir,
                        includeAutotile: true);
                }));

            dispatcher.RegisterTool(new McpTool(
                "world_export_tile_space",
                "Export a rectangular world region as project-twelve/tile-space/v1 JSON (legacy tile ids).",
                new JObject
                {
                    ["type"] = "object",
                    ["properties"] = new JObject
                    {
                        ["xMin"] = new JObject { ["type"] = "integer" },
                        ["yMin"] = new JObject { ["type"] = "integer" },
                        ["xMax"] = new JObject { ["type"] = "integer" },
                        ["yMax"] = new JObject { ["type"] = "integer" },
                        ["centerX"] = areaProperties["centerX"],
                        ["centerY"] = areaProperties["centerY"],
                        ["x"] = areaProperties["x"],
                        ["y"] = areaProperties["y"],
                        ["radius"] = areaProperties["radius"],
                        ["radiusX"] = areaProperties["radiusX"],
                        ["radiusY"] = areaProperties["radiusY"],
                        ["aroundPlayer"] = areaProperties["aroundPlayer"],
                        ["name"] = new JObject { ["type"] = "string", ["description"] = "Fixture name (default runtime-export)." },
                        ["writeFile"] = new JObject
                        {
                            ["type"] = "boolean",
                            ["description"] = "When true, also writes JSON to Application.persistentDataPath and returns filePath."
                        }
                    }
                },
                args =>
                {
                    SandboxWorld world = RequireWorld();
                    if (!McpTileDebug.TryResolveBounds(
                            args,
                            world,
                            McpTileDebug.MaxDiffScanCells,
                            out int xMin,
                            out int yMin,
                            out int xMax,
                            out int yMax,
                            out string boundsError))
                    {
                        throw new System.InvalidOperationException(boundsError);
                    }

                    bool writeFile = args.Value<bool?>("writeFile") ?? false;
                    string name = args["name"]?.Value<string>();
                    return McpTileDebug.BuildTileSpaceExport(
                        world,
                        xMin,
                        yMin,
                        xMax,
                        yMax,
                        writeFile,
                        name);
                }));

            dispatcher.RegisterTool(new McpTool(
                "autotile_diff_baseline",
                "Compare live autotile resolution against a committed baseline; returns mismatches only.",
                new JObject
                {
                    ["type"] = "object",
                    ["properties"] = new JObject
                    {
                        ["xMin"] = new JObject { ["type"] = "integer" },
                        ["yMin"] = new JObject { ["type"] = "integer" },
                        ["xMax"] = new JObject { ["type"] = "integer" },
                        ["yMax"] = new JObject { ["type"] = "integer" },
                        ["centerX"] = areaProperties["centerX"],
                        ["centerY"] = areaProperties["centerY"],
                        ["x"] = areaProperties["x"],
                        ["y"] = areaProperties["y"],
                        ["radius"] = areaProperties["radius"],
                        ["radiusX"] = areaProperties["radiusX"],
                        ["radiusY"] = areaProperties["radiusY"],
                        ["aroundPlayer"] = areaProperties["aroundPlayer"],
                        ["baselineName"] = new JObject
                        {
                            ["type"] = "string",
                            ["description"] = "Baseline file stem (default sandbox-scene-mountain)."
                        },
                        ["maxDiffs"] = new JObject
                        {
                            ["type"] = "integer",
                            ["description"] = "Maximum mismatch entries returned (default 100)."
                        },
                        ["compareLayer"] = new JObject
                        {
                            ["type"] = "string",
                            ["enum"] = new JArray("all", "ground", "cover"),
                            ["description"] = "Which autotile layer to compare (default all)."
                        }
                    }
                },
                args =>
                {
                    SandboxWorld world = RequireWorld();
                    if (!McpTileDebug.TryResolveBounds(
                            args,
                            world,
                            McpTileDebug.MaxDiffScanCells,
                            out int xMin,
                            out int yMin,
                            out int xMax,
                            out int yMax,
                            out string boundsError))
                    {
                        throw new System.InvalidOperationException(boundsError);
                    }

                    string baselineName = args["baselineName"]?.Value<string>() ?? "sandbox-scene-mountain";
                    int maxDiffs = args["maxDiffs"]?.Value<int>() ?? 100;
                    if (maxDiffs < 1)
                    {
                        maxDiffs = 1;
                    }

                    string compareLayer = args["compareLayer"]?.Value<string>() ?? "all";
                    return McpTileDebug.BuildAutotileDiffBaseline(
                        world,
                        world.TileVisualCatalog,
                        xMin,
                        yMin,
                        xMax,
                        yMax,
                        baselineName,
                        maxDiffs,
                        compareLayer);
                }));
        }

        private static SandboxWorld RequireWorld()
        {
            SandboxWorld world = FindWorld();
            if (world == null)
            {
                throw new System.InvalidOperationException("SandboxWorld not found in scene.");
            }

            return world;
        }

        private static JObject BuildPlayerState(SandboxPlayerController controller, SandboxWorld world)
        {
            if (controller == null)
            {
                throw new System.InvalidOperationException("SandboxPlayerController not found in scene.");
            }

            Vector2 position = controller.transform.position;
            Vector2 velocity = controller.Velocity;
            JObject result = new JObject
            {
                ["position"] = new JObject { ["x"] = position.x, ["y"] = position.y },
                ["velocity"] = new JObject { ["x"] = velocity.x, ["y"] = velocity.y },
                ["grounded"] = controller.IsGrounded
            };

            if (world != null)
            {
                Vector2Int tile = world.WorldPositionToTile(position);
                result["tile"] = new JObject { ["x"] = tile.x, ["y"] = tile.y };
            }

            return result;
        }

        private static JObject BuildWorldInfo(SandboxWorld world)
        {
            if (world == null)
            {
                throw new System.InvalidOperationException("SandboxWorld not found in scene.");
            }

            JObject result = new JObject
            {
                ["seed"] = world.Seed,
                ["tileSize"] = world.TileSize,
                ["loadedChunkCount"] = world.LoadedChunkCount
            };

            if (world.TryGetPlayerWorldPosition(out Vector2 position))
            {
                Vector2Int tile = world.WorldPositionToTile(position);
                Vector2Int chunk = SandboxWorld.WorldToChunkCoord(tile.x, tile.y);
                result["playerPosition"] = new JObject { ["x"] = position.x, ["y"] = position.y };
                result["playerTile"] = new JObject { ["x"] = tile.x, ["y"] = tile.y };
                result["playerChunk"] = new JObject { ["x"] = chunk.x, ["y"] = chunk.y };
            }
            else
            {
                result["playerPosition"] = null;
            }

            return result;
        }

        private static SandboxPlayerController FindPlayerController()
        {
            return Object.FindAnyObjectByType<SandboxPlayerController>();
        }

        private static SandboxWorld FindWorld()
        {
            return Object.FindAnyObjectByType<SandboxWorld>();
        }
    }
}
