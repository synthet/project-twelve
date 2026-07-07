using System.Collections.Generic;
using UnityEngine;

namespace ProjectTwelve.Visual.AutotileDebug
{
    /// <summary>
    /// Builds colored debug quads and digit labels for ground autotile overlays.
    /// </summary>
    public static class AutotileDebugMeshBuilder
    {
        private const float MarkerInset = 0.075f;
        private const float ZOffset = -0.03f;
        private const int DigitCellPixels = 8;
        private const int DigitColumns = 11;
        private const int MinusGlyphIndex = 10;
        private static readonly Rect SolidFillUv = new Rect(0f, 0f, 1f / (DigitCellPixels * (DigitColumns + 1)), 1f);

        private static Texture2D digitAtlas;
        private static readonly int[][] DigitPatterns =
        {
            new[] { 0x3C, 0x66, 0x6E, 0x76, 0x66, 0x66, 0x3C }, // 0
            new[] { 0x18, 0x38, 0x18, 0x18, 0x18, 0x18, 0x7E }, // 1
            new[] { 0x3C, 0x66, 0x06, 0x1C, 0x30, 0x60, 0x7E }, // 2
            new[] { 0x3C, 0x66, 0x06, 0x1C, 0x06, 0x66, 0x3C }, // 3
            new[] { 0x0C, 0x1C, 0x2C, 0x4C, 0x7E, 0x0C, 0x0C }, // 4
            new[] { 0x7E, 0x60, 0x7C, 0x06, 0x06, 0x66, 0x3C }, // 5
            new[] { 0x1C, 0x30, 0x60, 0x7C, 0x66, 0x66, 0x3C }, // 6
            new[] { 0x7E, 0x06, 0x0C, 0x18, 0x30, 0x30, 0x30 }, // 7
            new[] { 0x3C, 0x66, 0x66, 0x3C, 0x66, 0x66, 0x3C }, // 8
            new[] { 0x3C, 0x66, 0x66, 0x3E, 0x06, 0x0C, 0x38 }, // 9
            new[] { 0x00, 0x00, 0x00, 0x7E, 0x00, 0x00, 0x00 }, // minus
        };

        /// <summary>
        /// Ensures the shared digit atlas texture exists (0–9 and minus in a horizontal strip).
        /// </summary>
        public static Texture2D GetDigitAtlas()
        {
            if (digitAtlas != null)
            {
                return digitAtlas;
            }

            int width = DigitCellPixels * (DigitColumns + 1);
            int height = DigitCellPixels;
            Color32[] pixels = new Color32[width * height];
            Color32 on = new Color32(255, 255, 255, 255);
            Color32 off = new Color32(0, 0, 0, 0);

            for (int py = 0; py < height; py++)
            {
                for (int px = 0; px < DigitCellPixels; px++)
                {
                    pixels[py * width + px] = on;
                }
            }

            for (int digit = 0; digit < DigitColumns; digit++)
            {
                int[] pattern = DigitPatterns[digit];
                int digitOffsetX = DigitCellPixels + digit * DigitCellPixels;
                for (int row = 0; row < 7; row++)
                {
                    int rowBits = pattern[row];
                    for (int col = 0; col < 6; col++)
                    {
                        bool lit = (rowBits & (1 << (5 - col))) != 0;
                        int px = digitOffsetX + col + 1;
                        int py = height - 2 - row;
                        if (px >= 0 && px < width && py >= 0 && py < height)
                        {
                            pixels[py * width + px] = lit ? on : off;
                        }
                    }
                }
            }

            digitAtlas = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                name = "AutotileDebugDigitAtlas",
            };
            digitAtlas.SetPixels32(pixels);
            digitAtlas.Apply(false, true);
            return digitAtlas;
        }

        /// <summary>
        /// Appends a centered colored square marker for one tile cell.
        /// </summary>
        public static void AppendTileMarker(
            List<Vector3> vertices,
            List<int> triangles,
            List<Vector2> uvs,
            List<Color> colors,
            int localX,
            int localY,
            float tileSize,
            Color fillColor)
        {
            float left = localX * tileSize + tileSize * MarkerInset;
            float right = (localX + 1) * tileSize - tileSize * MarkerInset;
            float bottom = localY * tileSize + tileSize * MarkerInset;
            float top = (localY + 1) * tileSize - tileSize * MarkerInset;
            AppendQuad(vertices, triangles, uvs, colors, left, right, bottom, top, ZOffset, SolidFillUv, fillColor);
        }

