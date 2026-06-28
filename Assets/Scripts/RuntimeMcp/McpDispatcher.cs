using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace ProjectTwelve.RuntimeMcp
{
    /// <summary>
    /// Result of handling an MCP JSON-RPC message over HTTP.
    /// </summary>
    public readonly struct McpHandleResult
    {
        /// <summary>When true, the HTTP layer should respond with 202 and no JSON body.</summary>
        public bool IsNotification { get; }

        /// <summary>JSON-RPC response body when <see cref="IsNotification"/> is false.</summary>
        public string ResponseJson { get; }

        /// <summary>Creates an MCP HTTP routing result.</summary>
        /// <param name="isNotification">When true, respond with HTTP 202 and no body.</param>
        /// <param name="responseJson">JSON-RPC response when <paramref name="isNotification"/> is false.</param>
        public McpHandleResult(bool isNotification, string responseJson)
        {
            IsNotification = isNotification;
            ResponseJson = responseJson;
        }
    }

    /// <summary>
    /// Pure JSON-RPC router for MCP tools/list and tools/call. Network-free for unit tests.
    /// </summary>
    public sealed class McpDispatcher
    {
        private const string ProtocolVersion = "2024-11-05";
        private const string ServerName = "project-twelve-ingame-mcp";
        private const string ServerVersion = "1.0.0";

        private readonly Dictionary<string, McpTool> tools = new Dictionary<string, McpTool>(StringComparer.Ordinal);

        /// <summary>Registers a tool for tools/list and tools/call routing.</summary>
        public void RegisterTool(McpTool tool)
        {
            if (tool == null)
            {
                throw new ArgumentNullException(nameof(tool));
            }

            tools[tool.Name] = tool;
        }

        /// <summary>
        /// Returns true when the JSON-RPC method must run on the Unity main thread.
        /// </summary>
        public static bool RequiresMainThread(string method)
        {
            return string.Equals(method, "tools/call", StringComparison.Ordinal);
        }

        /// <summary>
        /// Parses the JSON-RPC method from a POST body (single object or batch array).
        /// </summary>
        public static bool TryParseRequestMethod(string jsonBody, out bool isNotification, out string method)
        {
            isNotification = false;
            method = string.Empty;

            if (string.IsNullOrWhiteSpace(jsonBody))
            {
                return false;
            }

            try
            {
                JToken token = JToken.Parse(jsonBody);
                JObject request;
                if (token.Type == JTokenType.Array)
                {
                    JArray batch = (JArray)token;
                    request = batch.Count > 0 ? batch[0] as JObject : null;
                }
                else
                {
                    request = token as JObject;
                }

                if (request == null)
                {
                    return false;
                }

                JToken idToken = request["id"];
                isNotification = idToken == null || idToken.Type == JTokenType.Null;
                method = request["method"]?.ToString() ?? string.Empty;
                return !string.IsNullOrEmpty(method);
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Handles a single JSON-RPC request or notification string.
        /// </summary>
        /// <param name="jsonBody">Raw POST body from the MCP HTTP transport.</param>
        /// <returns>Routing result for the HTTP layer.</returns>
        public McpHandleResult HandleMessage(string jsonBody)
        {
            JObject request;
            try
            {
                request = JObject.Parse(jsonBody);
            }
            catch (Exception ex)
            {
                return new McpHandleResult(
                    false,
                    BuildErrorResponse(null, -32700, "Parse error", ex.Message));
            }

            if (!string.Equals(request["jsonrpc"]?.ToString(), "2.0", StringComparison.Ordinal))
            {
                return new McpHandleResult(
                    false,
                    BuildErrorResponse(request["id"], -32600, "Invalid Request", "jsonrpc must be \"2.0\""));
            }

            JToken idToken = request["id"];
            bool isNotification = idToken == null || idToken.Type == JTokenType.Null;
            string method = request["method"]?.ToString() ?? string.Empty;

            if (isNotification)
            {
                if (method == "notifications/initialized")
                {
                    return new McpHandleResult(true, null);
                }

                return new McpHandleResult(true, null);
            }

            string responseJson = method switch
            {
                "initialize" => BuildInitializeResponse(idToken),
                "ping" => BuildSuccessResponse(idToken, new JObject()),
                "tools/list" => BuildToolsListResponse(idToken),
                "tools/call" => BuildToolsCallResponse(idToken, request["params"] as JObject),
                _ => BuildErrorResponse(idToken, -32601, "Method not found", $"Unknown method: {method}")
            };

            return new McpHandleResult(false, responseJson);
        }

        private string BuildInitializeResponse(JToken id)
        {
            JObject result = new JObject
            {
                ["protocolVersion"] = ProtocolVersion,
                ["capabilities"] = new JObject
                {
                    ["tools"] = new JObject()
                },
                ["serverInfo"] = new JObject
                {
                    ["name"] = ServerName,
                    ["version"] = ServerVersion
                }
            };

            return BuildSuccessResponse(id, result);
        }

        private string BuildToolsListResponse(JToken id)
        {
            JArray toolArray = new JArray();
            foreach (McpTool tool in tools.Values)
            {
                toolArray.Add(new JObject
                {
                    ["name"] = tool.Name,
                    ["description"] = tool.Description,
                    ["inputSchema"] = tool.InputSchema.DeepClone()
                });
            }

            return BuildSuccessResponse(id, new JObject { ["tools"] = toolArray });
        }

        private string BuildToolsCallResponse(JToken id, JObject parameters)
        {
            if (parameters == null)
            {
                return BuildErrorResponse(id, -32602, "Invalid params", "params object is required");
            }

            string toolName = parameters["name"]?.ToString() ?? string.Empty;
            if (string.IsNullOrEmpty(toolName))
            {
                return BuildErrorResponse(id, -32602, "Invalid params", "name is required");
            }

            if (!tools.TryGetValue(toolName, out McpTool tool))
            {
                return BuildToolErrorResponse(id, $"Unknown tool: {toolName}");
            }

            JObject arguments = parameters["arguments"] as JObject ?? new JObject();
            try
            {
                JObject payload = tool.Handler(arguments);
                return BuildToolSuccessResponse(id, payload);
            }
            catch (Exception ex)
            {
                return BuildToolErrorResponse(id, ex.Message);
            }
        }

        private static string BuildToolSuccessResponse(JToken id, JObject payload)
        {
            JObject result = new JObject
            {
                ["content"] = new JArray
                {
                    new JObject
                    {
                        ["type"] = "text",
                        ["text"] = payload.ToString(Newtonsoft.Json.Formatting.None)
                    }
                },
                ["isError"] = false
            };

            return BuildSuccessResponse(id, result);
        }

        private static string BuildToolErrorResponse(JToken id, string message)
        {
            JObject result = new JObject
            {
                ["content"] = new JArray
                {
                    new JObject
                    {
                        ["type"] = "text",
                        ["text"] = message
                    }
                },
                ["isError"] = true
            };

            return BuildSuccessResponse(id, result);
        }

        private static string BuildSuccessResponse(JToken id, JObject result)
        {
            JObject response = new JObject
            {
                ["jsonrpc"] = "2.0",
                ["id"] = id?.DeepClone(),
                ["result"] = result
            };

            return response.ToString(Newtonsoft.Json.Formatting.None);
        }

        private static string BuildErrorResponse(JToken id, int code, string message, string data)
        {
            JObject error = new JObject
            {
                ["code"] = code,
                ["message"] = message
            };

            if (!string.IsNullOrEmpty(data))
            {
                error["data"] = data;
            }

            JObject response = new JObject
            {
                ["jsonrpc"] = "2.0",
                ["error"] = error
            };

            if (id != null && id.Type != JTokenType.Null)
            {
                response["id"] = id.DeepClone();
            }

            return response.ToString(Newtonsoft.Json.Formatting.None);
        }
    }
}
