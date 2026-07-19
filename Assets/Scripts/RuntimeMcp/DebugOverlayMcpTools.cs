#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using Newtonsoft.Json.Linq;
using ProjectTwelve.Sandbox.Debug;

namespace ProjectTwelve.RuntimeMcp
{
    /// <summary>Registers Runtime MCP tools that share overlay state with hotkeys and console UI.</summary>
    public static class DebugOverlayMcpTools
    {
        public static void Register(McpDispatcher dispatcher, SandboxDebugOverlayState state)
        {
            if (dispatcher == null)
            {
                throw new ArgumentNullException(nameof(dispatcher));
            }

            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            dispatcher.RegisterTool(new McpTool(
                "debug_overlay_set",
                "Toggle or explicitly enable/disable one in-game debug overlay.",
                new JObject
                {
                    ["type"] = "object",
                    ["properties"] = new JObject
                    {
                        ["overlay"] = new JObject
                        {
                            ["type"] = "string",
                            ["enum"] = BuildOverlayNames()
                        },
                        ["enabled"] = new JObject
                        {
                            ["type"] = "boolean",
                            ["description"] = "Desired state. Omit to toggle."
                        }
                    },
                    ["required"] = new JArray("overlay")
                },
                args =>
                {
                    string name = args["overlay"]?.Value<string>() ?? string.Empty;
                    if (!SandboxDebugOverlayState.TryParseName(name, out SandboxDebugOverlay overlay))
                    {
                        throw new InvalidOperationException($"Unknown overlay: {name}");
                    }

                    JToken enabledToken = args["enabled"];
                    if (enabledToken == null || enabledToken.Type == JTokenType.Null)
                    {
                        state.Toggle(overlay);
                    }
                    else
                    {
                        state.SetEnabled(overlay, enabledToken.Value<bool>());
                    }

                    return new JObject
                    {
                        ["overlay"] = SandboxDebugOverlayState.GetName(overlay),
                        ["enabled"] = state.IsEnabled(overlay),
                        ["anyEnabled"] = state.AnyEnabled
                    };
                }));

            dispatcher.RegisterTool(new McpTool(
                "debug_overlay_state",
                "Read enabled state for every in-game debug overlay.",
                new JObject
                {
                    ["type"] = "object",
                    ["properties"] = new JObject()
                },
                _ => BuildState(state)));
        }

        private static JArray BuildOverlayNames()
        {
            JArray names = new JArray();
            foreach (SandboxDebugOverlay overlay in SandboxDebugOverlayState.AllOverlays)
            {
                names.Add(SandboxDebugOverlayState.GetName(overlay));
            }

            return names;
        }

        private static JObject BuildState(SandboxDebugOverlayState state)
        {
            JObject result = new JObject { ["anyEnabled"] = state.AnyEnabled };
            foreach (SandboxDebugOverlay overlay in SandboxDebugOverlayState.AllOverlays)
            {
                result[SandboxDebugOverlayState.GetName(overlay)] = state.IsEnabled(overlay);
            }

            return result;
        }
    }
}
#endif
