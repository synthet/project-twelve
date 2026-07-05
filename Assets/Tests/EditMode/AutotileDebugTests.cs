using System;
using System.Collections.Generic;
using NUnit.Framework;
using ProjectTwelve.Visual.AutotileDebug;
using ProjectTwelve.Visual.Tiles;
using UnityEngine;

public sealed class AutotileDebugTests
{
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
