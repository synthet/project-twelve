#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Keeps open scenes at a single audio listener while editing and before play mode starts.
/// </summary>
[InitializeOnLoad]
internal static class SingleAudioListenerSceneGuard
{
    private static bool isEnforcing;

    static SingleAudioListenerSceneGuard()
    {
        EditorApplication.hierarchyChanged += OnHierarchyChanged;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        EditorSceneManager.sceneOpened += OnSceneOpened;
    }

    private static void OnHierarchyChanged()
    {
        EnforceInOpenScenes();
    }

    private static void OnSceneOpened(Scene scene, OpenSceneMode mode)
    {
        EnforceInOpenScenes();
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingEditMode)
        {
            EnforceInOpenScenes();
        }
    }

    private static void EnforceInOpenScenes()
    {
        if (isEnforcing || EditorApplication.isPlaying)
        {
            return;
        }

        isEnforcing = true;
        try
        {
            SingleAudioListenerEnforcer.Enforce();
        }
        finally
        {
            isEnforcing = false;
        }
    }
}
#endif
