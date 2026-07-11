using NUnit.Framework;
using ProjectTwelve.Visual.Tiles;

public sealed class AutotileVisualOverrideRenderTests
{
    [Test]
    public void GroundOverride_KeepsAutomaticSnapshotAndUsesOverrideDecision()
    {
        AutotileTileset tileset = CreateFullGroundTileset();
        int[,] mask = new[,]
        {
            { 0, 1, 0 },
            { 1, 1, 0 },
            { 1, 1, 0 }
        };

        string autoSpriteId = AutotileResolver.ResolveSpriteId(tileset, mask, out bool automaticFlipX);
        AutotileVisualOverride visualOverride = new AutotileVisualOverride(
            new UnityEngine.Vector2Int(0, 0),
            AutotileVisualLayer.Ground,
            tileset.Name,
            autoSpriteId,
            automaticFlipX,
            "17",
            overrideFlipX: false,
            overrideFlipY: false,
            rotationDegrees: 0);

        VisualOverrideResult decision = VisualOverrideDecision.Apply(autoSpriteId, automaticFlipX, visualOverride, tileset);

        Assert.AreEqual("31", autoSpriteId);
        Assert.IsTrue(automaticFlipX);
        Assert.AreEqual("17", decision.SpriteId);
        Assert.IsFalse(decision.FlipX);
        Assert.IsFalse(decision.FlipY);
        Assert.AreEqual(0, decision.RotationDegrees);
        Assert.IsTrue(decision.OverrideApplied);
    }

    [Test]
    public void ApplyRotationAndFlipY_FromOverrideEntry()
    {
        AutotileTileset tileset = CreateFullGroundTileset();
        AutotileVisualOverride visualOverride = new AutotileVisualOverride(
            new UnityEngine.Vector2Int(0, 0),
            AutotileVisualLayer.Ground,
            tileset.Name,
            "31",
            true,
            "17",
            overrideFlipX: true,
            overrideFlipY: true,
            rotationDegrees: 180);

        VisualOverrideResult decision = VisualOverrideDecision.Apply("31", true, visualOverride, tileset);

        Assert.IsTrue(decision.OverrideApplied);
        Assert.IsTrue(decision.FlipX);
        Assert.IsTrue(decision.FlipY);
        Assert.AreEqual(180, decision.RotationDegrees);
    }

    [Test]
    public void MissingOverrideSprite_FallsBackToAuto()
    {
        AutotileTileset tileset = CreateFullGroundTileset();
        AutotileVisualOverride visualOverride = new AutotileVisualOverride(
            new UnityEngine.Vector2Int(0, 0),
            AutotileVisualLayer.Ground,
            tileset.Name,
            "31",
            true,
            "999");

        VisualOverrideResult decision = VisualOverrideDecision.Apply("8", false, visualOverride, tileset);

        Assert.IsFalse(decision.OverrideApplied);
        Assert.AreEqual("8", decision.SpriteId);
    }

    [Test]
    public void CoverOverride_UsesGrassASpriteDecision()
    {
        AutotileTileset tileset = CreateCoverTileset();
        // Masks are indexed [x, y] (AutotileRule.Matches compares pattern[x + y*3] to
        // mask[x, y]), so a horizontal "middle run" (cover both sides -> sprite "4") has
        // its 1s in row y = 1: mask[0,1] = mask[1,1] = mask[2,1] = 1. Written as a C#
        // literal that is the middle column, since the outer index is x.
        int[,] mask =
        {
            { 0, 1, 0 },
            { 0, 1, 0 },
            { 0, 1, 0 }
        };

        string autoSpriteId = AutotileResolver.ResolveSpriteId(tileset, mask, out bool automaticFlipX);
        AutotileVisualOverride visualOverride = new AutotileVisualOverride(
            new UnityEngine.Vector2Int(0, 0),
            AutotileVisualLayer.Cover,
            tileset.Name,
            autoSpriteId,
            automaticFlipX,
            "3",
            overrideFlipX: true);

        VisualOverrideResult decision = VisualOverrideDecision.Apply(autoSpriteId, automaticFlipX, visualOverride, tileset);

        Assert.AreEqual("4", autoSpriteId);
        Assert.AreEqual("3", decision.SpriteId);
        Assert.IsTrue(decision.FlipX);
        Assert.IsTrue(decision.OverrideApplied);
    }

    private static AutotileTileset CreateCoverTileset()
    {
        string[] names = { "0", "1", "2", "3", "4", "5" };
        return new AutotileTileset("GrassA", new UnityEngine.Texture2D(16, 16), CreateSprites(names));
    }

    private static AutotileTileset CreateFullGroundTileset()
    {
        string[] names = new string[AutotileRuleTables.GroundSpriteCount];
        for (int i = 0; i < names.Length; i++)
        {
            names[i] = i.ToString();
        }

        return new AutotileTileset("Humus", new UnityEngine.Texture2D(16, 16), CreateSprites(names));
    }

    private static System.Collections.Generic.List<UnityEngine.Sprite> CreateSprites(params string[] names)
    {
        UnityEngine.Texture2D texture = new UnityEngine.Texture2D(16, 16);
        System.Collections.Generic.List<UnityEngine.Sprite> sprites = new System.Collections.Generic.List<UnityEngine.Sprite>();
        for (int i = 0; i < names.Length; i++)
        {
            sprites.Add(UnityEngine.Sprite.Create(texture, new UnityEngine.Rect(0, 0, 16, 16), UnityEngine.Vector2.one * 0.5f, 16));
            sprites[i].name = names[i];
        }

        return sprites;
    }
}
