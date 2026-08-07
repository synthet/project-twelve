using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class SandboxUiPanel : MonoBehaviour
{
    private Image image;
    private Outline outline;

    public void Initialize(SandboxUiTheme theme)
    {
        image = GetComponent<Image>();
        ApplyTheme(theme);
    }

    public void ApplyTheme(SandboxUiTheme theme)
    {
        if (image == null)
        {
            image = GetComponent<Image>();
        }

        if (outline == null)
        {
            outline = GetComponent<Outline>();
            if (outline == null)
            {
                outline = gameObject.AddComponent<Outline>();
            }
        }

        image.sprite = theme.PanelSprite;
        image.type = theme.PanelSprite != null ? Image.Type.Sliced : Image.Type.Simple;
        image.color = theme.PanelSprite != null ? Color.white : theme.SurfaceColor;
        outline.enabled = theme.PanelSprite == null;
        outline.effectColor = theme.FrameColor;
        outline.effectDistance = new Vector2(2f, -2f);
    }
}

public sealed class SandboxUiLabel : MonoBehaviour
{
    private Text text;
    private bool muted;

    public Text Text => text != null ? text : text = GetComponent<Text>();

    public void Initialize(SandboxUiTheme theme, bool useMutedColor)
    {
        muted = useMutedColor;
        ApplyTheme(theme);
    }

    public void ApplyTheme(SandboxUiTheme theme)
    {
        Text.font = theme.Font;
        Text.color = muted ? theme.MutedTextColor : theme.TextColor;
    }
}

public sealed class SandboxUiButton : MonoBehaviour
{
    private Image image;
    private Text label;
    private Button button;

    public Button Button => button != null ? button : button = GetComponent<Button>();
    public Text Label => label;

    public void Initialize(SandboxUiTheme theme, Text text)
    {
        image = GetComponent<Image>();
        label = text;
        ApplyTheme(theme);
    }

    public void ApplyTheme(SandboxUiTheme theme)
    {
        if (image == null)
        {
            image = GetComponent<Image>();
        }

        image.color = theme.RaisedSurfaceColor;
        if (label != null)
        {
            label.font = theme.Font;
            label.color = theme.TextColor;
        }

        ColorBlock colors = Button.colors;
        colors.normalColor = theme.RaisedSurfaceColor;
        colors.highlightedColor = Color.Lerp(theme.RaisedSurfaceColor, theme.AccentColor, 0.35f);
        colors.pressedColor = Color.Lerp(theme.RaisedSurfaceColor, theme.FrameColor, 0.35f);
        colors.selectedColor = Color.Lerp(theme.RaisedSurfaceColor, theme.AccentColor, 0.5f);
        colors.disabledColor = new Color(
            theme.RaisedSurfaceColor.r,
            theme.RaisedSurfaceColor.g,
            theme.RaisedSurfaceColor.b,
            0.45f);
        colors.colorMultiplier = 1f;
        Button.colors = colors;
    }
}

public sealed class SandboxUiScrollView : MonoBehaviour
{
    private Image background;

    public ScrollRect ScrollRect { get; private set; }
    public RectTransform Content { get; private set; }

    public void Initialize(ScrollRect scrollRect, RectTransform content, SandboxUiTheme theme)
    {
        ScrollRect = scrollRect;
        Content = content;
        background = GetComponent<Image>();
        ApplyTheme(theme);
    }

    public void ApplyTheme(SandboxUiTheme theme)
    {
        if (background == null)
        {
            background = GetComponent<Image>();
        }

        background.color = theme.RaisedSurfaceColor;
    }
}

