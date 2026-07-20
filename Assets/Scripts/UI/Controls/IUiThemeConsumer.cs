using ProjectTwelve.UI.Theme;

namespace ProjectTwelve.UI.Controls
{
    /// <summary>
    /// Anything the framework can bind to a theme and ask to re-skin. Implemented both by the
    /// <see cref="UiControl"/> base and by <see cref="Selectable"/>-derived controls (e.g. buttons) that
    /// cannot inherit <see cref="UiControl"/>. <see cref="HudRoot"/> binds all consumers in its subtree.
    /// </summary>
    public interface IUiThemeConsumer
    {
        /// <summary>Binds this consumer to a theme provider.</summary>
        void Bind(UiThemeProvider provider);

        /// <summary>Re-applies the current theme.</summary>
        void Refresh();
    }
}
