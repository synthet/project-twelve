using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using ProjectTwelve.UI.Theme;

namespace ProjectTwelve.UI.Controls
{
    /// <summary>
    /// A themed button. Derives from <see cref="Selectable"/> so it gets uGUI directional navigation
    /// (keyboard/controller) and interactable/disabled handling for free, and drives its visuals from the
    /// theme's state colors instead of the built-in ColorTint transition. Fires <see cref="Clicked"/> on
    /// pointer click or the submit action, so mouse, keyboard, and controller all activate it identically.
    /// </summary>
    [RequireComponent(typeof(Image))]
    public sealed class UiButton : Selectable, IUiThemeConsumer, IPointerClickHandler, ISubmitHandler
    {
        [SerializeField] private UiSpriteRole spriteRole = UiSpriteRole.Button;

        private UiThemeProvider themeProvider;
        private static UiThemeProvider sharedDefault;

        /// <summary>Raised when the button is activated by mouse, keyboard, or controller.</summary>
        public event Action Clicked;

        private UiThemeProvider Provider => themeProvider ?? (themeProvider = SharedDefault());

        public void Bind(UiThemeProvider provider)
        {
            if (ReferenceEquals(themeProvider, provider))
            {
                return;
            }

            if (themeProvider != null)
            {
                themeProvider.ThemeChanged -= OnThemeChanged;
            }

            themeProvider = provider;
            if (themeProvider != null)
            {
                themeProvider.ThemeChanged += OnThemeChanged;
            }

            Refresh();
        }

        public void Refresh()
        {
            ApplySprite();
            DoStateTransition(currentSelectionState, true);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (IsInteractable() && eventData.button == PointerEventData.InputButton.Left)
            {
                Clicked?.Invoke();
            }
        }

        public void OnSubmit(BaseEventData eventData)
        {
            if (IsInteractable())
            {
                Clicked?.Invoke();
            }
        }

        protected override void DoStateTransition(SelectionState selectionState, bool instant)
        {
            base.DoStateTransition(selectionState, instant);
            if (targetGraphic == null)
            {
                return;
            }

            targetGraphic.canvasRenderer.SetColor(Provider.Active.ResolveStateColor(MapState(selectionState)));
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            if (targetGraphic == null)
            {
                targetGraphic = GetComponent<Image>();
            }

            transition = Transition.None; // We drive color ourselves from the theme.
            if (themeProvider != null)
            {
                themeProvider.ThemeChanged += OnThemeChanged;
            }

            Refresh();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            if (themeProvider != null)
            {
                themeProvider.ThemeChanged -= OnThemeChanged;
            }
        }

        private void OnThemeChanged(UiTheme theme)
        {
            Refresh();
        }

        private void ApplySprite()
        {
            Image img = targetGraphic as Image;
            if (img == null)
            {
                return;
            }

            Sprite sprite = Provider.Active.ResolveSprite(spriteRole);
            img.sprite = sprite;
            img.type = sprite != null ? Image.Type.Sliced : Image.Type.Simple;
        }

        private static UiControlState MapState(SelectionState selectionState)
        {
            switch (selectionState)
            {
                case SelectionState.Highlighted: return UiControlState.Hovered;
                case SelectionState.Pressed: return UiControlState.Pressed;
                case SelectionState.Selected: return UiControlState.Focused;
                case SelectionState.Disabled: return UiControlState.Disabled;
                default: return UiControlState.Normal;
            }
        }

        private static UiThemeProvider SharedDefault()
        {
            return sharedDefault ?? (sharedDefault = new UiThemeProvider());
        }
    }
}
