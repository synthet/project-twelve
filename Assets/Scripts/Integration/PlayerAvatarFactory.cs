using System;
using System.Collections.Generic;
using ProjectTwelve.Visual.Characters;
using ProjectTwelve.Visual.Creatures;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Creates sandbox player avatars using project-owned visual components.
/// </summary>
public static class PlayerAvatarFactory
{
    private static readonly string[] DemoScriptStripOrder =
    {
        "CharacterControls",
        "CharacterController2D",
        "CharacterAnimation",
        "CharacterBuilder",
        "Character",
    };

    /// <summary>
    /// Instantiates an avatar, strips demo gameplay scripts, and composes appearance.
    /// </summary>
    public static bool TryCreateRandomAvatar(
        Transform parent,
        GameObject prefabOverride,
        CharacterLayerCatalog layerCatalog,
        Vector3 localPosition,
        int bodySortingOrder,
        out Transform avatarRoot,
        out ISandboxPlayerLocomotion locomotion)
    {
        avatarRoot = null;
        locomotion = null;

        GameObject prefab = ResolvePrefab(prefabOverride);
        if (prefab == null)
        {
            Debug.LogWarning(
                "PlayerAvatarFactory: avatar prefab not found. Assign one in the inspector or " +
                "configure Assets/_Licensed submodule or config/visual-import.local-only.txt.");
            return false;
        }

        if (layerCatalog == null)
        {
            Debug.LogWarning("PlayerAvatarFactory: CharacterLayerCatalog is not assigned.");
            return false;
        }

        GameObject instance;
        try
        {
            instance = UnityEngine.Object.Instantiate(prefab, parent);
        }
        catch (InvalidCastException)
        {
            Debug.LogWarning(
                "PlayerAvatarFactory: avatar prefab reference does not resolve to a GameObject. " +
                "Re-assign the avatar prefab in the inspector (the scene reference may use a stale fileID).");
            return false;
        }

        instance.name = "PlayerAvatar";
        instance.SetActive(false);

        StripDemoScripts(instance);
        StripPhysicsComponents(instance);

        LayeredCharacterVisual visual = GetOrAddComponent<LayeredCharacterVisual>(instance);
        WireCreatureVisual(visual, instance);

        CharacterComposer composer = GetOrAddComponent<CharacterComposer>(instance);
        composer.layerCatalog = layerCatalog;
        composer.character = visual;
        composer.rebuildOnStart = false;
        composer.RandomizeRace();
        composer.RandomizeHumanAppearance();
        composer.RandomizeEquipment();
        composer.Rebuild();

        CharacterLocomotionDriver driver = GetOrAddComponent<CharacterLocomotionDriver>(instance);
        if (visual.Body != null)
        {
            visual.Body.sortingOrder = bodySortingOrder;
        }

        instance.transform.localPosition = localPosition;
        instance.transform.localRotation = Quaternion.identity;
        instance.transform.localScale = Vector3.one;

        avatarRoot = instance.transform;
        locomotion = driver;
        instance.SetActive(true);
        return true;
    }

    private static GameObject ResolvePrefab(GameObject prefabOverride)
    {
        if (prefabOverride != null)
        {
            return prefabOverride;
        }

        string configuredPath = LocalImportConfig.AvatarPrefabPath;
        if (string.IsNullOrEmpty(configuredPath))
        {
            return null;
        }

#if UNITY_EDITOR
        return AssetDatabase.LoadAssetAtPath<GameObject>(configuredPath);
#else
        return null;
#endif
    }

    private static void StripDemoScripts(GameObject root)
    {
        HashSet<string> configuredTypes = new HashSet<string>(StringComparer.Ordinal);
        IReadOnlyList<string> demoTypes = LocalImportConfig.DemoScriptTypeNames;
        for (int i = 0; i < demoTypes.Count; i++)
        {
            configuredTypes.Add(demoTypes[i]);
        }

        for (int i = 0; i < DemoScriptStripOrder.Length; i++)
        {
            StripScriptsByTypeName(root, DemoScriptStripOrder[i]);
        }

        foreach (string typeName in configuredTypes)
        {
            if (Array.IndexOf(DemoScriptStripOrder, typeName) >= 0)
            {
                continue;
            }

            StripScriptsByTypeName(root, typeName);
        }
    }

    private static void StripScriptsByTypeName(GameObject root, string typeName)
    {
        MonoBehaviour[] behaviours = root.GetComponentsInChildren<MonoBehaviour>(true);
        for (int i = 0; i < behaviours.Length; i++)
        {
            MonoBehaviour behaviour = behaviours[i];
            if (behaviour == null)
            {
                continue;
            }

            if (behaviour.GetType().Name == typeName)
            {
                DestroyComponentImmediate(behaviour);
            }
        }
    }

    private static void StripPhysicsComponents(GameObject root)
    {
        Rigidbody2D[] bodies = root.GetComponentsInChildren<Rigidbody2D>(true);
        for (int i = 0; i < bodies.Length; i++)
        {
            DestroyComponentImmediate(bodies[i]);
        }

        Collider2D[] colliders = root.GetComponentsInChildren<Collider2D>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            DestroyComponentImmediate(colliders[i]);
        }
    }

    private static void DestroyComponentImmediate(Component component)
    {
        if (component == null)
        {
            return;
        }

        UnityEngine.Object.DestroyImmediate(component);
    }

    private static void WireCreatureVisual(LayeredCharacterVisual visual, GameObject root)
    {
        Transform bodyTransform = root.transform.Find("Body");
        if (bodyTransform != null)
        {
            visual.Body = bodyTransform.GetComponent<SpriteRenderer>();
            visual.BodyLibrary = bodyTransform.GetComponent<UnityEngine.U2D.Animation.SpriteLibrary>();
        }

        visual.Animator = root.GetComponent<Animator>();
        if (root.TryGetComponent(out AudioSource audioSource))
        {
            visual.AudioSource = audioSource;
        }
    }

    private static T GetOrAddComponent<T>(GameObject go) where T : Component
    {
        if (!go.TryGetComponent(out T component))
        {
            component = go.AddComponent<T>();
        }

        return component;
    }
}
