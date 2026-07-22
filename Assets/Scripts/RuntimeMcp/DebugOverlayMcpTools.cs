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
                    ["required"] = new JArray("overlay"),
                    ["additionalProperties"] = false
                },
                args =>
                {
                    ValidateArguments(args, "overlay", "enabled");

                    JToken overlayToken = args["overlay"];
                    if (overlayToken == null || overlayToken.Type != JTokenType.String)
                    {
                        throw new InvalidOperationException("overlay must be a string");
                    }

                    string name = overlayToken.Value<string>();
                    if (!SandboxDebugOverlayState.TryParseName(name, out SandboxDebugOverlay overlay) ||
                        !string.Equals(
                            name,
                            SandboxDebugOverlayState.GetName(overlay),
                            StringComparison.Ordinal))
                    {
                        throw new InvalidOperationException($"Unknown overlay: {name}");
                    }

                    JToken enabledToken = args["enabled"];
                    if (enabledToken == null)
                    {
                        state.Toggle(overlay);
                    }
                    else
                    {
                        if (enabledToken.Type != JTokenType.Boolean)
                        {
                            throw new InvalidOperationException("enabled must be a boolean");
                        }

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
                    ["properties"] = new JObject(),
                    ["additionalProperties"] = false
                },
                args =>
                {
                    ValidateArguments(args);
                    return BuildState(state);
                }));
        }

        private static void ValidateArguments(JObject arguments, params string[] allowedNames)
        {
            foreach (JProperty property in arguments.Properties())
            {
                bool isAllowed = false;
                foreach (string allowedName in allowedNames)
                {
                    if (string.Equals(property.Name, allowedName, StringComparison.Ordinal))
                    {
                        isAllowed = true;
                        break;
                    }
                }

                if (!isAllowed)
                {
                    throw new InvalidOperationException($"Unexpected argument: {property.Name}");
                }
            }
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