        /// <summary>
        /// Appends a half-height marker (top or bottom half of the cell).
        /// </summary>
        public static void AppendHalfTileMarker(
            List<Vector3> vertices,
            List<int> triangles,
            List<Vector2> uvs,
            List<Color> colors,
            int localX,
            int localY,
            float tileSize,
            Color fillColor,
            bool topHalf)
        {
            float left = localX * tileSize + tileSize * MarkerInset;
            float right = (localX + 1) * tileSize - tileSize * MarkerInset;
            float mid = (localY + 0.5f) * tileSize;
            float bottom = topHalf ? mid : localY * tileSize + tileSize * MarkerInset;
            float top = topHalf ? (localY + 1) * tileSize - tileSize * MarkerInset : mid;
            AppendQuad(vertices, triangles, uvs, colors, left, right, bottom, top, ZOffset - 0.001f, SolidFillUv, fillColor);
        }

        /// <summary>
        /// Formats a 3×3 autotile mask as three slash-separated rows (north → south, west → east).
        /// </summary>
        public static string MaskToCompactString(int[,] mask)
        {
            if (mask == null || mask.GetLength(0) != 3 || mask.GetLength(1) != 3)
            {
                return string.Empty;
            }

            return string.Concat(
                mask[0, 0], mask[1, 0], mask[2, 0], '/',
                mask[0, 1], mask[1, 1], mask[2, 1], '/',
                mask[0, 2], mask[1, 2], mask[2, 2]);
        }

        /// <summary>
        /// Appends nine tiny 0/1 digits for the ground mask in the bottom of the cell.
        /// </summary>
        public static void AppendCompactMaskGrid(
            List<Vector3> vertices,
            List<int> triangles,
            List<Vector2> uvs,
            List<Color> colors,
            int localX,
            int localY,
            float tileSize,
            int[,] mask)
        {
            if (mask == null || mask.GetLength(0) != 3 || mask.GetLength(1) != 3)
            {
                return;
            }

            float cellLeft = localX * tileSize;
            float cellBottom = localY * tileSize;
            float digitWidth = tileSize * 0.12f;
            float digitHeight = tileSize * 0.14f;
            float rowGap = tileSize * 0.015f;
            float gridWidth = digitWidth * 3f;
            float gridHeight = digitHeight * 3f + rowGap * 2f;
            float startX = cellLeft + (tileSize - gridWidth) * 0.5f;
            float startY = cellBottom + tileSize * 0.06f;

            for (int row = 0; row < 3; row++)
            {
                for (int col = 0; col < 3; col++)
                {
                    int value = Mathf.Clamp(mask[col, row], 0, 1);
                    float left = startX + col * digitWidth;
                    float right = left + digitWidth;
                    float bottom = startY + row * (digitHeight + rowGap);
                    float top = bottom + digitHeight;
                    Rect digitUv = GetDigitUv(value);
                    AppendQuad(
                        vertices,
                        triangles,
                        uvs,
                        colors,
                        left,
                        right,
                        bottom,
                        top,
                        ZOffset - 0.003f,
                        digitUv,
                        AutotileDebugPalette.MaskDigitColor);
                }
            }
        }

        /// <summary>
        /// Appends world tile X (top) and Y (bottom) as signed integer labels.
        /// </summary>
        public static void AppendCoordinateLabel(
            List<Vector3> vertices,
            List<int> triangles,
            List<Vector2> uvs,
            List<Color> colors,
            int localX,
            int localY,
            float tileSize,
            int worldX,
            int worldY)
        {
            AppendSignedIntegerLabel(
                vertices,
                triangles,
                uvs,
                colors,
                localX,
                localY,
                tileSize,
                worldX,
                AutotileDebugPalette.LabelColor,
                verticalOffsetTiles: -0.18f,
                digitWidthScale: 0.18f,
                digitHeightScale: 0.22f);

            AppendSignedIntegerLabel(
                vertices,
                triangles,
                uvs,
                colors,
                localX,
                localY,
                tileSize,
                worldY,
                AutotileDebugPalette.LabelColor,
                verticalOffsetTiles: 0.18f,
                digitWidthScale: 0.18f,
                digitHeightScale: 0.22f);
        }

        /// <summary>
        /// Appends digit quads and an optional flip marker for sprite id labels.
        /// </summary>
        public static void AppendSpriteIdLabel(
            List<Vector3> vertices,
            List<int> triangles,
            List<Vector2> uvs,
            List<Color> colors,
            int localX,
            int localY,
            float tileSize,
            string spriteId,
            bool flipX)
        {
            AppendSpriteIdLabel(
                vertices,
                triangles,
                uvs,
                colors,
                localX,
                localY,
                tileSize,
                spriteId,
                flipX,
                AutotileDebugPalette.LabelColor,
                0f);
        }

