using System;
using NUnit.Framework;
using ProjectTwelve.Visual.Tiles;
using UnityEngine;

public sealed class AutotileVisualTests
{
    [Test]
    public void AutotileMaskBuilder_CornerRequiresBothCardinals()
    {
        int[,] mask = AutotileMaskBuilder.BuildGroundMask((x, y) => x == 0 && y == 0, 5, 5);

        Assert.AreEqual(0, mask[0, 0]);
        Assert.AreEqual(0, mask[2, 0]);
        Assert.AreEqual(1, mask[1, 1]);
    }

    [Test]
    public void AutotileRule_MatchesIsolatedCenter()
    {
        AutotileRule rule = new AutotileRule("20", new[,]
        {
            { 0, 0, 0 },
            { 0, 1, 0 },
            { 0, 0, 0 }
        });

        int[,] mask = new[,]
        {
            { 0, 0, 0 },
            { 0, 1, 0 },
            { 0, 0, 0 }
        };

        Assert.IsTrue(rule.Matches(mask, flipInput: false));
    }

    [Test]
    public void AutotileRule_FlipInputMirrorsHorizontalComparison()
    {
        AutotileRule rule = new AutotileRule("0", new[,]
        {
            { 0, 0, 0 },
            { 0, 1, 1 },
            { 0, 1, 1 }
        });

        int[,] mask = new[,]
        {
            { 0, 0, 0 },
            { 1, 1, 0 },
            { 1, 1, 0 }
        };

        Assert.IsTrue(rule.Matches(mask, flipInput: true));
    }

    [Test]
    public void AutotileResolver_IsDeterministicForSameMask()
    {
        AutotileTileset tileset = CreateTestTileset();
        int[,] mask = new[,]
        {
            { 1, 1, 1 },
            { 1, 1, 1 },
            { 1, 1, 1 }
        };

        Sprite first = AutotileResolver.ResolveSprite(tileset, mask, out bool flipFirst);
        Sprite second = AutotileResolver.ResolveSprite(tileset, mask, out bool flipSecond);

        Assert.AreSame(first, second);
        Assert.AreEqual(flipFirst, flipSecond);
    }

    [Test]
    public void AutotileCatalog_FindsGroundTilesetByName()
    {
        AutotileCatalog catalog = ScriptableObject.CreateInstance<AutotileCatalog>();
        Texture2D texture = new Texture2D(16, 16);
        AutotileTileset humus = new AutotileTileset("Humus", texture, CreateSprites("20"));
        catalog.SetGroundTilesets(new System.Collections.Generic.List<AutotileTileset> { humus });

        Assert.IsTrue(catalog.TryGetGroundTileset("Humus", out AutotileTileset found));
        Assert.AreEqual("Humus", found.Name);
    }

    [Test]
    public void AutotileCoverMask_SetsSideCliffCellsToTwo()
    {
        int[,] mask = AutotileMaskBuilder.BuildCoverMask(
            (x, y) => false,
            (x, y) => (x == 4 && y == 6) || (x == 4 && y == 5),
            5,
            5);

        // West neighbor is a solid wall with no cover -> west cell (middle row, left col) is a cliff.
        Assert.AreEqual(2, mask[1, 0]);
        Assert.AreEqual(1, mask[1, 1]);
        Assert.AreEqual(0, mask[1, 2]);
    }

    [Test]
    public void AutotileCoverMask_ConnectsHorizontalGrassRun()
    {
        // Grass cover to the west and east, dirt below, air above: a middle run tile.
        int[,] mask = AutotileMaskBuilder.BuildCoverMask(
            (x, y) => y == 5 && (x == 4 || x == 6),
            (x, y) => false,
            5,
            5);

        Assert.AreEqual(1, mask[1, 0]);
        Assert.AreEqual(1, mask[1, 2]);
        Assert.AreEqual(0, mask[0, 1]);
        Assert.AreEqual(0, mask[2, 1]);
    }

    [Test]
    public void AutotileResolver_SingleSpriteTileset_SkipsRuleMatching()
    {
        AutotileTileset tileset = new AutotileTileset("Rocks", new Texture2D(16, 16), CreateSprites("0"));
        int[,] mask = new[,]
        {
            { 1, 1, 1 },
            { 1, 1, 1 },
            { 1, 1, 1 }
        };

        Sprite resolved = AutotileResolver.ResolveSprite(tileset, mask, out bool flipX);

        Assert.AreSame(tileset.Sprites[0], resolved);
        Assert.IsFalse(flipX);
    }

    private static AutotileTileset CreateTestTileset()
    {
        Texture2D texture = new Texture2D(16, 16);
        return new AutotileTileset("Test", texture, CreateSprites("9", "10", "20"));
    }

    private static System.Collections.Generic.List<Sprite> CreateSprites(params string[] names)
    {
        Texture2D texture = new Texture2D(16, 16);
        System.Collections.Generic.List<Sprite> sprites = new System.Collections.Generic.List<Sprite>();
        for (int i = 0; i < names.Length; i++)
        {
            sprites.Add(Sprite.Create(texture, new Rect(0, 0, 16, 16), Vector2.one * 0.5f, 16));
            sprites[i].name = names[i];
        }

        return sprites;
    }
}
