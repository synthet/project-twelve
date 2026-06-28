using System.Collections.Generic;
using UnityEngine;

namespace ProjectTwelve.Visual.Characters
{
    /// <summary>
    /// Catalog of hero equipment layer textures for runtime composition.
    /// </summary>
    [CreateAssetMenu(fileName = "CharacterLayerCatalog", menuName = "ProjectTwelve/Visual/Character Layer Catalog")]
    public sealed class CharacterLayerCatalog : ScriptableObject
    {
        [SerializeField] private List<CharacterLayerEntry> layers = new List<CharacterLayerEntry>();

        public IReadOnlyList<CharacterLayerEntry> Layers => layers;

        /// <summary>
        /// Finds a layer entry by name.
        /// </summary>
        public bool TryGetLayer(string layerName, out CharacterLayerEntry entry)
        {
            for (int i = 0; i < layers.Count; i++)
            {
                if (layers[i] != null && layers[i].LayerName == layerName)
                {
                    entry = layers[i];
                    return true;
                }
            }

            entry = null;
            return false;
        }

        /// <summary>Editor/import helper to replace layer entries.</summary>
        public void SetLayers(List<CharacterLayerEntry> newLayers)
        {
            layers = newLayers ?? new List<CharacterLayerEntry>();
        }
    }
}
