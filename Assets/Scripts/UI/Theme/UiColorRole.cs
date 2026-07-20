namespace ProjectTwelve.UI.Theme
{
    /// <summary>
    /// Semantic color roles. Controls reference a role, never a literal color, so a single theme swap
    /// re-skins the whole UI. State-driven roles (Hovered/Pressed/...) mirror <see cref="UiControlState"/>.
    /// </summary>
    public enum UiColorRole
    {
        // Surfaces / text.
        PanelBackground = 0,
        PanelBorder = 1,
        Separator = 2,
        TextPrimary = 3,
        TextMuted = 4,

        // Interaction states.
        Normal = 5,
        Hovered = 6,
        Pressed = 7,
        Selected = 8,
        Disabled = 9,
        Focused = 10,

        // Semantic accents.
        Warning = 11,
        Destructive = 12,
        Accent = 13,
    }
}
