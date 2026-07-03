using System;
using System.Collections.Generic;
using NUnit.Framework;
using ProjectTwelve.Visual.Tiles;
using UnityEngine;

public sealed class AutotileVisualTests
{
    [Test]
    public void AutotileResolver_ConnectedGroundPlatform_ResolvesInteriorSprite()
    {
        AutotileTileset tileset = CreateFullGroundTileset();
        int[,] mask = AutotileMaskBuilder.BuildGroundMask((x, y) => true, 5, 5);

        Sprite resolved = AutotileResolver.ResolveSprite(tileset, mask, out _);

        Assert.IsNotNull(resolved);
        Assert.IsTrue(resolved.name == "9" || resolved.name == "10");
        Assert.AreNotEqual("20", resolved.name);
    }

    [Test]
    public void AutotileResolver_HorizontalGrassCoverRun_ResolvesMiddleSprite()
    {
        AutotileTileset tileset = CreateCoverTileset();
        int[,] mask = AutotileMaskBuilder.BuildCoverMask(
            (x, y) => y == 5 && (x == 4 || x == 6),
            (x, y) => false,
            5,
            5);

        Sprite resolved = AutotileResolver.ResolveSprite(tileset, mask, out _);

        Assert.IsNotNull(resolved);
        Assert.AreEqual("4", resolved.name);
    }

    [Test]
    public void AutotileCoverMask_StoneCliffBesideGrass_SetsCliffWithoutSharedGroundGroup()
    {
        int[,] mask = AutotileMaskBuilder.BuildCoverMask(
            (x, y) => false,
            (x, y) => x == 4 && (y == 5 || y == 6),
            5,
            5);

        Assert.AreEqual(2, mask[0, 1]);
    }

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

        // West neighbor is a solid wall with no cover -> west cell (center row, west col) is a cliff.
        Assert.AreEqual(2, mask[0, 1]);
        Assert.AreEqual(1, mask[1, 1]);
        Assert.AreEqual(0, mask[2, 1]);
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

        Assert.AreEqual(1, mask[0, 1]);
        Assert.AreEqual(1, mask[2, 1]);
        Assert.AreEqual(0, mask[1, 0]);
        Assert.AreEqual(0, mask[1, 2]);
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

    [Test]
    public void AutotileResolver_ThirtyTwoSpriteTileset_PicksDifferentVariantsForMasks()
    {
        AutotileTileset tileset = CreateFullGroundTileset();
        int[,] isolated = new[,]
        {
            { 0, 0, 0 },
            { 0, 1, 0 },
            { 0, 0, 0 }
        };
        int[,] surrounded = new[,]
        {
            { 1, 1, 1 },
            { 1, 1, 1 },
            { 1, 1, 1 }
        };

        Sprite isolatedSprite = AutotileResolver.ResolveSprite(tileset, isolated, out _);
        Sprite surroundedSprite = AutotileResolver.ResolveSprite(tileset, surrounded, out _);

        Assert.IsNotNull(isolatedSprite);
        Assert.IsNotNull(surroundedSprite);
        Assert.AreNotEqual(isolatedSprite.name, surroundedSprite.name);
        Assert.AreEqual("20", isolatedSprite.name);
    }

    [Test]
    public void AutotileRuleTables_GroundRules_MatchOwnPatterns()
    {
        IReadOnlyList<AutotileRule> rules = AutotileRuleTables.Ground;
        for (int i = 0; i < rules.Count; i++)
        {
            AutotileRule rule = rules[i];
            int[,] mask = rule.ToMask();
            Assert.IsTrue(
                rule.Matches(mask, flipInput: false) || rule.Matches(mask, flipInput: true),
                $"Ground rule '{rule.SpriteId}' does not match its encoded pattern.");
        }
    }

