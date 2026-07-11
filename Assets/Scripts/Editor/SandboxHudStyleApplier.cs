using UnityEditor;
using UnityEngine;
using System.IO;

public static class SandboxHudStyleApplier
{
    private const string HUD_PREFAB_PATH = "Assets/Prefabs/UI/SandboxHUD.prefab";
    private const string EXTRACT_DIR = "D:/Projects/project-twelve-extract";
    private const string TARGET_DIR = "Assets/_Licensed/PixelFantasy/Common/Sprites/UI/ExtractedHUD";



    [MenuItem("ProjectTwelve/HUD/Apply Starbound Style")]
    public static void ApplyStarboundStyle()
    {
        ApplyStyle("Starbound", new StyleConfig
        {
            PanelBackground = "Starbound/Unpacked/interface/chat/portraitbg.png",
            PanelBorder = "Starbound/Unpacked/interface/cockpit/coordinatesframe.png",
            HotbarSlot = "Starbound/Unpacked/interface/actionbar/selectedslot-essential.png",
            SelectedSlot = "Starbound/Unpacked/interface/actionbar/selectedslot-custom.png",
            HeartSprite = "Starbound/Unpacked/interface/inventory/heart.png",
            PortraitSprite = "Starbound/Unpacked/interface/inventory/portrait.png",
            
            PanelBgBorder = new Vector4(6, 6, 6, 6),
            PanelBorderBorder = new Vector4(12, 12, 12, 12),
            SlotBorder = new Vector4(6, 6, 6, 6),
            SelectionBorder = new Vector4(8, 8, 8, 8),
            PortraitBorder = new Vector4(6, 6, 6, 6)
        });
    }

    private class StyleConfig
    {
        public string PanelBackground;
        public string PanelBorder;
        public string HotbarSlot;
        public string SelectedSlot;
        public string HeartSprite;
        public string PortraitSprite;

        public Vector4 PanelBgBorder;
        public Vector4 PanelBorderBorder;
        public Vector4 SlotBorder;
        public Vector4 SelectionBorder;
        public Vector4 PortraitBorder;
    }

    private static void ApplyStyle(string styleName, StyleConfig config)
    {
        if (!Directory.Exists(EXTRACT_DIR))
        {
            Debug.LogError($"Graphics extract directory not found at: {EXTRACT_DIR}");
            return;
        }

        string styleTargetDir = Path.Combine(TARGET_DIR, styleName).Replace('\\', '/');
        if (!Directory.Exists(styleTargetDir))
        {
            Directory.CreateDirectory(styleTargetDir);
        }

        // Copy files
        string panelBgPath = CopyFile(config.PanelBackground, styleTargetDir, "panel_bg.png");
        string panelBorderPath = CopyFile(config.PanelBorder, styleTargetDir, "panel_border.png");
        string slotPath = CopyFile(config.HotbarSlot, styleTargetDir, "slot.png");
        string selectedSlotPath = CopyFile(config.SelectedSlot, styleTargetDir, "slot_selected.png");
        string heartPath = CopyFile(config.HeartSprite, styleTargetDir, "heart.png");
        string portraitPath = CopyFile(config.PortraitSprite, styleTargetDir, "portrait.png");

        AssetDatabase.Refresh();

        // Configure Import Settings
        ConfigureSprite(panelBgPath, config.PanelBgBorder);
        ConfigureSprite(panelBorderPath, config.PanelBorderBorder);
        ConfigureSprite(slotPath, config.SlotBorder);
        ConfigureSprite(selectedSlotPath, config.SelectionBorder);
        ConfigureSprite(heartPath, Vector4.zero); // Heart is single sprite, no border
        ConfigureSprite(portraitPath, config.PortraitBorder);

        AssetDatabase.Refresh();

        // Load Sprites
        Sprite panelBgSprite = AssetDatabase.LoadAssetAtPath<Sprite>(panelBgPath);
        Sprite panelBorderSprite = AssetDatabase.LoadAssetAtPath<Sprite>(panelBorderPath);
        Sprite slotSprite = AssetDatabase.LoadAssetAtPath<Sprite>(slotPath);
        Sprite selectedSlotSprite = AssetDatabase.LoadAssetAtPath<Sprite>(selectedSlotPath);
        Sprite heartSprite = AssetDatabase.LoadAssetAtPath<Sprite>(heartPath);
        Sprite portraitSprite = AssetDatabase.LoadAssetAtPath<Sprite>(portraitPath);

        // Update Prefab
        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(HUD_PREFAB_PATH);
        if (prefabRoot == null)
        {
            Debug.LogError($"HUD prefab not found at: {HUD_PREFAB_PATH}");
            return;
        }

        SandboxHudController hudController = prefabRoot.GetComponent<SandboxHudController>();
        if (hudController == null)
        {
            Debug.LogError("SandboxHudController component not found on prefab root.");
            PrefabUtility.UnloadPrefabContents(prefabRoot);
            return;
        }

        // Use SerializedObject to modify the private fields safely
        SerializedObject so = new SerializedObject(hudController);
        SetSerializedProperty(so, "panelSprite", panelBgSprite);
        SetSerializedProperty(so, "frameSprite", panelBorderSprite);
        SetSerializedProperty(so, "slotSprite", slotSprite);
        SetSerializedProperty(so, "selectionSprite", selectedSlotSprite);
        SetSerializedProperty(so, "heartSprite", heartSprite);
        SetSerializedProperty(so, "portraitSprite", portraitSprite);
        so.ApplyModifiedProperties();

        PrefabUtility.SaveAsPrefabAsset(prefabRoot, HUD_PREFAB_PATH);
        PrefabUtility.UnloadPrefabContents(prefabRoot);

        Debug.Log($"Successfully applied {styleName} style to HUD prefab!");
    }

    private static string CopyFile(string relSourcePath, string targetDir, string targetFileName)
    {
        string fullSourcePath = Path.Combine(EXTRACT_DIR, relSourcePath.Replace('\\', '/'));
        string fullTargetPath = Path.Combine(targetDir, targetFileName).Replace('\\', '/');

        if (File.Exists(fullSourcePath))
        {
            File.Copy(fullSourcePath, fullTargetPath, true);
            Debug.Log($"Copied {fullSourcePath} -> {fullTargetPath}");
        }
        else
        {
            Debug.LogWarning($"Source file not found: {fullSourcePath}");
        }

        return fullTargetPath;
    }

    private static void ConfigureSprite(string assetPath, Vector4 border)
    {
        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null) return;

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        
        TextureImporterSettings settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteMeshType = SpriteMeshType.FullRect;
        importer.SetTextureSettings(settings);

        if (border != Vector4.zero)
        {
            importer.spriteBorder = border;
        }

        EditorUtility.SetDirty(importer);
        importer.SaveAndReimport();
    }

    private static void SetSerializedProperty(SerializedObject so, string name, Object value)
    {
        SerializedProperty prop = so.FindProperty(name);
        if (prop != null)
        {
            prop.objectReferenceValue = value;
        }
        else
        {
            Debug.LogWarning($"Field '{name}' not found on SandboxHudController. If you just added it, wait for Unity compilation to finish.");
        }
    }
}
