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
        // numbering (air=0, dirt=1, grass=2, stone=3, bricks A-D=4-7, then
        // frozen=8, magma=9, sand=10).
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
                        { ""localX"": 8, ""localY"": 8, ""tile"": { ""id"": 8, ""light"": 0, ""fluid"": 0, ""metadata"": 0 } },
                        { ""localX"": 9, ""localY"": 9, ""tile"": { ""id"": 9, ""light"": 0, ""fluid"": 0, ""metadata"": 0 } },
                        { ""localX"": 10, ""localY"": 10, ""tile"": { ""id"": 10, ""light"": 0, ""fluid"": 0, ""metadata"": 0 } },
                        { ""localX"": 6, ""localY"": 7, ""tile"": { ""id"": 0, ""light"": 15, ""fluid"": 0, ""metadata"": 0 } }
                    ]
                }
            ]
        }");

        SandboxWorld world = CreateWorld();
        world.LoadFromPath(path);

        ContentRegistry<TileDefinition> tiles = SandboxRegistries.Tiles;
        Assert.AreEqual(tiles.GetIndex("core:stone"), world.GetTile(1, 2).id, "Legacy id 3 must load as stone.");
        Assert.AreEqual(tiles.GetIndex("core:bricks_d"), world.GetTile(4, 5).id, "Legacy id 7 must load as bricks D.");
        Assert.AreEqual(tiles.GetIndex("core:frozen"), world.GetTile(8, 8).id, "Legacy id 8 must load as frozen.");
        Assert.AreEqual(tiles.GetIndex("core:magma"), world.GetTile(9, 9).id, "Legacy id 9 must load as magma.");
        Assert.AreEqual(tiles.GetIndex("core:sand"), world.GetTile(10, 10).id, "Legacy id 10 must load as sand.");
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

        Assert.AreEqual((byte)0, world.GetTile(1, 2).light,
            "Legacy cached light must be ignored and recomputed as derived state.");
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
    public void SaveToPath_ClearsDerivedLightFromEveryTileEdit()
    {
        SandboxWorld world = CreateWorld();
        world.SetTile(0, 0, SandboxRegistries.Tiles.GetIndex("core:bricks_d"));

        string path = TempFile("derived-light-save.json");
        world.SaveToPath(path);

        SandboxSaveData saved = JsonUtility.FromJson<SandboxSaveData>(File.ReadAllText(path));
        Assert.IsNotEmpty(saved.chunks);
        foreach (SandboxChunkSaveData chunk in saved.chunks)
        {
            foreach (SandboxTileEditData edit in chunk.edits)
            {
                Assert.AreEqual(0, edit.tile.light,
                    $"Cached light persisted at chunk ({chunk.x},{chunk.y}) local ({edit.localX},{edit.localY}).");
            }
        }
    }

    [Test]
    public void SaveThenLoad_RoundTripSurvivesSimulatedRegistryReorder()
    {
        ContentRegistry<TileDefinition> originalTiles = SandboxRegistries.Tiles;
        int bricksDBefore = originalTiles.GetIndex("core:bricks_d");
        int dirtBefore = originalTiles.GetIndex("core:dirt");

        SandboxWorld world = CreateWorld();
        world.SetTile(3, 4, bricksDBefore);
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
            bricksDBefore,
            reordered.GetIndex("core:bricks_d"),
            "Precondition: the reorder must actually change bricks D's runtime index.");

        SandboxWorld reloaded = CreateWorld();
        reloaded.LoadFromPath(path);

        Assert.AreEqual(
            reordered.GetIndex("core:bricks_d"),
            reloaded.GetTile(3, 4).id,
            "Saved bricks D must resolve to its new runtime index via the palette.");
        Assert.AreEqual(
            reordered.GetIndex("core:dirt"),
            reloaded.GetTile(5, 6).id,
            "Saved dirt must resolve to dirt's new runtime index via the palette.");
    }

    [Test]
    public void LoadFromPath_ResolvesRetiredPaletteIdsThroughAliases()
    {
        // Palette saves written before the ore → bricks rename persist the retired string IDs;
        // the registry alias table must keep them loading as the brick tiles.
        string path = TempFile("renamed-palette.json");
        File.WriteAllText(path, @"{
            ""version"": 2,
            ""seed"": 1337,
            ""hasPlayerPosition"": false,
            ""playerX"": 0,
            ""playerY"": 0,
            ""tilePalette"": { ""entries"": [
                { ""id"": ""core:air"", ""runtimeIndex"": 0 },
                { ""id"": ""core:gold_ore"", ""runtimeIndex"": 1 }
            ] },
            ""chunks"": [
                {
                    ""x"": 0,
                    ""y"": 0,
                    ""edits"": [
                        { ""localX"": 1, ""localY"": 1, ""tile"": { ""id"": 1, ""light"": 0, ""fluid"": 0, ""metadata"": 0 } }
                    ]
                }
            ]
        }");

        SandboxWorld world = CreateWorld();
        world.LoadFromPath(path);

        Assert.AreEqual(
            SandboxRegistries.Tiles.GetIndex("core:bricks_d"),
            world.GetTile(1, 1).id,
            "A palette entry with the retired 'core:gold_ore' ID must resolve to bricks D.");
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

    [Test]
    public void LoadFromPath_RestoresRigidbodyAndTransformToSavedPose()
    {
        GameObject player = CreatePlayer(new Vector2(100f, 200f));
        SandboxWorld world = CreateWorld();
        world.SetPlayerTarget(player.transform);

        string path = TempFile("pose-sync.json");
        File.WriteAllText(path, BuildMinimalSaveJson(seed: 1337, playerX: 12.5f, playerY: 40.25f));

        world.LoadFromPath(path);

        Rigidbody2D body = player.GetComponent<Rigidbody2D>();
        Assert.AreEqual(12.5f, body.position.x, 0.0001f);
        Assert.AreEqual(40.25f, body.position.y, 0.0001f);
        Assert.AreEqual(12.5f, player.transform.position.x, 0.0001f);
        Assert.AreEqual(40.25f, player.transform.position.y, 0.0001f);
        Assert.AreEqual(0f, body.linearVelocity.x, 0.0001f);
        Assert.AreEqual(0f, body.linearVelocity.y, 0.0001f);
    }

    [Test]
    public void LoadFromPath_CentersChunkStreamingOnSavedPoseNotPreLoadTransform()
    {
        // Player is standing far from the saved pose; before the fix, RefreshLoadedChunks used the
        // stale Transform and streamed the wrong neighborhood.
        GameObject player = CreatePlayer(new Vector2(500f, 500f));
        SandboxWorld world = CreateWorld();
        world.SetPlayerTarget(player.transform);

        const float savedX = 3.5f;
        const float savedY = 42.5f;
        string path = TempFile("chunk-center.json");
        File.WriteAllText(path, BuildMinimalSaveJson(seed: 1337, playerX: savedX, playerY: savedY));

        world.LoadFromPath(path);

        Assert.IsTrue(world.TryGetPlayerWorldPosition(out Vector2 pose));
        Assert.AreEqual(savedX, pose.x, 0.0001f);
        Assert.AreEqual(savedY, pose.y, 0.0001f);

        Vector2Int expectedChunk = SandboxWorld.WorldToChunkCoord(
            Mathf.FloorToInt(savedX / world.TileSize),
            Mathf.FloorToInt(savedY / world.TileSize));
        Vector2Int farChunk = SandboxWorld.WorldToChunkCoord(
            Mathf.FloorToInt(500f / world.TileSize),
            Mathf.FloorToInt(500f / world.TileSize));
        Assert.AreNotEqual(farChunk, expectedChunk, "Precondition: far transform must be a different chunk.");

        // Destination neighborhood must be loaded; the far pre-load chunk must not be required.
        Assert.IsNotNull(
            world.transform.Find($"Chunk_{expectedChunk.x}_{expectedChunk.y}"),
            "Load must stream chunks around the saved pose.");
    }

    [Test]
    public void LoadFromPath_LiftsPlayerOutOfSolidTiles()
    {
        GameObject player = CreatePlayer(new Vector2(0.5f, 0.5f));
        BoxCollider2D box = player.GetComponent<BoxCollider2D>();
        box.offset = Vector2.zero;
        box.size = Vector2.one;

        SandboxWorld world = CreateWorld();
        world.SetPlayerTarget(player.transform);

        // Empty save: load regenerates seed terrain. y=8 is underground for the default surface.
        string path = TempFile("overlap-lift.json");
        File.WriteAllText(path, BuildMinimalSaveJson(seed: 1337, playerX: 0.5f, playerY: 8.5f));

        world.LoadFromPath(path);

        Assert.IsTrue(world.TryGetPlayerWorldPosition(out Vector2 pose));
        Assert.AreEqual(0.5f, pose.x, 0.0001f);
        Assert.Greater(pose.y, 8.5f, "Player must be lifted above the buried save pose.");
        Assert.IsFalse(
            SandboxPlayerLoadPose.OverlapsSolid(
                pose,
                box.offset,
                box.size,
                world.TileSize,
                (x, y) => world.GetTile(x, y).IsSolid),
            "Resolved pose must not overlap solid tiles.");
    }

    private SandboxWorld CreateWorld()
    {
        GameObject go = new GameObject("SaveLoadTestWorld");
        spawned.Add(go);
        return go.AddComponent<SandboxWorld>();
    }

    private GameObject CreatePlayer(Vector2 position)
    {
        GameObject player = new GameObject("SaveLoadTestPlayer");
        spawned.Add(player);
        player.transform.position = position;
        Rigidbody2D body = player.AddComponent<Rigidbody2D>();
        body.position = position;
        body.linearVelocity = new Vector2(3f, -4f);
        player.AddComponent<BoxCollider2D>();
        return player;
    }

    private static string BuildMinimalSaveJson(int seed, float playerX, float playerY)
    {
        return JsonUtility.ToJson(new SandboxSaveData
        {
            version = 1,
            seed = seed,
            hasPlayerPosition = true,
            playerX = playerX,
            playerY = playerY
        });
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