        /// <summary>
        /// Appends digit quads using a custom tint and vertical offset within the tile.
        /// </summary>
        public static void AppendSpriteIdLabel(
            List<Vector3> vertices,
            List<int> triangles,
            List<Vector2> uvs,
            List<Color> colors,
            int localX,
            int localY,
            float tileSize,
            string spriteId,
            bool flipX,
            Color labelColor,
            float verticalOffsetTiles)
        {
            if (!TryParseSpriteId(spriteId, out int value))
            {
                return;
            }

            AppendSignedIntegerLabel(
                vertices,
                triangles,
                uvs,
                colors,
                localX,
                localY,
                tileSize,
                value,
                labelColor,
                verticalOffsetTiles);

            if (flipX)
            {
                float notch = tileSize * 0.12f;
                float rightEdge = (localX + 1) * tileSize - tileSize * MarkerInset;
                float topEdge = (localY + 1) * tileSize - tileSize * MarkerInset;
                AppendQuad(
                    vertices,
                    triangles,
                    uvs,
                    colors,
                    rightEdge - notch,
                    rightEdge,
                    topEdge - notch,
                    topEdge,
                    ZOffset - 0.002f,
                    SolidFillUv,
                    AutotileDebugPalette.FlipMarkerColor);
            }
        }

        /// <summary>
        /// Appends centered signed-integer digit quads within one tile cell.
        /// </summary>
        public static void AppendSignedIntegerLabel(
            List<Vector3> vertices,
            List<int> triangles,
            List<Vector2> uvs,
            List<Color> colors,
            int localX,
            int localY,
            float tileSize,
            int value,
            Color labelColor,
            float verticalOffsetTiles,
            float digitWidthScale = 0.22f,
            float digitHeightScale = 0.28f)
        {
            float centerX = (localX + 0.5f) * tileSize;
            float centerY = (localY + 0.5f + verticalOffsetTiles) * tileSize;
            float digitWidth = tileSize * digitWidthScale;
            float digitHeight = tileSize * digitHeightScale;

            string text = value.ToString();
            float totalWidth = text.Length * digitWidth;
            float startX = centerX - totalWidth * 0.5f;
            float bottom = centerY - digitHeight * 0.5f;
            float top = centerY + digitHeight * 0.5f;

            for (int i = 0; i < text.Length; i++)
            {
                int glyphIndex = text[i] == '-' ? MinusGlyphIndex : text[i] - '0';
                float left = startX + i * digitWidth;
                float right = left + digitWidth;
                Rect digitUv = GetGlyphUv(glyphIndex);
                AppendQuad(
                    vertices,
                    triangles,
                    uvs,
                    colors,
                    left,
                    right,
                    bottom,
                    top,
                    ZOffset - 0.001f,
                    digitUv,
                    labelColor);
            }
        }

        private static bool TryParseSpriteId(string spriteId, out int value)
        {
            value = 0;
            return !string.IsNullOrEmpty(spriteId) && int.TryParse(spriteId, out value);
        }

        private static Rect GetDigitUv(int digit)
        {
            return GetGlyphUv(digit);
        }

        private static Rect GetGlyphUv(int glyphIndex)
        {
            glyphIndex = Mathf.Clamp(glyphIndex, 0, MinusGlyphIndex);
            float cell = 1f / (DigitColumns + 1);
            return new Rect(cell + glyphIndex * cell, 0f, cell, 1f);
        }

        private static void AppendQuad(
            List<Vector3> vertices,
            List<int> triangles,
            List<Vector2> uvs,
            List<Color> colors,
            float left,
            float right,
            float bottom,
            float top,
            float z,
            Rect uv,
            Color color)
        {
            int start = vertices.Count;
            vertices.Add(new Vector3(left, bottom, z));
            vertices.Add(new Vector3(right, bottom, z));
            vertices.Add(new Vector3(right, top, z));
            vertices.Add(new Vector3(left, top, z));

            uvs.Add(new Vector2(uv.xMin, uv.yMin));
            uvs.Add(new Vector2(uv.xMax, uv.yMin));
            uvs.Add(new Vector2(uv.xMax, uv.yMax));
            uvs.Add(new Vector2(uv.xMin, uv.yMax));

            triangles.Add(start);
            triangles.Add(start + 2);
            triangles.Add(start + 1);
            triangles.Add(start);
            triangles.Add(start + 3);
            triangles.Add(start + 2);

            colors.Add(color);
            colors.Add(color);
            colors.Add(color);
            colors.Add(color);
        }
    }
}