    [Test]
    public void AutotileResolver_GroundMatrix_EachRulePatternResolvesMatchingSpriteId()
    {
        AutotileTileset tileset = CreateFullGroundTileset();
        IReadOnlyList<AutotileRule> rules = AutotileRuleTables.Ground;
        for (int i = 0; i < rules.Count; i++)
        {
            AutotileRule rule = rules[i];
            int[,] mask = rule.ToMask();
            Sprite resolved = AutotileResolver.ResolveSprite(tileset, mask, out _);
            Assert.IsNotNull(resolved, $"Rule '{rule.SpriteId}' did not resolve.");

            bool matchesRuleSet = false;
            for (int j = 0; j < rules.Count; j++)
            {
                if (rules[j].SpriteId == resolved.name
                    && (rules[j].Matches(mask, flipInput: false) || rules[j].Matches(mask, flipInput: true)))
                {
                    matchesRuleSet = true;
                    break;
                }
            }

            Assert.IsTrue(
                matchesRuleSet,
                $"Mask for ground rule '{rule.SpriteId}' resolved to '{resolved.name}', which does not match the mask.");
        }
    }

    [Test]
    public void AutotileResolver_GroundMatrix_MatchesVendorStyleMasks()
    {
        AutotileTileset tileset = CreateFullGroundTileset();

        AssertResolvesTo(tileset, IsolatedMask(), "20");
        AssertResolvesTo(tileset, TopLeftOuterCornerMask(), "0");
        AssertResolvesTo(tileset, VerticalRunMask(), "21");
        AssertResolvesTo(tileset, TopTShapeMask(), "6");

        int[,] surrounded = SurroundedMask();
        Sprite interior = AutotileResolver.ResolveSprite(tileset, surrounded, out _);
        Assert.IsTrue(interior.name == "9" || interior.name == "10");
    }

    [Test]
    public void AutotileMaskBuilder_MaterialBoundary_BreaksGroundConnectivity()
    {
        const int worldX = 10;
        const int worldY = 5;

        int[,] dirtMask = AutotileMaskBuilder.BuildGroundMask(
            (x, y) =>
            {
                if (x == worldX && y == worldY - 1)
                {
                    return false;
                }

                return x == worldX && (y == worldY || y == worldY + 1);
            },
            worldX,
            worldY);

        Assert.AreEqual(0, dirtMask[1, 2], "Dirt should not connect to stone below.");

        int[,] stoneMask = AutotileMaskBuilder.BuildGroundMask(
            (x, y) =>
            {
                if (x == worldX && y == worldY)
                {
                    return false;
                }

                return x == worldX && (y == worldY - 1 || y == worldY - 2);
            },
            worldX,
            worldY - 1);

        Assert.AreEqual(0, stoneMask[1, 0], "Stone should not connect to dirt above.");

        AutotileTileset tileset = CreateFullGroundTileset();
        Sprite dirtSprite = AutotileResolver.ResolveSprite(tileset, dirtMask, out _);
        Sprite stoneSprite = AutotileResolver.ResolveSprite(tileset, stoneMask, out _);
        Assert.AreEqual(ResolveVendorStyle(dirtMask), dirtSprite.name);
        Assert.AreEqual(ResolveVendorStyle(stoneMask), stoneSprite.name);
    }

    [Test]
    public void SpriteMeshQuad_SpansFullTileCell_BottomPivot()
    {
        const float tileSize = 1f;
        const int x = 3;
        const int y = 7;
        Sprite sprite = Sprite.Create(
            new Texture2D(16, 16),
            new Rect(0, 0, 16, 16),
            new Vector2(0.5f, 0f),
            16f);

        Assert.IsTrue(AutotileSpriteMeshBuilder.SpansFullTileCell(x, y, tileSize, sprite, flipX: false));
    }

    [Test]
    public void SpriteMeshQuad_SpansFullTileCell_CenterPivot()
    {
        const float tileSize = 1f;
        const int x = 3;
        const int y = 7;
        Sprite sprite = Sprite.Create(
            new Texture2D(16, 16),
            new Rect(0, 0, 16, 16),
            new Vector2(0.5f, 0.5f),
            16f);

        Assert.IsTrue(AutotileSpriteMeshBuilder.SpansFullTileCell(x, y, tileSize, sprite, flipX: false));
    }

