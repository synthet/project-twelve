using System;

namespace ProjectTwelve.RuntimeMcp
{
    /// <summary>
    /// Runtime configuration for the in-game MCP HTTP server.
    /// </summary>
    public static class RuntimeMcpConfig
    {
        private const string EnabledEnvVar = "PROJECTTWELVE_MCP_ENABLED";
        private const string PortEnvVar = "PROJECTTWELVE_MCP_PORT";

        /// <summary>Default loopback port when no environment override is set.</summary>
        public const int DefaultPort = 8765;

        /// <summary>
        /// Whether the runtime MCP server should start. Defaults to enabled unless the env var is "0" or "false".
        /// </summary>
        public static bool IsEnabled
        {
            get
            {
                string value = Environment.GetEnvironmentVariable(EnabledEnvVar);
                if (string.IsNullOrWhiteSpace(value))
                {
                    return true;
                }

                return !string.Equals(value, "0", StringComparison.OrdinalIgnoreCase)
                    && !string.Equals(value, "false", StringComparison.OrdinalIgnoreCase);
            }
        }

        /// <summary>
        /// TCP port bound on 127.0.0.1. Override with <see cref="PortEnvVar"/>.
        /// </summary>
        public static int Port
        {
            get
            {
                string value = Environment.GetEnvironmentVariable(PortEnvVar);
                if (int.TryParse(value, out int port) && port > 0 && port <= 65535)
                {
                    return port;
                }

                return DefaultPort;
            }
        }

        /// <summary>Loopback MCP endpoint URL for documentation and logging.</summary>
        public static string EndpointUrl => $"http://127.0.0.1:{Port}/mcp";
    }
}
