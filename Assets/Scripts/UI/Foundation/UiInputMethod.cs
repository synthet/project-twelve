namespace ProjectTwelve.UI
{
    /// <summary>
    /// The input method most recently used to drive the UI. The framework shows the focus ring for
    /// keyboard/controller navigation but hides it under the mouse, so hover and directional focus do
    /// not fight each other. "Most recently used wins" is resolved by <see cref="UiFocusController"/>.
    /// </summary>
    public enum UiInputMethod
    {
        Mouse = 0,
        Keyboard = 1,
        Controller = 2,
    }
}
