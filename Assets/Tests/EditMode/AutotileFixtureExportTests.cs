using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using ProjectTwelve.Sandbox.Registry;
using ProjectTwelve.Visual.Tiles;
using UnityEngine;

/// <summary>
/// Exports autotile resolver expectations for tile-viz snippet fixtures and the tileset
/// manifest used by the Node compositor.
/// </summary>
public sealed class AutotileFixtureExportTests
{
    private static readonly string[] SnippetFiles =
    {
        "grass-cover-middle.json",
        "dug-west-gap.json",
        "material-boundary-vertical.json",
        "material-boundary-horizontal.json",
        "platform-grass-corners.json",
        "vertical-wall-run.json",
        "ceiling-underside.json",
        "overhang-inside-corner.json",
        "dirt-stone-reentrant-west.json",
        "corner-sides-left-right.json",
        "slope-ascending-stair-run.json",
        "slope-descending-stair-run.json",
        "slope-ascending-long.json",
        "slope-descending-long.json",
        "floating-clusters.json",
        "dirt-over-stone-slab.json",
        "floating-platform-underside.json",
        "dirt-hole-1x1.json",
        "dirt-hole-2x1.json",
        "dirt-hole-1x2.json",
        "dirt-hole-door.json",
        "roof-slope-left-vs-right.json",
        "one-sided-house-lip.json",
        "dirt-window-inner-edges.json",
        "mountain-window-corner.json",
        "open-sky-bridge-lintel.json",
        "dirt-gap-left-vertical-wall.json",
    };

    [Test]
    public void ExportSnippetExpectationsAndManifest()
    {
        ExportTilesetManifest();
        foreach (string file in SnippetFiles)
        {
            ExportSnippetExpectation(file);
        }
    }

    private static void ExportTilesetManifest()
    {
        var ci = CultureInfo.InvariantCulture;
        var sb = new StringBuilder();
        sb.Append("{\n");
        sb.Append("  \"format\": \"project-twelve/tileset-manifest/v1\",\n");
        sb.Append("  \"tileSize\": 16,\n");
        sb.Append("  \"tileIdToGroundTileset\": {\n");
        sb.Append("    \"1\": \"Humus\",\n");
        sb.Append("    \"2\": \"Humus\",\n");
        sb.Append("    \"3\": \"Rocks\",\n");
        sb.Append("    \"4\": \"BricksA\",\n");
        sb.Append("    \"5\": \"BricksB\",\n");
        sb.Append("    \"6\": \"BricksC\",\n");
        sb.Append("    \"7\": \"BricksD\"\n");
        sb.Append("  },\n");
        sb.Append("  \"grassCoverTileset\": \"GrassA\",\n");
        sb.Append("  \"tilesets\": [\n");
        AppendTileset(sb, ci, "Humus", "Ground/Humus.png", "ground", 32);
        sb.Append(",\n");
        AppendTileset(sb, ci, "Rocks", "Ground/Rocks.png", "ground", 32);
        sb.Append(",\n");
        AppendTileset(sb, ci, "GrassA", "Cover/GrassA.png", "cover", 6);
        sb.Append(",\n");
        AppendTileset(sb, ci, "BricksA", "Ground/BricksA.png", "ground", 32);
        sb.Append(",\n");
        AppendTileset(sb, ci, "BricksB", "Ground/BricksB.png", "ground", 32);
        sb.Append(",\n");
        AppendTileset(sb, ci, "BricksC", "Ground/BricksC.png", "ground", 32);
        sb.Append(",\n");
        AppendTileset(sb, ci, "BricksD", "Ground/BricksD.png", "ground", 32);
        sb.Append("\n  ]\n}\n");
        Directory.CreateDirectory(DataDir());
        File.WriteAllText(Path.Combine(DataDir(), "tileset-manifest.json"), sb.ToString());
    }

    private static void AppendTileset(
        StringBuilder sb,
        CultureInfo ci,
        string name,
        string relativePath,
        string layer,
        int spriteCount)
    {
        sb.Append("    {");
        sb.AppendFormat(ci, "\"name\":\"{0}\",\"path\":\"{1}\",\"layer\":\"{2}\",", name, relativePath, layer);
        sb.Append("\"cellSize\":16,");
        int columns = spriteCount == 6 ? 3 : 8;
        sb.AppendFormat(ci, "\"columns\":{0},", columns);
        sb.AppendFormat(ci, "\"spriteCount\":{0}", spriteCount);
        sb.Append('}');
    }

