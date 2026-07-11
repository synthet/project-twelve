using System;
using System.Collections.Generic;
using NUnit.Framework;
using ProjectTwelve.Visual.AutotileDebug;
using ProjectTwelve.Visual.Tiles;
using UnityEngine;

public sealed class AutotileDebugTests
{
    [Test]
    public void GroundAutotileDebugModes_Cycle_WalksAllModesWithoutGaps()
    {
        var mode = GroundAutotileDebugMode.Off;
        var seen = new HashSet<GroundAutotileDebugMode>();
        do
        {
            Assert.IsFalse(seen.Contains(mode), $"Cycle repeated before completing: {mode}");
            seen.Add(mode);
            mode = GroundAutotileDebugModes.Cycle(mode);
        }
        while (mode != GroundAutotileDebugMode.Off);

        Assert.AreEqual(5, seen.Count);
    }

    [Test]
    public void GroundAutotileDebugModes_Normalize_MapsLegacyEnumValues()
    {
        // 1, 2, 4, 5 are live enum members (Normalize returns them unchanged); only the
        // removed values 3/8 and 6/7/9 are remapped. (Value 1 is VisualOverrideEdit since
        // the enum was renumbered in the visual-override work.)
        Assert.AreEqual(GroundAutotileDebugMode.VisualOverrideEdit, GroundAutotileDebugModes.Normalize((GroundAutotileDebugMode)1));
        Assert.AreEqual(GroundAutotileDebugMode.SpriteIdLabel, GroundAutotileDebugModes.Normalize((GroundAutotileDebugMode)2));
        Assert.AreEqual(GroundAutotileDebugMode.SpriteIdLabel, GroundAutotileDebugModes.Normalize((GroundAutotileDebugMode)3));
        Assert.AreEqual(GroundAutotileDebugMode.GroundCoverSplit, GroundAutotileDebugModes.Normalize((GroundAutotileDebugMode)4));
        Assert.AreEqual(GroundAutotileDebugMode.VisualOverrideLabel, GroundAutotileDebugModes.Normalize((GroundAutotileDebugMode)6));
        Assert.AreEqual(GroundAutotileDebugMode.SpriteIdLabel, GroundAutotileDebugModes.Normalize((GroundAutotileDebugMode)8));
    }

    [Test]
    public void GroundAutotileDebugModes_FormatLogLine_IncludesModeNameAndIndex()
    {
        string line = GroundAutotileDebugModes.FormatLogLine(GroundAutotileDebugMode.VisualOverrideEdit);
        StringAssert.Contains("VisualOverrideEdit", line);
        StringAssert.Contains("2/5", line);
    }

    [Test]
    public void GroundAutotileDebugModes_IsOverlayActive()
    {
        Assert.IsFalse(GroundAutotileDebugModes.IsOverlayActive(GroundAutotileDebugMode.Off));
        Assert.IsFalse(GroundAutotileDebugModes.IsOverlayActive(GroundAutotileDebugMode.VisualOverrideEdit));
        Assert.IsTrue(GroundAutotileDebugModes.IsOverlayActive(GroundAutotileDebugMode.SpriteIdLabel));
    }

    [Test]
    public void GroundAutotileDebugModes_IsVisualOverrideEdit()
    {
        Assert.IsTrue(GroundAutotileDebugModes.IsVisualOverrideEdit(GroundAutotileDebugMode.VisualOverrideEdit));
        Assert.IsFalse(GroundAutotileDebugModes.IsVisualOverrideEdit(GroundAutotileDebugMode.SpriteIdLabel));
    }

    [Test]
    public void FormatHoverCoordinates_NegativeWorld_MapsChunkAndLocal()
    {
        string text = GroundAutotileDebugCoordinates.FormatHoverCoordinates(-1, -1);
        StringAssert.Contains("World (-1, -1)", text);
        StringAssert.Contains("Chunk (-1, -1) local (31, 31)", text);
    }

    [Test]
    public void AutotileDebugPalette_SpriteIdColors_AreDeterministic()
    {
        Color first = AutotileDebugPalette.ColorForSpriteId("16");
        Color second = AutotileDebugPalette.ColorForSpriteId("16");
        Assert.AreEqual(first, second);
        Assert.AreEqual(AutotileDebugPalette.OverlayAlpha, first.a, 0.001f);

        Color other = AutotileDebugPalette.ColorForSpriteId("24");
        Assert.AreNotEqual(first, other);
    }

    [Test]
    public void AutotileGroundResolve_MatchesChunkRendererMask()
    {
        AutotileTileset tileset = CreateGroundTileset();
        SandboxTileVisualCatalog catalog = CreateCatalog(tileset);
        SandboxTile center = new SandboxTile(SandboxTileIds.Dirt);
        Func<int, int, SandboxTile> tileLookup = (x, y) =>
            x == 5 && (y == 5 || y == 6) ? center : default;

        int[,] mask = AutotileGroundResolve.BuildGroundMask(
            catalog,
            tileLookup,
            center,
            5,
            6);

        Assert.AreEqual(0, mask[0, 1]);
        Assert.AreEqual(1, mask[1, 1]);
        Assert.AreEqual(0, mask[2, 1]);

        bool ok = AutotileGroundResolve.TryResolve(
            catalog,
            tileLookup,
            center,
            5,
            6,
            out AutotileGroundResolveResult result);

        Assert.IsTrue(ok);
        Assert.IsTrue(result.Resolved);
        Assert.IsNotNull(result.Mask);
    }

