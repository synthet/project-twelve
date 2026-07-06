using System;
using NUnit.Framework;
using ProjectTwelve.Sandbox.Registry;
using ProjectTwelve.Visual.Tiles;
using UnityEngine;

/// <summary>
/// Autotile exposure floor parity with tile-viz off-space air handling.
/// </summary>
public sealed class AutotileExposureTests
{
    [Test]
    public void ExposureFloor_BelowFloorCountsAsAirForSouthBlend()
    {
        SandboxTileVisualCatalog catalog = CreateCatalog();
        const int floorY = 0;

        SandboxTile Lookup(int x, int y)
        {
            if (y < 0)
            {
                return new SandboxTile(SandboxTileIds.Stone);
            }

            if (y <= 1)
            {
                return new SandboxTile(SandboxTileIds.Stone);
            }

            return new SandboxTile(SandboxTileIds.Air);
        }

        SandboxTile tile = Lookup(0, 0);
        int[,] withFloor = AutotileGroundResolve.BuildGroundMask(catalog, Lookup, tile, 0, 0, floorY);
        int[,] withoutFloor = AutotileGroundResolve.BuildGroundMask(
            catalog,
            Lookup,
            tile,
            0,
            0,
            AutotileExposure.NoFloor);

        Assert.AreEqual(0, withFloor[1, 2], "South center should be air when support is below exposure floor.");
        Assert.AreEqual(1, withoutFloor[1, 2], "South center should stay solid without floor clip.");
    }

    [Test]
    public void CreateIsSolid_NoFloor_MatchesDirectLookup()
    {
        SandboxTile Lookup(int x, int y) => y < 0 ? new SandboxTile(SandboxTileIds.Stone) : new SandboxTile(SandboxTileIds.Air);
        Func<int, int, bool> isSolid = AutotileExposure.CreateIsSolid(Lookup, AutotileExposure.NoFloor);
        Assert.IsTrue(isSolid(0, -1));
    }

    [Test]
    public void CreateIsSolid_WithFloor_IgnoresBelowFloor()
    {
        SandboxTile Lookup(int x, int y) => y < 0 ? new SandboxTile(SandboxTileIds.Stone) : new SandboxTile(SandboxTileIds.Air);
        Func<int, int, bool> isSolid = AutotileExposure.CreateIsSolid(Lookup, 0);
        Assert.IsFalse(isSolid(0, -1));
    }

    private static SandboxTileVisualCatalog CreateCatalog()
    {
        AutotileCatalog autotileCatalog = ScriptableObject.CreateInstance<AutotileCatalog>();
        autotileCatalog.SetGroundTilesets(new System.Collections.Generic.List<AutotileTileset>
        {
            CreateGroundTileset("Rocks"),
        });

        SandboxTileVisualCatalog catalog = ScriptableObject.CreateInstance<SandboxTileVisualCatalog>();
        var field = typeof(SandboxTileVisualCatalog).GetField(
            "autotileCatalog",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        field!.SetValue(catalog, autotileCatalog);
        return catalog;
    }

    private static AutotileTileset CreateGroundTileset(string name)
    {
        var sprites = new System.Collections.Generic.List<Sprite>();
        Texture2D texture = new Texture2D(16, 16);
        for (int i = 0; i < AutotileRuleTables.GroundSpriteCount; i++)
        {
            Sprite sprite = Sprite.Create(
                texture,
                new Rect(0, 0, 16, 16),
                new Vector2(0.5f, 0f),
                16f);
            sprite.name = i.ToString();
            sprites.Add(sprite);
        }

        return new AutotileTileset(name, texture, sprites);
    }
}
