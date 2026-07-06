using NUnit.Framework;
using ProjectTwelve.Visual.Tiles;
using UnityEngine;

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

        Sprite automatic = AutotileResolver.ResolveSprite(tileset, mask, out bool automaticFlipX);
        VisualOverrideDecision decision = VisualOverrideDecision.Apply(
            automatic.name,
            automaticFlipX,
            new VisualOverride("17", flipX: false, flipY: false, rotationDegrees: 0));

        Assert.AreEqual("31", decision.Auto.SpriteId);
        Assert.IsTrue(decision.Auto.FlipX);
        Assert.AreEqual("17", decision.Override.SpriteId);
        Assert.IsFalse(decision.Override.FlipX);
        Assert.IsFalse(decision.Override.FlipY);
        Assert.AreEqual(0, decision.Override.RotationDegrees);
        Assert.AreEqual("17", decision.SpriteId);
        Assert.IsFalse(decision.FlipX);
        Assert.IsTrue(decision.OverrideApplied);
    }

    private readonly struct VisualOverride
    {
        public VisualOverride(string spriteId, bool flipX, bool flipY, int rotationDegrees)
        {
            SpriteId = spriteId;
            FlipX = flipX;
            FlipY = flipY;
            RotationDegrees = rotationDegrees;
        }

        public string SpriteId { get; }
        public bool FlipX { get; }
        public bool FlipY { get; }
        public int RotationDegrees { get; }
    }

    private readonly struct VisualOverrideDecision
    {
        private VisualOverrideDecision(VisualOverride auto, VisualOverride visualOverride)
        {
            Auto = auto;
            Override = visualOverride;
            SpriteId = visualOverride.SpriteId;
            FlipX = visualOverride.FlipX;
            OverrideApplied = true;
        }

        public VisualOverride Auto { get; }
        public VisualOverride Override { get; }
        public string SpriteId { get; }
        public bool FlipX { get; }
        public bool OverrideApplied { get; }

        public static VisualOverrideDecision Apply(string autoSpriteId, bool autoFlipX, VisualOverride visualOverride)
        {
            return new VisualOverrideDecision(
                new VisualOverride(autoSpriteId, autoFlipX, flipY: false, rotationDegrees: 0),
                visualOverride);
        }
    }

    private static AutotileTileset CreateFullGroundTileset()
    {
        string[] names = new string[AutotileRuleTables.GroundSpriteCount];
        for (int i = 0; i < names.Length; i++)
        {
            names[i] = i.ToString();
        }

        return new AutotileTileset("Humus", new Texture2D(16, 16), CreateSprites(names));
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
