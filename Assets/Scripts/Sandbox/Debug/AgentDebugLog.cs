using System;
using System.IO;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace ProjectTwelve.Sandbox.Debug
{
    /// <summary>Session-scoped NDJSON debug logging for agent investigations.</summary>
    internal static class AgentDebugLog
    {
        private const string SessionId = "572dce";
        private const string LogFileName = "debug-572dce.log";

        internal static void Write(string hypothesisId, string location, string message, JObject data = null, string runId = "pre-fix")
        {
            try
            {
                string projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
                if (string.IsNullOrEmpty(projectRoot))
                {
                    return;
                }

                JObject payload = new JObject
                {
                    ["sessionId"] = SessionId,
                    ["runId"] = runId,
                    ["hypothesisId"] = hypothesisId,
                    ["location"] = location,
                    ["message"] = message,
                    ["timestamp"] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                };

                if (data != null)
                {
                    payload["data"] = data;
                }

                string path = Path.Combine(projectRoot, LogFileName);
                File.AppendAllText(path, payload.ToString(Newtonsoft.Json.Formatting.None) + Environment.NewLine);
            }
            catch
            {
                // Debug logging must never break gameplay.
            }
        }
    }
}