public sealed class SandboxUiItemSlot : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler,
    ISelectHandler,
    IDeselectHandler
{
    private int index;
    private Image background;
    private Image icon;
    private Text fallbackText;
    private Text countText;
    private Outline focusOutline;
    private SandboxUiTooltip tooltip;
    private SandboxInventorySlotViewData data;

    public int Index => index;
    public Button Button { get; private set; }

    public void Initialize(
        int slotIndex,
        SandboxUiTheme theme,
        Image iconImage,
        Text fallback,
        Text count,
        SandboxUiTooltip tooltipService)
    {
        index = slotIndex;
        background = GetComponent<Image>();
        Button = GetComponent<Button>();
        icon = iconImage;
        fallbackText = fallback;
        countText = count;
        tooltip = tooltipService;
        focusOutline = GetComponent<Outline>();
        if (focusOutline == null)
        {
            focusOutline = gameObject.AddComponent<Outline>();
        }

        focusOutline.effectDistance = new Vector2(2f, -2f);
        focusOutline.enabled = false;
        ApplyTheme(theme);
    }

    public void ApplyTheme(SandboxUiTheme theme)
    {
        background.sprite = theme.SlotSprite;
        background.type = theme.SlotSprite != null ? Image.Type.Sliced : Image.Type.Simple;
        background.color = theme.SlotSprite != null ? Color.white : theme.RaisedSurfaceColor;
        fallbackText.font = theme.Font;
        fallbackText.color = theme.TextColor;
        countText.font = theme.Font;
        countText.color = theme.TextColor;
        focusOutline.effectColor = theme.AccentColor;

        ColorBlock colors = Button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = Color.Lerp(Color.white, theme.AccentColor, 0.25f);
        colors.pressedColor = Color.Lerp(Color.white, theme.FrameColor, 0.25f);
        colors.selectedColor = Color.Lerp(Color.white, theme.AccentColor, 0.35f);
        Button.colors = colors;
    }

    public void Refresh(SandboxInventorySlotViewData slotData, Sprite itemIcon)
    {
        data = slotData;
        icon.sprite = itemIcon;
        icon.enabled = itemIcon != null && !slotData.IsEmpty;
        fallbackText.text = icon.enabled || slotData.IsEmpty ? string.Empty : Initials(slotData.ItemId);
        countText.text = slotData.IsEmpty ? string.Empty : slotData.Count.ToString();
        Button.interactable = !slotData.IsEmpty;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        RequestTooltip(immediate: false);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        tooltip?.Cancel(this);
    }

    public void OnSelect(BaseEventData eventData)
    {
        focusOutline.enabled = true;
        RequestTooltip(immediate: true);
    }

    public void OnDeselect(BaseEventData eventData)
    {
        focusOutline.enabled = false;
        tooltip?.Cancel(this);
    }

    private void RequestTooltip(bool immediate)
    {
        if (data.IsEmpty)
        {
            return;
        }

        tooltip?.Request(
            this,
            DisplayName(data.ItemId),
            $"Stack {data.Count} / {data.MaximumCount}",
            transform as RectTransform,
            immediate);
    }

    private static string Initials(string itemId)
    {
        string name = DisplayName(itemId);
        string[] words = name.Split(' ');
        if (words.Length == 1)
        {
            return words[0].Substring(0, Mathf.Min(2, words[0].Length)).ToUpperInvariant();
        }

        return string.Concat(words[0][0], words[1][0]).ToUpperInvariant();
    }

    internal static string DisplayName(string itemId)
    {
        if (string.IsNullOrEmpty(itemId))
        {
            return string.Empty;
        }

        int colon = itemId.IndexOf(':');
        string value = colon >= 0 ? itemId.Substring(colon + 1) : itemId;
        string[] words = value.Split('_');
        for (int i = 0; i < words.Length; i++)
        {
            if (words[i].Length > 0)
            {
                words[i] = char.ToUpperInvariant(words[i][0]) + words[i].Substring(1);
            }
        }

        return string.Join(" ", words);
    }
}

public readonly struct SandboxUiScrollViewParts
{
    public SandboxUiScrollViewParts(SandboxUiScrollView view, RectTransform viewport, RectTransform content)
    {
        View = view;
        Viewport = viewport;
        Content = content;
    }

    public SandboxUiScrollView View { get; }
    public RectTransform Viewport { get; }
    public RectTransform Content { get; }
}

public static class SandboxUiBuilder
{
    public static RectTransform CreateRect(string name, Transform parent)
    {
        GameObject gameObject = new GameObject(name, typeof(RectTransform));
        RectTransform rect = gameObject.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.localScale = Vector3.one;
        return rect;
    }

    public static SandboxUiPanel CreatePanel(string name, Transform parent, SandboxUiTheme theme)
    {
        RectTransform rect = CreateRect(name, parent);
        Image image = rect.gameObject.AddComponent<Image>();
        image.raycastTarget = true;
        rect.gameObject.AddComponent<Outline>();
        SandboxUiPanel panel = rect.gameObject.AddComponent<SandboxUiPanel>();
        panel.Initialize(theme);
        return panel;
    }

