using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace ProjectTwelve.Visual.AutotileDebug
{
    /// <summary>
    /// Loads committed autotile baseline JSON from StreamingAssets for drift RCA overlays.
    /// </summary>
    public static class AutotileBaselineStore
    {
        private const string BaselineFolder = "AutotileBaselines";
        private static readonly Dictionary<string, Dictionary<Vector2Int, BaselineCell>> Cache =
            new Dictionary<string, Dictionary<Vector2Int, BaselineCell>>();

        /// <summary>
        /// Returns a baseline cell map keyed by world tile coordinate, or null when missing.
        /// </summary>
        public static IReadOnlyDictionary<Vector2Int, BaselineCell> TryLoad(string baselineName)
        {
            if (string.IsNullOrWhiteSpace(baselineName))
            {
                baselineName = "sandbox-scene-mountain";
            }

            if (Cache.TryGetValue(baselineName, out Dictionary<Vector2Int, BaselineCell> cached))
            {
                return cached;
            }

            string fileName = baselineName.EndsWith("-autotile", StringComparison.Ordinal)
                ? $"{baselineName}.json"
                : $"{baselineName}-autotile.json";

            string streamingPath = Path.Combine(Application.streamingAssetsPath, BaselineFolder, fileName);
            if (File.Exists(streamingPath))
            {
                return Cache[baselineName] = ParseBaseline(File.ReadAllText(streamingPath));
            }

#if UNITY_EDITOR
            string projectPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "tools", "tile-viz", "test", "fixtures", "baselines", fileName));
            if (File.Exists(projectPath))
            {
                return Cache[baselineName] = ParseBaseline(File.ReadAllText(projectPath));
            }
#endif

            return null;
        }

        /// <summary>Clears cached baselines (tests and domain reload).</summary>
        public static void ClearCache()
        {
            Cache.Clear();
        }

        private static Dictionary<Vector2Int, BaselineCell> ParseBaseline(string json)
        {
            JObject doc = JObject.Parse(json);
            JArray cells = doc["cells"] as JArray;
            var map = new Dictionary<Vector2Int, BaselineCell>();
            if (cells == null)
            {
                return map;
            }

            foreach (JToken token in cells)
            {
                if (token is not JObject cell)
                {
                    continue;
                }

                int x = cell["x"]!.Value<int>();
                int y = cell["y"]!.Value<int>();
                map[new Vector2Int(x, y)] = BaselineCell.FromJson(cell);
            }

            return map;
        }
    }

    /// <summary>Normalized autotile compare fields for one baseline cell.</summary>
    public readonly struct BaselineCell
    {
        public BaselineCell(
            int tileId,
            string groundSpriteId,
            bool groundFlipX,
            bool innerCavity,
            bool coverRendered,
            string coverSpriteId,
            bool coverFlipX)
        {
            TileId = tileId;
            GroundSpriteId = groundSpriteId;
            GroundFlipX = groundFlipX;
            InnerCavity = innerCavity;
            CoverRendered = coverRendered;
            CoverSpriteId = coverSpriteId;
            CoverFlipX = coverFlipX;
        }

        public int TileId { get; }
        public string GroundSpriteId { get; }
        public bool GroundFlipX { get; }
        public bool InnerCavity { get; }
        public bool CoverRendered { get; }
        public string CoverSpriteId { get; }
        public bool CoverFlipX { get; }

        public static BaselineCell FromJson(JObject cell)
        {
            JObject ground = cell["ground"] as JObject;
            JObject cover = cell["cover"] as JObject;
            return new BaselineCell(
                cell["tileId"]?.Value<int>() ?? 0,
                ground?["spriteId"]?.Value<string>(),
                ground?["flipX"]?.Value<bool>() ?? false,
                ground?["normalization"]?["innerCavity"]?.Value<bool>() ?? false,
                cover?["rendered"]?.Value<bool>() ?? false,
                cover?["spriteId"]?.Value<string>(),
                cover?["flipX"]?.Value<bool>() ?? false);
        }
    }
}
