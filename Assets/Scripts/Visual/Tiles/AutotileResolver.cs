using System.Collections.Generic;
using UnityEngine;

namespace ProjectTwelve.Visual.Tiles
{
    /// <summary>
    /// Deterministic autotile sprite resolver for chunk meshes.
    /// </summary>
    public static class AutotileResolver
    {
        /// <summary>
        /// Resolves the sprite and horizontal flip for a tileset and neighbor mask.
        /// </summary>
        public static Sprite ResolveSprite(AutotileTileset tileset, int[,] mask, out bool flipX)
        {
            flipX = false;
            if (tileset?.Sprites == null || tileset.Sprites.Count == 0 || mask == null)
            {
                return null;
            }

            if (tileset.Sprites.Count == 1)
            {
                return tileset.Sprites[0];
            }

            IReadOnlyList<AutotileRule> rules = tileset.Rules;
            int index = FindRuleIndex(rules, mask, flipX: false);
            if (index >= 0)
            {
                flipX = false;
                return GetSpriteForRule(tileset, rules, index);
            }

            index = FindRuleIndex(rules, mask, flipX: true);
            if (index >= 0)
            {
                flipX = true;
                return GetSpriteForRule(tileset, rules, index);
            }

            return GetFallbackSprite(tileset);
        }

        private static int FindRuleIndex(IReadOnlyList<AutotileRule> rules, int[,] mask, bool flipX)
        {
            List<AutotileRule> matches = new List<AutotileRule>();
            for (int i = 0; i < rules.Count; i++)
            {
                if (rules[i].Matches(mask, flipX))
                {
                    matches.Add(rules[i]);
                }
            }

            if (matches.Count == 0)
            {
                return -1;
            }

            AutotileRule selected = PickDeterministic(matches, mask, flipX);
            for (int i = 0; i < rules.Count; i++)
            {
                if (ReferenceEquals(rules[i], selected))
                {
                    return i;
                }
            }

            return -1;
        }

        private static AutotileRule PickDeterministic(List<AutotileRule> matches, int[,] mask, bool flipX)
        {
            if (matches.Count == 1)
            {
                return matches[0];
            }

            int totalWeight = 0;
            for (int i = 0; i < matches.Count; i++)
            {
                totalWeight += Mathf.Max(1, matches[i].Weight);
            }

            int pick = HashMask(mask, flipX) % totalWeight;
            int state = 0;
            for (int i = 0; i < matches.Count; i++)
            {
                state += Mathf.Max(1, matches[i].Weight);
                if (pick < state)
                {
                    return matches[i];
                }
            }

            return matches[0];
        }

        private static int HashMask(int[,] mask, bool flipX)
        {
            unchecked
            {
                int hash = flipX ? 17 : 31;
                for (int x = 0; x < mask.GetLength(0); x++)
                {
                    for (int y = 0; y < mask.GetLength(1); y++)
                    {
                        hash = (hash * 31) + mask[x, y];
                    }
                }

                return Mathf.Abs(hash);
            }
        }

        private static Sprite GetSpriteForRule(
            AutotileTileset tileset,
            IReadOnlyList<AutotileRule> rules,
            int index)
        {
            string spriteName = rules[index].SpriteId;
            for (int i = 0; i < tileset.Sprites.Count; i++)
            {
                Sprite sprite = tileset.Sprites[i];
                if (sprite != null && sprite.name == spriteName)
                {
                    return sprite;
                }
            }

            return GetFallbackSprite(tileset);
        }

        private static Sprite GetFallbackSprite(AutotileTileset tileset)
        {
            for (int i = 0; i < tileset.Sprites.Count; i++)
            {
                Sprite sprite = tileset.Sprites[i];
                if (sprite != null && sprite.name == AutotileRuleTables.FallbackSpriteId)
                {
                    return sprite;
                }
            }

            return tileset.Sprites.Count > 0 ? tileset.Sprites[0] : null;
        }
    }
}
