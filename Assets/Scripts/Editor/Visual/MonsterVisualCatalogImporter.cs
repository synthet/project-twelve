#if UNITY_EDITOR

using System.Collections.Generic;
using System.IO;
using ProjectTwelve.Visual.Monsters;
using UnityEditor;
using UnityEngine;

namespace ProjectTwelve.Editor.Visual
{
    /// <summary>
    /// Builds monster visual catalogs from locally imported monster prefab folders.
    /// </summary>
    public static class MonsterVisualCatalogImporter
    {
        private const string OutputPath = "Assets/_Licensed/Settings/Visual/MonsterVisualCatalog.asset";
        private const string ExcludedPrefabsFolder = "/Common/Prefabs/";

        [MenuItem("ProjectTwelve/Visual/Import Monster Visual Catalog from Local Source")]
        public static void ImportFromLocalSource()
        {
            string monstersRoot = LocalImportConfig.MonsterPrefabsRoot;
            if (string.IsNullOrEmpty(monstersRoot) || !Directory.Exists(monstersRoot))
            {
                Debug.LogWarning(
                    "MonsterVisualCatalogImporter: monster prefab root missing. Initialize Assets/_Licensed submodule or set config/visual-import.local-only.txt.");
                return;
            }

            MonsterVisualCatalog catalog = LoadOrCreateCatalog();
            catalog.SetEntries(LoadEntries(monstersRoot));
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            Debug.Log($"MonsterVisualCatalogImporter: updated {OutputPath} ({catalog.Entries.Count} monsters).");
        }

        private static MonsterVisualCatalog LoadOrCreateCatalog()
        {
            MonsterVisualCatalog catalog = AssetDatabase.LoadAssetAtPath<MonsterVisualCatalog>(OutputPath);
            if (catalog != null)
            {
                return catalog;
            }

            string directory = Path.GetDirectoryName(OutputPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            catalog = ScriptableObject.CreateInstance<MonsterVisualCatalog>();
            AssetDatabase.CreateAsset(catalog, OutputPath);
            return catalog;
        }

        private static List<MonsterVisualCatalog.Entry> LoadEntries(string monstersRoot)
        {
            List<MonsterVisualCatalog.Entry> entries = new List<MonsterVisualCatalog.Entry>();
            HashSet<string> seenIds = new HashSet<string>();
            string[] prefabFiles = Directory.GetFiles(monstersRoot, "*.prefab", SearchOption.AllDirectories);
            for (int i = 0; i < prefabFiles.Length; i++)
            {
                string assetPath = prefabFiles[i].Replace('\\', '/');
                if (assetPath.Contains(ExcludedPrefabsFolder))
                {
                    continue;
                }

                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
                if (prefab == null)
                {
                    continue;
                }

                string monsterId = Path.GetFileNameWithoutExtension(assetPath);
                if (!seenIds.Add(monsterId))
                {
                    Debug.LogWarning(
                        $"MonsterVisualCatalogImporter: duplicate monster id '{monsterId}' at {assetPath}; skipping.");
                    continue;
                }

                entries.Add(MonsterVisualCatalog.Entry.Create(monsterId, prefab));
            }

            entries.Sort((a, b) => string.CompareOrdinal(a.MonsterId, b.MonsterId));
            return entries;
        }
    }
}

#endif
