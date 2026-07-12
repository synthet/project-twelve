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
    public const float SelectedItemLabelDurationSeconds = 1.75f;

    private static readonly Color PanelTint = new Color(0.18f, 0.19f, 0.22f, 0.97f);
    private static readonly Color HotbarTint = new Color(0.035f, 0.04f, 0.055f, 0.82f);
    private static readonly Color DebugPanelTint = new Color(0.03f, 0.04f, 0.06f, 0.7f);
    private static readonly Color ItemLabelTint = new Color(0.025f, 0.03f, 0.04f, 0.88f);
    private static readonly Color SelectionGold = new Color(1f, 0.76f, 0.24f, 1f);
    private static readonly Color Silver = new Color(0.78f, 0.82f, 0.86f, 1f);
    private static readonly Color HeartEmpty = new Color(0.16f, 0.08f, 0.1f, 0.8f);

    [Header("Scene sources (auto-discovered when empty)")]
    [SerializeField] private SandboxWorld world;
    [SerializeField] private SandboxPlayerController playerController;
    [SerializeField] private SandboxPlayerVitals playerVitals;

    [Header("Debug telemetry")]
    [SerializeField] private bool showDebugTelemetry = true;
    [SerializeField] private bool hideDebugTelemetryInReleaseBuilds = true;

    [Header("HUD theme")]
    [SerializeField] private Sprite panelSprite;
    [SerializeField] private Sprite frameSprite;
    [SerializeField] private Sprite selectionSprite;
    [SerializeField] private Sprite slotSprite;
    [SerializeField] private Font pixelFont;

    [Header("Core presentation")]
    [SerializeField] private Sprite portraitSprite;
    [SerializeField] private Sprite heartSprite;
    [SerializeField] private Sprite emptyHeartSprite;
    [SerializeField] private Sprite dirtIcon;
    [SerializeField] private Sprite grassIcon;
    [SerializeField] private Sprite stoneIcon;
    [SerializeField] private Sprite copperOreIcon;

    private readonly List<Image> heartFills = new List<Image>();
    private readonly List<GameObject> hotbarSelections = new List<GameObject>();
    private readonly List<RectTransform> hotbarSlots = new List<RectTransform>();
    private readonly List<Vector2> hotbarSlotPositions = new List<Vector2>();
    private Image selectedItemLabel;
    private Text selectedItemText;
    private Text worldInfoText;
    private GameObject worldInfoPanel;
    private SandboxCreativeHotbarState hotbar;
    private float selectedItemLabelHideAt;
    private Vector2Int lastWorldTile = new Vector2Int(int.MinValue, int.MinValue);

    public SandboxCreativeHotbarState Hotbar => hotbar;
    public int SlotViewCount => hotbarSelections.Count;
    public bool DebugTelemetryVisible => worldInfoPanel != null && worldInfoPanel.activeSelf;

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
        if (hotbar == null)
        {
            return;
        }

        HandleHotbarInput();
        UpdateSelectedItemLabelVisibility();
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
        RectTransform panel = CreatePanel("Vitals", parent, new Vector2(16f, -16f), new Vector2(210f, 70f),
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f));

        Image portraitFrame = CreateImage("PortraitFrame", panel, frameSprite, Silver);
        SetRect(portraitFrame.rectTransform, new Vector2(7f, -7f), new Vector2(56f, 56f),
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f));
        portraitFrame.type = Image.Type.Sliced;

        Image portrait = CreateImage("Portrait", portraitFrame.rectTransform, portraitSprite, Color.white);
        SetRect(portrait.rectTransform, Vector2.zero, new Vector2(38f, 38f),
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        portrait.preserveAspect = true;

        RectTransform hearts = CreateRect("Hearts", panel);
        SetRect(hearts, new Vector2(70f, -10f), new Vector2(130f, 50f),
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f));
        HorizontalLayoutGroup layout = hearts.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 1f;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

    }

    private void BuildHotbar(RectTransform parent)
    {
        Image panelImage = CreateImage("CreativeHotbar", parent, null, HotbarTint);
        SetRect(panelImage.rectTransform, new Vector2(0f, 14f), new Vector2(612f, 60f),
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f));
        RectTransform panel = panelImage.rectTransform;

        for (int i = 0; i < SandboxCreativeHotbarState.SlotCount; i++)
        {
            float x = 19f + i * 58f;
            Image slot = CreateImage($"Slot{i + 1}", panel,
                slotSprite != null ? slotSprite : panelSprite, Color.white);
            SetRect(slot.rectTransform, new Vector2(x, -4f), new Vector2(52f, 52f),
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f));
            slot.type = Image.Type.Sliced;
            hotbarSlots.Add(slot.rectTransform);
            hotbarSlotPositions.Add(slot.rectTransform.anchoredPosition);

            Sprite iconSprite = GetIcon(i);
            Image icon = CreateImage("Icon", slot.rectTransform, iconSprite, Color.white);
            SetRect(icon.rectTransform, new Vector2(0f, -1f), new Vector2(32f, 32f),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            icon.preserveAspect = true;
            icon.enabled = iconSprite != null;

            string keyLabel = i == 9 ? "0" : (i + 1).ToString();
            Text key = CreateText("Key", slot.rectTransform, keyLabel, 13, TextAnchor.UpperLeft, Silver);
            SetStretch(key.rectTransform, 10f, 7f, 7f, 6f);

            Text infinity = CreateText("Quantity", slot.rectTransform,
                hotbar.Slots[i].IsPopulated ? "∞" : string.Empty, 15, TextAnchor.LowerRight, Color.white);
            SetStretch(infinity.rectTransform, 7f, 10f, 6f, 7f);

            Image selection = CreateImage("Selected", slot.rectTransform,
                selectionSprite != null ? selectionSprite : frameSprite, Color.white);
            SetStretch(selection.rectTransform, -1f, -1f, -1f, -1f);
            selection.type = Image.Type.Sliced;
            Image bottomMarker = CreateImage("MarkerBottom", selection.rectTransform, null, SelectionGold);
            SetRect(bottomMarker.rectTransform, new Vector2(0f, 6f), new Vector2(28f, 2f),
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f));
            Image topMarker = CreateImage("MarkerTop", selection.rectTransform, null, SelectionGold);
            SetRect(topMarker.rectTransform, new Vector2(0f, -6f), new Vector2(28f, 2f),
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));
            Image leftMarker = CreateImage("MarkerLeft", selection.rectTransform, null, SelectionGold);
            SetRect(leftMarker.rectTransform, new Vector2(6f, 0f), new Vector2(2f, 28f),
                new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f));
            Image rightMarker = CreateImage("MarkerRight", selection.rectTransform, null, SelectionGold);
            SetRect(rightMarker.rectTransform, new Vector2(-6f, 0f), new Vector2(2f, 28f),
                new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f));
            selection.gameObject.SetActive(i == 0);
            hotbarSelections.Add(selection.gameObject);
        }

        selectedItemLabel = CreateImage("SelectedItem", hotbarSlots[0], null, ItemLabelTint);
        selectedItemText = CreateText("Text", selectedItemLabel.rectTransform, "Dirt", 15,
            TextAnchor.MiddleCenter, Color.white);
        SetStretch(selectedItemText.rectTransform, 5f, 5f, 2f, 2f);
        PositionSelectedItemLabel(hotbarSlots[0]);
    }

    private void BuildWorldPanel(RectTransform parent)
    {
        Image panel = CreateImage("WorldInfo", parent, null, DebugPanelTint);
        SetRect(panel.rectTransform, new Vector2(-16f, -16f), new Vector2(160f, 62f),
            new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f));
        worldInfoPanel = panel.gameObject;
        worldInfoText = CreateText("WorldInfoText", panel.rectTransform, string.Empty, 14,
            TextAnchor.MiddleLeft, Silver);
        SetStretch(worldInfoText.rectTransform, 8f, 8f, 6f, 6f);
        worldInfoText.lineSpacing = 1.05f;
        worldInfoPanel.SetActive(ShouldShowDebugTelemetry());
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
            heart.sizeDelta = new Vector2(12f, 12f);

            Image empty = CreateImage("Empty", heart,
                emptyHeartSprite != null ? emptyHeartSprite : heartSprite,
                emptyHeartSprite != null ? Color.white : HeartEmpty);
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
            var key = index == 9 ? keyboard.digit0Key : keyboard[(Key)((int)Key.Digit1 + index)];
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
            bool selected = i == index;
            hotbarSelections[i].SetActive(selected);
            hotbarSlots[i].anchoredPosition = hotbarSlotPositions[i] + (selected ? Vector2.up * 2f : Vector2.zero);
        }

        if (selectedItemText != null)
        {
            PositionSelectedItemLabel(hotbarSlots[index]);
            selectedItemText.text = slot.IsPopulated ? slot.DisplayName : "Empty Slot";
            selectedItemText.color = slot.IsPopulated ? Color.white : Silver;
            selectedItemLabel.gameObject.SetActive(true);
            selectedItemLabelHideAt = Time.unscaledTime + SelectedItemLabelDurationSeconds;
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

    }

    private void RefreshWorldInfo(bool force)
    {
        if (!DebugTelemetryVisible || world == null || worldInfoText == null ||
            !world.TryGetPlayerWorldPosition(out Vector2 position))
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

    public void SetDebugTelemetryVisible(bool visible)
    {
        showDebugTelemetry = visible;
        if (worldInfoPanel != null)
        {
            worldInfoPanel.SetActive(ShouldShowDebugTelemetry());
            if (worldInfoPanel.activeSelf)
            {
                RefreshWorldInfo(force: true);
            }
        }
    }

    public static string FormatWorldInfo(int seed, Vector2Int tile, Vector2Int chunk)
    {
        return $"DEBUG  SEED {seed}\nTILE  {tile.x}, {tile.y}\nCHUNK {chunk.x}, {chunk.y}";
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
        return CreatePanel(name, parent, position, size, panelSprite, anchorMin, anchorMax, pivot);
    }

    private void PositionSelectedItemLabel(RectTransform slot)
    {
        selectedItemLabel.rectTransform.SetParent(slot, false);
        SetRect(selectedItemLabel.rectTransform, new Vector2(0f, 8f), new Vector2(96f, 20f),
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 0f));
    }

    private void UpdateSelectedItemLabelVisibility()
    {
        if (selectedItemLabel != null && selectedItemLabel.gameObject.activeSelf &&
            IsSelectedItemLabelExpired(Time.unscaledTime, selectedItemLabelHideAt))
        {
            selectedItemLabel.gameObject.SetActive(false);
        }
    }

    internal static bool IsSelectedItemLabelExpired(float currentTime, float hideAt)
    {
        return currentTime >= hideAt;
    }

    private bool ShouldShowDebugTelemetry()
    {
        if (!showDebugTelemetry)
        {
            return false;
        }

        return !hideDebugTelemetryInReleaseBuilds || Application.isEditor || Debug.isDebugBuild;
    }

    private RectTransform CreatePanel(string name, RectTransform parent, Vector2 position, Vector2 size,
        Sprite sprite, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot)
    {
        Image image = CreateImage(name, parent, sprite, sprite != null ? Color.white : PanelTint);
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
