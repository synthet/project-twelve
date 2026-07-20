using System;
using UnityEngine;

namespace ProjectTwelve.UI.Theme
{
    /// <summary>
    /// Centralised design tokens (spacing, sizes, timings) and semantic color/sprite roles for the whole
    /// UI. Controls read from the active theme instead of hardcoding values, so scale, palette, and art
    /// can change at runtime with one swap. Every role has a built-in default, so an empty or partial
    /// theme still resolves — this is the "token fallback" the framework relies on.
    /// </summary>
    [CreateAssetMenu(menuName = "ProjectTwelve/UI/Theme", fileName = "UiTheme")]
    public sealed class UiTheme : ScriptableObject
    {
        [Serializable]
        public struct RoleColor
        {
            public UiColorRole role;
            public Color color;
        }

        [Serializable]
        public struct RoleSprite
        {
            public UiSpriteRole role;
            public Sprite sprite;
        }

        [Header("Spacing & sizing tokens (reference pixels)")]
        [SerializeField] private int baseSpacing = 4;
        [SerializeField] private int paddingSmall = 4;
        [SerializeField] private int paddingMedium = 8;
        [SerializeField] private int paddingLarge = 16;
        [SerializeField] private int borderThickness = 2;
        [SerializeField] private int rowHeight = 24;
        [SerializeField] private int buttonHeight = 28;
        [SerializeField] private int iconSize = 32;
        [SerializeField] private int slotSize = 48;

        [Header("Typography tokens")]
        [SerializeField] private int fontSizeSmall = 12;
        [SerializeField] private int fontSizeBody = 15;
        [SerializeField] private int fontSizeHeading = 20;
        [SerializeField] private Font font;

        [Header("Timing tokens (seconds)")]
        [SerializeField] private float tooltipDelay = 0.4f;
        [SerializeField] private float scrollSpeed = 40f;
        [SerializeField] private float animationFast = 0.08f;
        [SerializeField] private float animationNormal = 0.16f;

        [Header("Semantic roles (unset entries fall back to defaults)")]
        [SerializeField] private RoleColor[] colors = Array.Empty<RoleColor>();
        [SerializeField] private RoleSprite[] sprites = Array.Empty<RoleSprite>();

        public int BaseSpacing => baseSpacing;
        public int PaddingSmall => paddingSmall;
        public int PaddingMedium => paddingMedium;
        public int PaddingLarge => paddingLarge;
        public int BorderThickness => borderThickness;
        public int RowHeight => rowHeight;
        public int ButtonHeight => buttonHeight;
        public int IconSize => iconSize;
        public int SlotSize => slotSize;
        public int FontSizeSmall => fontSizeSmall;
        public int FontSizeBody => fontSizeBody;
        public int FontSizeHeading => fontSizeHeading;
        public Font Font => font;
        public float TooltipDelay => tooltipDelay;
        public float ScrollSpeed => scrollSpeed;
        public float AnimationFast => animationFast;
        public float AnimationNormal => animationNormal;

        /// <summary>Resolves a color role, falling back to the built-in default when the theme omits it.</summary>
        public Color ResolveColor(UiColorRole role)
        {
            if (colors != null)
            {
                for (int i = 0; i < colors.Length; i++)
                {
                    if (colors[i].role == role)
                    {
                        return colors[i].color;
                    }
                }
            }

            return DefaultColor(role);
        }

        /// <summary>Resolves the color for a control state via its matching color role.</summary>
        public Color ResolveStateColor(UiControlState state)
        {
            return ResolveColor(StateRole(state));
        }

        /// <summary>Resolves a sprite role, returning null when unset (controls then draw a flat tint).</summary>
        public Sprite ResolveSprite(UiSpriteRole role)
        {
            if (sprites != null)
            {
                for (int i = 0; i < sprites.Length; i++)
                {
                    if (sprites[i].role == role)
                    {
                        return sprites[i].sprite;
                    }
                }
            }

            return null;
        }

        /// <summary>Maps a control state to the color role used to tint it.</summary>
        public static UiColorRole StateRole(UiControlState state)
        {
            switch (state)
            {
                case UiControlState.Hovered: return UiColorRole.Hovered;
                case UiControlState.Pressed: return UiColorRole.Pressed;
                case UiControlState.Selected: return UiColorRole.Selected;
                case UiControlState.Disabled: return UiColorRole.Disabled;
                case UiControlState.Focused: return UiColorRole.Focused;
                default: return UiColorRole.Normal;
            }
        }

        /// <summary>The built-in fallback palette. Neutral, readable, and license-free.</summary>
        public static Color DefaultColor(UiColorRole role)
        {
            switch (role)
            {
                case UiColorRole.PanelBackground: return new Color(0.18f, 0.19f, 0.22f, 0.97f);
                case UiColorRole.PanelBorder: return new Color(0.35f, 0.37f, 0.42f, 1f);
                case UiColorRole.Separator: return new Color(0.30f, 0.32f, 0.36f, 1f);
                case UiColorRole.TextPrimary: return new Color(0.94f, 0.95f, 0.97f, 1f);
                case UiColorRole.TextMuted: return new Color(0.62f, 0.65f, 0.70f, 1f);
                case UiColorRole.Normal: return new Color(0.24f, 0.26f, 0.30f, 1f);
                case UiColorRole.Hovered: return new Color(0.32f, 0.35f, 0.40f, 1f);
                case UiColorRole.Pressed: return new Color(0.16f, 0.18f, 0.21f, 1f);
                case UiColorRole.Selected: return new Color(1f, 0.76f, 0.24f, 1f);
                case UiColorRole.Disabled: return new Color(0.20f, 0.21f, 0.23f, 0.6f);
                case UiColorRole.Focused: return new Color(0.40f, 0.70f, 1f, 1f);
                case UiColorRole.Warning: return new Color(0.95f, 0.73f, 0.20f, 1f);
                case UiColorRole.Destructive: return new Color(0.86f, 0.28f, 0.26f, 1f);
                case UiColorRole.Accent: return new Color(0.36f, 0.62f, 0.92f, 1f);
                default: return Color.white;
            }
        }
    }
}
