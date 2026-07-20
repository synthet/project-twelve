using System;
using UnityEngine;

namespace ProjectTwelve.UI.Theme
{
    /// <summary>
    /// Resolves the active <see cref="UiTheme"/> for the framework. When no theme is assigned it lazily
    /// creates a built-in default (all tokens/colors from <see cref="UiTheme"/> defaults) so controls
    /// always have a theme to read from. Swapping the theme raises <see cref="ThemeChanged"/> so every
    /// control can re-skin at runtime.
    /// </summary>
    public sealed class UiThemeProvider
    {
        private UiTheme assigned;
        private UiTheme fallback;

        /// <summary>Raised when the active theme changes; controls should re-apply on this event.</summary>
        public event Action<UiTheme> ThemeChanged;

        public UiThemeProvider(UiTheme theme = null)
        {
            assigned = theme;
        }

        /// <summary>The active theme: the assigned one, or a lazily-created default fallback.</summary>
        public UiTheme Active
        {
            get
            {
                if (assigned != null)
                {
                    return assigned;
                }

                if (fallback == null)
                {
                    fallback = ScriptableObject.CreateInstance<UiTheme>();
                    fallback.name = "DefaultUiTheme (fallback)";
                }

                return fallback;
            }
        }

        /// <summary>True when a theme asset has been explicitly assigned (not the built-in fallback).</summary>
        public bool HasAssignedTheme => assigned != null;

        /// <summary>Swaps the active theme at runtime and notifies listeners.</summary>
        public void SetTheme(UiTheme theme)
        {
            if (ReferenceEquals(assigned, theme))
            {
                return;
            }

            assigned = theme;
            ThemeChanged?.Invoke(Active);
        }
    }
}
