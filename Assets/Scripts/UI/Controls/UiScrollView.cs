using UnityEngine;
using UnityEngine.UI;
using ProjectTwelve.UI.Theme;

namespace ProjectTwelve.UI.Controls
{
    /// <summary>
    /// A themed scroll view wrapping a <see cref="ScrollRect"/>. Provides the standard viewport/content
    /// structure and applies the theme's scroll speed, so scroll-fallback behaviour (content larger than
    /// the panel) is consistent across screens. The content is where callers parent their scrollable UI.
    /// </summary>
    [RequireComponent(typeof(ScrollRect))]
    public sealed class UiScrollView : UiControl
    {
        [SerializeField] private RectTransform content;
        [SerializeField] private bool horizontal;
        [SerializeField] private bool vertical = true;

        private ScrollRect scrollRect;

        /// <summary>The underlying scroll rect.</summary>
        public ScrollRect ScrollRect => scrollRect != null ? scrollRect : (scrollRect = GetComponent<ScrollRect>());

        /// <summary>The content rect that callers populate with scrollable children.</summary>
        public RectTransform Content => content;

        /// <summary>
        /// Ensures the viewport (with a <see cref="RectMask2D"/>) and content rects exist. Uses RectMask2D
        /// rather than nested Mask components to avoid extra draw calls and mask stacking.
        /// </summary>
        public void EnsureStructure()
        {
            ScrollRect rect = ScrollRect;
            if (rect.viewport == null)
            {
                RectTransform viewport = UiFactory.CreateRect("Viewport", transform);
                UiFactory.SetStretch(viewport, 0f, 0f, 0f, 0f);
                viewport.gameObject.AddComponent<RectMask2D>();
                Image raycast = viewport.gameObject.AddComponent<Image>();
                raycast.color = new Color(0f, 0f, 0f, 0f);
                rect.viewport = viewport;
            }

            if (content == null)
            {
                content = UiFactory.CreateRect("Content", rect.viewport);
                content.anchorMin = new Vector2(0f, 1f);
                content.anchorMax = new Vector2(1f, 1f);
                content.pivot = new Vector2(0.5f, 1f);
                content.anchoredPosition = Vector2.zero;
            }

            rect.content = content;
            rect.horizontal = horizontal;
            rect.vertical = vertical;
        }

        protected override void ApplyTheme(UiTheme theme, UiControlState state)
        {
            ScrollRect.scrollSensitivity = theme.ScrollSpeed;
        }

        protected override void OnEnable()
        {
            EnsureStructure();
            base.OnEnable();
        }
    }
}
