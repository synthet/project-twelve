using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProjectTwelve.Visual.Characters;
using UnityEditor;
using UnityEngine;

public sealed class CharacterVisualTests
{
    // Contract source: docs/VISUAL_BEHAVIOR_SPEC.md § 4 (character sprite sheet contract).
    private static readonly string[] SpecRowClips =
    {
        "Roll", "Death", "Block", "Fire", "Shot", "Slash", "Jab", "Push",
        "Jump", "Climb", "Crawl", "Run", "Ready", "Idle"
    };

    private static readonly string[] SpecMergeOrder =
    {
        "Back", "Shield", "Body", "Arms", "Head", "Ears", "Armor",
        "Bracers", "Eyes", "Hair", "Cape", "Helmet", "Weapon", "Mask"
    };

    private readonly List<Object> spawned = new List<Object>();

    [TearDown]
    public void DestroySpawnedObjects()
    {
        for (int i = 0; i < spawned.Count; i++)
        {
            if (spawned[i] != null)
            {
                Object.DestroyImmediate(spawned[i]);
            }
        }

        spawned.Clear();
    }

    [Test]
    public void CharacterSheetLayout_ConstantsMatchSpecSheetContract()
    {
        Assert.AreEqual(576, CharacterSheetLayout.Width);
        Assert.AreEqual(928, CharacterSheetLayout.Height);
        Assert.AreEqual(64, CharacterSheetLayout.CellSize);
        Assert.AreEqual(16f, CharacterSheetLayout.PixelsPerUnit);
        Assert.AreEqual(new Vector2(0.5f, 0.125f), CharacterSheetLayout.Pivot);
        Assert.LessOrEqual(
            SpecRowClips.Length * CharacterSheetLayout.CellSize,
            CharacterSheetLayout.Height,
            "Clip rows must fit inside the sheet.");
    }

    [Test]
    public void CharacterSheetLayout_BuildFrameRects_CoversNineFramesForAllSpecClips()
    {
        Dictionary<string, Rect> frames = CharacterSheetLayout.BuildFrameRects();

        Assert.AreEqual(SpecRowClips.Length * 9, frames.Count);
        foreach (string clip in SpecRowClips)
        {
            for (int frame = 0; frame < 9; frame++)
            {
                Assert.IsTrue(
                    frames.ContainsKey($"{clip}_{frame}"),
                    $"Missing frame key '{clip}_{frame}'.");
            }

            Assert.IsFalse(frames.ContainsKey($"{clip}_9"), $"Unexpected tenth frame for '{clip}'.");
        }
    }

    [Test]
    public void CharacterSheetLayout_BuildFrameRects_RowOrderAndCellRectsMatchSpec()
    {
        Dictionary<string, Rect> frames = CharacterSheetLayout.BuildFrameRects();

        for (int row = 0; row < SpecRowClips.Length; row++)
        {
            for (int frame = 0; frame < 9; frame++)
            {
                Rect expected = new Rect(
                    frame * CharacterSheetLayout.CellSize,
                    row * CharacterSheetLayout.CellSize,
                    CharacterSheetLayout.CellSize,
                    CharacterSheetLayout.CellSize);
                Assert.AreEqual(
                    expected,
                    frames[$"{SpecRowClips[row]}_{frame}"],
                    $"Rect mismatch for '{SpecRowClips[row]}_{frame}'.");
            }
        }
    }

    [Test]
    public void CharacterSheetLayout_BuildFrameRects_IdleKeysUseLibraryKeyFormat()
    {
        Dictionary<string, Rect> frames = CharacterSheetLayout.BuildFrameRects();

        string[] idleKeys = frames.Keys.Where(k => k.StartsWith("Idle")).OrderBy(k => k).ToArray();

        CollectionAssert.AreEqual(
            new[]
            {
                "Idle_0", "Idle_1", "Idle_2", "Idle_3", "Idle_4",
                "Idle_5", "Idle_6", "Idle_7", "Idle_8"
            },
            idleKeys);
    }

    [Test]
    public void CharacterComposer_BuildLayers_MergesLayersInSpecOrder()
    {
        CharacterComposer composer = CreateComposerWithCatalog();
        ApplyFullEquipment(composer);

        Dictionary<string, Color32[]> layers = composer.BuildLayers();

        CollectionAssert.AreEqual(SpecMergeOrder, layers.Keys.ToArray());
    }

