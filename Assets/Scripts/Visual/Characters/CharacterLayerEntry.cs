using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using UnityEngine;

namespace ProjectTwelve.Visual.Characters
{
    /// <summary>
    /// One equipment layer in the character layer catalog.
    /// </summary>
    [Serializable]
    public sealed class CharacterLayerEntry
    {
        [SerializeField] private string layerName;
        [SerializeField] private List<Texture2D> textures = new List<Texture2D>();

        public string LayerName => layerName;
        public IReadOnlyList<Texture2D> Textures => textures;

        public static CharacterLayerEntry Create(string name, List<Texture2D> layerTextures)
        {
            CharacterLayerEntry entry = new CharacterLayerEntry();
            entry.layerName = name;
            entry.textures = layerTextures ?? new List<Texture2D>();
            return entry;
        }

        /// <summary>
        /// Parses a layer string and returns recolored pixels for the full hero sheet size.
        /// </summary>
        public Color32[] GetPixels(string layerData, int sheetWidth, int sheetHeight, Color32[] mask = null)
        {
            if (string.IsNullOrEmpty(layerData) || textures == null || textures.Count == 0)
            {
                return null;
            }

            Match match = Regex.Match(
                layerData,
                @"(?<Name>[\w\- \[\]]+)(?<Paint>#\w+)?(?:\/(?<H>[-\d]+):(?<S>[-\d]+):(?<V>[-\d]+))?");
            string name = match.Groups["Name"].Value;
            int index = textures.FindIndex(t => t != null && t.name == name);
            if (index < 0)
            {
                return null;
            }

            Texture2D texture = textures[index];
            Color paint = Color.white;
            if (match.Groups["Paint"].Success)
            {
                ColorUtility.TryParseHtmlString(match.Groups["Paint"].Value, out paint);
            }

            float hueShift = 0f;
            float satShift = 0f;
            float valShift = 0f;
            if (match.Groups["H"].Success)
            {
                hueShift = float.Parse(match.Groups["H"].Value, CultureInfo.InvariantCulture);
                satShift = float.Parse(match.Groups["S"].Value, CultureInfo.InvariantCulture);
                valShift = float.Parse(match.Groups["V"].Value, CultureInfo.InvariantCulture);
            }

            Color32[] source = texture.GetPixels32();
            Color32[] output = new Color32[sheetWidth * sheetHeight];
            int copyWidth = Mathf.Min(texture.width, sheetWidth);
            int copyHeight = Mathf.Min(texture.height, sheetHeight);

            for (int y = 0; y < copyHeight; y++)
            {
                for (int x = 0; x < copyWidth; x++)
                {
                    Color32 pixel = source[x + y * texture.width];
                    if (pixel.a == 0)
                    {
                        continue;
                    }

                    if (mask != null)
                    {
                        Color32 maskPixel = mask[x + y * sheetWidth];
                        if (maskPixel.a > 0 && maskPixel.a != pixel.a)
                        {
                            continue;
                        }
                    }

                    Color color = pixel;
                    if (paint != Color.white)
                    {
                        color = ApplyPaint(color, paint);
                    }

                    if (hueShift != 0f || satShift != 0f || valShift != 0f)
                    {
                        color = ApplyHsvShift(color, hueShift, satShift, valShift);
                    }

                    output[x + y * sheetWidth] = color;
                }
            }

            return output;
        }

        private static Color ApplyPaint(Color source, Color paint)
        {
            return new Color(
                source.r * paint.r,
                source.g * paint.g,
                source.b * paint.b,
                source.a);
        }

        private static Color ApplyHsvShift(Color color, float hueShift, float satShift, float valShift)
        {
            Color.RGBToHSV(color, out float h, out float s, out float v);
            h = Mathf.Repeat(h + hueShift / 360f, 1f);
            s = Mathf.Clamp01(s + satShift / 100f);
            v = Mathf.Clamp01(v + valShift / 100f);
            Color shifted = Color.HSVToRGB(h, s, v);
            shifted.a = color.a;
            return shifted;
        }
    }
}
