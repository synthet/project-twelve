using System.Collections.Generic;
using UnityEngine;

namespace ProjectTwelve.Visual.Characters
{
    /// <summary>
    /// Merges RGBA layer buffers into a single texture.
    /// </summary>
    public static class LayerTextureComposer
    {
        /// <summary>
        /// Overlays each layer onto the destination buffer (later layers win on alpha).
        /// </summary>
        public static void MergeLayers(Color32[] destination, IReadOnlyList<Color32[]> layers)
        {
            if (destination == null || layers == null)
            {
                return;
            }

            for (int layerIndex = 0; layerIndex < layers.Count; layerIndex++)
            {
                Color32[] layer = layers[layerIndex];
                if (layer == null || layer.Length != destination.Length)
                {
                    continue;
                }

                for (int i = 0; i < destination.Length; i++)
                {
                    Color32 pixel = layer[i];
                    if (pixel.a > 0)
                    {
                        destination[i] = pixel;
                    }
                }
            }
        }

        /// <summary>
        /// Clears a rectangular region to transparent.
        /// </summary>
        public static void ClearRect(Color32[] pixels, int width, int x, int y, int rectWidth, int rectHeight)
        {
            for (int py = y; py < y + rectHeight; py++)
            {
                for (int px = x; px < x + rectWidth; px++)
                {
                    pixels[px + py * width] = new Color32(0, 0, 0, 0);
                }
            }
        }
    }
}
