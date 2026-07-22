using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

public interface ISandboxUiScreen
{
    GameObject View { get; }
    Selectable InitialFocus { get; }
    bool BlocksGameplayInput { get; }
    bool IsModal { get; }
}

[DisallowMultipleComponent]
[RequireComponent(typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster))]
public sealed class SandboxUiRoot : MonoBehaviour
{
    private SandboxUiTheme hearthlandTheme;
    private SandboxUiTheme darkTheme;
    private SandboxUiTheme activeTheme;
    private SandboxUiScreenStack screens;
    private SandboxUiDemoWindow demoWindow;

    public event Action ThemeChanged;

    public RectTransform SafeAreaLayer { get; private set; }
    public RectTransform PersistentHudLayer { get; private set; }
    public RectTransform WindowLayer { get; private set; }
    public RectTransform PopupLayer { get; private set; }
    public RectTransform DragLayer { get; private set; }
    public RectTransform TooltipLayer { get; private set; }
    public RectTransform ModalLayer { get; private set; }
    public RectTransform DebugLayer { get; private set; }
    public SandboxUiTheme Theme => activeTheme;
    public SandboxUiScreenStack Screens => screens;
    public SandboxUiDemoWindow DemoWindow => demoWindow;
    public string ThemeName => ReferenceEquals(activeTheme, darkTheme) ? "Forged Night" : "Hearthland";

    private void Awake()
    {
        hearthlandTheme = SandboxUiTheme.CreateRuntimeFallback();
        darkTheme = SandboxUiTheme.CreateRuntimeDarkFallback();
        activeTheme = hearthlandTheme;
        screens = new SandboxUiScreenStack();
        screens.Changed += SyncGameplayInputGate;
        EnsureLayers();
        EnsureEventSystem();
        ApplySafeArea();
    }

    private void Start()
    {
        demoWindow = GetComponent<SandboxUiDemoWindow>();
        if (demoWindow == null)
        {
            demoWindow = gameObject.AddComponent<SandboxUiDemoWindow>();
        }

        demoWindow.Initialize(this);
    }

    private void Update()
    {
        ApplySafeAreaIfChanged();

        if (WasInventoryPressed() && (screens.TopOwner == null || ReferenceEquals(screens.TopOwner, demoWindow)))
        {
            demoWindow.Toggle();
        }

        if (WasCancelPressed() && screens.TopOwner is ISandboxUiScreen topScreen)
        {
            HideScreen(topScreen);
        }

        KeepModalFocusTrapped();
    }

    private void OnDestroy()
    {
        if (screens != null)
        {
            screens.Changed -= SyncGameplayInputGate;
        }

        SandboxUiInputGate.SetGameplayInputBlocked(false);
        DestroyRuntimeTheme(hearthlandTheme);
        DestroyRuntimeTheme(darkTheme);
    }

    private void OnDisable()
    {
        SandboxUiInputGate.SetGameplayInputBlocked(false);
    }

    private void OnEnable()
    {
        if (screens != null)
        {
            SyncGameplayInputGate();
        }
    }

    public void EnsureLayers()
    {
        SafeAreaLayer = EnsureLayer("SafeArea", transform);
        PersistentHudLayer = EnsureLayer("PersistentHudLayer", SafeAreaLayer);
        WindowLayer = EnsureLayer("WindowLayer", SafeAreaLayer);
        PopupLayer = EnsureLayer("PopupLayer", transform);
        DragLayer = EnsureLayer("DragLayer", transform);
        TooltipLayer = EnsureLayer("TooltipLayer", transform);
        ModalLayer = EnsureLayer("ModalLayer", transform);
        DebugLayer = EnsureLayer("DebugLayer", transform);

        SafeAreaLayer.SetAsFirstSibling();
        PersistentHudLayer.SetAsFirstSibling();
        WindowLayer.SetAsLastSibling();
        PopupLayer.SetAsLastSibling();
        DragLayer.SetAsLastSibling();
        TooltipLayer.SetAsLastSibling();
        ModalLayer.SetAsLastSibling();
        DebugLayer.SetAsLastSibling();
    }

