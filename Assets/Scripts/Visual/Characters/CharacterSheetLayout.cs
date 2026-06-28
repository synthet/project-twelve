using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectTwelve.Visual.Characters
{
    /// <summary>
    /// Hero sheet frame layout (see docs/VISUAL_BEHAVIOR_SPEC.md).
    /// </summary>
    public static class CharacterSheetLayout
    {
        public const int Width = 576;
        public const int Height = 928;
        public const int CellSize = 64;
        public const float PixelsPerUnit = 16f;
        public static readonly Vector2 Pivot = new Vector2(0.5f, 0.125f);

        private static readonly string[] RowClips =
        {
            "Roll", "Death", "Block", "Fire", "Shot", "Slash", "Jab", "Push",
            "Jump", "Climb", "Crawl", "Run", "Ready", "Idle"
        };

        /// <summary>
        /// Returns frame keys and sprite rects for the combined hero sheet.
        /// </summary>
        public static Dictionary<string, Rect> BuildFrameRects()
        {
            Dictionary<string, Rect> frames = new Dictionary<string, Rect>();
            for (int row = 0; row < RowClips.Length; row++)
            {
                for (int frame = 0; frame < 9; frame++)
                {
                    string key = $"{RowClips[row]}_{frame}";
                    frames[key] = new Rect(frame * CellSize, row * CellSize, CellSize, CellSize);
                }
            }

            return frames;
        }
    }
}