    public static SandboxUiLabel CreateLabel(
        string name,
        Transform parent,
        SandboxUiTheme theme,
        string value,
        int fontSize,
        TextAnchor alignment,
        bool muted = false)
    {
        RectTransform rect = CreateRect(name, parent);
        Text text = rect.gameObject.AddComponent<Text>();
        text.text = value;
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        SandboxUiLabel label = rect.gameObject.AddComponent<SandboxUiLabel>();
        label.Initialize(theme, muted);
        return label;
    }

    public static SandboxUiButton CreateButton(
        string name,
        Transform parent,
        SandboxUiTheme theme,
        string value,
        Action onClick)
    {
        RectTransform rect = CreateRect(name, parent);
        Image image = rect.gameObject.AddComponent<Image>();
        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        SandboxUiLabel label = CreateLabel("Label", rect, theme, value, 14, TextAnchor.MiddleCenter);
        SetStretch(label.transform as RectTransform, 4f, 4f, 2f, 2f);
        if (onClick != null)
        {
            button.onClick.AddListener(() => onClick());
        }

        SandboxUiButton themedButton = rect.gameObject.AddComponent<SandboxUiButton>();
        themedButton.Initialize(theme, label.Text);
        return themedButton;
    }

    public static SandboxUiScrollViewParts CreateScrollView(
        string name,
        Transform parent,
        SandboxUiTheme theme)
    {
        RectTransform root = CreateRect(name, parent);
        Image rootImage = root.gameObject.AddComponent<Image>();
        rootImage.color = theme.RaisedSurfaceColor;
        ScrollRect scrollRect = root.gameObject.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.scrollSensitivity = 28f;

        RectTransform viewport = CreateRect("Viewport", root);
        SetStretch(viewport, 4f, 4f, 4f, 4f);
        Image viewportImage = viewport.gameObject.AddComponent<Image>();
        viewportImage.color = Color.white;
        Mask mask = viewport.gameObject.AddComponent<Mask>();
        mask.showMaskGraphic = false;

        RectTransform content = CreateRect("Content", viewport);
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
        content.anchoredPosition = Vector2.zero;

        scrollRect.viewport = viewport;
        scrollRect.content = content;

        SandboxUiScrollView view = root.gameObject.AddComponent<SandboxUiScrollView>();
        view.Initialize(scrollRect, content, theme);
        return new SandboxUiScrollViewParts(view, viewport, content);
    }

    public static SandboxUiItemSlot CreateItemSlot(
        string name,
        Transform parent,
        SandboxUiTheme theme,
        int index,
        SandboxUiTooltip tooltip)
    {
        RectTransform rect = CreateRect(name, parent);
        Image image = rect.gameObject.AddComponent<Image>();
        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;

        RectTransform iconRect = CreateRect("Icon", rect);
        SetRect(iconRect, Vector2.zero, new Vector2(30f, 30f),
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        Image icon = iconRect.gameObject.AddComponent<Image>();
        icon.preserveAspect = true;
        icon.raycastTarget = false;

        SandboxUiLabel fallback = CreateLabel(
            "Fallback",
            rect,
            theme,
            string.Empty,
            12,
            TextAnchor.MiddleCenter);
        SetStretch(fallback.transform as RectTransform, 5f, 5f, 5f, 5f);

        SandboxUiLabel count = CreateLabel(
            "Count",
            rect,
            theme,
            string.Empty,
            12,
            TextAnchor.LowerRight);
        SetStretch(count.transform as RectTransform, 4f, 5f, 4f, 3f);

        SandboxUiItemSlot slot = rect.gameObject.AddComponent<SandboxUiItemSlot>();
        slot.Initialize(index, theme, icon, fallback.Text, count.Text, tooltip);
        return slot;
    }

    public static void SetRect(
        RectTransform rect,
        Vector2 position,
        Vector2 size,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 pivot)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    public static void SetStretch(RectTransform rect, float left, float right, float top, float bottom)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = new Vector2(left, bottom);
        rect.offsetMax = new Vector2(-right, -top);
    }
}
