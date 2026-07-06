using NUnit.Framework;
using UnityEngine;

public sealed class SandboxVisualOverrideSaveDataTests
{
    [Test]
    public void JsonRoundTrip_UsesTopLevelIntegerCoordinates()
    {
        SandboxVisualOverrideSaveData saveData = SandboxVisualOverrideSaveData.FromRuntime(
            new Vector2Int(-113, 25),
            spriteId: 17,
            flipX: true);

        string json = JsonUtility.ToJson(saveData);

        StringAssert.Contains("\"x\":-113", json);
        StringAssert.Contains("\"y\":25", json);
        Assert.IsFalse(json.Contains("coord"), "JSON DTO must not serialize a nested Vector2Int coordinate.");

        SandboxVisualOverrideSaveData reloaded = JsonUtility.FromJson<SandboxVisualOverrideSaveData>(json);

        Assert.AreEqual(-113, reloaded.x);
        Assert.AreEqual(25, reloaded.y);
        Assert.AreEqual(new Vector2Int(-113, 25), reloaded.ToCoord());
        Assert.AreEqual(17, reloaded.spriteId);
        Assert.IsTrue(reloaded.flipX);
    }

    [Test]
    public void RuntimeOverride_RoundTripsThroughSaveDataWithNegativeCoordinates()
    {
        AutotileVisualOverride runtime = new AutotileVisualOverride(new Vector2Int(-113, 25), 42, flipX: true);

        SandboxVisualOverrideSaveData saveData = runtime.ToSaveData();
        AutotileVisualOverride reloaded = AutotileVisualOverride.FromSaveData(saveData);

        Assert.AreEqual(-113, saveData.x);
        Assert.AreEqual(25, saveData.y);
        Assert.AreEqual(runtime.coord, reloaded.coord);
        Assert.AreEqual(runtime.spriteId, reloaded.spriteId);
        Assert.AreEqual(runtime.flipX, reloaded.flipX);
    }
}
