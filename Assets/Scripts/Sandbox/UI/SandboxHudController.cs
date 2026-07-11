using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[DisallowMultipleComponent]
[RequireComponent(typeof(Canvas))]
public sealed class SandboxHudController : MonoBehaviour
{
    public const int HealthPerHeart = 10;

    private static readonly Color PanelTint = new Color(0.18f, 0.19f, 0.22f, 0.97f);
    private static readonly Color SlotTint = new Color(0.12f, 0.13f, 0.16f, 0.96f);
    private static readonly Color Gold = new Color(1f, 0.72f, 0.25f, 1f);
    private static readonly Color Silver = new Color(0.78f, 0.82f, 0.86f, 1f);
    private static readonly Color HeartEmpty = new Color(0.16f, 0.08f, 0.1f, 0.8f);

    [Header("Scene sources (auto-discovered when empty)")]
    [SerializeField] private SandboxWorld world;
    [SerializeField] private SandboxPlayerController playerController;
    [SerializeField] private SandboxPlayerVitals playerVitals;

    [Header("Licensed PixelFantasy theme")]
    [SerializeField] private Sprite panelSprite;
    [SerializeField] private Sprite frameSprite;
    [SerializeField] private Sprite selectionSprite;
    [SerializeField] private Font pixelFont;

    [Header("Core presentation")]
    [SerializeField] private Sprite portraitSprite;
    [SerializeField] private Sprite heartSprite;
    [SerializeField] private Sprite dirtIcon;
    [SerializeField] private Sprite grassIcon;
    [SerializeField] private Sprite stoneIcon;
    [SerializeField] private Sprite copperOreIcon;

    private readonly List<Image> heartFills = new List<Image>();
    private readonly List<GameObject> hotbarSelections = new List<GameObject>();
    private Text healthText;
    private Text selectedItemText;
    private Text worldInfoText;
    private SandboxCreativeHotbarState hotbar;
    private Vector2Int lastWorldTile = new Vector2Int(int.MinValue, int.MinValue);

    public SandboxCreativeHotbarState Hotbar => hotbar;
    public int SlotViewCount => hotbarSelections.Count;

    private void Awake()
    {
        ResolveSources();
        hotbar = new SandboxCreativeHotbarState();
        BuildView();
        hotbar.SelectionChanged += OnHotbarSelectionChanged;

        if (playerVitals != null)
        {
            playerVitals.Changed += OnVitalsChanged;
            RebuildHearts(playerVitals.MaxHealth);
            OnVitalsChanged(playerVitals.CurrentHealth, playerVitals.MaxHealth);
        }

        ApplySelection(0, hotbar.SelectedSlot);
        RefreshWorldInfo(force: true);
    }

    private void OnDestroy()
    {
        if (hotbar != null)
        {
            hotbar.SelectionChanged -= OnHotbarSelectionChanged;
        }

        if (playerVitals != null)
        {
            playerVitals.Changed -= OnVitalsChanged;
        }
    }

    private void Update()
    {
        HandleHotbarInput();
        RefreshWorldInfo(force: false);
    }

    private void ResolveSources()
    {
        if (world == null)
        {
            world = FindAnyObjectByType<SandboxWorld>();
        }

        if (playerController == null)
        {
            playerController = FindAnyObjectByType<SandboxPlayerController>();
        }

        if (playerVitals == null && playerController != null)
        {
            playerVitals = playerController.GetComponent<SandboxPlayerVitals>();
        }
    }

    private void BuildView()
    {
        RectTransform canvasRect = transform as RectTransform;
        if (canvasRect == null)
        {
            return;
        }

        BuildVitalsPanel(canvasRect);
        BuildHotbar(canvasRect);
        BuildWorldPanel(canvasRect);
    }

    private void BuildVitalsPanel(RectTransform parent)
    {
        RectTransform panel = CreatePanel("Vitals", parent, new Vector2(16f, -16f), new Vector2(390f, 104f),
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f));

