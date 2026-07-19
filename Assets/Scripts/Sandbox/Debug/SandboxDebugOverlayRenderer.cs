#if UNITY_EDITOR || DEVELOPMENT_BUILD
using ProjectTwelve.Sandbox.Fluid;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace ProjectTwelve.Sandbox.Debug
{
    /// <summary>Immediate-mode, development-only world overlays for P2 debug state.</summary>
    public sealed class SandboxDebugOverlayRenderer : MonoBehaviour
    {
        private static readonly Color SolidityColor = new Color(1f, 0.1f, 0.1f, 0.28f);
        private static readonly Color FluidColor = new Color(0.05f, 0.55f, 1f, 0.45f);
        private static readonly Color FluidAwakeColor = new Color(1f, 0.1f, 1f, 0.9f);
        private static readonly Color ChunkLoadedColor = new Color(0.2f, 1f, 0.3f, 0.9f);
        private static readonly Color ChunkUnloadedColor = new Color(0.6f, 0.6f, 0.6f, 0.55f);
        private static readonly Color ColliderColor = new Color(1f, 0.45f, 0.05f, 0.7f);

        private SandboxDebugOverlayState state;
        private SandboxWorld world;
        private Camera targetCamera;
        private SandboxFluidController fluidController;

        public void Initialize(SandboxDebugOverlayState overlayState)
        {
            state = overlayState;
        }

        private void Update()
        {
            if (PressedFunctionKey(4))
            {
                Toggle(SandboxDebugOverlay.ChunkBorders);
            }
            else if (PressedFunctionKey(7))
            {
                Toggle(SandboxDebugOverlay.TileSolidity);
            }
            else if (PressedFunctionKey(10))
            {
                Toggle(SandboxDebugOverlay.LightHeatmap);
            }
            else if (PressedFunctionKey(11))
            {
                Toggle(SandboxDebugOverlay.Fluid);
            }
            else if (PressedFunctionKey(12))
            {
                Toggle(SandboxDebugOverlay.ColliderRebuilds);
            }
            else if (PressedFunctionKey(2))
            {
                Toggle(SandboxDebugOverlay.DirtyFlags);
            }
        }

        private void OnGUI()
        {
            if (state == null || !state.AnyEnabled || Event.current.type != EventType.Repaint)
            {
                return;
            }

            world ??= FindAnyObjectByType<SandboxWorld>();
            targetCamera ??= Camera.main;
            if (world == null || targetCamera == null || !targetCamera.orthographic)
            {
                return;
            }

            GetVisibleTileBounds(out int xMin, out int yMin, out int xMax, out int yMax);
            DrawTileOverlays(xMin, yMin, xMax, yMax);
            DrawChunkOverlays(xMin, yMin, xMax, yMax);
            GUI.color = Color.white;
        }

        private void DrawTileOverlays(int xMin, int yMin, int xMax, int yMax)
        {
            bool drawSolidity = state.IsEnabled(SandboxDebugOverlay.TileSolidity);
            bool drawLight = state.IsEnabled(SandboxDebugOverlay.LightHeatmap);
            bool drawFluid = state.IsEnabled(SandboxDebugOverlay.Fluid);
            if (!drawSolidity && !drawLight && !drawFluid)
            {
                return;
            }

            if (drawFluid && fluidController == null)
            {
                fluidController = FindAnyObjectByType<SandboxFluidController>();
            }

            SandboxFluidSimulator simulator = fluidController != null ? fluidController.Simulator : null;
            for (int x = xMin; x <= xMax; x++)
            {
                for (int y = yMin; y <= yMax; y++)
                {
                    if (!world.TryGetExistingTile(x, y, out SandboxTile tile))
                    {
                        continue;
                    }

                    Rect rect = WorldTileRect(x, y);
                    if (drawSolidity && tile.IsSolid)
                    {
                        DrawRect(rect, SolidityColor);
                    }

                    if (drawLight)
                    {
                        float light = tile.light / 15f;
                        DrawRect(rect, new Color(1f - light, light, 0.1f, 0.32f));
                    }

                    if (drawFluid && tile.fluid > 0f)
                    {
                        Rect fill = rect;
                        float fraction = Mathf.Clamp01(tile.fluid);
                        fill.y += fill.height * (1f - fraction);
                        fill.height *= fraction;
                        DrawRect(fill, FluidColor);
                        if (simulator != null && simulator.IsAwake(x, y))
                        {
                            DrawOutline(rect, FluidAwakeColor, 2f);
                        }
                    }
                }
            }
        }

        private void DrawChunkOverlays(int xMin, int yMin, int xMax, int yMax)
        {
            bool drawBorders = state.IsEnabled(SandboxDebugOverlay.ChunkBorders);
            bool drawRebuilds = state.IsEnabled(SandboxDebugOverlay.ColliderRebuilds);
            bool drawDirty = state.IsEnabled(SandboxDebugOverlay.DirtyFlags);
            if (!drawBorders && !drawRebuilds && !drawDirty)
            {
                return;
            }

            Vector2Int minChunk = SandboxWorld.WorldToChunkCoord(xMin, yMin);
            Vector2Int maxChunk = SandboxWorld.WorldToChunkCoord(xMax, yMax);
            for (int chunkX = minChunk.x; chunkX <= maxChunk.x; chunkX++)
            {
                for (int chunkY = minChunk.y; chunkY <= maxChunk.y; chunkY++)
                {
                    Vector2Int coord = new Vector2Int(chunkX, chunkY);
                    bool generated = world.TryGetChunkDebugState(coord, out SandboxChunkDebugState chunk);
                    Rect rect = WorldChunkRect(coord);

                    if (drawBorders)
                    {
                        Color color = generated && chunk.RendererLoaded ? ChunkLoadedColor : ChunkUnloadedColor;
                        DrawOutline(rect, color, 2f);
                    }

                    if (drawRebuilds && world.WasColliderRebuiltThisFrame(coord))
                    {
                        DrawRect(rect, ColliderColor);
                    }

                    if (drawDirty && generated)
                    {
                        string flags = FormatFlags(chunk);
                        if (!string.IsNullOrEmpty(flags))
                        {
                            GUI.color = Color.white;
                            GUI.Label(new Rect(rect.x + 3f, rect.y + 3f, rect.width - 6f, 22f),
                                $"[{coord.x},{coord.y}] {flags}");
                        }
                    }
                }
            }
        }

        private static string FormatFlags(SandboxChunkDebugState state)
        {
            string flags = string.Empty;
            if (state.NeedsRenderRebuild) flags += "R ";
            if (state.NeedsColliderRebuild) flags += "C ";
            if (state.IsSaveDirty) flags += "S ";
            if (state.HasEdits) flags += "E";
            return flags.TrimEnd();
        }

        private void GetVisibleTileBounds(out int xMin, out int yMin, out int xMax, out int yMax)
        {
            float halfHeight = targetCamera.orthographicSize;
            float halfWidth = halfHeight * targetCamera.aspect;
            Vector3 position = targetCamera.transform.position;
            float tileSize = world.TileSize;
            xMin = Mathf.FloorToInt((position.x - halfWidth) / tileSize) - 1;
            xMax = Mathf.CeilToInt((position.x + halfWidth) / tileSize) + 1;
            yMin = Mathf.FloorToInt((position.y - halfHeight) / tileSize) - 1;
            yMax = Mathf.CeilToInt((position.y + halfHeight) / tileSize) + 1;
        }

        private Rect WorldTileRect(int x, int y)
        {
            float size = world.TileSize;
            return WorldRect(x * size, y * size, size, size);
        }

        private Rect WorldChunkRect(Vector2Int coord)
        {
            float size = SandboxChunk.Size * world.TileSize;
            return WorldRect(coord.x * size, coord.y * size, size, size);
        }

        private Rect WorldRect(float x, float y, float width, float height)
        {
            Vector3 a = targetCamera.WorldToScreenPoint(new Vector3(x, y, 0f));
            Vector3 b = targetCamera.WorldToScreenPoint(new Vector3(x + width, y + height, 0f));
            float left = Mathf.Min(a.x, b.x);
            float top = Screen.height - Mathf.Max(a.y, b.y);
            return new Rect(left, top, Mathf.Abs(b.x - a.x), Mathf.Abs(b.y - a.y));
        }

        private static void DrawRect(Rect rect, Color color)
        {
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
        }

        private static void DrawOutline(Rect rect, Color color, float thickness)
        {
            DrawRect(new Rect(rect.x, rect.y, rect.width, thickness), color);
            DrawRect(new Rect(rect.x, rect.yMax - thickness, rect.width, thickness), color);
            DrawRect(new Rect(rect.x, rect.y, thickness, rect.height), color);
            DrawRect(new Rect(rect.xMax - thickness, rect.y, thickness, rect.height), color);
        }

        private void Toggle(SandboxDebugOverlay overlay)
        {
            bool enabled = state.Toggle(overlay);
            UnityEngine.Debug.Log($"Debug overlay {SandboxDebugOverlayState.GetName(overlay)}: {(enabled ? "on" : "off")}");
        }

        private static bool PressedFunctionKey(int number)
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null)
            {
                Key key = number switch
                {
                    2 => Key.F2,
                    4 => Key.F4,
                    7 => Key.F7,
                    10 => Key.F10,
                    11 => Key.F11,
                    12 => Key.F12,
                    _ => Key.None
                };
                if (key != Key.None && keyboard[key].wasPressedThisFrame)
                {
                    return true;
                }
            }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(number switch
            {
                2 => KeyCode.F2,
                4 => KeyCode.F4,
                7 => KeyCode.F7,
                10 => KeyCode.F10,
                11 => KeyCode.F11,
                12 => KeyCode.F12,
                _ => KeyCode.None
            });
#else
            return false;
#endif
        }
    }
}
#endif
