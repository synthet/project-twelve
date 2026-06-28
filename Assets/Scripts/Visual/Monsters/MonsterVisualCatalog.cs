using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectTwelve.Visual.Monsters
{
    /// <summary>
    /// Maps stable monster IDs to local prefab references.
    /// </summary>
    [CreateAssetMenu(fileName = "MonsterVisualCatalog", menuName = "ProjectTwelve/Visual/Monster Visual Catalog")]
    public sealed class MonsterVisualCatalog : ScriptableObject
    {
        [Serializable]
        public sealed class Entry
        {
            [SerializeField] private string monsterId;
            [SerializeField] private GameObject prefab;

            public string MonsterId => monsterId;
            public GameObject Prefab => prefab;

            internal static Entry Create(string id, GameObject monsterPrefab)
            {
                Entry entry = new Entry();
                entry.monsterId = id;
                entry.prefab = monsterPrefab;
                return entry;
            }
        }

        [SerializeField] private List<Entry> entries = new List<Entry>();

        public IReadOnlyList<Entry> Entries => entries;

        /// <summary>
        /// Finds a prefab by stable monster id.
        /// </summary>
        public bool TryGetPrefab(string monsterId, out GameObject prefab)
        {
            for (int i = 0; i < entries.Count; i++)
            {
                Entry entry = entries[i];
                if (entry != null && entry.MonsterId == monsterId)
                {
                    prefab = entry.Prefab;
                    return prefab != null;
                }
            }

            prefab = null;
            return false;
        }

        /// <summary>Editor/import helper to replace catalog entries.</summary>
        public void SetEntries(List<Entry> newEntries)
        {
            entries = newEntries ?? new List<Entry>();
        }
    }
}
