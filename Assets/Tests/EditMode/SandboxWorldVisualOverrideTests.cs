using System.Collections.Generic;
using NUnit.Framework;
using ProjectTwelve.Sandbox.Registry;
using ProjectTwelve.Visual.Tiles;
using UnityEngine;

public sealed class SandboxWorldVisualOverrideTests
{
    private readonly List<Object> spawned = new List<Object>();

    [TearDown]
    public void TearDown()
    {
        foreach (Object obj in spawned)
        {
            if (obj != null)
            {
                Object.DestroyImmediate(obj);
            }
        }

        spawned.Clear();
        SandboxRegistries.ResetForTests();
    }

    [Test]
    public void TryResolveAutoVisual_Cover_ReturnsGrassASpriteOnExposedGrass()
    {
        SandboxWorld world = CreateWorldWithCatalog(out int grass, out int air);
        world.SetTile(4, 10, grass);
        world.SetTile(4, 11, air);

        Assert.IsTrue(world.TryResolveAutoVisual(
            4,
            10,
            AutotileVisualLayer.Cover,
            out string spriteId,
            out bool flipX,
            out string tilesetName));
        Assert.AreEqual("GrassA", tilesetName);
        Assert.IsFalse(string.IsNullOrEmpty(spriteId));
    }

    [Test]
    public void TryResolveAutoVisual_Cover_ReturnsFalseWhenBlockedAbove()
    {
        SandboxWorld world = CreateWorldWithCatalog(out int grass, out int air);
        int stone = SandboxRegistries.Tiles.GetIndex("core:stone");
        world.SetTile(4, 10, grass);
        world.SetTile(4, 11, stone);

        Assert.IsFalse(world.TryResolveAutoVisual(
            4,
            10,
            AutotileVisualLayer.Cover,
            out _,
            out _,
            out _));
    }

    [Test]
    public void CanEditCoverAt_MatchesRendererEligibility()
    {
        SandboxTileVisualCatalog catalog = CreateCatalog();
        int grass = SandboxRegistries.Tiles.GetIndex("core:grass");
        int stone = SandboxRegistries.Tiles.GetIndex("core:stone");

        // Vendor cover is material-agnostic: any exposed-top ground cell (air above) is editable,
        // grass and stone alike. A ground cell with solid ground above it is buried, so not editable.
        Assert.IsTrue(catalog.CanEditCoverAt(grass, new SandboxTile(0), out AutotileTileset tileset));
        Assert.AreEqual("GrassA", tileset.Name);
        Assert.IsTrue(catalog.CanEditCoverAt(stone, new SandboxTile(0), out AutotileTileset stoneCover));
        Assert.AreEqual("GrassA", stoneCover.Name);
        Assert.IsFalse(catalog.CanEditCoverAt(grass, new SandboxTile(stone), out _));
        Assert.IsFalse(catalog.CanEditCoverAt(stone, new SandboxTile(stone), out _));
    }

    private SandboxWorld CreateWorldWithCatalog(out int grassIndex, out int airIndex)
    {
        grassIndex = SandboxRegistries.Tiles.GetIndex("core:grass");
        airIndex = SandboxRegistries.Tiles.GetIndex("core:air");

        GameObject go = new GameObject("VisualOverrideTestWorld");
        spawned.Add(go);
        SandboxWorld world = go.AddComponent<SandboxWorld>();

        var field = typeof(SandboxWorld).GetField(
            "tileVisualCatalog",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        field!.SetValue(world, CreateCatalog());
        return world;
    }

    private static SandboxTileVisualCatalog CreateCatalog()
    {
        AutotileCatalog autotileCatalog = ScriptableObject.CreateInstance<AutotileCatalog>();
        autotileCatalog.SetGroundTilesets(new List<AutotileTileset> { CreateGroundTileset("Humus") });
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
            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, 16, 16), new Vector2(0.5f, 0f), 16f);
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
            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, 16, 16), new Vector2(0.5f, 0f), 16f);
            sprite.name = i.ToString();
            sprites.Add(sprite);
        }

        return new AutotileTileset(name, texture, sprites);
    }
}
