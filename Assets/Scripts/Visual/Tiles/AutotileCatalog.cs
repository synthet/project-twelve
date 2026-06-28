using System.Collections.Generic;
using UnityEngine;

namespace ProjectTwelve.Visual.Tiles
{
    /// <summary>
    /// Catalog of ground and cover autotile tilesets for chunk rendering.
    /// </summary>
    [CreateAssetMenu(fileName = "AutotileCatalog", menuName = "ProjectTwelve/Visual/Autotile Catalog")]
    public sealed class AutotileCatalog : ScriptableObject
    {
        [SerializeField] private List<AutotileTileset> groundTilesets = new List<AutotileTileset>();
        [SerializeField] private List<AutotileTileset> coverTilesets = new List<AutotileTileset>();

        public IReadOnlyList<AutotileTileset> GroundTilesets => groundTilesets;
        public IReadOnlyList<AutotileTileset> CoverTilesets => coverTilesets;

        /// <summary>
        /// Finds a ground tileset by name.
        /// </summary>
        public bool TryGetGroundTileset(string tilesetName, out AutotileTileset tileset)
        {
            return TryFindTileset(groundTilesets, tilesetName, out tileset);
        }

        /// <summary>
        /// Finds a cover tileset by name.
        /// </summary>
        public bool TryGetCoverTileset(string tilesetName, out AutotileTileset tileset)
        {
            return TryFindTileset(coverTilesets, tilesetName, out tileset);
        }

        /// <summary>Editor/import helper to replace ground tilesets.</summary>
        public void SetGroundTilesets(List<AutotileTileset> tilesets)
        {
            groundTilesets = tilesets ?? new List<AutotileTileset>();
        }

        /// <summary>Editor/import helper to replace cover tilesets.</summary>
        public void SetCoverTilesets(List<AutotileTileset> tilesets)
        {
            coverTilesets = tilesets ?? new List<AutotileTileset>();
        }

        private static bool TryFindTileset(List<AutotileTileset> list, string tilesetName, out AutotileTileset tileset)
        {
            tileset = null;
            if (string.IsNullOrEmpty(tilesetName) || list == null)
            {
                return false;
            }

            for (int i = 0; i < list.Count; i++)
            {
                AutotileTileset candidate = list[i];
                if (candidate != null && candidate.Name == tilesetName)
                {
                    tileset = candidate;
                    return true;
                }
            }

            return false;
        }
    }
}
