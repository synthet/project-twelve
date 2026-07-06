using System.Collections.Generic;
using UnityEngine;

namespace ProjectTwelve.Visual.Tiles
{
    /// <summary>
    /// Canonical 128×64 / 32-sprite ground sheet layout per PixelFantasy row-major id ordering.
    /// </summary>
    public static class AutotileGroundSheetLayout
    {
        public const int SheetWidthPixels = 128;
        public const int SheetHeightPixels = 64;
        public const int CellSizePixels = 16;
        public const int Columns = 8;
        public const int Rows = 4;
        public const int SpriteCount = 32;
        public const float ExpectedPixelsPerUnit = 16f;

        /// <summary>
        /// Returns the expected texture-space rect for a ground sprite id (Unity bottom-left origin).
        /// </summary>
        public static Rect GetExpectedTextureRect(int spriteId, int textureHeightPixels)
        {
            int col = spriteId % Columns;
            int rowFromTop = spriteId / Columns;
            float x = col * CellSizePixels;
            float y = textureHeightPixels - ((rowFromTop + 1) * CellSizePixels);
            return new Rect(x, y, CellSizePixels, CellSizePixels);
        }

        /// <summary>
        /// Validates one ground sprite against the canonical sheet layout.
        /// </summary>
        public static bool ValidateSpriteRect(
            Sprite sprite,
            Texture2D texture,
            int expectedSpriteId,
            ICollection<string> errors)
        {
            if (sprite == null)
            {
                errors.Add($"sprite {expectedSpriteId}: missing");
                return false;
            }

            bool valid = true;
            if (sprite.name != expectedSpriteId.ToString())
            {
                errors.Add($"{texture.name}/{sprite.name}: expected name \"{expectedSpriteId}\".");
                valid = false;
            }

            if (!Mathf.Approximately(sprite.pixelsPerUnit, ExpectedPixelsPerUnit))
            {
                errors.Add(
                    $"{texture.name}/{sprite.name}: PPU {sprite.pixelsPerUnit} (expected {ExpectedPixelsPerUnit}).");
                valid = false;
            }

            Rect expected = GetExpectedTextureRect(expectedSpriteId, texture.height);
            Rect actual = sprite.textureRect;
            if (!Mathf.Approximately(actual.x, expected.x)
                || !Mathf.Approximately(actual.y, expected.y)
                || !Mathf.Approximately(actual.width, expected.width)
                || !Mathf.Approximately(actual.height, expected.height))
            {
                errors.Add(
                    $"{texture.name}/{sprite.name}: rect {actual} expected {expected}.");
                valid = false;
            }

            return valid;
        }

        /// <summary>
        /// Validates a full 32-sprite ground sheet texture and sprite list.
        /// </summary>
        public static bool ValidateGroundSheet(string textureName, Texture2D texture, IReadOnlyList<Sprite> sprites, ICollection<string> errors)
        {
            bool valid = true;
            if (texture == null)
            {
                errors.Add($"{textureName}: texture missing.");
                return false;
            }

            if (texture.width != SheetWidthPixels || texture.height != SheetHeightPixels)
            {
                errors.Add(
                    $"{textureName}: size {texture.width}×{texture.height} (expected {SheetWidthPixels}×{SheetHeightPixels}).");
                valid = false;
            }

            if (sprites == null || sprites.Count != SpriteCount)
            {
                errors.Add($"{textureName}: sprite count {sprites?.Count ?? 0} (expected {SpriteCount}).");
                return false;
            }

            for (int id = 0; id < SpriteCount; id++)
            {
                if (!ValidateSpriteRect(sprites[id], texture, id, errors))
                {
                    valid = false;
                }
            }

            return valid;
        }
    }
}
