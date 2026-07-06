using System.Collections.Generic;
using ProjectTwelve.Visual.Tiles;
using UnityEditor;
using UnityEngine;

namespace ProjectTwelve.Editor.Visual
{
    /// <summary>
    /// Editor validation for 128×64 / 32-sprite ground autotile sheets in the autotile catalog.
    /// </summary>
    public static class AutotileGroundSheetValidator
    {
        private const string CatalogPath = "Assets/_Licensed/Settings/Visual/AutotileCatalog.asset";

        [MenuItem("ProjectTwelve/Visual/Validate Ground Autotile Sheets")]
        public static void ValidateCatalogGroundSheets()
        {
            AutotileCatalog catalog = AssetDatabase.LoadAssetAtPath<AutotileCatalog>(CatalogPath);
            if (catalog == null)
            {
                Debug.LogError($"AutotileGroundSheetValidator: missing catalog at {CatalogPath}.");
                return;
            }

            var errors = new List<string>();
            int sheetCount = 0;
            foreach (AutotileTileset tileset in catalog.GroundTilesets)
            {
                if (tileset?.Sprites == null || tileset.Sprites.Count != AutotileGroundSheetLayout.SpriteCount)
                {
                    continue;
                }

                sheetCount++;
                string texturePath = AssetDatabase.GetAssetPath(tileset.Texture);
                TextureImporter importer = AssetImporter.GetAtPath(texturePath) as TextureImporter;
                if (importer != null)
                {
                    if (importer.textureCompression != TextureImporterCompression.Uncompressed)
                    {
                        errors.Add($"{tileset.Name}: texture compression is {importer.textureCompression} (expected Uncompressed).");
                    }

                    if (importer.filterMode != FilterMode.Point)
                    {
                        errors.Add($"{tileset.Name}: filter mode is {importer.filterMode} (expected Point).");
                    }
                }

                AutotileGroundSheetLayout.ValidateGroundSheet(tileset.Name, tileset.Texture, tileset.Sprites, errors);
            }

            if (sheetCount == 0)
            {
                Debug.LogWarning("AutotileGroundSheetValidator: no 32-sprite ground tilesets found in catalog.");
                return;
            }

            if (errors.Count == 0)
            {
                Debug.Log($"AutotileGroundSheetValidator: {sheetCount} ground sheet(s) passed layout checks.");
                return;
            }

            foreach (string error in errors)
            {
                Debug.LogError($"AutotileGroundSheetValidator: {error}");
            }
        }
    }
}
