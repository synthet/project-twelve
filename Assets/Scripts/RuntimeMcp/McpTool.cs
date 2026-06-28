using System;
using Newtonsoft.Json.Linq;

namespace ProjectTwelve.RuntimeMcp
{
    /// <summary>
    /// Describes a single MCP tool with JSON-schema input and a handler that returns structured output.
    /// </summary>
    public sealed class McpTool
    {
        /// <summary>Creates a tool descriptor.</summary>
        /// <param name="name">Unique tool name exposed via tools/list.</param>
        /// <param name="description">Human-readable description for MCP clients.</param>
        /// <param name="inputSchema">JSON Schema object describing accepted arguments.</param>
        /// <param name="handler">Handler invoked on the main thread; returns a JSON object payload.</param>
        public McpTool(string name, string description, JObject inputSchema, Func<JObject, JObject> handler)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Description = description ?? string.Empty;
            InputSchema = inputSchema ?? new JObject();
            Handler = handler ?? throw new ArgumentNullException(nameof(handler));
        }

        /// <summary>Tool name used in tools/call.</summary>
        public string Name { get; }

        /// <summary>Short description for MCP clients.</summary>
        public string Description { get; }

        /// <summary>JSON Schema for tool arguments.</summary>
        public JObject InputSchema { get; }

        /// <summary>Main-thread handler that returns a JSON result object.</summary>
        public Func<JObject, JObject> Handler { get; }

        /// <summary>Creates a tool with a minimal object input schema (for tests and simple tools).</summary>
        public static McpTool CreateSimple(string name, string description, Func<JObject, JObject> handler)
        {
            return new McpTool(name, description, new JObject { ["type"] = "object" }, handler);
        }

        /// <summary>Test helper that echoes a string <c>value</c> argument.</summary>
        public static McpTool CreateEchoToolForTests()
        {
            return CreateSimple(
                "echo",
                "Echoes a value.",
                args => new JObject { ["value"] = args["value"]?.ToString() ?? string.Empty });
        }
    }
}