    public void ShowScreen(ISandboxUiScreen screen)
    {
        if (screen?.View == null)
        {
            return;
        }

        screen.View.SetActive(true);
        screens.Push(screen, screen.BlocksGameplayInput, screen.IsModal);
        Select(screen.InitialFocus);
    }

    public void HideScreen(ISandboxUiScreen screen)
    {
        if (screen?.View == null)
        {
            return;
        }

        screen.View.SetActive(false);
        screens.Pop(screen);
        if (screens.TopOwner is ISandboxUiScreen topScreen)
        {
            Select(topScreen.InitialFocus);
        }
        else if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }
    }

    public void ToggleTheme()
    {
        activeTheme = ReferenceEquals(activeTheme, hearthlandTheme) ? darkTheme : hearthlandTheme;
        ThemeChanged?.Invoke();
    }

    private void SyncGameplayInputGate()
    {
        SandboxUiInputGate.SetGameplayInputBlocked(screens.GameplayInputBlocked);
    }

    private void KeepModalFocusTrapped()
    {
        if (!(screens.TopOwner is ISandboxUiScreen topScreen) || !topScreen.IsModal ||
            topScreen.View == null || EventSystem.current == null)
        {
            return;
        }

        GameObject selected = EventSystem.current.currentSelectedGameObject;
        if (selected == null || !selected.transform.IsChildOf(topScreen.View.transform))
        {
            Select(topScreen.InitialFocus);
        }
    }

    private static void Select(Selectable selectable)
    {
        if (selectable != null && selectable.IsActive() && selectable.IsInteractable() && EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(selectable.gameObject);
        }
    }

    private void EnsureEventSystem()
    {
        EventSystem eventSystem = FindAnyObjectByType<EventSystem>();
        if (eventSystem != null)
        {
            return;
        }

        GameObject eventSystemObject = new GameObject(
            "SandboxUIEventSystem",
            typeof(EventSystem),
            typeof(InputSystemUIInputModule));
        eventSystemObject.transform.SetParent(transform, false);
        eventSystemObject.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
    }

    private static RectTransform EnsureLayer(string name, Transform parent)
    {
        Transform existing = parent.Find(name);
        RectTransform rect;
        if (existing is RectTransform existingRect)
        {
            rect = existingRect;
        }
        else
        {
            GameObject layer = new GameObject(name, typeof(RectTransform));
            rect = layer.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
        }

        SandboxUiBuilder.SetStretch(rect, 0f, 0f, 0f, 0f);
        return rect;
    }

    private Rect lastSafeArea;

    private void ApplySafeAreaIfChanged()
    {
        if (Screen.safeArea != lastSafeArea)
        {
            ApplySafeArea();
        }
    }

    private void ApplySafeArea()
    {
        lastSafeArea = Screen.safeArea;
        if (SafeAreaLayer == null || Screen.width <= 0 || Screen.height <= 0)
        {
            return;
        }

        Rect safe = Screen.safeArea;
        SafeAreaLayer.anchorMin = new Vector2(safe.xMin / Screen.width, safe.yMin / Screen.height);
        SafeAreaLayer.anchorMax = new Vector2(safe.xMax / Screen.width, safe.yMax / Screen.height);
        SafeAreaLayer.offsetMin = Vector2.zero;
        SafeAreaLayer.offsetMax = Vector2.zero;
    }

    private static bool WasInventoryPressed()
    {
        if (Keyboard.current != null && Keyboard.current.iKey.wasPressedThisFrame)
        {
            return true;
        }

#if ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKeyDown(KeyCode.I);
#else
        return false;
#endif
    }

    private static bool WasCancelPressed()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            return true;
        }

#if ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKeyDown(KeyCode.Escape);
#else
        return false;
#endif
    }

    private static void DestroyRuntimeTheme(SandboxUiTheme theme)
    {
        if (theme == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(theme);
        }
        else
        {
            DestroyImmediate(theme);
        }
    }
}