    private static void ExportSnippetExpectation(string snippetFile)
    {
        string snippetPath = Path.Combine(SnippetsDir(), snippetFile);
        if (!File.Exists(snippetPath))
        {
            Assert.Ignore($"Snippet fixture missing: {snippetPath}");
            return;
        }

        JObject doc = JObject.Parse(File.ReadAllText(snippetPath));
        Dictionary<Vector2Int, SandboxTile> space = BuildTileLookup(
            doc,
            out int xMin,
            out int yMin,
            out int xMax,
            out int yMax);

        AutotileTileset groundTileset = CreateGroundTileset("Humus");
        AutotileTileset coverTileset = CreateCoverTileset("GrassA");

        var sb = new StringBuilder();
        var ci = CultureInfo.InvariantCulture;
        sb.Append('{');
        sb.AppendFormat(ci, "\"name\":\"{0}\",\"tiles\":[", doc["name"]?.Value<string>() ?? snippetFile);
        bool first = true;
        for (int y = yMin; y <= yMax; y++)
        {
            for (int x = xMin; x <= xMax; x++)
            {
                Vector2Int key = new Vector2Int(x, y);
                if (!space.TryGetValue(key, out SandboxTile tile) || !tile.IsSolid)
                {
                    continue;
                }

                if (!first)
                {
                    sb.Append(',');
                }

                first = false;
                sb.AppendFormat(ci, "{{\"x\":{0},\"y\":{1},", x, y);
                AppendGroundAutotile(sb, ci, space, groundTileset, x, y, tile);
                sb.Append(',');
                AppendCoverAutotile(sb, ci, space, coverTileset, x, y, tile);
                sb.Append('}');
            }
        }

        sb.Append("]}");
        string outName = Path.GetFileNameWithoutExtension(snippetFile) + ".json";
        Directory.CreateDirectory(ExpectedDir());
        File.WriteAllText(Path.Combine(ExpectedDir(), outName), sb.ToString());
    }

    private static void AppendGroundAutotile(
        StringBuilder sb,
        CultureInfo ci,
        Dictionary<Vector2Int, SandboxTile> space,
        AutotileTileset tileset,
        int x,
        int y,
        SandboxTile tile)
    {
        sb.Append("\"ground\":{");
        string groundName = GetGroundTilesetName(tile.id);
        if (groundName == null)
        {
            sb.Append("\"resolved\":false}");
            return;
        }

        bool SharesGround(int nx, int ny)
        {
            Vector2Int key = new Vector2Int(nx, ny);
            return space.TryGetValue(key, out SandboxTile neighbor)
                && SharesGroundGroup(tile.id, neighbor.id);
        }

        bool IsSolid(int nx, int ny)
        {
            Vector2Int key = new Vector2Int(nx, ny);
            return space.TryGetValue(key, out SandboxTile neighbor) && neighbor.IsSolid;
        }

        bool IsSurfaceTile(int nx, int ny)
        {
            Vector2Int key = new Vector2Int(nx, ny);
            Vector2Int aboveKey = new Vector2Int(nx, ny + 1);
            space.TryGetValue(aboveKey, out SandboxTile above);
            return space.TryGetValue(key, out SandboxTile neighbor)
                && neighbor.id == SandboxTileIds.Grass
                && !above.IsSolid;
        }

        GroundMaskBuildResult maskBuild = AutotileMaskBuilder.BuildGroundMaskDetailed(
            SharesGround,
            IsSolid,
            x,
            y,
            IsSurfaceTile);
        int[,] mask = maskBuild.FinalMask;
        string spriteId = AutotileResolver.ResolveSpriteId(tileset, mask, out bool flipX);
        sb.AppendFormat(ci, "\"tileset\":\"{0}\",", groundName);
        sb.AppendFormat(ci, "\"materialGroup\":\"{0}\",", groundName);
        AppendMask(sb, "visualMask", maskBuild.VisualMask);
        sb.Append(',');
        if (maskBuild.SolidMask != null)
        {
            AppendMask(sb, "solidMask", maskBuild.SolidMask);
            sb.Append(',');
        }

        AppendMask(sb, "connectivityMask", maskBuild.ConnectivityMask);
        sb.Append(',');
        AppendMask(sb, "rawMask", maskBuild.ConnectivityMask);
        sb.Append(',');
        AppendMask(sb, "normalizedMask", mask);
        sb.Append(',');
        AppendMask(sb, "mask", mask);
        sb.AppendFormat(
            ci,
            ",\"normalization\":{{\"stairInterior\":{0},\"cavityUnderside\":{1},\"materialBoundary\":{2},\"innerCavity\":{3}}}",
            maskBuild.StairInteriorRemap ? "true" : "false",
            maskBuild.CavityUndersideRemap ? "true" : "false",
            maskBuild.MaterialBoundaryRemap ? "true" : "false",
            maskBuild.InnerCavityRemap ? "true" : "false");
        sb.AppendFormat(
            ci,
            ",\"matchedRuleId\":\"{0}\",\"spriteId\":\"{0}\",\"flipX\":{1},\"finalSpriteId\":\"{0}\",\"partnerSubstitution\":false,",
            spriteId ?? "null",
            flipX ? "true" : "false");
        AppendNeighborTileIds(sb, space, x, y);
        sb.Append(",\"resolved\":true");
        sb.Append('}');
    }

