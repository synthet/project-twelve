using UnityEngine;

[CreateAssetMenu(fileName = "SandboxUiTheme", menuName = "ProjectTwelve/UI/Theme")]
public sealed class SandboxUiTheme : ScriptableObject
{
    [Header("Layout tokens")]
    [SerializeField] private float spacingUnit = 4f;
    [SerializeField] private float panelPadding = 8f;
    [SerializeField] private float inventorySlotSize = 48f;
    [SerializeField] private float buttonHeight = 32f;
    [SerializeField] private float tooltipDelaySeconds = 0.35f;
    [SerializeField, Min(1)] private int pixelModule = 1;

    [Header("Colors")]
    [SerializeField] private Color surfaceColor = new Color32(243, 211, 154, 255);
    [SerializeField] private Color raisedSurfaceColor = new Color32(248, 228, 183, 255);
    [SerializeField] private Color frameColor = new Color32(110, 53, 29, 255);
    [SerializeField] private Color accentColor = new Color32(242, 184, 62, 255);
    [SerializeField] private Color textColor = new Color32(58, 33, 24, 255);
    [SerializeField] private Color mutedTextColor = new Color32(121, 83, 59, 255);

    [Header("Public-safe assets (optional)")]
    [SerializeField] private Sprite panelSprite;
    [SerializeField] private Sprite slotSprite;
    [SerializeField] private Sprite selectedSlotSprite;
    [SerializeField] private Font font;

    public float SpacingUnit => Mathf.Max(1f, spacingUnit);
    public float PanelPadding => Mathf.Max(0f, panelPadding);
    public float InventorySlotSize => Mathf.Max(16f, inventorySlotSize);
    public float ButtonHeight => Mathf.Max(20f, buttonHeight);
    public float TooltipDelaySeconds => Mathf.Max(0f, tooltipDelaySeconds);
    public int PixelModule => Mathf.Max(1, pixelModule);
    public Color SurfaceColor => surfaceColor;
    public Color RaisedSurfaceColor => raisedSurfaceColor;
    public Color FrameColor => frameColor;
    public Color AccentColor => accentColor;
    public Color TextColor => textColor;
    public Color MutedTextColor => mutedTextColor;
    public Sprite PanelSprite => panelSprite;
    public Sprite SlotSprite => slotSprite;
    public Sprite SelectedSlotSprite => selectedSlotSprite;
    public Font Font => font != null ? font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

    public static SandboxUiTheme CreateRuntimeFallback()
    {
        SandboxUiTheme theme = CreateInstance<SandboxUiTheme>();
        theme.name = "Runtime Hearthland Fallback";
        theme.hideFlags = HideFlags.DontSave;
        return theme;
    }

    public static SandboxUiTheme CreateRuntimeDarkFallback()
    {
        SandboxUiTheme theme = CreateInstance<SandboxUiTheme>();
        theme.name = "Runtime Forged Night Fallback";
        theme.surfaceColor = new Color32(34, 37, 44, 250);
        theme.raisedSurfaceColor = new Color32(48, 52, 62, 255);
        theme.frameColor = new Color32(162, 170, 181, 255);
        theme.accentColor = new Color32(242, 184, 62, 255);
        theme.textColor = new Color32(238, 232, 214, 255);
        theme.mutedTextColor = new Color32(184, 190, 199, 255);
        theme.hideFlags = HideFlags.DontSave;
        return theme;
    }
}
