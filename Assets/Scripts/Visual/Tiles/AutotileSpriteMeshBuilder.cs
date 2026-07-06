using System.Collections.Generic;
using UnityEngine;

namespace ProjectTwelve.Visual.Tiles
{
    /// <summary>
    /// Builds chunk mesh geometry from Unity sprite meshes (includes extrude padding for seamless tiles).
    /// </summary>
    public static class AutotileSpriteMeshBuilder
    {
        private const float BoundsEpsilon = 0.0001f;

        /// <summary>
        /// Appends sprite mesh triangles into the target buffers, anchored to tile cell origin
        /// <c>[x, x+1) × [y, y+1)</c> at the given <paramref name="tileSize"/>.
        /// </summary>
        public static void AppendSprite(
            List<Vector3> vertices,
            List<int> triangles,
            List<Vector2> uvs,
            List<Color> colors,
            int x,
            int y,
            float tileSize,
            Sprite sprite,
            bool flipX,
            Color color,
            float zOffset,
            bool flipY = false,
            int rotationDegrees = 0)
        {
            if (sprite == null)
            {
                return;
            }

            Vector2[] spriteVerts = sprite.vertices;
            Vector2[] spriteUvs = sprite.uv;
            ushort[] spriteTriangles = sprite.triangles;

            if (spriteVerts == null || spriteVerts.Length == 0
                || spriteUvs == null || spriteUvs.Length == 0
                || spriteTriangles == null || spriteTriangles.Length == 0)
            {
                AppendFixedCellQuad(vertices, triangles, uvs, colors, x, y, tileSize, sprite, flipX, color, zOffset, flipY, rotationDegrees);
                return;
            }

            Bounds bounds = sprite.bounds;
            float cellWidth = bounds.max.x - bounds.min.x;
            float cellOriginX = x * tileSize;
            float cellOriginY = y * tileSize;

            GetSpriteUvBounds(spriteUvs, out float uMin, out float uMax, out float vMin, out float vMax);
            int normalizedRotation = NormalizeRotationDegrees(rotationDegrees);

            int start = vertices.Count;
            for (int i = 0; i < spriteVerts.Length; i++)
            {
                float offsetX = spriteVerts[i].x - bounds.min.x;
                if (flipX)
                {
                    offsetX = cellWidth - offsetX;
                }

                float offsetY = spriteVerts[i].y - bounds.min.y;
                vertices.Add(new Vector3(cellOriginX + offsetX, cellOriginY + offsetY, zOffset));

                Vector2 uv = TransformUv(spriteUvs[i], uMin, uMax, vMin, vMax, flipX, flipY, normalizedRotation);

                uvs.Add(uv);
                colors.Add(color);
            }

            AppendSpriteTriangles(triangles, start, spriteTriangles, flipX);
        }

        /// <summary>
        /// Maps a sprite vertex to world X using bounds-relative cell anchoring (matches tile-viz blit flip).
        /// </summary>
        internal static float ComputeWorldX(
            int cellX,
            float tileSize,
            Sprite sprite,
            float spriteLocalX,
            bool flipX)
        {
            Bounds bounds = sprite.bounds;
            float cellWidth = bounds.max.x - bounds.min.x;
            float offsetX = spriteLocalX - bounds.min.x;
            if (flipX)
            {
                offsetX = cellWidth - offsetX;
            }

            return cellX * tileSize + offsetX;
        }

        private static void AppendSpriteTriangles(
            List<int> triangles,
            int start,
            ushort[] spriteTriangles,
            bool flipX)
        {
            if (!flipX)
            {
                for (int i = 0; i < spriteTriangles.Length; i++)
                {
                    triangles.Add(start + spriteTriangles[i]);
                }

                return;
            }

            for (int i = 0; i + 2 < spriteTriangles.Length; i += 3)
            {
                triangles.Add(start + spriteTriangles[i]);
                triangles.Add(start + spriteTriangles[i + 2]);
                triangles.Add(start + spriteTriangles[i + 1]);
            }
        }

