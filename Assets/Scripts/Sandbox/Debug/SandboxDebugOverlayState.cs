#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;

namespace ProjectTwelve.Sandbox.Debug
{
    /// <summary>Debug overlays implemented by the P2-TOOL-002 runtime renderer.</summary>
    public enum SandboxDebugOverlay
    {
        ChunkBorders = 0,
        TileSolidity = 1,
        LightHeatmap = 2,
        Fluid = 3,
        ColliderRebuilds = 4,
        DirtyFlags = 5
    }

    /// <summary>
    /// Pure shared state for overlay hotkeys, the debug console, and Runtime MCP tools.
    /// The renderer checks <see cref="AnyEnabled"/> before doing any world or camera work.
    /// </summary>
    public sealed class SandboxDebugOverlayState
    {
        private static readonly SandboxDebugOverlay[] overlays =
        {
            SandboxDebugOverlay.ChunkBorders,
            SandboxDebugOverlay.TileSolidity,
            SandboxDebugOverlay.LightHeatmap,
            SandboxDebugOverlay.Fluid,
            SandboxDebugOverlay.ColliderRebuilds,
            SandboxDebugOverlay.DirtyFlags
        };

        private readonly bool[] enabled = new bool[overlays.Length];
        private int enabledCount;

        /// <summary>Stable overlay order used by UI and MCP state responses.</summary>
        public static IReadOnlyList<SandboxDebugOverlay> AllOverlays => overlays;

        /// <summary>False when the runtime renderer can return without doing any work.</summary>
        public bool AnyEnabled => enabledCount > 0;

        public bool IsEnabled(SandboxDebugOverlay overlay)
        {
            return enabled[(int)overlay];
        }

        public void SetEnabled(SandboxDebugOverlay overlay, bool value)
        {
            int index = (int)overlay;
            if (enabled[index] == value)
            {
                return;
            }

            enabled[index] = value;
            enabledCount += value ? 1 : -1;
        }

        public bool Toggle(SandboxDebugOverlay overlay)
        {
            bool value = !IsEnabled(overlay);
            SetEnabled(overlay, value);
            return value;
        }

        /// <summary>Canonical name used by console and MCP tools.</summary>
        public static string GetName(SandboxDebugOverlay overlay)
        {
            return overlay switch
            {
                SandboxDebugOverlay.ChunkBorders => "chunk_borders",
                SandboxDebugOverlay.TileSolidity => "tile_solidity",
                SandboxDebugOverlay.LightHeatmap => "light_heatmap",
                SandboxDebugOverlay.Fluid => "fluid",
                SandboxDebugOverlay.ColliderRebuilds => "collider_rebuilds",
                SandboxDebugOverlay.DirtyFlags => "dirty_flags",
                _ => throw new ArgumentOutOfRangeException(nameof(overlay), overlay, null)
            };
        }

        /// <summary>Hotkey label for help text and the on-screen legend.</summary>
        public static string GetHotkeyLabel(SandboxDebugOverlay overlay)
        {
            return overlay switch
            {
                SandboxDebugOverlay.ChunkBorders => "F4",
                SandboxDebugOverlay.TileSolidity => "F7",
                SandboxDebugOverlay.LightHeatmap => "F10",
                SandboxDebugOverlay.Fluid => "F11",
                SandboxDebugOverlay.ColliderRebuilds => "F12",
                SandboxDebugOverlay.DirtyFlags => "F2",
                _ => throw new ArgumentOutOfRangeException(nameof(overlay), overlay, null)
            };
        }

        /// <summary>Parses a canonical overlay name (case-insensitive).</summary>
        public static bool TryParseName(string name, out SandboxDebugOverlay overlay)
        {
            for (int i = 0; i < overlays.Length; i++)
            {
                SandboxDebugOverlay candidate = overlays[i];
                if (string.Equals(GetName(candidate), name, StringComparison.OrdinalIgnoreCase))
                {
                    overlay = candidate;
                    return true;
                }
            }

            overlay = default;
            return false;
        }

        /// <summary>Comma-separated canonical names for error messages and help text.</summary>
        public static string JoinNames()
        {
            string[] names = new string[overlays.Length];
            for (int i = 0; i < overlays.Length; i++)
            {
                names[i] = GetName(overlays[i]);
            }

            return string.Join(", ", names);
        }
    }

    /// <summary>Process-local debug state shared by every runtime debug surface.</summary>
    public static class SandboxDebugRuntime
    {
        public static SandboxDebugOverlayState OverlayState { get; } = new SandboxDebugOverlayState();
    }
}
#endif
