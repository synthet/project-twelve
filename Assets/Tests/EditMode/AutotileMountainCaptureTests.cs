using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using ProjectTwelve.Sandbox.Registry;
using ProjectTwelve.Visual.AutotileDebug;
using ProjectTwelve.Visual.Tiles;
using UnityEngine;

/// <summary>
/// Parity checks for the sandbox-scene-mountain tile-space capture against tile-viz expectations.
/// Play Mode still uses procedural/saved world data; this test proves C# resolver logic on the
/// same frozen capture that produced tools/tile-viz/out-slope/sandbox-scene-mountain.png.
/// </summary>
public sealed class AutotileMountainCaptureTests
{
    private static readonly string[] FangUndersideSpriteIds = { "24", "25", "14", "31" };

    [Test]
    public void MountainCapture_MatchesTileVizSpotChecks()
    {
        Dictionary<Vector2Int, SandboxTile> space = LoadCaptureSpace(out _);
        SandboxTileVisualCatalog catalog = CreateProductionLikeCatalog();
        JArray spots = LoadSpots()["spots"] as JArray;
        Assert.NotNull(spots, "spots fixture missing");

        SandboxTile Lookup(int x, int y)
        {
            Vector2Int key = new Vector2Int(x, y);
            return space.TryGetValue(key, out SandboxTile tile) ? tile : new SandboxTile(SandboxTileIds.Air);
        }

        foreach (JToken token in spots)
        {
            int x = token["x"]!.Value<int>();
            int y = token["y"]!.Value<int>();
            SandboxTile tile = Lookup(x, y);
            Assert.IsTrue(tile.IsSolid, $"({x},{y}) expected solid tile");

            Assert.IsTrue(
                AutotileGroundResolve.TryResolve(catalog, Lookup, tile, x, y, out AutotileGroundResolveResult result),
                $"({x},{y}) ground resolve failed");
            Assert.IsTrue(result.Resolved, $"({x},{y}) sprite unresolved");

            string expectedSpriteId = token["ground"]!["spriteId"]!.Value<string>();
            bool expectedFlipX = token["ground"]!["flipX"]?.Value<bool>() ?? false;
            Assert.AreEqual(expectedSpriteId, result.SpriteId, $"({x},{y}) spriteId");
            Assert.AreEqual(expectedFlipX, result.FlipX, $"({x},{y}) flipX");
        }
    }

    [Test]
    public void MountainCapture_FullBaselineGroundAndCoverParity()
    {
        AutotileBaselineStore.ClearCache();
        IReadOnlyDictionary<Vector2Int, BaselineCell> baseline = AutotileBaselineStore.TryLoad("sandbox-scene-mountain");
        if (baseline == null)
        {
            Assert.Ignore("Mountain autotile baseline missing from StreamingAssets or tools/tile-viz/test/fixtures/baselines");
        }

        Dictionary<Vector2Int, SandboxTile> space = LoadCaptureSpace(out _);
        SandboxTileVisualCatalog catalog = CreateProductionLikeCatalog();

        SandboxTile Lookup(int x, int y)
        {
            Vector2Int key = new Vector2Int(x, y);
            return space.TryGetValue(key, out SandboxTile tile) ? tile : new SandboxTile(SandboxTileIds.Air);
        }

        var groundErrors = new StringBuilder();
        var coverErrors = new StringBuilder();
        int compared = 0;

        foreach (KeyValuePair<Vector2Int, BaselineCell> entry in baseline)
        {
            if (!space.TryGetValue(entry.Key, out SandboxTile tile) || !tile.IsSolid)
            {
                continue;
            }

            compared++;
            Vector2Int coord = entry.Key;
            BaselineCell expected = entry.Value;
            int legacyTileId = AutotileBaselineCompare.ToLegacyTileId(tile.id);

            Assert.IsTrue(
                AutotileGroundResolve.TryResolve(catalog, Lookup, tile, coord.x, coord.y, out AutotileGroundResolveResult ground),
                $"({coord.x},{coord.y}) ground resolve failed");

            if (!AutotileBaselineCompare.GroundMatches(legacyTileId, ground, expected))
            {
                groundErrors.AppendLine(
                    $"({coord.x},{coord.y}) ground: legacyTile={legacyTileId} sprite={ground.SpriteId} flip={ground.FlipX} expected tile={expected.TileId} sprite={expected.GroundSpriteId} flip={expected.GroundFlipX}");
            }

            bool coverRendered = TryResolveCover(catalog, Lookup, tile, coord, out string coverSpriteId, out bool coverFlipX);
            if (!AutotileBaselineCompare.CoverMatches(coverRendered, coverSpriteId, coverFlipX, expected))
            {
                coverErrors.AppendLine(
                    $"({coord.x},{coord.y}) cover: rendered={coverRendered} sprite={coverSpriteId} flip={coverFlipX} expected rendered={expected.CoverRendered} sprite={expected.CoverSpriteId} flip={expected.CoverFlipX}");
            }
        }

        Assert.Greater(compared, 2000, "Expected most baseline cells to exist in the mountain capture");
        Assert.AreEqual(string.Empty, groundErrors.ToString(), "Ground baseline mismatches on mountain capture");
        Assert.AreEqual(string.Empty, coverErrors.ToString(), "Cover baseline mismatches on mountain capture");
    }

