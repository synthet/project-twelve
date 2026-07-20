namespace ProjectTwelve.UI.Theme
{
    /// <summary>
    /// Semantic sprite slots a theme can supply. Controls request a role; the theme maps it to a
    /// (licensed or placeholder) 9-slice sprite. A null mapping means "draw a flat tinted rect", so the
    /// framework still renders without any art — placeholder-friendly and public-repo safe.
    /// </summary>
    public enum UiSpriteRole
    {
        Panel = 0,
        FramedPanel = 1,
        Button = 2,
        Slot = 3,
        Selection = 4,
        TooltipPlate = 5,
        Separator = 6,
        ProgressTrack = 7,
        ProgressFill = 8,
    }
}
