using UnityEngine;
using UnityEngine.UI;
using ProjectTwelve.UI.Theme;

namespace ProjectTwelve.UI.Controls
{
    /// <summary>
    /// A themed 9-slice panel. Uses the theme's <see cref="UiSpriteRole.FramedPanel"/> sprite when present
    /// (drawn Sliced so pixel-art borders never stretch), otherwise falls back to a flat
    /// <see cref="UiColorRole.PanelBackground"/> tint so it still renders with no art assigned.
    /// </summary>
    [RequireComponent(typeof(Image))]
    public sealed class UiFramedPanel : UiControl
    {
        [SerializeField] private UiSpriteRole spriteRole = UiSpriteRole.FramedPanel;
        [SerializeField] private UiColorRole tintRole = UiColorRole.PanelBackground;

        private Image image;

        /// <summary>The panel's background image.</summary>
        public Image Image => image != null ? image : (image = GetComponent<Image>());

        protected override void ApplyTheme(UiTheme theme, UiControlState state)
        {
            Image img = Image;
            Sprite sprite = theme.ResolveSprite(spriteRole);
            img.sprite = sprite;
            img.type = sprite != null ? Image.Type.Sliced : Image.Type.Simple;
            // With a sprite, tint white so the art shows; without one, tint with the role color.
            img.color = sprite != null ? Color.white : theme.ResolveColor(tintRole);
        }
    }
}