    [Test]
    public void MountainCapture_DirtOnStone_NeverUsesFangUndersideSprites()
    {
        Dictionary<Vector2Int, SandboxTile> space = LoadCaptureSpace(out _);
        SandboxTileVisualCatalog catalog = CreateProductionLikeCatalog();

        SandboxTile Lookup(int x, int y)
        {
            Vector2Int key = new Vector2Int(x, y);
            return space.TryGetValue(key, out SandboxTile tile) ? tile : new SandboxTile(SandboxTileIds.Air);
        }

        int dirtIndex = SandboxTileIds.Dirt;
        int grassIndex = SandboxTileIds.Grass;
        int stoneIndex = SandboxTileIds.Stone;

        foreach (KeyValuePair<Vector2Int, SandboxTile> entry in space)
        {
            int tileId = entry.Value.id;
            if (tileId != dirtIndex && tileId != grassIndex)
            {
                continue;
            }

            SandboxTile below = Lookup(entry.Key.x, entry.Key.y - 1);
            if (below.id != stoneIndex)
            {
                continue;
            }

            Assert.IsTrue(
                AutotileGroundResolve.TryResolve(
                    catalog,
                    Lookup,
                    entry.Value,
                    entry.Key.x,
                    entry.Key.y,
                    out AutotileGroundResolveResult result),
                $"({entry.Key.x},{entry.Key.y}) resolve failed");
            Assert.IsFalse(
                System.Array.IndexOf(FangUndersideSpriteIds, result.SpriteId) >= 0,
                $"({entry.Key.x},{entry.Key.y}) dirt/grass on stone must not use underside sprite {result.SpriteId}");
        }
    }

    private static JObject LoadSpots()
    {
        string path = Path.Combine(FixturesDir(), "expected", "sandbox-scene-mountain-spots.json");
        if (!File.Exists(path))
        {
            Assert.Ignore($"Missing spots fixture: {path}");
        }

        return JObject.Parse(File.ReadAllText(path));
    }

    private static Dictionary<Vector2Int, SandboxTile> LoadCaptureSpace(out string capturePath)
    {
        capturePath = Path.Combine(FixturesDir(), "captures", "sandbox-scene-mountain.json");
        if (!File.Exists(capturePath))
        {
            Assert.Ignore($"Missing capture fixture: {capturePath}");
        }

        return BuildTileLookup(JObject.Parse(File.ReadAllText(capturePath)));
    }