        /// <summary>
        /// Returns axis-aligned bounds of the mesh that would be emitted for a sprite at the given tile cell.
        /// </summary>
        public static void GetTileCellBounds(
            int x,
            int y,
            float tileSize,
            Sprite sprite,
            bool flipX,
            out float left,
            out float right,
            out float bottom,
            out float top)
        {
            left = float.PositiveInfinity;
            right = float.NegativeInfinity;
            bottom = float.PositiveInfinity;
            top = float.NegativeInfinity;

            if (sprite == null)
            {
                left = x * tileSize;
                right = (x + 1) * tileSize;
                bottom = y * tileSize;
                top = (y + 1) * tileSize;
                return;
            }

            Vector2[] spriteVerts = sprite.vertices;
            if (spriteVerts == null || spriteVerts.Length == 0)
            {
                left = x * tileSize;
                right = (x + 1) * tileSize;
                bottom = y * tileSize;
                top = (y + 1) * tileSize;
                return;
            }

            Bounds bounds = sprite.bounds;
            float cellWidth = bounds.max.x - bounds.min.x;
            float cellOriginX = x * tileSize;
            float cellOriginY = y * tileSize;

            for (int i = 0; i < spriteVerts.Length; i++)
            {
                float offsetX = spriteVerts[i].x - bounds.min.x;
                if (flipX)
                {
                    offsetX = cellWidth - offsetX;
                }

                float worldX = cellOriginX + offsetX;
                float worldY = cellOriginY + (spriteVerts[i].y - bounds.min.y);
                left = Mathf.Min(left, worldX);
                right = Mathf.Max(right, worldX);
                bottom = Mathf.Min(bottom, worldY);
                top = Mathf.Max(top, worldY);
            }
        }

        /// <summary>
        /// True when the sprite mesh spans the full logical tile cell.
        /// </summary>
        public static bool SpansFullTileCell(int x, int y, float tileSize, Sprite sprite, bool flipX)
        {
            GetTileCellBounds(x, y, tileSize, sprite, flipX, out float left, out float right, out float bottom, out float top);
            return Mathf.Abs(left - x * tileSize) < BoundsEpsilon
                && Mathf.Abs(right - (x + 1) * tileSize) < BoundsEpsilon
                && Mathf.Abs(bottom - y * tileSize) < BoundsEpsilon
                && Mathf.Abs(top - (y + 1) * tileSize) < BoundsEpsilon;
        }

        /// <summary>
        /// Appends a ground autotile using full-cell UV mapping for tile-viz <c>blitSprite</c> parity.
        /// Cover layers continue to use tight sprite mesh geometry via <see cref="AppendSprite"/>.
        /// </summary>
        public static void AppendGroundAutotileSprite(
            List<Vector3> vertices,
            List<int> triangles,
            List<Vector2> uvs,
            List<Color> colors,
            int x,
            int y,
            float tileSize,
            Sprite sprite,
            bool flipX,
            Color color,
            float zOffset)
        {
            if (sprite == null)
            {
                return;
            }

            AppendFixedCellQuad(
                vertices,
                triangles,
                uvs,
                colors,
                x,
                y,
                tileSize,
                sprite,
                flipX,
                color,
                zOffset,
                flipY: false,
                rotationDegrees: 0);
        }

