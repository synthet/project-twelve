using UnityEngine;
using ProjectTwelve.UI.Theme;

namespace ProjectTwelve.UI.Controls
{
    /// <summary>
    /// Base class for every framework control. Owns the shared theme binding and the current interaction
    /// state, and re-skins on theme change — event-driven, never per-frame. Subclasses implement
    /// <see cref="ApplyTheme"/> to paint themselves from tokens/roles. Controls without an explicit binding
    /// fall back to a shared default provider, so a primitive still renders when dropped in standalone.
    /// </summary>
    [DisallowMultipleComponent]
    public abstract class UiControl : MonoBehaviour, IUiThemeConsumer
    {
        private static UiThemeProvider sharedDefault;

        private UiThemeProvider themeProvider;
        private UiControlState state = UiControlState.Normal;
        private bool interactable = true;

        /// <summary>The theme provider this control reads from (shared default until explicitly bound).</summary>
        protected UiThemeProvider ThemeProvider => themeProvider ?? (themeProvider = SharedDefault());

        /// <summary>Convenience accessor for the active theme.</summary>
        protected UiTheme Theme => ThemeProvider.Active;

        /// <summary>Current interaction state driving this control's visuals.</summary>
        public UiControlState State => state;

        /// <summary>Whether this control can be interacted with / focused.</summary>
        public bool Interactable
        {
            get => interactable;
            set
            {
                if (interactable == value)
                {
                    return;
                }

                interactable = value;
                SetState(interactable ? UiControlState.Normal : UiControlState.Disabled);
            }
        }

        /// <summary>Binds this control (and, by convention, its children) to a theme provider.</summary>
        public void Bind(UiThemeProvider provider)
        {
            if (ReferenceEquals(themeProvider, provider))
            {
                return;
            }

            Unsubscribe();
            themeProvider = provider;
            Subscribe();
            Refresh();
        }

        /// <summary>Forces a re-skin from the current theme and state.</summary>
        public void Refresh()
        {
            ApplyTheme(Theme, state);
        }

        /// <summary>Sets the interaction state and re-skins if it changed.</summary>
        protected void SetState(UiControlState next)
        {
            if (state == next)
            {
                return;
            }

            state = next;
            ApplyTheme(Theme, state);
        }

        /// <summary>Paints the control from the theme for the given state. Implemented by each primitive.</summary>
        protected abstract void ApplyTheme(UiTheme theme, UiControlState state);

        protected virtual void OnEnable()
        {
            Subscribe();
            Refresh();
        }

        protected virtual void OnDisable()
        {
            Unsubscribe();
        }

        private void Subscribe()
        {
            if (themeProvider != null)
            {
                themeProvider.ThemeChanged += OnThemeChanged;
            }
        }

        private void Unsubscribe()
        {
            if (themeProvider != null)
            {
                themeProvider.ThemeChanged -= OnThemeChanged;
            }
        }

        private void OnThemeChanged(UiTheme theme)
        {
            ApplyTheme(theme, state);
        }

        private static UiThemeProvider SharedDefault()
        {
            return sharedDefault ?? (sharedDefault = new UiThemeProvider());
        }
    }
}
