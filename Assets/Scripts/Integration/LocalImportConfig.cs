using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// Reads visual import paths from the assets submodule config, with optional local override.
/// </summary>
public static class LocalImportConfig
{
    private const string LocalOverrideRelativePath = "config/visual-import.local-only.txt";
    private const string SubmoduleConfigRelativePath = "Assets/_Licensed/config/visual-import.txt";

    private static Dictionary<string, string> cache;
    private static bool loadAttempted;

    public static string AvatarPrefabPath => GetValue("avatar_prefab");

    public static string HeroSpritesRoot => GetValue("hero_sprites_root");

    public static string TileSpritesRoot => GetValue("tile_sprites_root");

    public static string MonsterPrefabsRoot => GetValue("monster_prefabs_root");

    public static IReadOnlyList<string> HeroExtraLayerRoots => GetListValues("hero_extra_layer", useDefaultsWhenEmpty: false);

    public static IReadOnlyList<string> DemoScriptTypeNames => GetListValues("strip_demo_script_type");

    private static string GetValue(string key)
    {
        EnsureLoaded();
        return cache != null && cache.TryGetValue(key, out string value) ? value : string.Empty;
    }

    private static IReadOnlyList<string> GetListValues(string keyPrefix, bool useDefaultsWhenEmpty = true)
    {
        EnsureLoaded();
        List<string> values = new List<string>();
        if (cache == null)
        {
            return useDefaultsWhenEmpty ? DefaultDemoScriptTypeNames() : values;
        }

        foreach (KeyValuePair<string, string> entry in cache)
        {
            if (entry.Key == keyPrefix || entry.Key.StartsWith(keyPrefix + ".", StringComparison.Ordinal))
            {
                if (!string.IsNullOrWhiteSpace(entry.Value))
                {
                    values.Add(entry.Value.Trim());
                }
            }
        }

        if (values.Count == 0 && useDefaultsWhenEmpty)
        {
            return DefaultDemoScriptTypeNames();
        }

        return values;
    }

    private static List<string> DefaultDemoScriptTypeNames()
    {
        return new List<string>
        {
            "CharacterControls",
            "CharacterController2D",
            "CharacterAnimation",
            "CharacterBuilder",
            "Character",
        };
    }

    private static void EnsureLoaded()
    {
        if (loadAttempted)
        {
            return;
        }

        loadAttempted = true;
        cache = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        string repositoryRoot = GetRepositoryRoot();
        string localOverridePath = Path.Combine(repositoryRoot, LocalOverrideRelativePath);
        if (File.Exists(localOverridePath))
        {
            MergeConfigFile(localOverridePath);
            return;
        }

        string submoduleConfigPath = Path.Combine(repositoryRoot, SubmoduleConfigRelativePath);
        if (File.Exists(submoduleConfigPath))
        {
            MergeConfigFile(submoduleConfigPath);
        }
    }

    private static void MergeConfigFile(string path)
    {
        foreach (string line in File.ReadAllLines(path))
        {
            string trimmed = line.Trim();
            if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("#", StringComparison.Ordinal))
            {
                continue;
            }

            int separator = trimmed.IndexOf('=');
            if (separator <= 0)
            {
                continue;
            }

            string key = trimmed.Substring(0, separator).Trim();
            string value = trimmed.Substring(separator + 1).Trim();
            if (!string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(value))
            {
                cache[key] = value.Replace('\\', '/');
            }
        }
    }

    private static string GetRepositoryRoot()
    {
        string dataPath = Application.dataPath;
        if (string.IsNullOrEmpty(dataPath))
        {
            return Directory.GetCurrentDirectory();
        }

        return Path.GetFullPath(Path.Combine(dataPath, ".."));
    }
}
