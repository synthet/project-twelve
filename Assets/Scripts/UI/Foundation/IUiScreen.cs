namespace ProjectTwelve.UI
{
    /// <summary>
    /// A self-contained UI screen (window, dialog, or overlay) that the framework can show, hide,
    /// and reason about for input routing. Screens never read or mutate world data directly; they
    /// talk to domain systems through view-models and command handlers.
    /// </summary>
    public interface IUiScreen
    {
        /// <summary>
        /// When true, gameplay input must be suppressed while this screen is the active blocking
        /// screen (e.g. inventory, modal dialog). Non-blocking overlays (HUD, tooltips) return false.
        /// </summary>
        bool BlocksGameplayInput { get; }

        /// <summary>The UI layer this screen occupies. Determines draw/sort order and focus banding.</summary>
        UiLayer Layer { get; }

        /// <summary>Makes the screen visible and eligible for focus.</summary>
        void Show();

        /// <summary>Hides the screen and releases any focus it owns.</summary>
        void Hide();
    }
}
