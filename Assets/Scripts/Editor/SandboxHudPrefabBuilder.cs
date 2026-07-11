using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class SandboxHudPrefabBuilder
{
    private const string PrefabDirectory = "Assets/Prefabs/UI";
    private const string PrefabPath = PrefabDirectory + "/SandboxHUD.prefab";
    private const string ScenePath = "Assets/Scene.unity";

    [MenuItem("ProjectTwelve/UI/Rebuild Sandbox HUD")]
    public static void Rebuild()
    {
        EnsureFolder("Assets/Prefabs");
        EnsureFolder(PrefabDirectory);

        GameObject root = new GameObject(
            "SandboxHUD",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(SandboxHudController));

        try
        {
            root.layer = LayerMask.NameToLayer("UI");
            Canvas canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.pixelPerfect = true;
            canvas.sortingOrder = 100;

            CanvasScaler scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            scaler.referencePixelsPerUnit = 100f;

            AssignTheme(root.GetComponent<SandboxHudController>());
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        }
        finally
        {
            Object.DestroyImmediate(root);
        }

        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        foreach (GameObject sceneRoot in scene.GetRootGameObjects())
        {
            if (sceneRoot.name == "SandboxHUD")
            {
                Object.DestroyImmediate(sceneRoot);
                break;
            }
        }

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
        instance.transform.SetAsLastSibling();

        GameObject player = GameObject.Find("Player");
        if (player != null && player.GetComponent<SandboxPlayerVitals>() == null)
        {
            player.AddComponent<SandboxPlayerVitals>();
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log($"Rebuilt {PrefabPath} and wired it into {ScenePath}.");
    }

    private static void AssignTheme(SandboxHudController controller)
    {
        SerializedObject serialized = new SerializedObject(controller);
        
        const string baseDir = "Assets/Sprites/UI/Generated";

        SetAsset<Sprite>(serialized, "panelSprite", $"{baseDir}/hud_panel_main.png");
        SetAsset<Sprite>(serialized, "frameSprite", $"{baseDir}/hud_portrait_frame.png");
        SetAsset<Sprite>(serialized, "slotSprite", $"{baseDir}/hud_slot_normal.png");
        SetAsset<Sprite>(serialized, "selectionSprite", $"{baseDir}/hud_slot_selected.png");
        SetAsset<Sprite>(serialized, "heartSprite", $"{baseDir}/hud_heart_full.png");
        SetAsset<Sprite>(serialized, "emptyHeartSprite", $"{baseDir}/hud_heart_empty.png");
        SetAsset<Sprite>(serialized, "portraitSprite", "Assets/Sprites/Core/core_player_idle_00.png");
        
        SetAsset<Font>(serialized, "pixelFont", "Assets/_Licensed/PixelFantasy/Common/Fonts/Pribambas [by Misha Panfilov].ttf");
        SetAsset<Sprite>(serialized, "dirtIcon", "Assets/Sprites/Core/core_tile_dirt_00.png");
        SetAsset<Sprite>(serialized, "grassIcon", "Assets/Sprites/Core/core_tile_grass_00.png");
        SetAsset<Sprite>(serialized, "stoneIcon", "Assets/Sprites/Core/core_tile_stone_00.png");
        SetAsset<Sprite>(serialized, "copperOreIcon", "Assets/Sprites/Core/core_tile_ore_copper_00.png");
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetAsset<T>(SerializedObject serialized, string propertyName, string path)
        where T : Object
    {
        T asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset == null)
        {
            throw new System.InvalidOperationException($"HUD asset missing at '{path}'.");
        }

        serialized.FindProperty(propertyName).objectReferenceValue = asset;
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
        {
            return;
        }

        int separator = path.LastIndexOf('/');
        string parent = path.Substring(0, separator);
        string name = path.Substring(separator + 1);
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, name);
    }
}