    [Test]
    public void SpriteMeshQuad_FlipX_PreservesCellCoverage()
    {
        const float tileSize = 1f;
        Sprite sprite = Sprite.Create(
            new Texture2D(16, 16),
            new Rect(0, 0, 16, 16),
            new Vector2(0.5f, 0f),
            16f);

        Assert.IsTrue(AutotileSpriteMeshBuilder.SpansFullTileCell(2, 4, tileSize, sprite, flipX: true));
    }

    [Test]
    public void SpriteMeshQuad_FallbackSpansFullTileCell_WhenVerticesMissing()
    {
        const float tileSize = 1f;
        Sprite sprite = Sprite.Create(
            new Texture2D(16, 16),
            new Rect(0, 0, 16, 16),
            new Vector2(0.5f, 0f),
            16f);

        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector2> uvs = new List<Vector2>();
        List<Color> colors = new List<Color>();

        AutotileSpriteMeshBuilder.AppendSprite(
            vertices,
            triangles,
            uvs,
            colors,
            1,
            2,
            tileSize,
            sprite,
            flipX: false,
            Color.white,
            zOffset: 0f);

        Assert.GreaterOrEqual(vertices.Count, 4);
        AutotileSpriteMeshBuilder.GetTileCellBounds(
            1,
            2,
            tileSize,
            sprite,
            flipX: false,
            out float left,
            out float right,
            out float bottom,
            out float top);

        Assert.AreEqual(1f, left, 0.0001f);
        Assert.AreEqual(2f, right, 0.0001f);
        Assert.AreEqual(2f, bottom, 0.0001f);
        Assert.AreEqual(3f, top, 0.0001f);
    }

    [Test]
    public void SandboxTileVisualCatalog_OreTilesDoNotShareGroundGroup()
    {
        SandboxTileVisualCatalog catalog = ScriptableObject.CreateInstance<SandboxTileVisualCatalog>();

        Assert.IsTrue(catalog.SharesGroundAutotileGroup(SandboxTileIds.CopperOre, SandboxTileIds.CopperOre));
        Assert.IsFalse(catalog.SharesGroundAutotileGroup(SandboxTileIds.CopperOre, SandboxTileIds.IronOre));
        Assert.IsFalse(catalog.SharesGroundAutotileGroup(SandboxTileIds.IronOre, SandboxTileIds.SilverOre));
        Assert.IsTrue(catalog.SharesGroundAutotileGroup(SandboxTileIds.GoldOre, SandboxTileIds.GoldOre));
    }

    [Test]
    public void BottomPivotSpriteBounds_SpanFullTileCell()
    {
        const float tileSize = 1f;
        const int x = 3;
        const int y = 7;
        Sprite sprite = Sprite.Create(
            new Texture2D(16, 16),
            new Rect(0, 0, 16, 16),
            new Vector2(0.5f, 0f),
            16f);

        AssertSpriteQuadSpansTileCell(x, y, tileSize, sprite);
    }

    [Test]
    public void CenterPivotSpriteBounds_SpanFullTileCell()
    {
        const float tileSize = 1f;
        const int x = 3;
        const int y = 7;
        Sprite sprite = Sprite.Create(
            new Texture2D(16, 16),
            new Rect(0, 0, 16, 16),
            new Vector2(0.5f, 0.5f),
            16f);

        AssertSpriteQuadSpansTileCell(x, y, tileSize, sprite);
    }

    [Test]
    public void AutotileResolver_DirectMatch_ResolvesWithoutFlip()
    {
        AutotileTileset tileset = CreateAsymmetricRuleTileset();
        int[,] mask = new[,]
        {
            { 0, 1, 1 },
            { 0, 1, 1 },
            { 0, 1, 1 }
        };

        Sprite resolved = AutotileResolver.ResolveSprite(tileset, mask, out bool flipX);

        Assert.IsNotNull(resolved);
        Assert.AreEqual("L", resolved.name);
        Assert.IsFalse(flipX);
    }

