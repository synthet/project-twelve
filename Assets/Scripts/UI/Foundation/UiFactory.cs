using UnityEngine;
using UnityEngine.UI;

namespace ProjectTwelve.UI
{
    /// <summary>
    /// Shared uGUI construction helpers, generalised from the per-HUD factory methods so every control
    /// builds its sub-tree the same way. Kept static and allocation-light; callers own parenting/sizing.
    /// UI layer index 5 matches the project's existing "UI" layer used by <c>SandboxHudController</c>.
    /// </summary>
    public static class UiFactory
    {
        /// <summary>Unity layer used for UI GameObjects (matches the project's existing HUD convention).</summary>
        public const int UiGameObjectLayer = 5;

        /// <summary>Creates an empty <see cref="RectTransform"/> child.</summary>
        public static RectTransform CreateRect(string name, Transform parent)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.layer = UiGameObjectLayer;
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            return rect;
        }

        /// <summary>Creates an <see cref="Image"/> child with the given sprite/color.</summary>
        public static Image CreateImage(string name, Transform parent, Sprite sprite, Color color, bool raycastTarget = false)
        {
            RectTransform rect = CreateRect(name, parent);
            Image image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.type = sprite != null ? Image.Type.Sliced : Image.Type.Simple;
            image.raycastTarget = raycastTarget;
            return image;
        }

        /// <summary>Creates a legacy <see cref="Text"/> child with a drop shadow, matching HUD styling.</summary>
        public static Text CreateText(string name, Transform parent, string value, Font font, int fontSize,
            TextAnchor alignment, Color color)
        {
            RectTransform rect = CreateRect(name, parent);
            Text text = rect.gameObject.AddComponent<Text>();
            text.font = font != null ? font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = value;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = color;
            text.raycastTarget = false;
            Shadow shadow = rect.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.8f);
            shadow.effectDistance = new Vector2(2f, -2f);
            return text;
        }

        /// <summary>Sets anchor/pivot/position/size in one call.</summary>
        public static void SetRect(RectTransform rect, Vector2 position, Vector2 size,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        /// <summary>Stretches a rect to fill its parent with per-edge insets.</summary>
        public static void SetStretch(RectTransform rect, float left, float right, float top, float bottom)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
        }
    }
}
