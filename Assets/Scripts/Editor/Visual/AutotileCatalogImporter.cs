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
            if (!TryLoadTilesets(tilesRoot, "Ground", out List<AutotileTileset> groundTilesets)
                || !TryLoadTilesets(tilesRoot, "Cover", out List<AutotileTileset> coverTilesets))
            {
                return;
            }

            catalog.SetGroundTilesets(groundTilesets);
            catalog.SetCoverTilesets(coverTilesets);

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

        private static bool TryLoadTilesets(
            string tilesRoot,
            string subFolder,
            out List<AutotileTileset> tilesets)
        {
            tilesets = new List<AutotileTileset>();
            string folder = Path.Combine(tilesRoot, subFolder).Replace('\\', '/');
            if (!Directory.Exists(folder))
            {
                return true;
            }

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

                if (subFolder == "Ground" && sprites.Count != AutotileRuleTables.GroundSpriteCount)
                {
                    Debug.LogError(
                        $"AutotileCatalogImporter: {texture.name} has {sprites.Count} sprites " +
                        $"(expected {AutotileRuleTables.GroundSpriteCount} for ground autotile sheets). Skipping import.");
                    return false;
                }

                Debug.Log($"AutotileCatalogImporter: {texture.name} ({subFolder}) -> {sprites.Count} sprites.");
                tilesets.Add(new AutotileTileset(texture.name, texture, sprites));
            }

            return true;
        }
    }
}

#endif
