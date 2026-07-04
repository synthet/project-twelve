using System.Collections.Generic;
using UnityEngine;

namespace ProjectTwelve.Visual.Tiles
{
    /// <summary>
    /// Deterministic autotile sprite resolver for chunk meshes.
    /// </summary>
    public static class AutotileResolver
    {
        private enum MatchPass
        {
            Direct,
            FlipRows,
            FlipColumns
        }

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
            int index = FindRuleIndex(rules, mask, MatchPass.Direct);
            if (index >= 0)
            {
                flipX = false;
                return GetSpriteForRule(tileset, rules, index);
            }

            index = FindRuleIndex(rules, mask, MatchPass.FlipRows);
            if (index >= 0)
            {
                flipX = true;
                return GetSpriteForRule(tileset, rules, index);
            }

            index = FindRuleIndex(rules, mask, MatchPass.FlipColumns);
            if (index >= 0)
            {
                flipX = true;
                return GetSpriteForRule(tileset, rules, index);
            }

            return GetFallbackSprite(tileset);
        }

        private static int FindRuleIndex(IReadOnlyList<AutotileRule> rules, int[,] mask, MatchPass pass)
        {
            List<AutotileRule> matches = new List<AutotileRule>();
            for (int i = 0; i < rules.Count; i++)
            {
                if (RuleMatches(rules[i], mask, pass))
                {
                    matches.Add(rules[i]);
                }
            }

            if (matches.Count == 0)
            {
                return -1;
            }

            if (matches.Count == 1)
            {
                return IndexOfRule(rules, matches[0]);
            }

            if (AllSharePattern(matches))
            {
                bool flipForHash = pass != MatchPass.Direct;
                AutotileRule selected = PickDeterministic(matches, mask, flipForHash);
                return IndexOfRule(rules, selected);
            }

            // Distinct patterns that both match: first rule in table order wins (vendor style).
            for (int i = 0; i < rules.Count; i++)
            {
                if (RuleMatches(rules[i], mask, pass))
                {
                    return i;
                }
            }

            return -1;
        }

        private static bool RuleMatches(AutotileRule rule, int[,] mask, MatchPass pass)
        {
            switch (pass)
            {
                case MatchPass.Direct:
                    return rule.Matches(mask, flipInput: false);
                case MatchPass.FlipRows:
                    return rule.Matches(mask, flipInput: true);
                case MatchPass.FlipColumns:
                    return rule.MatchesColumns(mask, flipColumns: true);
                default:
                    return false;
            }
        }

        private static int IndexOfRule(IReadOnlyList<AutotileRule> rules, AutotileRule rule)
        {
            for (int i = 0; i < rules.Count; i++)
            {
                if (ReferenceEquals(rules[i], rule))
                {
                    return i;
                }
            }

            return -1;
        }

        private static bool AllSharePattern(IReadOnlyList<AutotileRule> matches)
        {
            int[,] first = matches[0].ToMask();
            for (int i = 1; i < matches.Count; i++)
            {
                if (!MasksEqual(first, matches[i].ToMask()))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool MasksEqual(int[,] left, int[,] right)
        {
            if (left == null || right == null
                || left.GetLength(0) != right.GetLength(0)
                || left.GetLength(1) != right.GetLength(1))
            {
                return false;
            }

            for (int x = 0; x < left.GetLength(0); x++)
            {
                for (int y = 0; y < left.GetLength(1); y++)
                {
                    if (left[x, y] != right[x, y])
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private static AutotileRule PickDeterministic(List<AutotileRule> matches, int[,] mask, bool flipForHash)
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

            int pick = HashMask(mask, flipForHash) % totalWeight;
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

        private static int HashMask(int[,] mask, bool flipForHash)
        {
            unchecked
            {
                int hash = flipForHash ? 17 : 31;
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
            string fallbackId = tileset.Sprites.Count == AutotileRuleTables.GroundSpriteCount
                ? AutotileRuleTables.FallbackSpriteId
                : "0";

            foreach (string candidate in new[] { fallbackId, AutotileRuleTables.FallbackSpriteId, "0" })
            {
                for (int i = 0; i < tileset.Sprites.Count; i++)
                {
                    Sprite sprite = tileset.Sprites[i];
                    if (sprite != null && sprite.name == candidate)
                    {
                        return sprite;
                    }
                }
            }

            return tileset.Sprites.Count > 0 ? tileset.Sprites[0] : null;
        }
    }
}