    [Test]
    public void AutotileResolver_MirroredMask_ResolvesSameRuleWithFlipX()
    {
        AutotileTileset tileset = CreateAsymmetricRuleTileset();
        // Horizontal mirror of the "L" rule pattern (second index reversed).
        int[,] mirrored = new[,]
        {
            { 1, 1, 0 },
            { 1, 1, 0 },
            { 1, 1, 0 }
        };

        Sprite resolved = AutotileResolver.ResolveSprite(tileset, mirrored, out bool flipX);

        Assert.IsNotNull(resolved);
        Assert.AreEqual("L", resolved.name);
        Assert.IsTrue(flipX);
    }

    [Test]
    public void AutotileResolver_WeightedRules_SameMaskAlwaysPicksSameVariant()
    {
        AutotileTileset tileset = CreateWeightedVariantTileset(heavyWeight: 4, lightWeight: 1);
        int[,] mask = new[,]
        {
            { 1, 1, 1 },
            { 1, 1, 1 },
            { 1, 1, 1 }
        };

        Sprite first = AutotileResolver.ResolveSprite(tileset, mask, out bool flipFirst);
        Sprite second = AutotileResolver.ResolveSprite(tileset, mask, out bool flipSecond);

        Assert.IsNotNull(first);
        Assert.AreSame(first, second);
        Assert.AreEqual(flipFirst, flipSecond);
        Assert.IsTrue(first.name == "A" || first.name == "B", $"Unexpected variant '{first.name}'.");
    }

    [Test]
    public void AutotileResolver_WeightedRules_NonPositiveWeightsStillResolve()
    {
        AutotileTileset tileset = CreateWeightedVariantTileset(heavyWeight: 0, lightWeight: 0);
        int[,] mask = new[,]
        {
            { 1, 1, 1 },
            { 1, 1, 1 },
            { 1, 1, 1 }
        };

        Sprite resolved = AutotileResolver.ResolveSprite(tileset, mask, out _);

        Assert.IsNotNull(resolved);
        Assert.IsTrue(resolved.name == "A" || resolved.name == "B", $"Unexpected variant '{resolved.name}'.");
    }

    [Test]
    public void AutotileResolver_UnmatchedMask_ReturnsFallbackSprite()
    {
        AutotileTileset tileset = CreateAsymmetricRuleTileset();
        int[,] mask = new[,]
        {
            { 0, 0, 0 },
            { 0, 1, 0 },
            { 0, 0, 0 }
        };

        Sprite resolved = AutotileResolver.ResolveSprite(tileset, mask, out bool flipX);

        Assert.IsNotNull(resolved);
        Assert.AreEqual(AutotileRuleTables.FallbackSpriteId, resolved.name);
        Assert.IsFalse(flipX);
    }

    private static void AssertSpriteQuadSpansTileCell(int x, int y, float tileSize, Sprite sprite)
    {
        Bounds bounds = sprite.bounds;
        float anchorX = x * tileSize - bounds.min.x;
        float anchorY = y * tileSize - bounds.min.y;
        float left = anchorX + bounds.min.x;
        float right = anchorX + bounds.max.x;
        float bottom = anchorY + bounds.min.y;
        float top = anchorY + bounds.max.y;

        Assert.AreEqual(x * tileSize, left, 0.0001f);
        Assert.AreEqual((x + 1) * tileSize, right, 0.0001f);
        Assert.AreEqual(y * tileSize, bottom, 0.0001f);
        Assert.AreEqual((y + 1) * tileSize, top, 0.0001f);
    }

    private static void AssertResolvesTo(AutotileTileset tileset, int[,] mask, string expectedSpriteId)
    {
        Sprite resolved = AutotileResolver.ResolveSprite(tileset, mask, out _);
        Assert.IsNotNull(resolved);
        Assert.AreEqual(expectedSpriteId, resolved.name);
        AssertMaskResolvesToMatchingRule(tileset, mask, expectedSpriteId);
    }

