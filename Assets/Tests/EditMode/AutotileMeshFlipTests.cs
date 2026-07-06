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
    public void VisualTransform_NormalizesNegativeQuarterTurns_AndRejectsInvalidAngles()
    {
        Assert.AreEqual(270, AutotileSpriteMeshBuilder.NormalizeRotationDegrees(-90));
        Assert.AreEqual(180, AutotileSpriteMeshBuilder.NormalizeRotationDegrees(-180));
        Assert.AreEqual(90, AutotileSpriteMeshBuilder.NormalizeRotationDegrees(450));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => AutotileSpriteMeshBuilder.NormalizeRotationDegrees(45));
    }

    [Test]
    public void VisualTransform_AppliesFlipXThenFlipYThenClockwiseRotation()
    {
        Vector2 transformed = AutotileSpriteMeshBuilder.TransformUv(
            new Vector2(0f, 0f),
            0f,
            1f,
            0f,
            1f,
            flipX: true,
            flipY: false,
            rotationDegrees: 90);

        Assert.AreEqual(new Vector2(0f, 0f), transformed);
        Assert.AreNotEqual(new Vector2(1f, 1f), transformed, "Transform order must not rotate before flipX.");
    }

    [Test]
    public void VisualTransform_AsymmetricCorners_ProduceEightDistinctMappings()
    {
        HashSet<string> outputs = new HashSet<string>();
        foreach (bool flipX in new[] { false, true })
        {
            foreach (bool flipY in new[] { false, true })
            {
                foreach (int rotationDegrees in new[] { 0, 90, 180, 270 })
                {
                    outputs.Add(CornerMappingKey(flipX, flipY, rotationDegrees));
                }
            }
        }

        Assert.AreEqual(8, outputs.Count, "The square's D4 transform group should produce eight distinct asymmetric corner mappings.");
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

    [Test]
    public void AppendGroundAutotileSprite_AlwaysUsesFullCellQuad_EvenWhenMeshSpansCell()
    {
        const float tileSize = 1f;
        const int x = 2;
        const int y = 5;
        Sprite sprite = Sprite.Create(
            new Texture2D(16, 16),
            new Rect(0, 0, 16, 16),
            new Vector2(0f, 0f),
            16f);

        Assert.IsTrue(AutotileSpriteMeshBuilder.SpansFullTileCell(x, y, tileSize, sprite, flipX: false));

        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector2> uvs = new List<Vector2>();
        List<Color> colors = new List<Color>();

        AutotileSpriteMeshBuilder.AppendGroundAutotileSprite(
            vertices,
            triangles,
            uvs,
            colors,
            x,
            y,
            tileSize,
            sprite,
            flipX: false,
            Color.white,
            zOffset: 0f);

        Assert.AreEqual(4, vertices.Count);
        Assert.AreEqual(x * tileSize, vertices[0].x, 0.0001f);
        Assert.AreEqual(y * tileSize, vertices[0].y, 0.0001f);
        Assert.AreEqual((x + 1) * tileSize, vertices[2].x, 0.0001f);
        Assert.AreEqual((y + 1) * tileSize, vertices[2].y, 0.0001f);
    }

    [Test]
    public void AppendGroundAutotileSprite_UsesFullCellQuad_WhenMeshDoesNotSpanCell()
    {
        const float tileSize = 1f;
        const int x = 2;
        const int y = 3;
        Sprite sprite = CreateTightMeshSprite();

        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector2> uvs = new List<Vector2>();
        List<Color> colors = new List<Color>();

        AutotileSpriteMeshBuilder.AppendGroundAutotileSprite(
            vertices,
            triangles,
            uvs,
            colors,
            x,
            y,
            tileSize,
            sprite,
            flipX: false,
            Color.white,
            zOffset: 0f);

        Assert.AreEqual(4, vertices.Count);
        Assert.AreEqual(x * tileSize, vertices[0].x, 0.0001f);
        Assert.AreEqual(y * tileSize, vertices[0].y, 0.0001f);
        Assert.AreEqual((x + 1) * tileSize, vertices[2].x, 0.0001f);
        Assert.AreEqual((y + 1) * tileSize, vertices[2].y, 0.0001f);
    }

    private static string CornerMappingKey(bool flipX, bool flipY, int rotationDegrees)
    {
        Vector2[] corners =
        {
            new Vector2(0f, 0f),
            new Vector2(1f, 0f),
            new Vector2(1f, 1f),
            new Vector2(0f, 1f),
        };

        List<string> mapped = new List<string>();
        foreach (Vector2 corner in corners)
        {
            Vector2 uv = AutotileSpriteMeshBuilder.TransformUv(corner, 0f, 1f, 0f, 1f, flipX, flipY, rotationDegrees);
            mapped.Add($"{uv.x:0},{uv.y:0}");
        }

        return string.Join("|", mapped);
    }

    private static Sprite CreateTightMeshSprite()
    {
        Texture2D texture = new Texture2D(16, 16, TextureFormat.RGBA32, false);
        Color32[] pixels = new Color32[16 * 16];
        pixels[8 * 16 + 8] = new Color32(255, 255, 255, 255);
        texture.SetPixels32(pixels);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, 16, 16), new Vector2(0.5f, 0f), 16f);
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

    private static int[,] Mask(params int[] values)
    {
        int[,] mask = new int[3, 3];
        for (int i = 0; i < values.Length; i++)
        {
            mask[i % 3, i / 3] = values[i];
        }

        return mask;
    }

    private static int[,] WestOuterCornerMask() => Mask(0, 0, 0, 0, 1, 1, 0, 1, 1);

    private static int[,] EastOuterCornerMask() => Mask(0, 0, 0, 1, 1, 0, 1, 1, 0);

    private static int[,] WestOpenFaceMask() => Mask(0, 1, 1, 0, 1, 1, 0, 1, 1);

    private static int[,] EastOpenFaceMask() => Mask(1, 1, 0, 1, 1, 0, 1, 1, 0);

    private static int[,] WestReentrantUndersideMask() => Mask(0, 1, 1, 0, 1, 1, 0, 0, 0);

    private static int[,] EastReentrantUndersideMask() => Mask(1, 1, 0, 1, 1, 0, 0, 0, 0);

    private static int[,] EastOneSidedLipMask() => Mask(0, 0, 0, 1, 1, 0, 0, 0, 0);

    private static int[,] BridgeMask() => Mask(0, 0, 0, 1, 1, 1, 0, 0, 0);

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