        Image portraitFrame = CreateImage("PortraitFrame", panel, frameSprite, Silver);
        SetRect(portraitFrame.rectTransform, new Vector2(14f, -14f), new Vector2(76f, 76f),
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f));
        portraitFrame.type = Image.Type.Sliced;

        Image portrait = CreateImage("Portrait", portraitFrame.rectTransform, portraitSprite, Color.white);
        SetRect(portrait.rectTransform, Vector2.zero, new Vector2(48f, 48f),
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        portrait.preserveAspect = true;

        RectTransform hearts = CreateRect("Hearts", panel);
        SetRect(hearts, new Vector2(104f, -20f), new Vector2(260f, 46f),
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f));
        HorizontalLayoutGroup layout = hearts.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 4f;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        healthText = CreateText("HealthValue", panel, "100 / 100", 20, TextAnchor.MiddleLeft, Silver);
        SetRect(healthText.rectTransform, new Vector2(104f, -68f), new Vector2(250f, 24f),
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f));
    }

    private void BuildHotbar(RectTransform parent)
    {
        RectTransform panel = CreatePanel("CreativeHotbar", parent, new Vector2(0f, 18f), new Vector2(664f, 94f),
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f));

        selectedItemText = CreateText("SelectedItem", parent, "Dirt", 22, TextAnchor.MiddleCenter, Color.white);
        SetRect(selectedItemText.rectTransform, new Vector2(0f, 116f), new Vector2(320f, 32f),
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f));

        for (int i = 0; i < SandboxCreativeHotbarState.SlotCount; i++)
        {
            float x = 14f + i * 64f;
            Image slot = CreateImage($"Slot{i + 1}", panel, frameSprite, SlotTint);
            SetRect(slot.rectTransform, new Vector2(x, -15f), new Vector2(58f, 64f),
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f));
            slot.type = Image.Type.Sliced;

            Sprite iconSprite = GetIcon(i);
            Image icon = CreateImage("Icon", slot.rectTransform, iconSprite, Color.white);
            SetRect(icon.rectTransform, new Vector2(0f, -3f), new Vector2(32f, 32f),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            icon.preserveAspect = true;
            icon.enabled = iconSprite != null;

            string keyLabel = i == 9 ? "0" : (i + 1).ToString();
            Text key = CreateText("Key", slot.rectTransform, keyLabel, 14, TextAnchor.UpperLeft, Silver);
            SetStretch(key.rectTransform, 6f, 5f, 3f, 4f);

            Text infinity = CreateText("Quantity", slot.rectTransform,
                hotbar.Slots[i].IsPopulated ? "∞" : string.Empty, 17, TextAnchor.LowerRight, Color.white);
            SetStretch(infinity.rectTransform, 4f, 6f, 4f, 3f);

            Image selection = CreateImage("Selected", slot.rectTransform,
                selectionSprite != null ? selectionSprite : frameSprite, Gold);
            SetStretch(selection.rectTransform, -3f, -3f, -3f, -3f);
            selection.type = Image.Type.Sliced;
            selection.gameObject.SetActive(i == 0);
            hotbarSelections.Add(selection.gameObject);
        }
    }

    private void BuildWorldPanel(RectTransform parent)
    {
        RectTransform panel = CreatePanel("WorldInfo", parent, new Vector2(-16f, -16f), new Vector2(244f, 116f),
            new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f));
        worldInfoText = CreateText("WorldInfoText", panel, string.Empty, 18, TextAnchor.MiddleLeft, Silver);
        SetStretch(worldInfoText.rectTransform, 24f, 24f, 18f, 18f);
        worldInfoText.lineSpacing = 1.12f;
    }

    private void RebuildHearts(int maximum)
    {
        Transform heartsParent = transform.Find("Vitals/Hearts");
        if (heartsParent == null)
        {
            return;
        }

        for (int i = heartsParent.childCount - 1; i >= 0; i--)
        {
            Destroy(heartsParent.GetChild(i).gameObject);
        }

        heartFills.Clear();
        int count = Mathf.Max(1, Mathf.CeilToInt(maximum / (float)HealthPerHeart));
        for (int i = 0; i < count; i++)
        {
            RectTransform heart = CreateRect($"Heart{i + 1}", heartsParent);
            heart.sizeDelta = new Vector2(20f, 20f);

            Image empty = CreateImage("Empty", heart, heartSprite, HeartEmpty);
            SetStretch(empty.rectTransform, 0f, 0f, 0f, 0f);
            empty.preserveAspect = true;

            Image fill = CreateImage("Fill", heart, heartSprite, Color.white);
            SetStretch(fill.rectTransform, 0f, 0f, 0f, 0f);
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = (int)Image.OriginHorizontal.Left;
            fill.preserveAspect = true;
            heartFills.Add(fill);
        }
    }

    private void HandleHotbarInput()
    {
        for (int i = 0; i < SandboxCreativeHotbarState.SlotCount; i++)
        {
            if (WasSlotPressed(i))
            {
                hotbar.Select(i);
                return;
            }
        }

        float scroll = ReadScroll();
        if (!Mathf.Approximately(scroll, 0f))
        {
            hotbar.CyclePopulated(scroll > 0f ? -1 : 1);
        }
    }

    private static bool WasSlotPressed(int index)
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null)
        {
            KeyControl key = index == 9 ? keyboard.digit0Key : keyboard[(Key)((int)Key.Digit1 + index)];
            if (key != null && key.wasPressedThisFrame)
            {
                return true;
            }
        }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        KeyCode keyCode = index == 9 ? KeyCode.Alpha0 : (KeyCode)((int)KeyCode.Alpha1 + index);
        return Input.GetKeyDown(keyCode);
