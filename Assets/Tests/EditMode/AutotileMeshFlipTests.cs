using System.Collections.Generic;
using NUnit.Framework;
using ProjectTwelve.Visual.Tiles;
using UnityEngine;

/// <summary>
/// Verifies Unity mesh flipX mirrors inside the tile cell (tile-viz blit parity), not around sprite origin.
/// </summary>
public sealed class AutotileMeshFlipTests
{
    [Test]
    public void ComputeWorldX_AsymmetricBounds_FlipsInsideCell_NotAroundOrigin()
    {
        const float tileSize = 1f;
        const int cellX = 4;
        Sprite sprite = Sprite.Create(
            new Texture2D(16, 16),
            new Rect(0, 0, 16, 16),
            new Vector2(0f, 0f),
            16f);

        float westLocalX = sprite.bounds.min.x + 0.1f;
        float unflipped = AutotileSpriteMeshBuilder.ComputeWorldX(cellX, tileSize, sprite, westLocalX, flipX: false);
        float flipped = AutotileSpriteMeshBuilder.ComputeWorldX(cellX, tileSize, sprite, westLocalX, flipX: true);

        Assert.AreEqual(cellX * tileSize + 0.1f, unflipped, 0.0001f);
        Assert.AreEqual((cellX + 1) * tileSize - 0.1f, flipped, 0.0001f);

        float legacyOriginFlip = cellX * tileSize - sprite.bounds.min.x - westLocalX;
        Assert.That(
            flipped,
            Is.Not.EqualTo(legacyOriginFlip).Within(0.0001f),
            "Origin mirror must not match cell-local flip.");
    }

    [Test]
    public void AppendSprite_FlipX_PreservesTileCellBounds_ForBottomLeftPivotSprite()
    {
        const float tileSize = 1f;
        const int x = 2;
        const int y = 5;
        Sprite sprite = Sprite.Create(
            new Texture2D(16, 16),
            new Rect(0, 0, 16, 16),
            new Vector2(0f, 0f),
            16f);

        AssertCellBoundsMatch(x, y, tileSize, sprite, flipX: false);
        AssertCellBoundsMatch(x, y, tileSize, sprite, flipX: true);
    }

    [Test]
    public void Resolver_FlipEdgeRules_KeepSpriteIdWithoutPartnerSubstitution()
    {
        AutotileTileset tileset = CreateGroundTileset();

        AssertRuleFlip(tileset, WestOuterCornerMask(), "0", flipX: false);
        AssertRuleFlip(tileset, EastOuterCornerMask(), "0", flipX: true);
        AssertRuleFlip(tileset, WestOpenFaceMask(), "8", flipX: false);
        AssertRuleFlip(tileset, EastOpenFaceMask(), "8", flipX: true);
        AssertRuleFlip(tileset, WestReentrantUndersideMask(), "16", flipX: false);
        AssertRuleFlip(tileset, EastReentrantUndersideMask(), "16", flipX: true);
        AssertRuleFlip(tileset, EastOneSidedLipMask(), "24", flipX: true);
        AssertRuleFlip(tileset, BridgeMask(), "25", flipX: false);
    }

    [Test]
    public void Resolver_OneSidedLip_Is24WithFlip_Not25()
    {
        AutotileTileset tileset = CreateGroundTileset();
        Sprite resolved = AutotileResolver.ResolveSprite(tileset, EastOneSidedLipMask(), out bool flipX);
        Assert.AreEqual("24", resolved.name);
        Assert.IsTrue(flipX);
        Assert.AreNotEqual("25", resolved.name);
    }

    private static void AssertCellBoundsMatch(int x, int y, float tileSize, Sprite sprite, bool flipX)
    {
        AutotileSpriteMeshBuilder.GetTileCellBounds(
            x,
            y,
            tileSize,
            sprite,
            flipX,
            out float left,
            out float right,
            out float bottom,
            out float top);

        Assert.AreEqual(x * tileSize, left, 0.0001f);
        Assert.AreEqual((x + 1) * tileSize, right, 0.0001f);
        Assert.AreEqual(y * tileSize, bottom, 0.0001f);
        Assert.AreEqual((y + 1) * tileSize, top, 0.0001f);
        Assert.IsTrue(AutotileSpriteMeshBuilder.SpansFullTileCell(x, y, tileSize, sprite, flipX));
    }

    private static void AssertRuleFlip(AutotileTileset tileset, int[,] mask, string expectedId, bool flipX)
    {
        Sprite resolved = AutotileResolver.ResolveSprite(tileset, mask, out bool resolvedFlipX);
        Assert.IsNotNull(resolved);
        Assert.AreEqual(expectedId, resolved.name);
        Assert.AreEqual(flipX, resolvedFlipX);
    }

    private static int[,] WestOuterCornerMask() =>
        new[,]
        {
            { 0, 0, 0 },
            { 0, 1, 1 },
            { 0, 1, 1 }
        };

    private static int[,] EastOuterCornerMask() =>
        new[,]
        {
            { 0, 0, 0 },
            { 1, 1, 0 },
            { 1, 1, 0 }
        };

    private static int[,] WestOpenFaceMask() =>
        new[,]
        {
            { 0, 1, 1 },
            { 0, 1, 1 },
            { 0, 1, 1 }
        };

    private static int[,] EastOpenFaceMask() =>
        new[,]
        {
            { 1, 1, 0 },
            { 1, 1, 0 },
            { 1, 1, 0 }
        };

    private static int[,] WestReentrantUndersideMask() =>
        new[,]
        {
            { 0, 1, 1 },
            { 0, 1, 1 },
            { 0, 0, 0 }
        };

    private static int[,] EastReentrantUndersideMask() =>
        new[,]
        {
            { 1, 1, 0 },
            { 1, 1, 0 },
            { 0, 0, 0 }
        };

    private static int[,] EastOneSidedLipMask() =>
        new[,]
        {
            { 0, 1, 0 },
            { 0, 1, 0 },
            { 0, 0, 0 }
        };

    private static int[,] BridgeMask() =>
        new[,]
        {
            { 0, 0, 0 },
            { 0, 1, 0 },
            { 0, 0, 0 }
        };

    private static AutotileTileset CreateGroundTileset()
    {
        Texture2D texture = new Texture2D(16, 16);
        List<Sprite> sprites = new List<Sprite>();
        for (int i = 0; i < AutotileRuleTables.GroundSpriteCount; i++)
        {
            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, 16, 16), new Vector2(0.5f, 0f), 16f);
            sprite.name = i.ToString();
            sprites.Add(sprite);
        }

        return new AutotileTileset("Humus", texture, sprites);
    }
}
