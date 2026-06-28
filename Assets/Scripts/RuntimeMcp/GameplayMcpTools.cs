using Newtonsoft.Json.Linq;
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
                            ["description"] = "Tile id to place. Use 0 (Air) to remove."
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
                    int tileId = args["tileId"]?.Value<int>() ?? SandboxTileIds.Air;
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
