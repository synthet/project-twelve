using UnityEngine;

namespace ProjectTwelve.Visual.Background
{
    /// <summary>
    /// Samples a sprite edge band for use as a camera clear color.
    /// </summary>
    public static class BackdropClearColorSampler
    {
        private const float EdgeBandSizeRatio = 0.12f;

        /// <summary>
        /// Returns the average color of the bottom edge band in a backdrop sprite.
        /// This matches the dark mountain-base tone visible below the parallax strip.
        /// </summary>
        /// <param name="sprite">Backdrop sprite to sample.</param>
        /// <returns>Average edge color, or black when sampling fails.</returns>
        public static Color SampleBackdropClearColor(Sprite sprite)
        {
            if (sprite == null || sprite.texture == null)
            {
                return Color.black;
            }

            Rect rect = sprite.textureRect;
            int width = Mathf.Max(1, Mathf.RoundToInt(rect.width));
            int height = Mathf.Max(1, Mathf.RoundToInt(rect.height));
            int x = Mathf.RoundToInt(rect.x);
            int yStart = Mathf.RoundToInt(rect.y);
            int bandHeight = Mathf.Max(1, Mathf.RoundToInt(height * EdgeBandSizeRatio));

            return SampleHorizontalBandAverage(sprite.texture, x, yStart, width, bandHeight);
        }

        private static Color SampleHorizontalBandAverage(
            Texture2D source,
            int x,
            int y,
            int width,
            int bandHeight)
        {
            RenderTexture previous = RenderTexture.active;
            RenderTexture renderTarget = RenderTexture.GetTemporary(
                source.width,
                source.height,
                0,
                RenderTextureFormat.ARGB32,
                RenderTextureReadWrite.sRGB);

            Graphics.Blit(source, renderTarget);
            RenderTexture.active = renderTarget;

            var buffer = new Texture2D(width, bandHeight, TextureFormat.RGBA32, false);
            buffer.ReadPixels(new Rect(x, y, width, bandHeight), 0, 0);
            buffer.Apply();

            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(renderTarget);

            Color sum = Color.black;
            int count = 0;
            for (int row = 0; row < bandHeight; row++)
            {
                for (int column = 0; column < width; column++)
                {
                    Color pixel = buffer.GetPixel(column, row);
                    if (pixel.a < 0.01f)
                    {
                        continue;
                    }

                    sum.r += pixel.r;
                    sum.g += pixel.g;
                    sum.b += pixel.b;
                    count++;
                }
            }

            if (Application.isPlaying)
            {
                Object.Destroy(buffer);
            }
            else
            {
                Object.DestroyImmediate(buffer);
            }

            if (count == 0)
            {
                return Color.black;
            }

            float inverseCount = 1f / count;
            return new Color(sum.r * inverseCount, sum.g * inverseCount, sum.b * inverseCount, 1f);
        }
    }
}
