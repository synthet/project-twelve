using System;
using UnityEngine;

namespace ProjectTwelve.Visual.Tiles
{
    /// <summary>
    /// Autotile rule: neighbor pattern, sprite id, and optional weight.
    /// </summary>
    [Serializable]
    public sealed class AutotileRule
    {
        [SerializeField] private string spriteId = "0";
        [SerializeField] private int weight = 1;
        [SerializeField] private int[] pattern = new int[9];

        public string SpriteId => spriteId;
        public int Weight => weight;

        public AutotileRule()
        {
        }

        public AutotileRule(string spriteId, int[,] mask, int weight = 1)
        {
            this.spriteId = spriteId;
            this.weight = weight;
            pattern = FlattenMask(mask);
        }

        /// <summary>
        /// Returns whether the rule pattern matches the neighbor mask.
        /// </summary>
        /// <param name="mask">3×3 neighbor mask.</param>
        /// <param name="flipInput">When true, horizontally flip the input mask before compare.</param>
        public bool Matches(int[,] mask, bool flipInput)
        {
            if (mask == null || mask.GetLength(0) != 3 || mask.GetLength(1) != 3)
            {
                return false;
            }

            for (int x = 0; x < 3; x++)
            {
                for (int y = 0; y < 3; y++)
                {
                    int my = flipInput ? 2 - y : y;
                    if (pattern[x + y * 3] != mask[x, my])
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Returns whether the rule pattern matches when west/east columns are mirrored.
        /// </summary>
        /// <param name="mask">3×3 neighbor mask.</param>
        /// <param name="flipColumns">When true, mirror mask columns (x ↔ 2−x) before compare.</param>
        public bool MatchesColumns(int[,] mask, bool flipColumns)
        {
            if (mask == null || mask.GetLength(0) != 3 || mask.GetLength(1) != 3)
            {
                return false;
            }

            for (int x = 0; x < 3; x++)
            {
                for (int y = 0; y < 3; y++)
                {
                    int mx = flipColumns ? 2 - x : x;
                    if (pattern[x + y * 3] != mask[mx, y])
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        public int[,] ToMask()
        {
            int[,] mask = new int[3, 3];
            for (int x = 0; x < 3; x++)
            {
                for (int y = 0; y < 3; y++)
                {
                    mask[x, y] = pattern[x + y * 3];
                }
            }

            return mask;
        }

        private static int[] FlattenMask(int[,] mask)
        {
            int[] flat = new int[9];
            for (int x = 0; x < 3; x++)
            {
                for (int y = 0; y < 3; y++)
                {
                    flat[x + y * 3] = mask[x, y];
                }
            }

            return flat;
        }
    }
}
