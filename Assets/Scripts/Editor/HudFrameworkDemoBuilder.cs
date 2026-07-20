using ProjectTwelve.Sandbox.UI;
using ProjectTwelve.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

namespace ProjectTwelve.Editor
{
    /// <summary>
    /// Builds the HUD-framework demo prefab: an overlay canvas with the pixel-perfect UI scale controller,
    /// a <see cref="HudRoot"/>, and the <see cref="HudFrameworkDemo"/> driver (which constructs the window,
    /// grid, tooltip, scale stepper, and modal at runtime). Mirrors the conventions of
    /// <c>SandboxHudPrefabBuilder</c> (ScreenSpaceOverlay, ConstantPixelSize, 1280x720 / 100 PPU) so the
    /// framework demo scales identically to the shipping HUD. Nothing here touches the existing HUD.
    /// </summary>
    public static class HudFrameworkDemoBuilder
    {
        private const string PrefabPath = "Assets/Prefabs/UI/HudFrameworkDemo.prefab";

        [MenuItem("ProjectTwelve/UI/Rebuild HUD Framework Demo")]
        public static void Rebuild()
        {
            GameObject root = new GameObject("HudFrameworkDemo",
                typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            root.layer = UiFactory.UiGameObjectLayer;

            Canvas canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.pixelPerfect = true;
            canvas.sortingOrder = 50;

            CanvasScaler scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.referencePixelsPerUnit = 100f;

            root.AddComponent<UiScaleController>();
            root.AddComponent<HudRoot>();
            root.AddComponent<HudFrameworkDemo>();

            EnsureEventSystem();

            System.IO.Directory.CreateDirectory("Assets/Prefabs/UI");
            GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);

            AssetDatabase.SaveAssets();
            EditorGUIUtility.PingObject(saved);
            Debug.Log($"Built HUD framework demo prefab at {PrefabPath}");
        }

        private static void EnsureEventSystem()
        {
            if (Object.FindAnyObjectByType<EventSystem>() != null)
            {
                return;
            }

            GameObject eventSystem = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
            eventSystem.AddComponent<InputSystemUIInputModule>();
#else
            eventSystem.AddComponent<StandaloneInputModule>();
#endif
        }
    }
}
