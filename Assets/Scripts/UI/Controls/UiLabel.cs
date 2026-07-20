using UnityEngine;
using UnityEngine.UI;
using ProjectTwelve.UI.Theme;

namespace ProjectTwelve.UI.Controls
{
    /// <summary>
    /// A themed text label. Reads font, size, and color role from the theme (separating text content from
    /// visual assets, as the pixel-art rules require) and supports wrapping/ellipsis so long, unlocalised
    /// strings never silently overflow the control.
    /// </summary>
    [RequireComponent(typeof(Text))]
    public sealed class UiLabel : UiControl
    {
        public enum Size { Small, Body, Heading }

        [SerializeField] private Size size = Size.Body;
        [SerializeField] private UiColorRole colorRole = UiColorRole.TextPrimary;
        [SerializeField] private bool wrap = true;
        [SerializeField] private bool ellipsisOnOverflow = true;

        private Text text;

        /// <summary>The underlying legacy text component.</summary>
        public Text Text => text != null ? text : (text = GetComponent<Text>());

        /// <summary>The displayed string.</summary>
        public string Value
        {
            get => Text.text;
            set => Text.text = value;
        }

        /// <summary>Sets the semantic color role (e.g. Warning, Destructive) and re-skins.</summary>
        public void SetColorRole(UiColorRole role)
        {
            colorRole = role;
            Refresh();
        }

        protected override void ApplyTheme(UiTheme theme, UiControlState state)
        {
            Text t = Text;
            if (theme.Font != null)
            {
                t.font = theme.Font;
            }

            t.fontSize = ResolveFontSize(theme);
            t.color = state == UiControlState.Disabled
                ? theme.ResolveColor(UiColorRole.Disabled)
                : theme.ResolveColor(colorRole);
            t.horizontalOverflow = wrap ? HorizontalWrapMode.Wrap : HorizontalWrapMode.Overflow;
            t.verticalOverflow = ellipsisOnOverflow ? VerticalWrapMode.Truncate : VerticalWrapMode.Overflow;
        }

        private int ResolveFontSize(UiTheme theme)
        {
            switch (size)
            {
                case Size.Small: return theme.FontSizeSmall;
                case Size.Heading: return theme.FontSizeHeading;
                default: return theme.FontSizeBody;
            }
        }
    }
}
