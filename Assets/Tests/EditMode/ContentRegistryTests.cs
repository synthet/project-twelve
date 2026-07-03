using System;
using System.Collections.Generic;
using NUnit.Framework;
using ProjectTwelve.Sandbox.Registry;

/// <summary>
/// EditMode coverage for the P2-DATA-001 registry contract: registration validation, freeze
/// semantics, deterministic runtime index assignment, save palette round-trips, and the legacy
/// numeric tile ID migration table. No Unity scene or licensed-asset dependencies.
/// </summary>
public sealed class ContentRegistryTests
{
    private static TileDefinition Tile(string id, bool solid = true)
    {
        return new TileDefinition(id, solid, atlasSprite: "TestTiles", dropItemId: null);
    }

    [Test]
    public void Register_DuplicateStringIdThrows()
    {
        ContentRegistry<TileDefinition> registry = new ContentRegistry<TileDefinition>();
        registry.Register(Tile("test:dirt"));

        Assert.Throws<ArgumentException>(() => registry.Register(Tile("test:dirt")));
    }

    [TestCase("Dirt")]
    [TestCase("test:")]
    [TestCase(":dirt")]
    [TestCase("test:Dirt")]
    [TestCase("test:dirt:extra")]
    [TestCase("test dirt")]
    [TestCase("")]
    public void Register_MalformedIdThrows(string malformedId)
    {
        ContentRegistry<TileDefinition> registry = new ContentRegistry<TileDefinition>();

        Assert.Throws<ArgumentException>(() => registry.Register(Tile(malformedId)));
    }

    [Test]
    public void Register_AfterFreezeThrows()
    {
        ContentRegistry<TileDefinition> registry = new ContentRegistry<TileDefinition>();
        registry.Register(Tile("test:dirt"));
        registry.Freeze();

        Assert.Throws<InvalidOperationException>(() => registry.Register(Tile("test:stone")));
    }

    [Test]
    public void Get_UnknownStringIdThrowsAndTryGetReturnsFalse()
    {
        ContentRegistry<TileDefinition> registry = new ContentRegistry<TileDefinition>();
        registry.Register(Tile("test:dirt"));
        registry.Freeze();

        Assert.Throws<KeyNotFoundException>(() => registry.Get("test:missing"));
        Assert.Throws<KeyNotFoundException>(() => registry.GetIndex("test:missing"));
        Assert.IsFalse(registry.TryGet("test:missing", out _));
        Assert.IsFalse(registry.TryGet(null, out _));
    }

    [Test]
    public void Get_ByIndexBeforeFreezeThrows()
    {
        ContentRegistry<TileDefinition> registry = new ContentRegistry<TileDefinition>();
        registry.Register(Tile("test:dirt"));

        Assert.Throws<InvalidOperationException>(() => registry.Get(0));
        Assert.Throws<InvalidOperationException>(() => registry.GetIndex("test:dirt"));
    }

    [Test]
    public void Freeze_WithoutDeclaredEmptyDefinitionThrows()
    {
        ContentRegistry<TileDefinition> registry = new ContentRegistry<TileDefinition>("test:air");
        registry.Register(Tile("test:dirt"));

        Assert.Throws<InvalidOperationException>(() => registry.Freeze());
    }

    [Test]
    public void Freeze_IndicesAreDeterministicAcrossRegistrationOrder()
    {
        string[] ids = { "test:dirt", "test:stone", "test:gold_ore", "test:copper_ore" };

        ContentRegistry<TileDefinition> forward = new ContentRegistry<TileDefinition>("test:air");
        forward.Register(Tile("test:air", solid: false));
        foreach (string id in ids)
        {
            forward.Register(Tile(id));
        }

        forward.Freeze();

        ContentRegistry<TileDefinition> reversed = new ContentRegistry<TileDefinition>("test:air");
        for (int i = ids.Length - 1; i >= 0; i--)
        {
            reversed.Register(Tile(ids[i]));
        }

        reversed.Register(Tile("test:air", solid: false));
        reversed.Freeze();

        Assert.AreEqual(forward.Count, reversed.Count);
        foreach (string id in ids)
        {
            Assert.AreEqual(forward.GetIndex(id), reversed.GetIndex(id), $"Index of '{id}' must not depend on registration order.");
        }

        for (int i = 1; i < forward.All.Count - 1; i++)
        {
            Assert.Less(
                string.CompareOrdinal(forward.All[i].Id, forward.All[i + 1].Id), 0,
                "Non-empty definitions must be ordered by ordinal string ID.");
        }
    }

    [Test]
    public void CoreTileRegistry_AirIsRuntimeIndexZero()
    {
        ContentRegistry<TileDefinition> tiles = SandboxCoreContent.CreateTileRegistry();

        Assert.AreEqual(SandboxCoreContent.AirTileId, tiles.Get(0).Id);
        Assert.AreEqual(0, tiles.GetIndex(SandboxCoreContent.AirTileId));
        Assert.IsFalse(tiles.Get(0).Solid);
        Assert.AreEqual(8, tiles.Count);
    }

