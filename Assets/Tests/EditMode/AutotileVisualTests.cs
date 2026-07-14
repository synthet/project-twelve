using System;
using System.Collections.Generic;
using NUnit.Framework;
using ProjectTwelve.Sandbox.Registry;
using ProjectTwelve.Visual.Tiles;
using UnityEngine;
using UnityEngine.TestTools;

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
            (x, y) => y == 5 && (x == 4 || x == 5 || x == 6),
            5,
            5);

        Sprite resolved = AutotileResolver.ResolveSprite(tileset, mask, out _);

        Assert.IsNotNull(resolved);
        Assert.AreEqual("4", resolved.name);
    }

    [Test]
    public void AutotileResolver_GrassCoverEastEnd_ResolvesRule3WithFlipX()
    {
        AutotileTileset tileset = CreateCoverTileset();
        int[,] mask = AutotileMaskBuilder.BuildCoverMask(
            (x, y) => y == 5 && x == 4,
            5,
            5);

        Sprite resolved = AutotileResolver.ResolveSprite(tileset, mask, out bool flipX);

        Assert.IsNotNull(resolved);
        Assert.AreEqual("3", resolved.name);
        Assert.IsTrue(flipX);
    }

    [Test]
    public void AutotileResolver_GroundEastRunEnd_ResolvesRule0WithFlipX()
    {
        AutotileTileset tileset = CreateFullGroundTileset();
        int[,] mask = new[,]
        {
            { 0, 1, 1 },
            { 0, 1, 1 },
            { 0, 0, 0 }
        };

        Sprite resolved = AutotileResolver.ResolveSprite(tileset, mask, out bool flipX);

        Assert.IsNotNull(resolved);
        Assert.AreEqual("0", resolved.name);
        Assert.IsTrue(flipX);
    }

    [Test]
    public void AutotileResolver_OneSidedLipColumnMirror_ResolvesRule24WithFlipX()
    {
        AutotileTileset tileset = CreateFullGroundTileset();
        int[,] mask = new[,]
        {
            { 0, 1, 0 },
            { 0, 1, 0 },
            { 0, 0, 0 }
        };

        Sprite resolved = AutotileResolver.ResolveSprite(tileset, mask, out bool flipX);

        Assert.IsNotNull(resolved);
        Assert.AreEqual("24", resolved.name);
        Assert.IsTrue(flipX);
    }

    [Test]
    public void AutotileResolver_VerticalPillarMask_StaysSprite21WithoutFlipX()
    {
        AutotileTileset tileset = CreateFullGroundTileset();
        int[,] mask = VerticalRunMask();

        Sprite resolved = AutotileResolver.ResolveSprite(tileset, mask, out bool flipX);

        Assert.IsNotNull(resolved);
        Assert.AreEqual("21", resolved.name);
        Assert.IsFalse(flipX);
    }

    [Test]
    public void AutotileResolver_EastReentrantCorner_ResolvesRule16WithFlipX()
    {
        AutotileTileset tileset = CreateFullGroundTileset();
        int[,] eastCornerMask = new[,]
        {
            { 1, 1, 0 },
            { 1, 1, 0 },
            { 0, 0, 0 }
        };

        Sprite resolved = AutotileResolver.ResolveSprite(tileset, eastCornerMask, out bool flipX);

        Assert.IsNotNull(resolved);
        Assert.AreEqual("16", resolved.name);
        Assert.IsTrue(flipX);
    }

    [Test]
    public void AutotileResolver_CoverUnmatchedMask_FallsBackToSprite0()
    {
        AutotileTileset tileset = CreateCoverTileset();
        int[,] mask = new[,]
        {
            { 0, 0, 0 },
            { 0, 1, 0 },
            { 0, 0, 0 }
        };

        Sprite resolved = AutotileResolver.ResolveSprite(tileset, mask, out _);

        Assert.IsNotNull(resolved);
        Assert.AreEqual("0", resolved.name);
    }

    [Test]
    public void AutotileResolver_TryGetSpriteById_DoesNotFallbackForMissingSprite()
    {
        AutotileTileset tileset = CreateCoverTileset();

        bool found = AutotileResolver.TryGetSpriteById(tileset, "missing-sidecar-id", out Sprite sprite);

        Assert.IsFalse(found);
        Assert.IsNull(sprite);
    }

    [Test]
    public void SandboxChunkRenderer_MissingOverrideSpriteWarning_IsDeduplicatedAndContextual()
    {
        SandboxChunkRenderer.ClearMissingOverrideSpriteWarningsForTests();
        AutotileTileset tileset = new AutotileTileset("Humus", new Texture2D(16, 16), CreateSprites("0"));
        Vector2Int firstCell = new Vector2Int(12, 34);
        Vector2Int secondCell = new Vector2Int(56, 78);

        LogAssert.Expect(
            LogType.Warning,
            "SandboxChunkRenderer: missing autotile override sprite; ignoring visual entry " +
            "at cell (12, 34), layer 'ground', tileset 'Humus', sprite id 'missing-sidecar-id'.");

        SandboxChunkRenderer.WarnMissingOverrideSpriteOnce(tileset, "ground", "missing-sidecar-id", firstCell);
        SandboxChunkRenderer.WarnMissingOverrideSpriteOnce(tileset, "ground", "missing-sidecar-id", secondCell);
        SandboxChunkRenderer.WarnMissingOverrideSpriteOnce(tileset, "ground", "missing-sidecar-id", firstCell);

        LogAssert.NoUnexpectedReceived();
    }

    [Test]
    public void AutotileRule_FlipColumnsMirrorsWestEastComparison()
    {
        AutotileRule rule = new AutotileRule("3", new[,]
        {
            { 0, 0, 0 },
            { 0, 1, 0 },
            { 0, 1, 1 }
        });

        int[,] mask = new[,]
        {
            { 0, 1, 1 },
            { 0, 1, 0 },
            { 0, 0, 0 }
        };

        Assert.IsTrue(rule.MatchesColumns(mask, flipColumns: true));
    }

    [Test]
    public void AutotileCoverMask_StoneCliffBesideGrass_SetsCliffWithoutSharedGroundGroup()
    {
        // West neighbor is a two-tall solid wall (ground here and above) -> a rising cliff step.
        int[,] mask = AutotileMaskBuilder.BuildCoverMask(
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
            (x, y) => (x == 4 && y == 6) || (x == 4 && y == 5),
            5,
            5);

        // West neighbor is a two-tall solid wall -> west cell (center row, west col) is a cliff step.
        Assert.AreEqual(2, mask[0, 1]);
        Assert.AreEqual(1, mask[1, 1]);
        Assert.AreEqual(0, mask[2, 1]);
    }

    [Test]
    public void AutotileCoverMask_ConnectsHorizontalGrassRun()
    {
        // Exposed-top ground to the west and east (air above each), so the cover run continues flat.
        int[,] mask = AutotileMaskBuilder.BuildCoverMask(
            (x, y) => y == 5 && (x == 4 || x == 5 || x == 6),
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
    public void AutotileMaskBuilder_MaterialBoundary_DoesNotBlendForeignSolidBelow()
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
            (x, y) => x == worldX && (y == worldY - 1 || y == worldY || y == worldY + 1),
            worldX,
            worldY);

        Assert.AreEqual(0, dirtMask[1, 2], "Vendor GetMask: foreign solid below must not connect.");

        int[,] stoneMask = AutotileMaskBuilder.BuildGroundMask(
            (x, y) =>
            {
                if (x == worldX && y == worldY)
                {
                    return false;
                }

                return x == worldX && (y == worldY - 1 || y == worldY - 2);
            },
            (x, y) => x == worldX && (y == worldY || y == worldY - 1 || y == worldY - 2),
            worldX,
            worldY - 1);

        Assert.AreEqual(0, stoneMask[1, 0], "Stone should not connect to dirt above.");

        AutotileTileset tileset = CreateFullGroundTileset();
        Sprite dirtSprite = AutotileResolver.ResolveSprite(tileset, dirtMask, out _);
        Assert.AreEqual("29", dirtSprite.name, "Dirt column over foreign stone reads vendor bottom cap.");
    }

    [Test]
    public void AutotileMaskBuilder_MaterialBoundaryCorner_RemapsWestLipToCornerCap()
    {
        const int worldX = 5;
        const int worldY = 6;

        int[,] mask = AutotileMaskBuilder.BuildGroundMask(
            (x, y) =>
                (x == worldX && (y == worldY || y == worldY - 1))
                || (x == worldX + 1 && y == worldY),
            (x, y) =>
                (x == worldX && (y == worldY || y == worldY - 1))
                || (x == worldX + 1 && y == worldY)
                || (x == worldX - 1 && y == worldY),
            worldX,
            worldY);

        Assert.AreEqual(0, mask[0, 2]);
        Assert.AreEqual(1, mask[1, 1]);
        Assert.AreEqual(1, mask[2, 1]);

        AutotileTileset tileset = CreateFullGroundTileset();
        Sprite resolved = AutotileResolver.ResolveSprite(tileset, mask, out bool flipX);
        Assert.AreEqual("7", resolved.name);
        Assert.IsTrue(flipX);
    }

    [Test]
    public void AutotileMaskBuilder_MaterialBoundaryCorner_RemapsWestLipWithNorthConnectedToRule16()
    {
        const int worldX = 5;
        const int worldY = 6;

        int[,] mask = AutotileMaskBuilder.BuildGroundMask(
            (x, y) =>
                (x == worldX && (y == worldY || y == worldY - 1 || y == worldY + 1))
                || (x == worldX + 1 && (y == worldY || y == worldY + 1)),
            (x, y) =>
                (x == worldX && (y == worldY || y == worldY - 1 || y == worldY + 1))
                || (x == worldX + 1 && (y == worldY || y == worldY + 1))
                || (x == worldX - 1 && y == worldY),
            worldX,
            worldY);

        AutotileTileset tileset = CreateFullGroundTileset();
        Sprite resolved = AutotileResolver.ResolveSprite(tileset, mask, out _);
        Assert.AreEqual("30", resolved.name);
    }

    [Test]
    public void AutotileMaskBuilder_VisualMask_ExcludesForeignStoneOnSides()
    {
        const int worldX = 10;
        const int worldY = 5;

        int[,] visual = AutotileMaskBuilder.BuildVisualGroundMask(
            (x, y) => x == worldX && (y == worldY || y == worldY + 1),
            worldX,
            worldY);

        Assert.AreEqual(0, visual[0, 1], "Stone west must not appear in visual mask.");
        Assert.AreEqual(0, visual[2, 1]);
    }

    [Test]
    public void AutotileMaskBuilder_SolidMask_IncludesForeignStoneBelow()
    {
        const int worldX = 10;
        const int worldY = 5;

        int[,] solid = AutotileMaskBuilder.BuildSolidGroundMask(
            (x, y) => x == worldX && (y == worldY - 1 || y == worldY || y == worldY + 1),
            worldX,
            worldY);

        Assert.AreEqual(1, solid[1, 2], "Foreign solid below counts in solid mask.");
    }

    [Test]
    public void AutotileMaskBuilder_DetailedResult_ReportsMaterialBoundaryNormalization()
    {
        GroundMaskBuildResult result = AutotileMaskBuilder.BuildGroundMaskDetailed(
            (x, y) => x >= 0 && x <= 2 && y == 20,
            (x, y) => y == 19 ? x >= 0 && x <= 3 : x >= 0 && x <= 3 && y == 20,
            2,
            20,
            null);

        Assert.IsFalse(result.MaterialBoundaryRemap);
        AutotileTileset tileset = CreateFullGroundTileset();
        Sprite resolved = AutotileResolver.ResolveSprite(tileset, result.FinalMask, out bool flipX);
        Assert.AreEqual("24", resolved.name);
        Assert.IsTrue(flipX);
    }

    [Test]
    public void AutotileMaskBuilder_BuildNormalizationTrace_MirrorsTileVizVocabulary()
    {
        // Must stay field-for-field identical to buildNormalizationTrace in tile-viz maskBuilder.js.
        CollectionAssert.AreEqual(
            new[] { "stairInterior: skipped", "innerCavity: skipped", "cavityUnderside: skipped", "materialBoundary: skipped" },
            AutotileMaskBuilder.BuildNormalizationTrace(false, false, false, false));

        CollectionAssert.AreEqual(
            new[] { "stairInterior: skipped", "innerCavity: skipped", "cavityUnderside: applied: bridge -> underside" },
            AutotileMaskBuilder.BuildNormalizationTrace(false, false, true, false));

        CollectionAssert.AreEqual(
            new[] { "stairInterior: applied: diagonal step -> interior fill" },
            AutotileMaskBuilder.BuildNormalizationTrace(true, false, false, false));
    }

    [Test]
    public void AutotileMaskBuilder_DetailedResult_TraceIsConsistentWithFlags()
    {
        GroundMaskBuildResult result = AutotileMaskBuilder.BuildGroundMaskDetailed(
            (x, y) => x >= 0 && x <= 2 && y == 20,
            (x, y) => y == 19 ? x >= 0 && x <= 3 : x >= 0 && x <= 3 && y == 20,
            2,
            20,
            null);

        Assert.IsNotNull(result.NormalizationTrace);
        Assert.AreEqual(
            AutotileMaskBuilder.BuildNormalizationTrace(
                result.StairInteriorRemap,
                result.InnerCavityRemap,
                result.CavityUndersideRemap,
                result.MaterialBoundaryRemap),
            result.NormalizationTrace);
    }

    [Test]
    public void AutotileMaskBuilder_ReentrantDirtBesideStone_ResolvesRule11NotInteriorFill()
    {
        GroundMaskBuildResult result = AutotileMaskBuilder.BuildGroundMaskDetailed(
            (x, y) =>
                (x == 1 && (y == 1 || y == 2))
                || (x == 2 && (y == 1 || y == 2)),
            (x, y) =>
                (x == 0 && y >= 0 && y <= 2)
                || (x == 1 && y >= 0 && y <= 2)
                || (x == 2 && y >= 0 && y <= 2),
            1,
            1,
            null);

        AutotileTileset tileset = CreateFullGroundTileset();
        Sprite resolved = AutotileResolver.ResolveSprite(tileset, result.FinalMask, out _);
        Assert.AreEqual("16", resolved.name);
        Assert.AreNotEqual("9", resolved.name);
        Assert.AreNotEqual("10", resolved.name);
    }

    [Test]
    public void AutotileMaskBuilder_CeilingUnderside_ResolvesAuthoredUndersideSprites()
    {
        // Ceiling-attached middle: mass above and to both sides, air below (mask[x, y] columns).
        int[,] ceilingMiddle = new[,]
        {
            { 1, 1, 0 },
            { 1, 1, 0 },
            { 1, 1, 0 }
        };

        int[,] normalized = AutotileMaskBuilder.NormalizeGroundMask(ceilingMiddle, null, null, 0, 0, null);
        Assert.AreEqual(ceilingMiddle, normalized, "Underside masks must not be remapped to top families.");

        AutotileTileset tileset = CreateFullGroundTileset();
        Sprite middle = AutotileResolver.ResolveSprite(tileset, ceilingMiddle, out bool middleFlip);
        Assert.AreEqual("17", middle.name, "Cave ceiling middle should use the authored underside sprite.");
        Assert.IsFalse(middleFlip);

        // Hanging platform west corner: mass above and east, air west/below.
        int[,] hangingCorner = new[,]
        {
            { 0, 0, 0 },
            { 1, 1, 0 },
            { 1, 1, 0 }
        };

        Sprite corner = AutotileResolver.ResolveSprite(tileset, hangingCorner, out bool cornerFlip);
        Assert.AreEqual("16", corner.name, "Hanging corner should use the authored underside cap.");
        Assert.IsFalse(cornerFlip);
    }

    [Test]
    public void AutotileMaskBuilder_StairInteriorDiagonal_RemapsBesideSurfaceStep()
    {
        int[,] stairSupport = new[,]
        {
            { 1, 1, 1 },
            { 1, 1, 1 },
            { 0, 1, 1 }
        };

        Assert.IsTrue(AutotileMaskBuilder.TryRemapStairInteriorDiagonalMask(
            stairSupport,
            (x, y) => x == 1 && y == 0,
            0,
            0,
            out int[,] remapped));

        Assert.AreEqual(1, remapped[0, 0]);
        Assert.AreEqual(1, remapped[1, 0]);
        Assert.AreEqual(1, remapped[2, 0]);

        AutotileTileset tileset = CreateFullGroundTileset();
        Sprite resolved = AutotileResolver.ResolveSprite(tileset, remapped, out bool flipX);
        Assert.IsTrue(resolved.name == "9" || resolved.name == "10");
        Assert.IsFalse(flipX);
    }

    [Test]
    public void AutotileMaskBuilder_StairInteriorDiagonal_KeepsOverhangCornerWithoutSurfaceStep()
    {
        int[,] overhangCorner = new[,]
        {
            { 1, 1, 1 },
            { 1, 1, 1 },
            { 0, 1, 1 }
        };

        Assert.IsFalse(AutotileMaskBuilder.TryRemapStairInteriorDiagonalMask(
            overhangCorner,
            (x, y) => false,
            0,
            0,
            out _));

        AutotileTileset tileset = CreateFullGroundTileset();
        Sprite resolved = AutotileResolver.ResolveSprite(tileset, overhangCorner, out bool flipX);
        Assert.AreEqual("11", resolved.name);
        Assert.IsTrue(flipX);
    }

    [Test]
    public void AutotileMaskBuilder_CavityBridgeLintel_RemapsToUndersideRule17()
    {
        int[,] bridge = new[,]
        {
            { 0, 1, 0 },
            { 0, 1, 0 },
            { 0, 1, 0 }
        };

        Assert.IsTrue(AutotileMaskBuilder.IsBridgeMask(bridge));

        bool SharesGroup(int x, int y) => y == 2 && x >= 0 && x <= 2;
        bool IsSolid(int x, int y) => SharesGroup(x, y);

        Assert.IsTrue(AutotileMaskBuilder.TryRemapCavityBridgeToUnderside(
            bridge,
            SharesGroup,
            IsSolid,
            1,
            2,
            out int[,] remapped));

        Assert.AreEqual(1, remapped[0, 0]);
        Assert.AreEqual(0, remapped[0, 2]);

        AutotileTileset tileset = CreateFullGroundTileset();
        Sprite resolved = AutotileResolver.ResolveSprite(tileset, remapped, out bool flipX);
        Assert.AreEqual("17", resolved.name);
        Assert.IsFalse(flipX);
    }

    [Test]
    public void AutotileMaskBuilder_CavityInnerCornerMask_PreservesVendorSprite18()
    {
        int[,] westCorner = new[,]
        {
            { 1, 1, 1 },
            { 1, 1, 1 },
            { 1, 1, 0 }
        };

        Assert.IsTrue(AutotileMaskBuilder.IsCavityInnerCornerMask(westCorner));
        Assert.IsFalse(AutotileMaskBuilder.TryRemapCavityLintelToUnderside(
            westCorner,
            (_, _) => true,
            -104,
            26,
            out _));

        AutotileTileset tileset = CreateFullGroundTileset();
        Sprite resolved = AutotileResolver.ResolveSprite(tileset, westCorner, out bool flipX);
        Assert.AreEqual("18", resolved.name);
        Assert.IsTrue(flipX);
    }

    [Test]
    public void AutotileMaskBuilder_CavityFlatLintelSpan_StillResolvesUnderside17()
    {
        int[,] flatMiddle = new[,]
        {
            { 1, 1, 0 },
            { 1, 1, 0 },
            { 1, 1, 0 }
        };

        Assert.IsFalse(AutotileMaskBuilder.IsCavityInnerCornerMask(flatMiddle));

        AutotileTileset tileset = CreateFullGroundTileset();
        Sprite resolved = AutotileResolver.ResolveSprite(tileset, flatMiddle, out bool flipX);
        Assert.AreEqual("17", resolved.name);
        Assert.IsFalse(flipX);
    }

    [Test]
    public void AutotileMaskBuilder_OpenSkyBridgeLintel_ResolvesVendorRule25WithoutNormalization()
    {
        // Mirrors tools/tile-viz/test/fixtures/snippets/open-sky-bridge-lintel.json (center 0,1).
        bool SharesGroup(int x, int y) =>
            (y == 1 && x >= -1 && x <= 1)
            || (y == 0 && (x == -1 || x == 1));

        bool IsSolid(int x, int y) => SharesGroup(x, y);

        GroundMaskBuildResult result = AutotileMaskBuilder.BuildGroundMaskDetailed(
            SharesGroup,
            IsSolid,
            0,
            1,
            null);

        Assert.IsFalse(result.StairInteriorRemap);
        Assert.IsFalse(result.InnerCavityRemap);
        Assert.IsFalse(result.CavityUndersideRemap);
        Assert.IsFalse(result.MaterialBoundaryRemap);
        Assert.AreEqual(0, result.FinalMask[0, 0]);
        Assert.AreEqual(0, result.FinalMask[1, 0]);
        Assert.AreEqual(0, result.FinalMask[2, 0]);
        Assert.AreEqual(1, result.FinalMask[0, 1]);
        Assert.AreEqual(1, result.FinalMask[1, 1]);
        Assert.AreEqual(1, result.FinalMask[2, 1]);
        Assert.AreEqual(0, result.FinalMask[0, 2]);
        Assert.AreEqual(0, result.FinalMask[1, 2]);
        Assert.AreEqual(0, result.FinalMask[2, 2]);

        AutotileTileset tileset = CreateFullGroundTileset();
        Sprite resolved = AutotileResolver.ResolveSprite(tileset, result.FinalMask, out bool flipX);
        Assert.AreEqual("25", resolved.name);
        Assert.IsFalse(flipX);
    }

    [Test]
    public void AutotileMaskBuilder_CliffFaceWithSideMass_ResolvesVendorFaceSprites()
    {
        AutotileTileset tileset = CreateFullGroundTileset();
        bool IsDirt(int x, int y) => (x == 0 || x == 1) && y >= 2 && y <= 4;

        // West face of the column: open west, mass east → authored west-open face, no pillar remap.
        int[,] westFace = AutotileMaskBuilder.BuildGroundMask(IsDirt, IsDirt, 0, 3);
        Sprite west = AutotileResolver.ResolveSprite(tileset, westFace, out bool westFlip);
        Assert.AreEqual("8", west.name, "Face with side mass must not collapse to pillar sprite 21.");
        Assert.IsFalse(westFlip);

        // East face mirrors via flipX on the same sprite id (vendor baseline).
        int[,] eastFace = AutotileMaskBuilder.BuildGroundMask(IsDirt, IsDirt, 1, 3);
        Sprite east = AutotileResolver.ResolveSprite(tileset, eastFace, out bool eastFlip);
        Assert.AreEqual("8", east.name);
        Assert.IsTrue(eastFlip);

        // Face top cap keeps its outside-corner mask.
        int[,] topCap = AutotileMaskBuilder.BuildGroundMask(IsDirt, IsDirt, 0, 4);
        Sprite cap = AutotileResolver.ResolveSprite(tileset, topCap, out bool capFlip);
        Assert.AreEqual("0", cap.name);
        Assert.IsFalse(capFlip);
    }

    [Test]
    public void AutotileMaskBuilder_VerticalWallRun_ResolvesTopMiddleBottomCaps()
    {
        AutotileTileset tileset = CreateFullGroundTileset();
        bool IsDirt(int x, int y) =>
            (x == 0 && y >= 2 && y <= 4)
            || (x >= 1 && x <= 2 && y >= 0 && y <= 2);

        AssertVerticalWallSprite(tileset, 0, 4, IsDirt, "28", false);
        AssertVerticalWallSprite(tileset, 0, 3, IsDirt, "21", false);
        AssertVerticalWallSprite(tileset, 0, 2, IsDirt, "15", true);
    }

    private static void AssertVerticalWallSprite(
        AutotileTileset tileset,
        int worldX,
        int worldY,
        Func<int, int, bool> isDirt,
        string expectedSpriteId,
        bool expectedFlipX)
    {
        int[,] mask = AutotileMaskBuilder.BuildGroundMask(
            isDirt,
            isDirt,
            worldX,
            worldY);
        Sprite resolved = AutotileResolver.ResolveSprite(tileset, mask, out bool flipX);
        Assert.IsNotNull(resolved);
        Assert.AreEqual(expectedSpriteId, resolved.name);
        Assert.AreEqual(expectedFlipX, flipX);
    }

    [Test]
    public void AutotileMaskBuilder_CoverExposedGroundNeighbor_ContinuesRunRegardlessOfMaterial()
    {
        // Vendor cover is material-agnostic: an exposed-top ground cell east of the tile (solid on
        // its row, air above) carries its own cover, so the run continues (1) rather than reading as
        // a cliff (2). A same-row ground body with air above is a surface, not a wall.
        int[,] mask = AutotileMaskBuilder.BuildCoverMask(
            (x, y) => y == 5 && (x == 4 || x == 6),
            5,
            5);

        Assert.AreEqual(1, mask[2, 1], "Exposed ground east of the tile continues the cover run.");
        Assert.AreEqual(1, mask[0, 1], "Exposed ground west of the tile continues the cover run.");
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
    public void SpriteMeshQuad_FlipX_AsymmetricBounds_MirrorsWithinCell()
    {
        const float tileSize = 1f;
        const int x = 2;
        const int y = 4;
        Sprite sprite = Sprite.Create(
            new Texture2D(16, 16),
            new Rect(0, 0, 16, 16),
            new Vector2(0f, 0f),
            16f);

        AutotileSpriteMeshBuilder.GetTileCellBounds(
            x,
            y,
            tileSize,
            sprite,
            flipX: false,
            out float unflippedLeft,
            out float unflippedRight,
            out _,
            out _);
        AutotileSpriteMeshBuilder.GetTileCellBounds(
            x,
            y,
            tileSize,
            sprite,
            flipX: true,
            out float flippedLeft,
            out float flippedRight,
            out _,
            out _);

        Assert.AreEqual(x * tileSize, unflippedLeft, 0.0001f);
        Assert.AreEqual((x + 1) * tileSize, unflippedRight, 0.0001f);
        Assert.AreEqual(x * tileSize, flippedLeft, 0.0001f);
        Assert.AreEqual((x + 1) * tileSize, flippedRight, 0.0001f);
        Assert.IsTrue(AutotileSpriteMeshBuilder.SpansFullTileCell(x, y, tileSize, sprite, flipX: true));
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
    public void AutotileVisualOverrideMap_KeysByCellLayerAndTileset()
    {
        AutotileVisualOverrideMap overrides = new AutotileVisualOverrideMap();
        Vector2Int cell = new Vector2Int(3, 4);

        overrides.SetOverride(new AutotileVisualOverride(
            cell,
            AutotileVisualLayer.Ground,
            "Humus",
            autoSpriteId: string.Empty,
            autoFlipX: false,
            overrideSpriteId: "17"));

        Assert.IsTrue(overrides.TryGetOverride(cell, AutotileVisualOverrideMap.GroundLayer, "Humus", out AutotileVisualOverride entry));
        Assert.AreEqual("17", entry.overrideSpriteId);
        Assert.IsFalse(overrides.TryGetOverride(cell, AutotileVisualOverrideMap.CoverLayer, "Humus", out _));
        Assert.IsFalse(overrides.TryGetOverride(cell, AutotileVisualOverrideMap.GroundLayer, "Rocks", out _));
    }

    [Test]
    public void AutotileTileset_TryGetSpriteOnlyReturnsExactSpriteId()
    {
        AutotileTileset tileset = CreateTestTileset();

        Assert.IsTrue(tileset.TryGetSprite("10", out Sprite sprite));
        Assert.AreEqual("10", sprite.name);
        Assert.IsFalse(tileset.TryGetSprite("missing", out _));
    }

    [Test]
    public void SpriteMeshQuad_FixedCellOverridePathSpansFullTileCell()
    {
        const float tileSize = 1f;
        Sprite sprite = Sprite.Create(
            new Texture2D(16, 16),
            new Rect(0, 0, 16, 16),
            new Vector2(0.5f, 0.5f),
            16f);

        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector2> uvs = new List<Vector2>();
        List<Color> colors = new List<Color>();

        AutotileSpriteMeshBuilder.AppendFixedCellQuad(
            vertices,
            triangles,
            uvs,
            colors,
            2,
            5,
            tileSize,
            sprite,
            flipX: false,
            Color.white,
            zOffset: 0f,
            flipY: false,
            rotationDegrees: 0);

        Assert.AreEqual(new Vector3(2f, 5f, 0f), vertices[0]);
        Assert.AreEqual(new Vector3(3f, 6f, 0f), vertices[2]);
        Assert.AreEqual(6, triangles.Count);
    }

    [Test]
    public void SandboxTileVisualCatalog_BrickTilesDoNotShareGroundGroup()
    {
        SandboxTileVisualCatalog catalog = ScriptableObject.CreateInstance<SandboxTileVisualCatalog>();

        Assert.IsTrue(catalog.SharesGroundAutotileGroup(SandboxTileIds.BricksA, SandboxTileIds.BricksA));
        Assert.IsFalse(catalog.SharesGroundAutotileGroup(SandboxTileIds.BricksA, SandboxTileIds.BricksB));
        Assert.IsFalse(catalog.SharesGroundAutotileGroup(SandboxTileIds.BricksB, SandboxTileIds.BricksC));
        Assert.IsTrue(catalog.SharesGroundAutotileGroup(SandboxTileIds.BricksD, SandboxTileIds.BricksD));

        int frozen = SandboxRegistries.Tiles.GetIndex("core:frozen");
        int magma = SandboxRegistries.Tiles.GetIndex("core:magma");
        int sand = SandboxRegistries.Tiles.GetIndex("core:sand");
        Assert.IsTrue(catalog.SharesGroundAutotileGroup(frozen, frozen));
        Assert.IsTrue(catalog.SharesGroundAutotileGroup(magma, magma));
        Assert.IsTrue(catalog.SharesGroundAutotileGroup(sand, sand));
        Assert.IsFalse(catalog.SharesGroundAutotileGroup(frozen, magma));
        Assert.IsFalse(catalog.SharesGroundAutotileGroup(magma, sand));
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
