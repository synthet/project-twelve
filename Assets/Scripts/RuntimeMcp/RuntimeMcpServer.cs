using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using UnityEngine;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
using ProjectTwelve.Sandbox.Debug;
#endif
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ProjectTwelve.RuntimeMcp
{
    /// <summary>
    /// Hosts an MCP JSON-RPC endpoint over HTTP while the game is running.
    /// Network I/O runs on a background thread; Unity-touching tool calls execute on the main thread.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(10000)]
    public sealed class RuntimeMcpServer : MonoBehaviour
    {
        private const float MainThreadRequestTimeoutSeconds = 30f;
        private const float FpsSmoothing = 0.1f;

        private readonly ConcurrentQueue<PendingRequest> pendingRequests = new ConcurrentQueue<PendingRequest>();
        private readonly McpDispatcher dispatcher = new McpDispatcher();

        private HttpListener listener;
        private Thread listenerThread;
        private volatile bool listenerRunning;
        private float smoothedDeltaTime = 1f / 60f;

        /// <summary>Shared dispatcher for tests and gameplay tool registration.</summary>
        public McpDispatcher Dispatcher => dispatcher;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        /// <summary>Overlay state shared by MCP, hotkeys, console, and the runtime renderer.</summary>
        public SandboxDebugOverlayState OverlayState => SandboxDebugRuntime.OverlayState;
#endif

        /// <summary>Smoothed frame time in seconds for the perf tool.</summary>
        public float SmoothedDeltaTime => smoothedDeltaTime;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (!RuntimeMcpConfig.IsEnabled)
            {
                return;
            }

            if (FindAnyObjectByType<RuntimeMcpServer>() != null)
            {
                return;
            }

            GameObject host = new GameObject("RuntimeMcpServer");
            host.AddComponent<RuntimeMcpServer>();
            DontDestroyOnLoad(host);
        }

        private void Awake()
        {
            Application.runInBackground = true;
            GameplayMcpTools.Register(dispatcher, this);
            VisualOverrideMcpTools.Register(dispatcher);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            DebugOverlayMcpTools.Register(dispatcher, OverlayState);
#endif
            StartListener();
#if UNITY_EDITOR
            EditorApplication.update += PumpPendingRequests;
#endif
        }

        private void Update()
        {
            smoothedDeltaTime = Mathf.Lerp(smoothedDeltaTime, Time.unscaledDeltaTime, FpsSmoothing);
            PumpPendingRequests();
        }

        private void LateUpdate()
        {
            PumpPendingRequests();
        }

        private void OnDisable()
        {
            StopListener();
        }

        private void OnDestroy()
        {
#if UNITY_EDITOR
            EditorApplication.update -= PumpPendingRequests;
#endif
            StopListener();
        }

        private void PumpPendingRequests()
        {
            while (pendingRequests.TryDequeue(out PendingRequest pending))
            {
                if (pending.TimedOut)
                {
                    pending.Signal.Dispose();
                    continue;
                }

                try
                {
                    McpHandleResult result = dispatcher.HandleMessage(pending.Body);
                    pending.ResponseJson = result.ResponseJson;
                    pending.IsNotification = result.IsNotification;
                }
                catch (Exception ex)
                {
                    pending.ResponseJson = null;
                    pending.ErrorMessage = ex.Message;
                }
                finally
                {
                    if (!pending.TimedOut)
                    {
                        pending.Signal.Set();
                    }
                }
            }
        }

        private void StartListener()
        {
            if (listenerRunning)
            {
                return;
            }

            try
            {
                listener = new HttpListener();
                listener.Prefixes.Add($"http://127.0.0.1:{RuntimeMcpConfig.Port}/");
                listener.Start();
                listenerRunning = true;
                listenerThread = new Thread(ListenLoop)
                {
                    IsBackground = true,
                    Name = "RuntimeMcpServer"
                };
                listenerThread.Start();
                Debug.Log($"Runtime MCP listening on {RuntimeMcpConfig.EndpointUrl}");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Runtime MCP failed to start on {RuntimeMcpConfig.EndpointUrl}: {ex.Message}");
                StopListener();
            }
        }

        private void StopListener()
        {
            listenerRunning = false;

            if (listener != null)
            {
                try
                {
                    listener.Stop();
                    listener.Close();
                }
                catch (Exception)
                {
                    // Listener may already be closed during shutdown.
                }

                listener = null;
            }

            if (listenerThread != null && listenerThread.IsAlive)
            {
                listenerThread.Join(1000);
                listenerThread = null;
            }

            while (pendingRequests.TryDequeue(out PendingRequest pending))
            {
                pending.TimedOut = true;
                pending.ErrorMessage = "Server stopped";
                pending.Signal.Set();
            }
        }

        private void ListenLoop()
        {
            while (listenerRunning && listener != null && listener.IsListening)
            {
                HttpListenerContext context = null;
                try
                {
                    context = listener.GetContext();
                    HandleContext(context);
                }
                catch (HttpListenerException)
                {
                    if (!listenerRunning)
                    {
                        break;
                    }
                }
                catch (Exception ex)
                {
                    if (context?.Response != null)
                    {
                        WriteTextResponse(context.Response, 500, ex.Message);
                    }
                }
            }
        }

        private void HandleContext(HttpListenerContext context)
        {
            HttpListenerRequest request = context.Request;
            HttpListenerResponse response = context.Response;
            string path = request.Url.AbsolutePath ?? string.Empty;

            if (string.Equals(request.HttpMethod, "GET", StringComparison.OrdinalIgnoreCase)
                && string.Equals(path, "/mcp", StringComparison.OrdinalIgnoreCase))
            {
                WriteJsonResponse(response, 200, "{\"status\":\"ready\"}");
                return;
            }

            if (!string.Equals(request.HttpMethod, "POST", StringComparison.OrdinalIgnoreCase)
                || !string.Equals(path, "/mcp", StringComparison.OrdinalIgnoreCase))
            {
                WriteTextResponse(response, 404, "Not Found");
                return;
            }

            string body;
            using (StreamReader reader = new StreamReader(request.InputStream, request.ContentEncoding ?? Encoding.UTF8))
            {
                body = reader.ReadToEnd();
            }

            if (McpDispatcher.TryParseRequestMethod(body, out bool isNotification, out string method)
                && !McpDispatcher.RequiresMainThread(method))
            {
                McpHandleResult result = dispatcher.HandleMessage(body);
                if (result.IsNotification)
                {
                    WriteEmptyResponse(response, 202);
                }
                else
                {
                    WriteJsonResponse(response, 200, result.ResponseJson ?? "{}");
                }

                return;
            }

            PendingRequest pending = new PendingRequest
            {
                Body = body,
                Signal = new ManualResetEventSlim(false)
            };

            pendingRequests.Enqueue(pending);

            if (!pending.Signal.Wait(TimeSpan.FromSeconds(MainThreadRequestTimeoutSeconds)))
            {
                pending.TimedOut = true;
                WriteTextResponse(response, 504, "Gateway Timeout");
                return;
            }

            if (pending.IsNotification)
            {
                WriteEmptyResponse(response, 202);
            }
            else if (!string.IsNullOrEmpty(pending.ErrorMessage))
            {
                WriteTextResponse(response, 500, pending.ErrorMessage);
            }
            else
            {
                WriteJsonResponse(response, 200, pending.ResponseJson ?? "{}");
            }

            pending.Signal.Dispose();
        }

        private static void WriteJsonResponse(HttpListenerResponse response, int statusCode, string json)
        {
            byte[] buffer = Encoding.UTF8.GetBytes(json);
            response.StatusCode = statusCode;
            response.ContentType = "application/json";
            response.ContentEncoding = Encoding.UTF8;
            response.ContentLength64 = buffer.Length;
            response.OutputStream.Write(buffer, 0, buffer.Length);
            response.OutputStream.Close();
        }

        private static void WriteTextResponse(HttpListenerResponse response, int statusCode, string message)
        {
            byte[] buffer = Encoding.UTF8.GetBytes(message ?? string.Empty);
            response.StatusCode = statusCode;
            response.ContentType = "text/plain";
            response.ContentEncoding = Encoding.UTF8;
            response.ContentLength64 = buffer.Length;
            response.OutputStream.Write(buffer, 0, buffer.Length);
            response.OutputStream.Close();
        }

        private static void WriteEmptyResponse(HttpListenerResponse response, int statusCode)
        {
            response.StatusCode = statusCode;
            response.ContentLength64 = 0;
            response.OutputStream.Close();
        }

        private sealed class PendingRequest
        {
            public string Body;
            public ManualResetEventSlim Signal;
            public string ResponseJson;
            public bool IsNotification;
            public string ErrorMessage;
            public volatile bool TimedOut;
        }
    }
}
