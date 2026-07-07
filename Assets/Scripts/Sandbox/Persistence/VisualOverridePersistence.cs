using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ProjectTwelve.Visual.Tiles;
using UnityEngine;

/// <summary>
/// Reads and writes visual override sidecars with legacy format migration.
/// </summary>
public static class VisualOverridePersistence
{
    public const int CurrentVersion = 1;

    public static string Serialize(AutotileVisualOverrideMap map)
    {
        JArray entries = new JArray();
        if (map != null)
        {
            foreach (AutotileVisualOverride entry in map.GetAll())
            {
                entries.Add(EntryToJson(entry));
            }
        }

        JObject document = new JObject
        {
            ["visualOverridesVersion"] = CurrentVersion,
            ["visualOverrides"] = entries,
        };
        return document.ToString(Formatting.Indented);
    }

    public static void DeserializeInto(string json, AutotileVisualOverrideMap map)
    {
        if (map == null)
        {
            throw new ArgumentNullException(nameof(map));
        }

        map.Clear();
        if (string.IsNullOrWhiteSpace(json))
        {
            return;
        }

        JObject document = JObject.Parse(json);
        JToken entriesToken = document["visualOverrides"]
            ?? document["overrides"]
            ?? document["tiles"];

        if (entriesToken is not JArray entries)
        {
            return;
        }

        foreach (JToken token in entries)
        {
            if (token is not JObject obj)
            {
                continue;
            }

            AutotileVisualOverride entry = ParseEntry(obj);
            if (entry == null || string.IsNullOrEmpty(entry.overrideSpriteId))
            {
                continue;
            }

            map.SetOverride(entry);
        }
    }

    public static void WriteToPath(string path, AutotileVisualOverrideMap map)
    {
        string directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(path, Serialize(map));
    }

    public static void ReadFromPath(string path, AutotileVisualOverrideMap map)
    {
        if (!File.Exists(path))
        {
            map?.Clear();
            return;
        }

        DeserializeInto(File.ReadAllText(path), map);
    }

    private static JObject EntryToJson(AutotileVisualOverride entry)
    {
        return new JObject
        {
            ["x"] = entry.x,
            ["y"] = entry.y,
            ["layer"] = entry.layer,
            ["tileset"] = entry.tileset,
            ["autoSpriteId"] = entry.autoSpriteId,
            ["autoFlipX"] = entry.autoFlipX,
            ["overrideSpriteId"] = entry.overrideSpriteId,
            ["overrideFlipX"] = entry.overrideFlipX,
            ["overrideFlipY"] = entry.overrideFlipY,
            ["rotation"] = entry.rotation,
            ["note"] = entry.note ?? string.Empty,
        };
    }

    private static AutotileVisualOverride ParseEntry(JObject obj)
    {
        if (!obj.TryGetValue("x", out JToken xToken) || !obj.TryGetValue("y", out JToken yToken))
        {
            return null;
        }

        int x = xToken.Value<int>();
        int y = yToken.Value<int>();
        string layer = obj["layer"]?.ToString();
        if (string.IsNullOrEmpty(layer))
        {
            layer = AutotileVisualLayerNames.Ground;
        }

        string tileset = obj["tileset"]?.ToString();
        if (string.IsNullOrEmpty(tileset))
        {
            tileset = "Humus";
        }
        string overrideSpriteId = obj["overrideSpriteId"]?.ToString();
        if (string.IsNullOrEmpty(overrideSpriteId))
        {
            overrideSpriteId = obj["spriteId"]?.ToString();
        }

        if (string.IsNullOrEmpty(overrideSpriteId) && obj["spriteId"]?.Type == JTokenType.Integer)
        {
            overrideSpriteId = obj["spriteId"]!.Value<int>().ToString();
        }

        if (string.IsNullOrEmpty(overrideSpriteId))
        {
            return null;
        }

        bool overrideFlipX = obj["overrideFlipX"]?.Value<bool>()
            ?? obj["flipX"]?.Value<bool>()
            ?? false;
        bool overrideFlipY = obj["overrideFlipY"]?.Value<bool>()
            ?? obj["flipY"]?.Value<bool>()
            ?? false;
        int rotation = obj["rotation"]?.Value<int>()
            ?? obj["rotationDegrees"]?.Value<int>()
            ?? 0;

        return new AutotileVisualOverride(
            new Vector2Int(x, y),
            AutotileVisualLayerNames.Parse(layer),
            tileset,
            obj["autoSpriteId"]?.ToString() ?? string.Empty,
            obj["autoFlipX"]?.Value<bool>() ?? false,
            overrideSpriteId,
            overrideFlipX,
            overrideFlipY,
            rotation,
            obj["note"]?.ToString());
    }
}
