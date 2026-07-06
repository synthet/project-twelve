using NUnit.Framework;
using ProjectTwelve.Visual.Tiles;
using UnityEngine;

/// <summary>
/// Canonical 128×64 ground sheet layout validation.
/// </summary>
public sealed class AutotileGroundSheetLayoutTests
{
    [Test]
    public void GetExpectedTextureRect_Id0_IsTopLeft_InBottomLeftUnitySpace()
    {
        Rect rect = AutotileGroundSheetLayout.GetExpectedTextureRect(0, AutotileGroundSheetLayout.SheetHeightPixels);
        Assert.AreEqual(0f, rect.x);
        Assert.AreEqual(48f, rect.y);
        Assert.AreEqual(16f, rect.width);
        Assert.AreEqual(16f, rect.height);
    }

    [Test]
    public void GetExpectedTextureRect_Id31_IsBottomRight()
    {
        Rect rect = AutotileGroundSheetLayout.GetExpectedTextureRect(31, AutotileGroundSheetLayout.SheetHeightPixels);
        Assert.AreEqual(112f, rect.x);
        Assert.AreEqual(0f, rect.y);
    }

    [Test]
    public void ValidateGroundSheet_PassesForSyntheticLayout()
    {
        Texture2D texture = new Texture2D(
            AutotileGroundSheetLayout.SheetWidthPixels,
            AutotileGroundSheetLayout.SheetHeightPixels,
            TextureFormat.RGBA32,
            false);

        var sprites = new Sprite[AutotileGroundSheetLayout.SpriteCount];
        for (int id = 0; id < AutotileGroundSheetLayout.SpriteCount; id++)
        {
            Rect rect = AutotileGroundSheetLayout.GetExpectedTextureRect(id, texture.height);
            Sprite sprite = Sprite.Create(texture, rect, new Vector2(0.5f, 0f), 16f);
            sprite.name = id.ToString();
            sprites[id] = sprite;
        }

        var errors = new System.Collections.Generic.List<string>();
        Assert.IsTrue(AutotileGroundSheetLayout.ValidateGroundSheet("TestHumus", texture, sprites, errors));
        Assert.AreEqual(0, errors.Count);
    }

    [Test]
    public void ValidateSpriteRect_FailsWhenRowOrderSwapped()
    {
        Texture2D texture = new Texture2D(128, 64, TextureFormat.RGBA32, false);
        Rect wrongRect = new Rect(0f, 0f, 16f, 16f);
        Sprite sprite = Sprite.Create(texture, wrongRect, new Vector2(0.5f, 0f), 16f);
        sprite.name = "0";

        var errors = new System.Collections.Generic.List<string>();
        Assert.IsFalse(AutotileGroundSheetLayout.ValidateSpriteRect(sprite, texture, 0, errors));
        Assert.IsNotEmpty(errors);
    }
}
