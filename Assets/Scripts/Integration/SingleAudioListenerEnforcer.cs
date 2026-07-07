using UnityEngine;

/// <summary>
/// Ensures the loaded scene has exactly one <see cref="AudioListener"/>.
/// Prefers the listener on the main camera when duplicates appear.
/// </summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(10000)]
public sealed class SingleAudioListenerEnforcer : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AfterSceneLoad()
    {
        Enforce();
    }

    private void Awake()
    {
        Enforce();
    }

    private void Start()
    {
        Enforce();
    }

    /// <summary>
    /// Removes extra audio listeners, keeping a single preferred listener when possible.
    /// </summary>
    public static void Enforce()
    {
        AudioListener[] listeners = Object.FindObjectsByType<AudioListener>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        if (listeners.Length <= 1)
        {
            return;
        }

        AudioListener keeper = SelectKeeper(listeners);
        for (int i = 0; i < listeners.Length; i++)
        {
            AudioListener listener = listeners[i];
            if (listener == null || listener == keeper)
            {
                continue;
            }

            RemoveListener(listener);
        }
    }

    private static AudioListener SelectKeeper(AudioListener[] listeners)
    {
        for (int i = 0; i < listeners.Length; i++)
        {
            AudioListener listener = listeners[i];
            if (listener != null && listener.CompareTag("MainCamera"))
            {
                return listener;
            }
        }

        Camera mainCamera = Camera.main;
        if (mainCamera != null && mainCamera.TryGetComponent(out AudioListener mainListener))
        {
            return mainListener;
        }

        for (int i = 0; i < listeners.Length; i++)
        {
            AudioListener listener = listeners[i];
            if (listener != null && listener.isActiveAndEnabled)
            {
                return listener;
            }
        }

        return listeners[0];
    }

    private static void RemoveListener(AudioListener listener)
    {
        if (listener == null)
        {
            return;
        }

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            Object.DestroyImmediate(listener);
            return;
        }
#endif
        Object.Destroy(listener);
    }
}
