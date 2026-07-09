using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using ProjectTwelve.Sandbox.Registry;
using ProjectTwelve.Visual.Tiles;
using UnityEngine;

/// <summary>
/// EditMode coverage for the P2-DATA-002 save migration path: version-1 legacy saves load
/// through the fixed legacy tile-id table, new saves persist the registry palette, and a
/// palette save survives a registry reordering in-engine (through the real SandboxWorld
/// save/load path, not just the palette class).
/// </summary>
public sealed class SandboxSaveLoadTests
{
    private readonly List<GameObject> spawned = new List<GameObject>();
    private readonly List<string> tempFiles = new List<string>();

    [TearDown]
    public void TearDown()
    {
        foreach (GameObject go in spawned)
        {
            if (go != null)
            {
                Object.DestroyImmediate(go);
            }
        }

        spawned.Clear();

        foreach (string path in tempFiles)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }

        tempFiles.Clear();
        SandboxRegistries.ResetForTests();
    }

    [Test]
    public void LoadFromPath_V1LegacyFixtureMapsThroughLegacyTable()
    {
        // Version-1 prototype save shape: no tilePalette, tile ids in the fixed legacy
        // numbering (air=0, dirt=1, grass=2, stone=3, copper=4, iron=5, silver=6, gold=7).
        string path = TempFile("legacy-v1.json");
        File.WriteAllText(path, @"{
            ""version"": 1,
            ""seed"": 1337,
            ""hasPlayerPosition"": false,
            ""playerX"": 0,
            ""playerY"": 0,
            ""chunks"": [
                {
                    ""x"": 0,
                    ""y"": 0,
                    ""edits"": [
                        { ""localX"": 1, ""localY"": 2, ""tile"": { ""id"": 3, ""light"": 0, ""fluid"": 0, ""metadata"": 0 } },
                        { ""localX"": 4, ""localY"": 5, ""tile"": { ""id"": 7, ""light"": 0, ""fluid"": 0, ""metadata"": 0 } },
                        { ""localX"": 6, ""localY"": 7, ""tile"": { ""id"": 0, ""light"": 15, ""fluid"": 0, ""metadata"": 0 } }
                    ]
                }
            ]
        }");

        SandboxWorld world = CreateWorld();
        world.LoadFromPath(path);

        ContentRegistry<TileDefinition> tiles = SandboxRegistries.Tiles;
        Assert.AreEqual(tiles.GetIndex("core:stone"), world.GetTile(1, 2).id, "Legacy id 3 must load as stone.");
        Assert.AreEqual(tiles.GetIndex("core:gold_ore"), world.GetTile(4, 5).id, "Legacy id 7 must load as gold ore.");
        Assert.AreEqual(tiles.GetIndex("core:air"), world.GetTile(6, 7).id, "Legacy id 0 must load as air.");
    }

    [Test]
    public void LoadFromPath_UpgradesLegacyEditedTilesWithUnsetLight()
    {
        string path = TempFile("legacy-unset-light.json");
        File.WriteAllText(path, @"{
            ""version"": 1,
            ""seed"": 1337,
            ""hasPlayerPosition"": false,
            ""playerX"": 0,
            ""playerY"": 0,
            ""chunks"": [
                {
                    ""x"": 0,
                    ""y"": 0,
                    ""edits"": [
                        { ""localX"": 1, ""localY"": 2, ""tile"": { ""id"": 3, ""light"": 0, ""fluid"": 0, ""metadata"": 0 } }
                    ]
                }
            ]
        }");

        SandboxWorld world = CreateWorld();
        world.LoadFromPath(path);

        SandboxTerrainGenerator generator = world.CreateTerrainGenerator();
        byte expected = SandboxTerrainGenerator.GetPrototypeLight(2, generator.GetSurfaceHeight(1));
        Assert.AreEqual(expected, world.GetTile(1, 2).light);
        Assert.AreEqual((byte)4, expected);
    }

    [Test]
    public void SaveToPath_WritesPaletteMatchingCurrentRegistry()
    {
        SandboxWorld world = CreateWorld();
        world.SetTile(0, 0, SandboxRegistries.Tiles.GetIndex("core:stone"));

        string path = TempFile("palette-save.json");
        world.SaveToPath(path);

        SandboxSaveData saved = JsonUtility.FromJson<SandboxSaveData>(File.ReadAllText(path));
        Assert.IsTrue(saved.HasTilePalette, "New saves must carry the tile palette.");

        ContentRegistry<TileDefinition> tiles = SandboxRegistries.Tiles;
        Assert.AreEqual(tiles.Count, saved.tilePalette.entries.Count, "Palette must cover every registered tile.");
        foreach (RegistryPalette.Entry entry in saved.tilePalette.entries)
        {
            Assert.AreEqual(
                tiles.GetIndex(entry.id),
                entry.runtimeIndex,
                $"Palette entry '{entry.id}' must record the live runtime index.");
        }
    }

    [Test]
    public void SaveThenLoad_RoundTripSurvivesSimulatedRegistryReorder()
    {
        ContentRegistry<TileDefinition> originalTiles = SandboxRegistries.Tiles;
        int goldBefore = originalTiles.GetIndex("core:gold_ore");
        int dirtBefore = originalTiles.GetIndex("core:dirt");

        SandboxWorld world = CreateWorld();
        world.SetTile(3, 4, goldBefore);
        world.SetTile(5, 6, dirtBefore);

        string path = TempFile("reorder-roundtrip.json");
        world.SaveToPath(path);

        // Simulate a mod registering an extra tile: "core:basalt" sorts before the other core
        // IDs, shifting every non-air runtime index relative to the save.
        ContentRegistry<TileDefinition> reordered = new ContentRegistry<TileDefinition>(SandboxCoreContent.AirTileId);
        foreach (TileDefinition def in originalTiles.All)
        {
            reordered.Register(def);
        }

        reordered.Register(new TileDefinition("core:basalt", solid: true, opaque: true, atlasSprite: "Rocks"));
        reordered.Freeze();
        SandboxRegistries.ResetForTests(reordered);

        Assert.AreNotEqual(
            goldBefore,
            reordered.GetIndex("core:gold_ore"),
            "Precondition: the reorder must actually change gold's runtime index.");

        SandboxWorld reloaded = CreateWorld();
        reloaded.LoadFromPath(path);

        Assert.AreEqual(
            reordered.GetIndex("core:gold_ore"),
            reloaded.GetTile(3, 4).id,
            "Saved gold ore must resolve to gold ore's new runtime index via the palette.");
        Assert.AreEqual(
            reordered.GetIndex("core:dirt"),
            reloaded.GetTile(5, 6).id,
            "Saved dirt must resolve to dirt's new runtime index via the palette.");
    }

    [Test]
    public void SaveToPath_WritesVisualOverrideSidecarNextToSave()
    {
        SandboxWorld world = CreateWorld();
        string path = TempFile("sandbox-world.json");

        world.SaveToPath(path);

        string sidecarPath = SandboxWorld.GetVisualOverrideSidecarPath(path);
        tempFiles.Add(sidecarPath);
        Assert.AreEqual(
            Path.Combine(Path.GetDirectoryName(path), "sandbox-world.visual-overrides.json"),
            sidecarPath);
        Assert.IsTrue(File.Exists(sidecarPath), "Saving must write the visual override sidecar next to the normal save.");

        AutotileVisualOverrideMap sidecarMap = new AutotileVisualOverrideMap();
        VisualOverridePersistence.ReadFromPath(sidecarPath, sidecarMap);
        Assert.IsFalse(sidecarMap.HasOverrides, "The default sidecar is an empty override map.");
    }

    [Test]
    public void SaveToPath_WritesPopulatedVisualOverrideSidecar()
    {
        SandboxWorld world = CreateWorld();
        world.SetVisualOverride(2, 3, AutotileVisualLayer.Ground, "Humus", "17", overrideFlipX: true);
        string path = TempFile("override-save.json");
        world.SaveToPath(path);

        string sidecarPath = SandboxWorld.GetVisualOverrideSidecarPath(path);
        tempFiles.Add(sidecarPath);
        AutotileVisualOverrideMap loaded = new AutotileVisualOverrideMap();
        VisualOverridePersistence.ReadFromPath(sidecarPath, loaded);

        Assert.IsTrue(loaded.TryGetOverride(new Vector2Int(2, 3), AutotileVisualLayer.Ground, "Humus", out AutotileVisualOverride entry));
        Assert.AreEqual("17", entry.overrideSpriteId);
        Assert.IsTrue(entry.overrideFlipX);
    }

    [Test]
    public void LoadFromPath_MissingVisualOverrideSidecarLoadsEmptyMapWithoutChangingTiles()
    {
        int stone = SandboxRegistries.Tiles.GetIndex("core:stone");
        SandboxWorld savedWorld = CreateWorld();
        savedWorld.SetTile(2, 3, stone);

        string path = TempFile("missing-sidecar-save.json");
        savedWorld.SaveToPath(path);
        string sidecarPath = SandboxWorld.GetVisualOverrideSidecarPath(path);
        tempFiles.Add(sidecarPath);
        File.Delete(sidecarPath);

        SandboxWorld loadedWorld = CreateWorld();
        loadedWorld.LoadFromPath(path);

        Assert.IsFalse(loadedWorld.HasVisualOverrides, "Missing sidecar files must not be required for compatibility.");
        Assert.AreEqual(stone, loadedWorld.GetTile(2, 3).id, "Loading sidecar metadata must not change simulation tile ids.");
    }

    [Test]
    public void SaveToPath_WritesPopulatedCoverVisualOverrideSidecar()
    {
        SandboxWorld world = CreateWorld();
        world.SetVisualOverride(2, 3, AutotileVisualLayer.Cover, "GrassA", "4", overrideFlipX: true);
        string path = TempFile("cover-override-save.json");
        world.SaveToPath(path);

        string sidecarPath = SandboxWorld.GetVisualOverrideSidecarPath(path);
        tempFiles.Add(sidecarPath);
        AutotileVisualOverrideMap loaded = new AutotileVisualOverrideMap();
        VisualOverridePersistence.ReadFromPath(sidecarPath, loaded);

        Assert.IsTrue(loaded.TryGetOverride(new Vector2Int(2, 3), AutotileVisualLayer.Cover, "GrassA", out AutotileVisualOverride entry));
        Assert.AreEqual("4", entry.overrideSpriteId);
        Assert.IsTrue(entry.overrideFlipX);
        Assert.AreEqual(AutotileVisualLayerNames.Cover, entry.layer);
    }

    [Test]
    public void LoadFromPath_HydratesVisualOverrideSidecarIntoMap()
    {
        SandboxWorld savedWorld = CreateWorld();
        savedWorld.SetVisualOverride(4, 5, AutotileVisualLayer.Ground, "Humus", "12");
        string path = TempFile("hydrate-overrides.json");
        savedWorld.SaveToPath(path);

        SandboxWorld loadedWorld = CreateWorld();
        loadedWorld.LoadFromPath(path);

        Assert.IsTrue(loadedWorld.HasVisualOverrides);
        Assert.IsTrue(loadedWorld.AutotileVisualOverrides.TryGetOverride(
            new Vector2Int(4, 5),
            AutotileVisualLayer.Ground,
            "Humus",
            out AutotileVisualOverride entry));
        Assert.AreEqual("12", entry.overrideSpriteId);
    }

    [Test]
    public void LoadFromPath_MissingFileLogsWarningAndLeavesWorldUnchanged()
    {
        SandboxWorld world = CreateWorld();
        int stone = SandboxRegistries.Tiles.GetIndex("core:stone");
        world.SetTile(2, 2, stone);

        UnityEngine.TestTools.LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("Sandbox save file not found"));
        world.LoadFromPath(TempFile("does-not-exist.json"));

        Assert.AreEqual(stone, world.GetTile(2, 2).id, "A missing save file must not mutate the world.");
    }

    private SandboxWorld CreateWorld()
    {
        GameObject go = new GameObject("SaveLoadTestWorld");
        spawned.Add(go);
        return go.AddComponent<SandboxWorld>();
    }

    private string TempFile(string name)
    {
        string dir = Path.Combine(Application.temporaryCachePath, "sandbox-save-tests");
        Directory.CreateDirectory(dir);
        string path = Path.Combine(dir, name);
        tempFiles.Add(path);
        return path;
    }
}
