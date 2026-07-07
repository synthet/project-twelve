using NUnit.Framework;
using ProjectTwelve.Visual.Tiles;
using UnityEngine;

public sealed class SandboxVisualOverrideSaveDataTests
{
    [Test]
    public void JsonRoundTrip_UsesTopLevelIntegerCoordinates()
    {
        SandboxVisualOverrideEntrySaveData entry = new SandboxVisualOverrideEntrySaveData
        {
            x = -113,
            y = 25,
            layer = AutotileVisualLayerNames.Ground,
            tileset = "Humus",
            autoSpriteId = "31",
            autoFlipX = true,
            overrideSpriteId = "17",
            overrideFlipX = false,
            overrideFlipY = false,
            rotation = 0,
            note = "underside",
        };

        string json = JsonUtility.ToJson(entry);

        StringAssert.Contains("\"x\":-113", json);
        StringAssert.Contains("\"y\":25", json);
        Assert.IsFalse(json.Contains("coord"), "JSON DTO must not serialize a nested Vector2Int coordinate.");

        SandboxVisualOverrideEntrySaveData reloaded = JsonUtility.FromJson<SandboxVisualOverrideEntrySaveData>(json);

        Assert.AreEqual(-113, reloaded.x);
        Assert.AreEqual(25, reloaded.y);
        Assert.AreEqual(new Vector2Int(-113, 25), reloaded.ToCoord());
        Assert.AreEqual("17", reloaded.overrideSpriteId);
        Assert.IsTrue(reloaded.autoFlipX);
    }

    [Test]
    public void RuntimeOverride_RoundTripsThroughSaveDataWithNegativeCoordinates()
    {
        AutotileVisualOverride runtime = new AutotileVisualOverride(
            new Vector2Int(-113, 25),
            AutotileVisualLayer.Ground,
            "Humus",
            autoSpriteId: "31",
            autoFlipX: true,
            overrideSpriteId: "17");

        SandboxVisualOverrideEntrySaveData saveData = runtime.ToSaveData();
        AutotileVisualOverride reloaded = AutotileVisualOverride.FromSaveData(saveData);

        Assert.AreEqual(-113, saveData.x);
        Assert.AreEqual(25, saveData.y);
        Assert.AreEqual(runtime.Cell, reloaded.Cell);
        Assert.AreEqual(runtime.overrideSpriteId, reloaded.overrideSpriteId);
        Assert.AreEqual(runtime.autoFlipX, reloaded.autoFlipX);
    }

    [Test]
    public void Persistence_RoundTripsFullSchema()
    {
        AutotileVisualOverrideMap map = new AutotileVisualOverrideMap();
        map.SetOverride(new AutotileVisualOverride(
            new Vector2Int(-113, 25),
            AutotileVisualLayer.Ground,
            "Humus",
            "31",
            autoFlipX: true,
            "17",
            overrideFlipX: false,
            overrideFlipY: true,
            rotationDegrees: 90,
            note: "slope"));

        string json = VisualOverridePersistence.Serialize(map);
        AutotileVisualOverrideMap reloaded = new AutotileVisualOverrideMap();
        VisualOverridePersistence.DeserializeInto(json, reloaded);

        Assert.AreEqual(1, reloaded.Count);
        Assert.IsTrue(reloaded.TryGetOverride(new Vector2Int(-113, 25), AutotileVisualLayer.Ground, "Humus", out AutotileVisualOverride entry));
        Assert.AreEqual("17", entry.overrideSpriteId);
        Assert.IsTrue(entry.overrideFlipY);
        Assert.AreEqual(90, entry.rotation);
    }

    [Test]
    public void Persistence_MigratesLegacyIntSpriteIdSidecar()
    {
        const string legacy = "{\"overrides\":[{\"x\":1,\"y\":2,\"spriteId\":17,\"flipX\":true}]}";
        AutotileVisualOverrideMap map = new AutotileVisualOverrideMap();
        VisualOverridePersistence.DeserializeInto(legacy, map);

        Assert.IsTrue(map.TryGetOverride(new Vector2Int(1, 2), AutotileVisualLayer.Ground, "Humus", out AutotileVisualOverride entry));
        Assert.AreEqual("17", entry.overrideSpriteId);
        Assert.IsTrue(entry.overrideFlipX);
    }
}
