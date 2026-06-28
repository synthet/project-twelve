#if UNITY_EDITOR

using System.Collections.Generic;
using System.IO;
using System.Linq;
using ProjectTwelve.Visual.Tiles;
using UnityEditor;
using UnityEngine;

namespace ProjectTwelve.Editor.Visual
{
    /// <summary>
    /// Builds project autotile catalogs from locally imported tile source folders.
    /// </summary>
    public static class AutotileCatalogImporter
    {
        private const string OutputPath = "Assets/_Licensed/Settings/Visual/AutotileCatalog.asset";

        [MenuItem("ProjectTwelve/Visual/Import Autotile Catalog from Local Source")]
        public static void ImportFromLocalSource()
        {
            string tilesRoot = LocalImportConfig.TileSpritesRoot;
            if (string.IsNullOrEmpty(tilesRoot) || !Directory.Exists(tilesRoot))
            {
                Debug.LogWarning(
                    "AutotileCatalogImporter: tile source path missing. Initialize Assets/_Licensed submodule or set config/visual-import.local-only.txt.");
                return;
            }

            AutotileCatalog catalog = LoadOrCreateCatalog();
            catalog.SetGroundTilesets(LoadTilesets(tilesRoot, "Ground"));
            catalog.SetCoverTilesets(LoadTilesets(tilesRoot, "Cover"));

            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            Debug.Log($"AutotileCatalogImporter: updated {OutputPath} " +
                      $"({catalog.GroundTilesets.Count} ground, {catalog.CoverTilesets.Count} cover tilesets).");
        }

        private static AutotileCatalog LoadOrCreateCatalog()
        {
            AutotileCatalog catalog = AssetDatabase.LoadAssetAtPath<AutotileCatalog>(OutputPath);
            if (catalog != null)
            {
                return catalog;
            }

            string directory = Path.GetDirectoryName(OutputPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            catalog = ScriptableObject.CreateInstance<AutotileCatalog>();
            AssetDatabase.CreateAsset(catalog, OutputPath);
            return catalog;
        }

        private static List<AutotileTileset> LoadTilesets(string tilesRoot, string subFolder)
        {
            string folder = Path.Combine(tilesRoot, subFolder).Replace('\\', '/');
            if (!Directory.Exists(folder))
            {
                return new List<AutotileTileset>();
            }

            List<AutotileTileset> tilesets = new List<AutotileTileset>();
            string[] pngFiles = Directory.GetFiles(folder, "*.png", SearchOption.TopDirectoryOnly);
            for (int i = 0; i < pngFiles.Length; i++)
            {
                string assetPath = pngFiles[i].Replace('\\', '/');
                Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
                if (texture == null)
                {
                    continue;
                }

                List<Sprite> sprites = AssetDatabase.LoadAllAssetsAtPath(assetPath)
                    .OfType<Sprite>()
                    .OrderBy(sprite => int.TryParse(sprite.name, out int index) ? index : 0)
                    .ToList();

                tilesets.Add(new AutotileTileset(texture.name, texture, sprites));
            }

            return tilesets;
        }
    }
}

#endif
