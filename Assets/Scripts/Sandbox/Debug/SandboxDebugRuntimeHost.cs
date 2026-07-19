#if UNITY_EDITOR || DEVELOPMENT_BUILD
using UnityEngine;

namespace ProjectTwelve.Sandbox.Debug
{
    /// <summary>Creates development-only overlay and console surfaces without scene wiring.</summary>
    [DefaultExecutionOrder(9000)]
    public sealed class SandboxDebugRuntimeHost : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (FindAnyObjectByType<SandboxDebugRuntimeHost>() != null)
            {
                return;
            }

            GameObject host = new GameObject("SandboxDebugTools");
            DontDestroyOnLoad(host);
            host.AddComponent<SandboxDebugRuntimeHost>();
        }

        private void Awake()
        {
            SandboxDebugOverlayRenderer renderer = gameObject.AddComponent<SandboxDebugOverlayRenderer>();
            renderer.Initialize(SandboxDebugRuntime.OverlayState);

            SandboxDebugConsole console = gameObject.AddComponent<SandboxDebugConsole>();
            console.Initialize(SandboxDebugRuntime.OverlayState);
        }
    }
}
#endif
