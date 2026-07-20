using UnityEngine;
using UnityEngine.UI;
using ProjectTwelve.UI.Theme;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace ProjectTwelve.UI.Controls
{
    /// <summary>
    /// Owns the single shared tooltip visual and shows it, after the theme's hover delay, on the Tooltips
    /// layer so it always draws above panels and never clips inside a scroll view. Targets request/cancel
    /// through this service rather than each spawning their own tooltip, keeping one tooltip on screen.
    /// </summary>
    public sealed class TooltipService : MonoBehaviour
    {
        private UiThemeProvider provider;
        private RectTransform layerRoot;
        private UiFramedPanel panel;
        private UiLabel label;
        private RectTransform panelRect;

        private object pendingSource;
        private string pendingText;
        private float hoverElapsed;
        private bool visible;

        /// <summary>Pure timing rule: the tooltip becomes visible once hover time reaches the delay.</summary>
        public static bool ReadyToShow(float hoverElapsed, float delay)
        {
            return hoverElapsed >= delay;
        }

        /// <summary>Wires the service to its layer root and theme provider, and builds the tooltip visual.</summary>
        public void Initialize(RectTransform tooltipLayerRoot, UiThemeProvider themeProvider)
        {
            layerRoot = tooltipLayerRoot;
            provider = themeProvider;
            BuildVisual();
            HideNow();
        }

        /// <summary>Requests a tooltip for a source. The delay restarts when the source changes.</summary>
        public void Request(object source, string text)
        {
            if (!ReferenceEquals(source, pendingSource))
            {
                pendingSource = source;
                hoverElapsed = 0f;
            }

            pendingText = text;
        }

        /// <summary>Cancels the tooltip for a source (pointer/focus left). Hides if it was showing it.</summary>
        public void Cancel(object source)
        {
            if (ReferenceEquals(source, pendingSource))
            {
                pendingSource = null;
                pendingText = null;
                HideNow();
            }
        }

        /// <summary>True while the tooltip is on screen.</summary>
        public bool IsVisible => visible;

        private void Update()
        {
            if (pendingSource == null || string.IsNullOrEmpty(pendingText))
            {
                return;
            }

            if (!visible)
            {
                hoverElapsed += Time.unscaledDeltaTime;
                float delay = provider != null ? provider.Active.TooltipDelay : 0.4f;
                if (ReadyToShow(hoverElapsed, delay))
                {
                    ShowNow(pendingText);
                }
            }

            if (visible)
            {
                FollowPointer();
            }
        }

        private void ShowNow(string text)
        {
            if (panel == null)
            {
                return;
            }

            label.Value = text;
            panel.gameObject.SetActive(true);
            visible = true;
        }

        private void HideNow()
        {
            if (panel != null)
            {
                panel.gameObject.SetActive(false);
            }

            visible = false;
            hoverElapsed = 0f;
        }

        private void FollowPointer()
        {
            Vector2 mouse = ReadPointer();
            panelRect.position = new Vector3(mouse.x + 16f, mouse.y - 16f, 0f);
        }

        private static Vector2 ReadPointer()
        {
#if ENABLE_INPUT_SYSTEM
            if (Pointer.current != null)
            {
                return Pointer.current.position.ReadValue();
            }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            return Input.mousePosition;
#else
            return Vector2.zero;
#endif
        }

        private void BuildVisual()
        {
            if (panel != null || layerRoot == null)
            {
                return;
            }

            Image image = UiFactory.CreateImage("Tooltip", layerRoot, null, Color.white);
            panelRect = image.rectTransform;
            panelRect.sizeDelta = new Vector2(180f, 40f);
            panelRect.pivot = new Vector2(0f, 1f);
            panel = image.gameObject.AddComponent<UiFramedPanel>();
            panel.Bind(provider);

            Text text = UiFactory.CreateText("Text", panelRect, string.Empty,
                provider != null ? provider.Active.Font : null, 14, TextAnchor.MiddleLeft, Color.white);
            UiFactory.SetStretch(text.rectTransform, 8f, 8f, 6f, 6f);
            label = text.gameObject.AddComponent<UiLabel>();
            label.Bind(provider);
        }
    }
}