    private static void AppendCoverAutotile(
        StringBuilder sb,
        CultureInfo ci,
        Dictionary<Vector2Int, SandboxTile> space,
        AutotileTileset tileset,
        int x,
        int y,
        SandboxTile tile)
    {
        sb.Append("\"cover\":{");
        Vector2Int aboveKey = new Vector2Int(x, y + 1);
        space.TryGetValue(aboveKey, out SandboxTile above);
        // Grass-growth cover: only a grass tile with an exposed (non-solid) top carries a cover overlay.
        if (tile.id != SandboxRegistries.GrassIndex || above.IsSolid)
        {
            sb.Append("\"rendered\":false}");
            return;
        }

        int[,] mask = AutotileMaskBuilder.BuildCoverMask(
            (nx, ny) =>
            {
                Vector2Int key = new Vector2Int(nx, ny);
                return space.TryGetValue(key, out SandboxTile neighbor) && neighbor.IsSolid;
            },
            x,
            y);
        string spriteId = AutotileResolver.ResolveSpriteId(tileset, mask, out bool flipX);
        sb.Append("\"rendered\":true,");
        sb.Append("\"tileset\":\"GrassA\",");
        AppendMask(sb, mask);
        sb.AppendFormat(
            ci,
            ",\"spriteId\":\"{0}\",\"flipX\":{1},\"resolved\":true",
            spriteId ?? "null",
            flipX ? "true" : "false");
        sb.Append('}');
    }

    private static int MapLegacyId(int legacyId)
    {
        var legacy = SandboxCoreContent.LegacyTileIdToStringId;
        Assert.That(
            legacyId,
            Is.InRange(0, legacy.Count - 1),
            "Snippet tile id is outside the legacy tile-id table.");
        return SandboxRegistries.Tiles.GetIndex(legacy[legacyId]);
    }

    private static string GetGroundTilesetName(int tileId)
    {
        ContentRegistry<TileDefinition> tiles = SandboxRegistries.Tiles;
        if (tileId <= 0 || tileId >= tiles.Count)
        {
            return null;
        }

        string atlasSprite = tiles.Get(tileId).AtlasSprite;
        return string.IsNullOrEmpty(atlasSprite) ? null : atlasSprite;
    }

    private static bool SharesGroundGroup(int tileIdA, int tileIdB)
    {
        if (tileIdA == SandboxTileIds.Air || tileIdB == SandboxTileIds.Air)
        {
            return false;
        }

        string nameA = GetGroundTilesetName(tileIdA);
        string nameB = GetGroundTilesetName(tileIdB);
        return nameA != null && nameA == nameB;
    }

    private static AutotileTileset CreateGroundTileset(string name)
    {
        var sprites = new List<Sprite>();
        Texture2D texture = new Texture2D(16, 16);
        for (int i = 0; i < AutotileRuleTables.GroundSpriteCount; i++)
        {
            sprites.Add(Sprite.Create(
                texture,
                new Rect(0, 0, 16, 16),
                new Vector2(0.5f, 0f),
                16f));
        }

        return new AutotileTileset(name, texture, sprites);
    }