    [Test]
    public void Palette_RoundTripSurvivesRegistryInsertionAndReordering()
    {
        ContentRegistry<TileDefinition> original = SandboxCoreContent.CreateTileRegistry();
        RegistryPalette palette = RegistryPalette.Capture(original);

        // A new definition sorting before the existing ores shifts every later runtime index.
        ContentRegistry<TileDefinition> extended = new ContentRegistry<TileDefinition>(SandboxCoreContent.AirTileId);
        extended.Register(Tile("core:coal_ore"));
        foreach (TileDefinition def in original.All)
        {
            extended.Register(def);
        }

        extended.Freeze();

        int[] remap = palette.BuildRemap(extended);

        Assert.AreEqual(original.Count, remap.Length);
        for (int savedIndex = 0; savedIndex < remap.Length; savedIndex++)
        {
            Assert.AreEqual(
                original.Get(savedIndex).Id,
                extended.Get(remap[savedIndex]).Id,
                $"Saved index {savedIndex} must resolve to the same string ID after remap.");
        }

        Assert.AreEqual(0, remap[0], "Air must remap to index 0.");
    }

    [Test]
    public void Palette_UnknownStringIdFailsLoudly()
    {
        ContentRegistry<TileDefinition> tiles = SandboxCoreContent.CreateTileRegistry();
        RegistryPalette palette = new RegistryPalette();
        palette.entries.Add(new RegistryPalette.Entry("core:removed_tile", 0));

        Assert.Throws<KeyNotFoundException>(() => palette.BuildRemap(tiles));
    }

    [Test]
    public void Palette_DuplicateOrMissingSavedIndexFailsLoudly()
    {
        ContentRegistry<TileDefinition> tiles = SandboxCoreContent.CreateTileRegistry();

        RegistryPalette duplicate = new RegistryPalette();
        duplicate.entries.Add(new RegistryPalette.Entry(SandboxCoreContent.AirTileId, 0));
        duplicate.entries.Add(new RegistryPalette.Entry("core:dirt", 0));
        Assert.Throws<InvalidOperationException>(() => duplicate.BuildRemap(tiles));

        RegistryPalette sparse = new RegistryPalette();
        sparse.entries.Add(new RegistryPalette.Entry(SandboxCoreContent.AirTileId, 0));
        sparse.entries.Add(new RegistryPalette.Entry("core:dirt", 2));
        Assert.Throws<InvalidOperationException>(() => sparse.BuildRemap(tiles));
    }

    [Test]
    public void LegacyTable_MapsSandboxTileIdsConstantsOneToOne()
    {
        IReadOnlyList<string> legacy = SandboxCoreContent.LegacyTileIdToStringId;

        Assert.AreEqual(8, legacy.Count);
        Assert.AreEqual("core:air", legacy[SandboxTileIds.Air]);
        Assert.AreEqual("core:dirt", legacy[SandboxTileIds.Dirt]);
        Assert.AreEqual("core:grass", legacy[SandboxTileIds.Grass]);
        Assert.AreEqual("core:stone", legacy[SandboxTileIds.Stone]);
        Assert.AreEqual("core:copper_ore", legacy[SandboxTileIds.CopperOre]);
        Assert.AreEqual("core:iron_ore", legacy[SandboxTileIds.IronOre]);
        Assert.AreEqual("core:silver_ore", legacy[SandboxTileIds.SilverOre]);
        Assert.AreEqual("core:gold_ore", legacy[SandboxTileIds.GoldOre]);

        ContentRegistry<TileDefinition> tiles = SandboxCoreContent.CreateTileRegistry();
        foreach (string id in legacy)
        {
            Assert.IsTrue(tiles.TryGet(id, out _), $"Legacy string ID '{id}' must resolve in the core tile registry.");
        }
    }

    [Test]
    public void LegacyTable_SolidityMatchesSandboxTileBehavior()
    {
        ContentRegistry<TileDefinition> tiles = SandboxCoreContent.CreateTileRegistry();

        for (int legacyId = 0; legacyId < SandboxCoreContent.LegacyTileIdToStringId.Count; legacyId++)
        {
            TileDefinition def = tiles.Get(SandboxCoreContent.LegacyTileIdToStringId[legacyId]);
            Assert.AreEqual(
                new SandboxTile(legacyId).IsSolid,
                def.Solid,
                $"Registry solidity for legacy ID {legacyId} must match SandboxTile.IsSolid.");
        }
    }

    [Test]
    public void ValidateTileReferences_PassesForCoreContent()
    {
        Assert.DoesNotThrow(() => SandboxCoreContent.ValidateTileReferences(
            SandboxCoreContent.CreateTileRegistry(),
            SandboxCoreContent.CreateItemRegistry()));
    }

    [Test]
    public void ValidateTileReferences_FailsOnUnresolvedDropItem()
    {
        ContentRegistry<TileDefinition> tiles = new ContentRegistry<TileDefinition>("test:air");
        tiles.Register(new TileDefinition("test:air", solid: false, hardness: 0f));
        tiles.Register(new TileDefinition("test:dirt", solid: true, atlasSprite: "TestTiles", dropItemId: "test:missing_item"));
        tiles.Freeze();

        ContentRegistry<ItemDefinition> items = new ContentRegistry<ItemDefinition>();
        items.Freeze();

        Assert.Throws<InvalidOperationException>(() => SandboxCoreContent.ValidateTileReferences(tiles, items));
    }

    [Test]
    public void ValidateTileReferences_FailsOnMissingVisualKey()
    {
        ContentRegistry<TileDefinition> tiles = new ContentRegistry<TileDefinition>("test:air");
        tiles.Register(new TileDefinition("test:air", solid: false, hardness: 0f));
        tiles.Register(new TileDefinition("test:dirt", solid: true, atlasSprite: null));
        tiles.Freeze();

        ContentRegistry<ItemDefinition> items = new ContentRegistry<ItemDefinition>();
        items.Freeze();

        Assert.Throws<InvalidOperationException>(() => SandboxCoreContent.ValidateTileReferences(tiles, items));
    }
}
