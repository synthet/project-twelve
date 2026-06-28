#if UNITY_EDITOR

using UnityEditor;

namespace ProjectTwelve.Editor.Visual
{
    /// <summary>
    /// Batch-mode entry point for regenerating visual catalogs in the assets submodule.
    /// </summary>
    public static class VisualCatalogBatchImporter
    {
        public static void ImportAllFromCommandLine()
        {
            AutotileCatalogImporter.ImportFromLocalSource();
            CharacterLayerCatalogImporter.ImportFromLocalSource();
            MonsterVisualCatalogImporter.ImportFromLocalSource();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorApplication.Exit(0);
        }
    }
}

#endif