        public static void AppendFixedCellQuad(
            List<Vector3> vertices,
            List<int> triangles,
            List<Vector2> uvs,
            List<Color> colors,
            int x,
            int y,
            float tileSize,
            Sprite sprite,
            bool flipX,
            Color color,
            float zOffset,
            bool flipY,
            int rotationDegrees)
        {
            UvCorners uv = GetSpriteUvCorners(sprite, flipX, flipY, rotationDegrees);
            float left = x * tileSize;
            float right = left + tileSize;
            float bottom = y * tileSize;
            float top = bottom + tileSize;

            int start = vertices.Count;
            vertices.Add(new Vector3(left, bottom, zOffset));
            vertices.Add(new Vector3(right, bottom, zOffset));
            vertices.Add(new Vector3(right, top, zOffset));
            vertices.Add(new Vector3(left, top, zOffset));

            uvs.Add(uv.BottomLeft);
            uvs.Add(uv.BottomRight);
            uvs.Add(uv.TopRight);
            uvs.Add(uv.TopLeft);

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

        internal static int NormalizeRotationDegrees(int rotationDegrees)
        {
            int normalized = rotationDegrees % 360;
            if (normalized < 0)
            {
                normalized += 360;
            }

            return normalized switch
            {
                0 or 90 or 180 or 270 => normalized,
                _ => throw new System.ArgumentOutOfRangeException(nameof(rotationDegrees), rotationDegrees, "Visual override rotationDegrees must normalize to 0, 90, 180, or 270.")
            };
        }

        internal static Vector2 TransformUv(Vector2 uv, float uMin, float uMax, float vMin, float vMax, bool flipX, bool flipY, int rotationDegrees)
        {
            float uSpan = uMax - uMin;
            float vSpan = vMax - vMin;
            float u = uSpan == 0f ? 0.5f : (uv.x - uMin) / uSpan;
            float v = vSpan == 0f ? 0.5f : (uv.y - vMin) / vSpan;

            if (flipX)
            {
                u = 1f - u;
            }

            if (flipY)
            {
                v = 1f - v;
            }

            switch (NormalizeRotationDegrees(rotationDegrees))
            {
                case 90:
                    (u, v) = (v, 1f - u);
                    break;
                case 180:
                    u = 1f - u;
                    v = 1f - v;
                    break;
                case 270:
                    (u, v) = (1f - v, u);
                    break;
            }

            return new Vector2(uMin + u * uSpan, vMin + v * vSpan);
        }

        private static UvCorners GetSpriteUvCorners(Sprite sprite, bool flipX, bool flipY, int rotationDegrees)
        {
            Rect rect = sprite.textureRect;
            Texture2D texture = sprite.texture;
            float uMin = rect.x / texture.width;
            float uMax = (rect.x + rect.width) / texture.width;
            float vMin = rect.y / texture.height;
            float vMax = (rect.y + rect.height) / texture.height;

            return new UvCorners(
                TransformUv(new Vector2(uMin, vMin), uMin, uMax, vMin, vMax, flipX, flipY, rotationDegrees),
                TransformUv(new Vector2(uMax, vMin), uMin, uMax, vMin, vMax, flipX, flipY, rotationDegrees),
                TransformUv(new Vector2(uMax, vMax), uMin, uMax, vMin, vMax, flipX, flipY, rotationDegrees),
                TransformUv(new Vector2(uMin, vMax), uMin, uMax, vMin, vMax, flipX, flipY, rotationDegrees));
        }

        private static void GetSpriteUvBounds(Vector2[] spriteUvs, out float uMin, out float uMax, out float vMin, out float vMax)
        {
            uMin = float.PositiveInfinity;
            uMax = float.NegativeInfinity;
            vMin = float.PositiveInfinity;
            vMax = float.NegativeInfinity;
            for (int i = 0; i < spriteUvs.Length; i++)
            {
                uMin = Mathf.Min(uMin, spriteUvs[i].x);
                uMax = Mathf.Max(uMax, spriteUvs[i].x);
                vMin = Mathf.Min(vMin, spriteUvs[i].y);
                vMax = Mathf.Max(vMax, spriteUvs[i].y);
            }
        }

        private readonly struct UvCorners
        {
            public UvCorners(Vector2 bottomLeft, Vector2 bottomRight, Vector2 topRight, Vector2 topLeft)
            {
                BottomLeft = bottomLeft;
                BottomRight = bottomRight;
                TopRight = topRight;
                TopLeft = topLeft;
            }

            public Vector2 BottomLeft { get; }
            public Vector2 BottomRight { get; }
            public Vector2 TopRight { get; }
            public Vector2 TopLeft { get; }
        }
    }
}