    private static Dictionary<Vector2Int, SandboxTile> BuildTileLookup(JObject doc)
    {
        var space = new Dictionary<Vector2Int, SandboxTile>();
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

            int x = entry["x"]!.Value<int>();
            int y = entry["y"]!.Value<int>();
            int legacyId = entry["id"]?.Value<int>() ?? 0;
            int id = MapLegacyId(legacyId);
            byte light = (byte)(entry["light"]?.Value<int>() ?? (id == SandboxTileIds.Air ? (byte)0 : (byte)15));
            space[new Vector2Int(x, y)] = new SandboxTile(id, light);
        }

        return space;
    }

    private static int MapLegacyId(int legacyId)
    {
        var legacy = SandboxCoreContent.LegacyTileIdToStringId;
        Assert.That(legacyId, Is.InRange(0, legacy.Count - 1));
        return SandboxRegistries.Tiles.GetIndex(legacy[legacyId]);
    }

    private static SandboxTileVisualCatalog CreateProductionLikeCatalog()
    {
        AutotileCatalog autotileCatalog = ScriptableObject.CreateInstance<AutotileCatalog>();
        autotileCatalog.SetGroundTilesets(new List<AutotileTileset>
        {
            CreateGroundTileset("Humus"),
            CreateGroundTileset("Rocks"),
        });
        autotileCatalog.SetCoverTilesets(new List<AutotileTileset> { CreateCoverTileset("GrassA") });

        SandboxTileVisualCatalog catalog = ScriptableObject.CreateInstance<SandboxTileVisualCatalog>();
        var field = typeof(SandboxTileVisualCatalog).GetField(
            "autotileCatalog",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        field!.SetValue(catalog, autotileCatalog);
        return catalog;
    }

    private static AutotileTileset CreateGroundTileset(string name)
    {
        var sprites = new List<Sprite>();
        Texture2D texture = new Texture2D(16, 16);
        for (int i = 0; i < AutotileRuleTables.GroundSpriteCount; i++)
        {
            Sprite sprite = Sprite.Create(
                texture,
                new Rect(0, 0, 16, 16),
                new Vector2(0.5f, 0f),
                16f);
            sprite.name = i.ToString();
            sprites.Add(sprite);
        }

        return new AutotileTileset(name, texture, sprites);
    }

    private static AutotileTileset CreateCoverTileset(string name)
    {
        var sprites = new List<Sprite>();
        Texture2D texture = new Texture2D(16, 16);
        for (int i = 0; i < 6; i++)
        {
            Sprite sprite = Sprite.Create(
                texture,
                new Rect(0, 0, 16, 16),
                new Vector2(0.5f, 0f),
                16f);
            sprite.name = i.ToString();
            sprites.Add(sprite);
        }

        return new AutotileTileset(name, texture, sprites);
    }

    private static bool TryResolveCover(
        SandboxTileVisualCatalog visualCatalog,
        System.Func<int, int, SandboxTile> tileLookup,
        SandboxTile tile,
        Vector2Int worldCoord,
        out string coverSpriteId,
        out bool coverFlipX)
    {
        coverSpriteId = null;
        coverFlipX = false;
        SandboxTile tileAbove = tileLookup(worldCoord.x, worldCoord.y + 1);
        if (!visualCatalog.ShouldRenderGrassCover(tile.id, tileAbove)
            || !visualCatalog.TryGetCoverTileset(tile.id, out AutotileTileset tileset))
        {
            return false;
        }

        int[,] mask = AutotileMaskBuilder.BuildCoverMask(
            (x, y) =>
            {
                SandboxTile neighbor = tileLookup(x, y);
                return visualCatalog.SharesCoverAutotileGroup(tile.id, neighbor.id);
            },
            (x, y) => tileLookup(x, y).IsSolid,
            worldCoord.x,
            worldCoord.y);

        Sprite sprite = AutotileResolver.ResolveSprite(tileset, mask, out coverFlipX);
        if (sprite == null)
        {
            return false;
        }

        coverSpriteId = sprite.name;
        return true;
    }

    private static string FixturesDir() =>
        Path.GetFullPath(Path.Combine(Application.dataPath, "..", "tools", "tile-viz", "test", "fixtures"));
}
