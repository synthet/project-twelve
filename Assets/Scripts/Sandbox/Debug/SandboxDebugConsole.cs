#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace ProjectTwelve.Sandbox.Debug
{
    /// <summary>
    /// Lightweight Play Mode debug console. Command parsing is static so EditMode tests can
    /// exercise it without a scene; the MonoBehaviour hosts the on-screen input field.
    /// </summary>
    public sealed class SandboxDebugConsole : MonoBehaviour
    {
        private static readonly Regex SlotNamePattern = new Regex(
            @"^[A-Za-z0-9][A-Za-z0-9_-]*$",
            RegexOptions.CultureInvariant | RegexOptions.Compiled);

        private SandboxDebugOverlayState state;
        private bool visible;
        private string input = string.Empty;
        private string lastResult = string.Empty;

        /// <summary>Binds shared overlay state used by the <c>overlay</c> command.</summary>
        public void Initialize(SandboxDebugOverlayState overlayState)
        {
            state = overlayState ?? throw new ArgumentNullException(nameof(overlayState));
        }

        /// <summary>
        /// Save/load slot names must be simple identifiers — no path separators or whitespace.
        /// </summary>
        public static bool IsValidSlotName(string name)
        {
            return !string.IsNullOrEmpty(name) && name.Length <= 40 && SlotNamePattern.IsMatch(name);
        }

        /// <summary>Parses and runs one console line against optional world/player hooks.</summary>
        public static string ExecuteCommand(
            string line,
            SandboxDebugOverlayState overlayState,
            SandboxWorld world,
            SandboxPlayerController player)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                return string.Empty;
            }

            string[] parts = line.Trim().Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            string command = parts[0].ToLowerInvariant();

            switch (command)
            {
                case "help":
                    return BuildHelp();
                case "overlay":
                    return ExecuteOverlay(parts, overlayState);
                case "teleport":
                    return ExecuteTeleport(parts, world, player);
                case "set_tile":
                    return ExecuteSetTile(parts, world);
                case "get_tile":
                    return ExecuteGetTile(parts, world);
                case "generate_chunk":
                    return ExecuteGenerateChunk(parts, world);
                case "dump_chunk":
                    return ExecuteDumpChunk(parts, world);
                case "save":
                case "load":
                    return ExecuteSaveLoad(command, parts, world);
                default:
                    return $"Unknown command '{parts[0]}'. Type 'help' for the command list.";
            }
        }

        private void Update()
        {
            if (PressedBackquote())
            {
                visible = !visible;
            }
        }

        private void OnGUI()
        {
            if (!visible)
            {
                return;
            }

            const float height = 28f;
            Rect field = new Rect(8f, Screen.height - height - 8f, Screen.width - 16f, height);
            GUI.SetNextControlName("SandboxDebugConsoleInput");
            input = GUI.TextField(field, input);
            GUI.FocusControl("SandboxDebugConsoleInput");

            Event e = Event.current;
            if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Return)
            {
                lastResult = ExecuteCommand(input, state, FindAnyObjectByType<SandboxWorld>(), FindAnyObjectByType<SandboxPlayerController>());
                input = string.Empty;
                e.Use();
            }

            if (!string.IsNullOrEmpty(lastResult))
            {
                GUI.Label(new Rect(8f, Screen.height - height - 32f, Screen.width - 16f, 24f), lastResult);
            }
        }

        private static string ExecuteOverlay(string[] parts, SandboxDebugOverlayState overlayState)
        {
            if (overlayState == null)
            {
                return "Overlay state unavailable.";
            }

            if (parts.Length < 2)
            {
                return $"Usage: overlay <name> [on|off|toggle]. Names: {SandboxDebugOverlayState.JoinNames()}";
            }

            if (!SandboxDebugOverlayState.TryParseName(parts[1], out SandboxDebugOverlay overlay))
            {
                return $"Unknown overlay '{parts[1]}'. Names: {SandboxDebugOverlayState.JoinNames()}";
            }

            string mode = parts.Length >= 3 ? parts[2].ToLowerInvariant() : "toggle";
            switch (mode)
            {
                case "on":
                case "true":
                case "1":
                    overlayState.SetEnabled(overlay, true);
                    break;
                case "off":
                case "false":
                case "0":
                    overlayState.SetEnabled(overlay, false);
                    break;
                case "toggle":
                    overlayState.Toggle(overlay);
                    break;
                default:
                    return $"Unknown overlay mode '{parts[2]}'. Use on, off, or toggle.";
            }

            string name = SandboxDebugOverlayState.GetName(overlay);
            return $"{name}: {(overlayState.IsEnabled(overlay) ? "on" : "off")}";
        }

        private static string ExecuteTeleport(
            string[] parts,
            SandboxWorld world,
            SandboxPlayerController player)
        {
            if (!CanMutate(world, out string error))
            {
                return error;
            }

            if (player == null)
            {
                return "Player not found.";
            }

            if (!TryFloat(parts, 1, out float x) || !TryFloat(parts, 2, out float y))
            {
                return "Usage: teleport <x> <y>";
            }

            player.TeleportTo(new Vector2(x, y));
            return $"Player teleported to ({x}, {y}).";
        }

        private static string ExecuteSetTile(string[] parts, SandboxWorld world)
        {
            if (!CanMutate(world, out string error))
            {
                return error;
            }

            if (!TryInt(parts, 1, out int x)
                || !TryInt(parts, 2, out int y)
                || !TryInt(parts, 3, out int tileId))
            {
                return "Usage: set_tile <x> <y> <runtime-tile-id>";
            }

            return world.TrySetDebugOverrideTile(x, y, tileId)
                ? $"Tile ({x}, {y}) set to {tileId}."
                : "Tile edit rejected by debug override mode.";
        }

        private static string ExecuteGetTile(string[] parts, SandboxWorld world)
        {
            if (world == null)
            {
                return "World not found.";
            }

            if (!TryInt(parts, 1, out int x) || !TryInt(parts, 2, out int y))
            {
                return "Usage: get_tile <x> <y>";
            }

            if (!world.TryGetExistingTile(x, y, out SandboxTile tile))
            {
                return $"Tile ({x}, {y}) is in an ungenerated chunk.";
            }

            return $"Tile ({x}, {y}): id={tile.id} solid={tile.IsSolid} light={tile.light} fluid={tile.fluid:0.###} metadata={tile.metadata}.";
        }

        private static string ExecuteGenerateChunk(string[] parts, SandboxWorld world)
        {
            if (!CanMutate(world, out string error))
            {
                return error;
            }

            if (!TryInt(parts, 1, out int x) || !TryInt(parts, 2, out int y))
            {
                return "Usage: generate_chunk <chunk-x> <chunk-y>";
            }

            world.GetTile(x * SandboxChunk.Size, y * SandboxChunk.Size);
            return $"Chunk ({x}, {y}) generated through SandboxWorld.GetTile.";
        }

        private static string ExecuteDumpChunk(string[] parts, SandboxWorld world)
        {
            if (world == null)
            {
                return "World not found.";
            }

            if (!TryInt(parts, 1, out int x) || !TryInt(parts, 2, out int y))
            {
                return "Usage: dump_chunk <chunk-x> <chunk-y>";
            }

            if (!world.TryGetChunkDebugState(new Vector2Int(x, y), out SandboxChunkDebugState state))
            {
                return $"Chunk ({x}, {y}) is not generated.";
            }

            return $"Chunk ({x}, {y}): loaded={state.RendererLoaded} renderDirty={state.NeedsRenderRebuild} colliderDirty={state.NeedsColliderRebuild} saveDirty={state.IsSaveDirty} hasEdits={state.HasEdits} navVersion={state.NavVersion}.";
        }

        private static string ExecuteSaveLoad(string command, string[] parts, SandboxWorld world)
        {
            if (!CanMutate(world, out string error))
            {
                return error;
            }

            if (parts.Length != 2 || !IsValidSlotName(parts[1]))
            {
                return $"Usage: {command} <slot>; use only letters, digits, '-' and '_'.";
            }

            string path = Path.Combine(Application.persistentDataPath, "debug-saves", parts[1] + ".json");
            if (command == "save")
            {
                world.SaveToPath(path);
                return $"Saved slot '{parts[1]}'.";
            }

            if (!File.Exists(path))
            {
                return $"Save slot '{parts[1]}' does not exist.";
            }

            world.LoadFromPath(path);
            return $"Loaded slot '{parts[1]}'.";
        }

        private static bool CanMutate(SandboxWorld world, out string error)
        {
            if (world == null)
            {
                error = "World not found.";
                return false;
            }

            if (!world.IsDebugOverrideModeEnabled)
            {
                error = "Debug override mode is disabled on SandboxWorld.";
                return false;
            }

            error = null;
            return true;
        }

        private static bool TryInt(string[] parts, int index, out int value)
        {
            value = 0;
            return index < parts.Length
                && int.TryParse(parts[index], NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
        }

        private static bool TryFloat(string[] parts, int index, out float value)
        {
            value = 0f;
            return index < parts.Length
                && float.TryParse(parts[index], NumberStyles.Float, CultureInfo.InvariantCulture, out value);
        }

        private static string BuildHelp()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("Commands: help; overlay <name> [on|off|toggle]; teleport <x> <y>; ");
            sb.Append("set_tile <x> <y> <id>; get_tile <x> <y>; generate_chunk/dump_chunk <cx> <cy>; ");
            sb.Append("save/load <slot>. Overlays: ");
            sb.Append(SandboxDebugOverlayState.JoinNames());
            sb.Append(". Hotkeys:");
            foreach (SandboxDebugOverlay overlay in SandboxDebugOverlayState.AllOverlays)
            {
                sb.Append(' ');
                sb.Append(SandboxDebugOverlayState.GetHotkeyLabel(overlay));
                sb.Append('=');
                sb.Append(SandboxDebugOverlayState.GetName(overlay));
            }

            return sb.ToString();
        }

        private static bool PressedBackquote()
        {
#if ENABLE_INPUT_SYSTEM
            UnityEngine.InputSystem.Keyboard keyboard = UnityEngine.InputSystem.Keyboard.current;
            if (keyboard != null && keyboard.backquoteKey.wasPressedThisFrame)
            {
                return true;
            }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(KeyCode.BackQuote);
#else
            return false;
#endif
        }
    }
}
#endif