    private static void AssertMaskResolvesToMatchingRule(
        AutotileTileset tileset,
        int[,] mask,
        string expectedSpriteId)
    {
        IReadOnlyList<AutotileRule> rules = tileset.Rules;
        bool expectedMatches = false;
        for (int i = 0; i < rules.Count; i++)
        {
            if (rules[i].SpriteId == expectedSpriteId
                && (rules[i].Matches(mask, flipInput: false) || rules[i].Matches(mask, flipInput: true)))
            {
                expectedMatches = true;
                break;
            }
        }

        Assert.IsTrue(
            expectedMatches,
            $"Expected sprite '{expectedSpriteId}' to match the mask under ground rules.");
    }

    private static string ResolveVendorStyle(int[,] mask)
    {
        IReadOnlyList<AutotileRule> rules = AutotileRuleTables.Ground;
        for (int flipPass = 0; flipPass < 2; flipPass++)
        {
            bool flipInput = flipPass == 1;
            for (int i = 0; i < rules.Count; i++)
            {
                if (rules[i].Matches(mask, flipInput))
                {
                    return rules[i].SpriteId;
                }
            }
        }

        return AutotileRuleTables.FallbackSpriteId;
    }

    private static int[,] MaskForGroundRule(string spriteId)
    {
        IReadOnlyList<AutotileRule> rules = AutotileRuleTables.Ground;
        for (int i = 0; i < rules.Count; i++)
        {
            if (rules[i].SpriteId == spriteId)
            {
                return rules[i].ToMask();
            }
        }

        Assert.Fail($"Ground rule '{spriteId}' was not found.");
        return new int[3, 3];
    }

    private static int[,] IsolatedMask() => new[,]
    {
        { 0, 0, 0 },
        { 0, 1, 0 },
        { 0, 0, 0 }
    };

    private static int[,] TopLeftOuterCornerMask() => new[,]
    {
        { 0, 0, 0 },
        { 0, 1, 1 },
        { 0, 1, 1 }
    };

    private static int[,] VerticalRunMask() => MaskForGroundRule("21");

    private static int[,] TopTShapeMask() => new[,]
    {
        { 0, 1, 0 },
        { 0, 1, 1 },
        { 0, 1, 0 }
    };

    private static int[,] SurroundedMask() => new[,]
    {
        { 1, 1, 1 },
        { 1, 1, 1 },
        { 1, 1, 1 }
    };

    private static AutotileTileset CreateFullGroundTileset()
    {
        string[] names = new string[AutotileRuleTables.GroundSpriteCount];
        for (int i = 0; i < names.Length; i++)
        {
            names[i] = i.ToString();
        }

        Texture2D texture = new Texture2D(16, 16);
        return new AutotileTileset("BricksStyle", texture, CreateSprites(names));
    }

    private static AutotileTileset CreateCoverTileset()
    {
        Texture2D texture = new Texture2D(16, 16);
        return new AutotileTileset("GrassA", texture, CreateSprites("0", "1", "2", "3", "4", "5"));
    }

    private static AutotileTileset CreateTestTileset()
    {
        Texture2D texture = new Texture2D(16, 16);
        return new AutotileTileset("Test", texture, CreateSprites("9", "10", "20"));
    }

    private static AutotileTileset CreateAsymmetricRuleTileset()
    {
        AutotileTileset tileset = new AutotileTileset(
            "AsymmetricRules",
            new Texture2D(16, 16),
            CreateSprites("L", "20"));
        tileset.SetCustomRules(new List<AutotileRule>
        {
            new AutotileRule("L", new[,]
            {
                { 0, 1, 1 },
                { 0, 1, 1 },
                { 0, 1, 1 }
            })
        });

        return tileset;
    }

    private static AutotileTileset CreateWeightedVariantTileset(int heavyWeight, int lightWeight)
    {
        int[,] allSolid =
        {
            { 1, 1, 1 },
            { 1, 1, 1 },
            { 1, 1, 1 }
        };
        AutotileTileset tileset = new AutotileTileset(
            "WeightedVariants",
            new Texture2D(16, 16),
            CreateSprites("A", "B", "20"));
        tileset.SetCustomRules(new List<AutotileRule>
        {
            new AutotileRule("A", allSolid, heavyWeight),
            new AutotileRule("B", allSolid, lightWeight)
        });

        return tileset;
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
