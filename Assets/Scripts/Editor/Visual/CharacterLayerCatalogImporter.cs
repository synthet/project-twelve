#if UNITY_EDITOR

using System;

using System.Collections.Generic;

using System.IO;

using System.Linq;

using ProjectTwelve.Visual.Characters;

using UnityEditor;

using UnityEngine;



namespace ProjectTwelve.Editor.Visual

{

    /// <summary>

    /// Builds character layer catalogs from locally imported sprite source folders.

    /// </summary>

    public static class CharacterLayerCatalogImporter

    {

        private const string OutputPath = "Assets/_Licensed/Settings/Visual/CharacterLayerCatalog.asset";



        [MenuItem("ProjectTwelve/Visual/Import Character Layer Catalog from Local Source")]

        public static void ImportFromLocalSource()

        {

            string spritesRoot = LocalImportConfig.HeroSpritesRoot;

            if (string.IsNullOrEmpty(spritesRoot) || !Directory.Exists(spritesRoot))

            {

                Debug.LogWarning(

                    "CharacterLayerCatalogImporter: sprite source path missing. Initialize Assets/_Licensed submodule or set config/visual-import.local-only.txt.");

                return;

            }



            CharacterLayerCatalog catalog = LoadOrCreateCatalog();

            catalog.SetLayers(BuildLayers(spritesRoot, LocalImportConfig.HeroExtraLayerRoots));

            EditorUtility.SetDirty(catalog);

            AssetDatabase.SaveAssets();

            Debug.Log($"CharacterLayerCatalogImporter: updated {OutputPath} ({catalog.Layers.Count} layers).");

        }



        private static CharacterLayerCatalog LoadOrCreateCatalog()

        {

            CharacterLayerCatalog catalog = AssetDatabase.LoadAssetAtPath<CharacterLayerCatalog>(OutputPath);

            if (catalog != null)

            {

                return catalog;

            }



            string directory = Path.GetDirectoryName(OutputPath);

            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))

            {

                Directory.CreateDirectory(directory);

            }



            catalog = ScriptableObject.CreateInstance<CharacterLayerCatalog>();

            AssetDatabase.CreateAsset(catalog, OutputPath);

            return catalog;

        }



        private static List<CharacterLayerEntry> BuildLayers(string spritesRoot, IReadOnlyList<string> extraLayerRoots)
        {
            List<CharacterLayerEntry> layers = new List<CharacterLayerEntry>();
            string[] layerDirs = Directory.GetDirectories(spritesRoot)
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToArray();
            for (int i = 0; i < layerDirs.Length; i++)
            {
                TryAddLayerFromDirectory(layerDirs[i], layers);
            }

            if (extraLayerRoots != null)
            {
                for (int i = 0; i < extraLayerRoots.Count; i++)
                {
                    string extraRoot = extraLayerRoots[i];
                    if (string.IsNullOrEmpty(extraRoot) || !Directory.Exists(extraRoot))
                    {
                        continue;
                    }

                    TryAddLayerFromDirectory(extraRoot, layers);
                }
            }

            return layers;
        }

        private static void TryAddLayerFromDirectory(string layerPath, List<CharacterLayerEntry> layers)
        {
            layerPath = layerPath.Replace('\\', '/');
            string layerName = Path.GetFileName(layerPath.TrimEnd('/'));
            List<Texture2D> textures = Directory.GetFiles(layerPath, "*.png", SearchOption.TopDirectoryOnly)
                .OrderBy(path => path, StringComparer.Ordinal)
                .Select(path => AssetDatabase.LoadAssetAtPath<Texture2D>(path.Replace('\\', '/')))
                .Where(texture => texture != null)
                .ToList();

            if (textures.Count == 0)
            {
                return;
            }

            for (int i = 0; i < layers.Count; i++)
            {
                if (layers[i].LayerName == layerName)
                {
                    return;
                }
            }

            layers.Add(CharacterLayerEntry.Create(layerName, textures));
        }

    }

}

#endif