    [Test]
    public void CharacterComposer_BuildLayers_FirearmModeOmitsArmsAndBracers()
    {
        CharacterComposer composer = CreateComposerWithCatalog();
        ApplyFullEquipment(composer);
        composer.Firearm = "Musket";

        Dictionary<string, Color32[]> layers = composer.BuildLayers();

        CollectionAssert.DoesNotContain(layers.Keys, "Arms");
        CollectionAssert.DoesNotContain(layers.Keys, "Bracers");
        CollectionAssert.Contains(layers.Keys, "Body");
        CollectionAssert.Contains(layers.Keys, "Armor");
    }

    [Test]
    public void CharacterComposer_BuildLayers_LizardHeadClearsHairHelmetMask()
    {
        CharacterComposer composer = CreateComposerWithCatalog();
        ApplyFullEquipment(composer);
        composer.Head = "Lizard";

        Dictionary<string, Color32[]> layers = composer.BuildLayers();

        CollectionAssert.DoesNotContain(layers.Keys, "Hair");
        CollectionAssert.DoesNotContain(layers.Keys, "Helmet");
        CollectionAssert.DoesNotContain(layers.Keys, "Mask");
        Assert.IsEmpty(composer.Hair);
        Assert.IsEmpty(composer.Helmet);
        Assert.IsEmpty(composer.Mask);
    }

    private CharacterComposer CreateComposerWithCatalog()
    {
        CharacterLayerCatalog catalog = ScriptableObject.CreateInstance<CharacterLayerCatalog>();
        spawned.Add(catalog);
        catalog.SetLayers(new List<CharacterLayerEntry>
        {
            CreateEntry("Back", "LeatherQuiver"),
            CreateEntry("Shield", "RoundShield"),
            CreateEntry("Body", "Human"),
            CreateEntry("Arms", "Human"),
            CreateEntry("Head", "Human", "Lizard"),
            CreateEntry("Ears", "Human"),
            CreateEntry("Armor", "LeatherArmor"),
            CreateEntry("Bracers", "LeatherArmor"),
            CreateEntry("Eyes", "Human"),
            CreateEntry("Hair", "Curly"),
            CreateEntry("Cape", "RedCape"),
            CreateEntry("Helmet", "OpenHelm [ShowEars]"),
            CreateEntry("Weapon", "Sword"),
            CreateEntry("Mask", "ClothMask")
        });

        GameObject host = new GameObject("CharacterComposerTest");
        spawned.Add(host);
        CharacterComposer composer = host.AddComponent<CharacterComposer>();

        SerializedObject serialized = new SerializedObject(composer);
        serialized.FindProperty("layerCatalog").objectReferenceValue = catalog;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        return composer;
    }

    private static void ApplyFullEquipment(CharacterComposer composer)
    {
        composer.Body = "Human";
        composer.Head = "Human";
        composer.Ears = "Human";
        composer.Eyes = "Human";
        composer.Hair = "Curly";
        composer.Armor = "LeatherArmor";
        // "[ShowEars]" keeps the Ears layer active per the composer contract.
        composer.Helmet = "OpenHelm [ShowEars]";
        composer.Weapon = "Sword";
        composer.Firearm = string.Empty;
        composer.Shield = "RoundShield";
        composer.Cape = "RedCape";
        composer.Back = "LeatherQuiver";
        composer.Mask = "ClothMask";
    }

    private CharacterLayerEntry CreateEntry(string layerName, params string[] textureNames)
    {
        List<Texture2D> textures = new List<Texture2D>();
        foreach (string textureName in textureNames)
        {
            Texture2D texture = new Texture2D(8, 8, TextureFormat.RGBA32, false);
            spawned.Add(texture);
            Color32[] pixels = new Color32[8 * 8];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = new Color32(200, 150, 100, 255);
            }

            texture.SetPixels32(pixels);
            texture.Apply();
            texture.name = textureName;
            textures.Add(texture);
        }

        return CharacterLayerEntry.Create(layerName, textures);
    }
}
