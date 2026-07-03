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
            float zOffset)
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
                AppendFixedCellQuad(vertices, triangles, uvs, colors, x, y, tileSize, sprite, flipX, color, zOffset);
                return;
            }

            Bounds bounds = sprite.bounds;
            Vector2 pivotWorld = new Vector2(
                x * tileSize - bounds.min.x,
                y * tileSize - bounds.min.y);

            float uMin = float.PositiveInfinity;
            float uMax = float.NegativeInfinity;
            for (int i = 0; i < spriteUvs.Length; i++)
            {
                uMin = Mathf.Min(uMin, spriteUvs[i].x);
                uMax = Mathf.Max(uMax, spriteUvs[i].x);
            }

            int start = vertices.Count;
            for (int i = 0; i < spriteVerts.Length; i++)
            {
                float localX = flipX ? -spriteVerts[i].x : spriteVerts[i].x;
                float localY = spriteVerts[i].y;
                vertices.Add(new Vector3(pivotWorld.x + localX, pivotWorld.y + localY, zOffset));

                Vector2 uv = spriteUvs[i];
                if (flipX)
                {
                    uv.x = uMin + uMax - uv.x;
                }

                uvs.Add(uv);
                colors.Add(color);
            }

            for (int i = 0; i < spriteTriangles.Length; i++)
            {
                triangles.Add(start + spriteTriangles[i]);
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
            Vector2 pivotWorld = new Vector2(
                x * tileSize - bounds.min.x,
                y * tileSize - bounds.min.y);

            for (int i = 0; i < spriteVerts.Length; i++)
            {
                float localX = flipX ? -spriteVerts[i].x : spriteVerts[i].x;
                float worldX = pivotWorld.x + localX;
                float worldY = pivotWorld.y + spriteVerts[i].y;
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

        private static void AppendFixedCellQuad(
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
            Rect uv = GetSpriteUv(sprite, flipX);
            float left = x * tileSize;
            float right = left + tileSize;
            float bottom = y * tileSize;
            float top = bottom + tileSize;

            int start = vertices.Count;
            vertices.Add(new Vector3(left, bottom, zOffset));
            vertices.Add(new Vector3(right, bottom, zOffset));
            vertices.Add(new Vector3(right, top, zOffset));
            vertices.Add(new Vector3(left, top, zOffset));

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

        private static Rect GetSpriteUv(Sprite sprite, bool flipX)
        {
            Rect rect = sprite.textureRect;
            Texture2D texture = sprite.texture;
            float uMin = rect.x / texture.width;
            float uMax = (rect.x + rect.width) / texture.width;
            float vMin = rect.y / texture.height;
            float vMax = (rect.y + rect.height) / texture.height;

            if (flipX)
            {
                float swap = uMin;
                uMin = uMax;
                uMax = swap;
            }

            return new Rect(uMin, vMin, uMax - uMin, vMax - vMin);
        }
    }
}
