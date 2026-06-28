using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectTwelve.Visual.Tiles
{
    /// <summary>
    /// Named tileset with sprites and autotile rules resolved by sprite count.
    /// </summary>
    [Serializable]
    public sealed class AutotileTileset
    {
        [SerializeField] private string tilesetName;
        [SerializeField] private Texture2D texture;
        [SerializeField] private List<Sprite> sprites = new List<Sprite>();
        [SerializeField] private List<AutotileRule> customRules = new List<AutotileRule>();

        public string Name => tilesetName;
        public Texture2D Texture => texture;
        public IReadOnlyList<Sprite> Sprites => sprites;

        public AutotileTileset()
        {
        }

        public AutotileTileset(string name, Texture2D texture, List<Sprite> sprites)
        {
            tilesetName = name;
            this.texture = texture;
            this.sprites = sprites ?? new List<Sprite>();
        }

        /// <summary>
        /// Active rules: custom if set, otherwise default ground/cover tables.
        /// </summary>
        public IReadOnlyList<AutotileRule> Rules =>
            customRules != null && customRules.Count > 0
                ? customRules
                : AutotileRuleTables.GetRulesForSpriteCount(Sprites.Count);

        public void SetCustomRules(List<AutotileRule> rules)
        {
            customRules = rules ?? new List<AutotileRule>();
        }
    }
}