    [Test]
    public void AutotileDebugMeshBuilder_MaskToCompactString_FormatsNorthToSouthRows()
    {
        int[,] mask = Mask(
            0, 1, 1,
            0, 1, 1,
            0, 0, 0);

        Assert.AreEqual("011/011/000", AutotileDebugMeshBuilder.MaskToCompactString(mask));
    }

    [Test]
    public void AutotileDebugMeshBuilder_LabelMode_AddsExtraVertices()
    {
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector2> uvs = new List<Vector2>();
        List<Color> colors = new List<Color>();

        AutotileDebugMeshBuilder.AppendTileMarker(vertices, triangles, uvs, colors, 0, 0, 1f, Color.red);
        int markerVertices = vertices.Count;

        AutotileDebugMeshBuilder.AppendSpriteIdLabel(
            vertices,
            triangles,
            uvs,
            colors,
            0,
            0,
            1f,
            "16",
            flipX: true);

        Assert.Greater(vertices.Count, markerVertices);
        Assert.IsNotNull(AutotileDebugMeshBuilder.GetDigitAtlas());
    }

    [Test]
    public void AutotileDebugMeshBuilder_SignedIntegerLabel_RendersMinus()
    {
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector2> uvs = new List<Vector2>();
        List<Color> colors = new List<Color>();

        AutotileDebugMeshBuilder.AppendSignedIntegerLabel(
            vertices,
            triangles,
            uvs,
            colors,
            0,
            0,
            1f,
            -7,
            AutotileDebugPalette.LabelColor,
            verticalOffsetTiles: 0f);

        Assert.AreEqual(8, vertices.Count, "Expected two quads for '-7'");
    }

    [Test]
    public void AutotileDebugMeshBuilder_CoordinateLabel_AddsVerticesForNegativeX()
    {
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector2> uvs = new List<Vector2>();
        List<Color> colors = new List<Color>();

        AutotileDebugMeshBuilder.AppendTileMarker(vertices, triangles, uvs, colors, 0, 0, 1f, Color.red);
        int markerVertices = vertices.Count;

        AutotileDebugMeshBuilder.AppendCoordinateLabel(
            vertices,
            triangles,
            uvs,
            colors,
            0,
            0,
            1f,
            -114,
            29);

        Assert.Greater(vertices.Count, markerVertices);
    }

    [Test]
    public void AutotileBaselineStore_LoadsMountainBaselineInEditor()
    {
        AutotileBaselineStore.ClearCache();
        IReadOnlyDictionary<Vector2Int, BaselineCell> baseline = AutotileBaselineStore.TryLoad("sandbox-scene-mountain");
        Assert.NotNull(baseline, "Expected mountain baseline under StreamingAssets or tools/tile-viz/test/fixtures/baselines");
        Assert.IsTrue(baseline.ContainsKey(new Vector2Int(-114, 29)), "Window lintel cell missing from baseline");
    }

    [Test]
    public void AutotileBaselineCompare_DetectsSpriteMismatch()
    {
        var baseline = new BaselineCell(
            tileId: 1,
            groundSpriteId: "17",
            groundFlipX: false,
            innerCavity: true,
            coverRendered: false,
            coverSpriteId: null,
            coverFlipX: false);

        var resolve = new AutotileGroundResolveResult(
            hasGroundTileset: true,
            resolved: true,
            spriteId: "18",
            flipX: false,
            tilesetName: "Humus",
            mask: null);

        Assert.IsFalse(AutotileBaselineCompare.GroundMatches(1, resolve, baseline));
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

    private static AutotileTileset CreateGroundTileset()
    {
        Texture2D texture = new Texture2D(16, 16);
        List<Sprite> sprites = new List<Sprite>();
        for (int i = 0; i < 32; i++)
        {
            sprites.Add(Sprite.Create(texture, new Rect(0, 0, 16, 16), new Vector2(0.5f, 0f), 16f));
            sprites[i].name = i.ToString();
        }

        return new AutotileTileset("Humus", texture, sprites);
    }

    private static SandboxTileVisualCatalog CreateCatalog(AutotileTileset tileset)
    {
        AutotileCatalog autotileCatalog = ScriptableObject.CreateInstance<AutotileCatalog>();
        autotileCatalog.SetGroundTilesets(new List<AutotileTileset> { tileset });

        SandboxTileVisualCatalog catalog = ScriptableObject.CreateInstance<SandboxTileVisualCatalog>();
        var field = typeof(SandboxTileVisualCatalog).GetField(
            "autotileCatalog",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        field.SetValue(catalog, autotileCatalog);
        return catalog;
    }
}