#else
        return false;
#endif
    }

    private static float ReadScroll()
    {
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null)
        {
            float value = Mouse.current.scroll.ReadValue().y;
            if (!Mathf.Approximately(value, 0f))
            {
                return value;
            }
        }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        return Input.mouseScrollDelta.y;
#else
        return 0f;
#endif
    }

    private void OnHotbarSelectionChanged(int index, SandboxCreativeHotbarState.Slot slot)
    {
        ApplySelection(index, slot);
    }

    private void ApplySelection(int index, SandboxCreativeHotbarState.Slot slot)
    {
        for (int i = 0; i < hotbarSelections.Count; i++)
        {
            hotbarSelections[i].SetActive(i == index);
        }

        if (selectedItemText != null)
        {
            selectedItemText.text = slot.IsPopulated ? slot.DisplayName : "Empty Slot";
            selectedItemText.color = slot.IsPopulated ? Color.white : Silver;
        }

        if (playerController != null)
        {
            if (slot.IsPopulated)
            {
                playerController.TrySetActivePlacementTile(slot.TileId);
            }
            else
            {
                playerController.ClearActivePlacementTile();
            }
        }
    }

    private void OnVitalsChanged(int current, int maximum)
    {
        int expectedCount = Mathf.Max(1, Mathf.CeilToInt(maximum / (float)HealthPerHeart));
        if (heartFills.Count != expectedCount)
        {
            RebuildHearts(maximum);
        }

        for (int i = 0; i < heartFills.Count; i++)
        {
            heartFills[i].fillAmount = GetHeartFill(current, maximum, i);
        }

        if (healthText != null)
        {
            healthText.text = $"{current} / {maximum}";
        }
    }

    private void RefreshWorldInfo(bool force)
    {
        if (world == null || worldInfoText == null || !world.TryGetPlayerWorldPosition(out Vector2 position))
        {
            return;
        }

        Vector2Int tile = world.WorldPositionToTile(position);
        if (!force && tile == lastWorldTile)
        {
            return;
        }

        lastWorldTile = tile;
        Vector2Int chunk = SandboxWorld.WorldToChunkCoord(tile.x, tile.y);
        worldInfoText.text = FormatWorldInfo(world.Seed, tile, chunk);
    }

    public static string FormatWorldInfo(int seed, Vector2Int tile, Vector2Int chunk)
    {
        return $"SEED  {seed}\nTILE  {tile.x}, {tile.y}\nCHUNK {chunk.x}, {chunk.y}";
    }

    public static float GetHeartFill(int current, int maximum, int heartIndex)
    {
        if (maximum <= 0 || heartIndex < 0)
        {
            return 0f;
        }

        int heartStart = heartIndex * HealthPerHeart;
        int capacity = Mathf.Clamp(maximum - heartStart, 0, HealthPerHeart);
        if (capacity == 0)
        {
            return 0f;
        }

        return Mathf.Clamp01((current - heartStart) / (float)capacity);
    }

    private Sprite GetIcon(int index)
    {
        return index switch
        {
            0 => dirtIcon,
            1 => grassIcon,
            2 => stoneIcon,
            3 => copperOreIcon,
            _ => null,
        };
    }

    private RectTransform CreatePanel(string name, RectTransform parent, Vector2 position, Vector2 size,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot)
    {
        Image image = CreateImage(name, parent, panelSprite, PanelTint);
        image.type = Image.Type.Sliced;
        SetRect(image.rectTransform, position, size, anchorMin, anchorMax, pivot);
        return image.rectTransform;
    }

    private Image CreateImage(string name, Transform parent, Sprite sprite, Color color)
    {
        RectTransform rect = CreateRect(name, parent);
        Image image = rect.gameObject.AddComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private Text CreateText(string name, Transform parent, string value, int fontSize, TextAnchor alignment, Color color)
    {
        RectTransform rect = CreateRect(name, parent);
        Text text = rect.gameObject.AddComponent<Text>();
        text.font = pixelFont != null ? pixelFont : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.text = value;
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = color;
        text.raycastTarget = false;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        Shadow shadow = rect.gameObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.8f);
        shadow.effectDistance = new Vector2(2f, -2f);
        return text;
    }

    private static RectTransform CreateRect(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.layer = 5;
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        return rect;
    }

    private static void SetRect(RectTransform rect, Vector2 position, Vector2 size,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    private static void SetStretch(RectTransform rect, float left, float right, float top, float bottom)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(left, bottom);
        rect.offsetMax = new Vector2(-right, -top);
    }
}
