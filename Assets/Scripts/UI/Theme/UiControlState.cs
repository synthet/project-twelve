namespace ProjectTwelve.UI.Theme
{
    /// <summary>
    /// Interaction state of a control. Every primitive resolves its visuals (color role, sprite) from
    /// its current state through the active <see cref="UiTheme"/>, so states look consistent everywhere.
    /// </summary>
    public enum UiControlState
    {
        Normal = 0,
        Hovered = 1,
        Pressed = 2,
        Selected = 3,
        Disabled = 4,
        Focused = 5,
    }
}