    private static AutotileTileset CreateCoverTileset(string name)
    {
        var sprites = new List<Sprite>();
        Texture2D texture = new Texture2D(16, 16);
        for (int i = 0; i < 6; i++)
        {
            sprites.Add(Sprite.Create(
                texture,
                new Rect(0, 0, 16, 16),
                new Vector2(0.5f, 0f),
                16f));
        }

        return new AutotileTileset(name, texture, sprites);
    }

    private static void AppendMask(StringBuilder sb, int[,] mask)
    {
        AppendMask(sb, "mask", mask);
    }

    private static void AppendMask(StringBuilder sb, string propertyName, int[,] mask)
    {
        sb.Append('"');
        sb.Append(propertyName);
        sb.Append("\":[");
        for (int mx = 0; mx < 3; mx++)
        {
            sb.Append('[');
            for (int my = 0; my < 3; my++)
            {
                if (my > 0)
                {
                    sb.Append(',');
                }

                sb.Append(mask[mx, my]);
            }

            sb.Append(']');
            if (mx < 2)
            {
                sb.Append(',');
            }
        }

        sb.Append(']');
    }

    private static void AppendNeighborTileIds(
        StringBuilder sb,
        Dictionary<Vector2Int, SandboxTile> space,
        int x,
        int y)
    {
        sb.Append("\"neighborTileIds\":[");
        for (int dx = -1; dx <= 1; dx++)
        {
            if (dx > -1)
            {
                sb.Append(',');
            }

            sb.Append('[');
            for (int dy = 1; dy >= -1; dy--)
            {
                if (dy < 1)
                {
                    sb.Append(',');
                }

                Vector2Int key = new Vector2Int(x + dx, y + dy);
                sb.Append(space.TryGetValue(key, out SandboxTile neighbor) ? neighbor.id : SandboxTileIds.Air);
            }

            sb.Append(']');
        }

        sb.Append(']');
    }

    private static Dictionary<Vector2Int, SandboxTile> BuildTileLookup(
        JObject doc,
        out int xMin,
        out int yMin,
        out int xMax,
        out int yMax)
    {
        var space = new Dictionary<Vector2Int, SandboxTile>();
        int originX = doc["origin"]?["x"]?.Value<int>() ?? 0;
        int originY = doc["origin"]?["y"]?.Value<int>() ?? 0;
        xMin = doc["xMin"]?.Value<int>() ?? originX;
        yMin = doc["yMin"]?.Value<int>() ?? originY;
        xMax = doc["xMax"]?.Value<int>() ?? originX + (doc["width"]?.Value<int>() ?? 1) - 1;
        yMax = doc["yMax"]?.Value<int>() ?? originY + (doc["height"]?.Value<int>() ?? 1) - 1;

        if (doc["tiles"] is not JArray tiles)
        {
            return space;
        }

        foreach (JToken token in tiles)
        {
            if (token is not JObject entry)
            {
                continue;
            }

            int x = entry["x"]?.Value<int>() ?? originX + (entry["dx"]?.Value<int>() ?? 0);
            int y = entry["y"]?.Value<int>() ?? originY + (entry["dy"]?.Value<int>() ?? 0);
            // The tile-space/v1 snippet format encodes legacy numeric ids (shared with the
            // tile-viz JS resolver); map them to registry runtime indices for the C# side.
            int legacyId = entry["id"]?.Value<int>() ?? entry["tileId"]?.Value<int>() ?? 0;
            int id = MapLegacyId(legacyId);
            byte light = (byte)(entry["light"]?.Value<int>() ?? (id == SandboxTileIds.Air ? 0 : 15));
            space[new Vector2Int(x, y)] = new SandboxTile(id, light);
            xMin = Mathf.Min(xMin, x);
            yMin = Mathf.Min(yMin, y);
            xMax = Mathf.Max(xMax, x);
            yMax = Mathf.Max(yMax, y);
        }

        return space;
    }

    private static string DataDir()
    {
        return Path.GetFullPath(Path.Combine(Application.dataPath, "..", "tools", "tile-viz", "data"));
    }

    private static string SnippetsDir()
    {
        return Path.GetFullPath(Path.Combine(
            Application.dataPath,
            "..",
            "tools",
            "tile-viz",
            "test",
            "fixtures",
            "snippets"));
    }

    private static string ExpectedDir()
    {
        return Path.GetFullPath(Path.Combine(
            Application.dataPath,
            "..",
            "tools",
            "tile-viz",
            "test",
            "fixtures",
            "expected"));
    }
}
