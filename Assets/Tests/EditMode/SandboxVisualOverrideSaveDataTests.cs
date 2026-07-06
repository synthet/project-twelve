using NUnit.Framework;
using UnityEngine;

public sealed class SandboxVisualOverrideSaveDataTests
{
    [Test]
    public void JsonRoundTrip_UsesTopLevelIntegerCoordinates()
    {
        SandboxVisualOverrideEntrySaveData entry = SandboxVisualOverrideEntrySaveData.FromRuntime(
            new Vector2Int(-113, 25),
            spriteId: 17,
            flipX: true);

        string json = JsonUtility.ToJson(entry);

        StringAssert.Contains("\"x\":-113", json);
        StringAssert.Contains("\"y\":25", json);
        Assert.IsFalse(json.Contains("coord"), "JSON DTO must not serialize a nested Vector2Int coordinate.");

        SandboxVisualOverrideEntrySaveData reloaded = JsonUtility.FromJson<SandboxVisualOverrideEntrySaveData>(json);

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

        SandboxVisualOverrideEntrySaveData saveData = runtime.ToSaveData();
        AutotileVisualOverride reloaded = AutotileVisualOverride.FromSaveData(saveData);

        Assert.AreEqual(-113, saveData.x);
        Assert.AreEqual(25, saveData.y);
        Assert.AreEqual(runtime.coord, reloaded.coord);
        Assert.AreEqual(runtime.spriteId, reloaded.spriteId);
        Assert.AreEqual(runtime.flipX, reloaded.flipX);
    }

    [Test]
    public void ContainerSidecar_SerializesOverrideList()
    {
        SandboxVisualOverrideSaveData sidecar = new SandboxVisualOverrideSaveData
        {
            overrides =
            {
                SandboxVisualOverrideEntrySaveData.FromRuntime(new Vector2Int(-113, 25), 42, flipX: true)
            }
        };

        string json = JsonUtility.ToJson(sidecar, true);
        SandboxVisualOverrideSaveData reloaded = JsonUtility.FromJson<SandboxVisualOverrideSaveData>(json);

        Assert.IsTrue(reloaded.HasOverrides);
        Assert.AreEqual(1, reloaded.overrides.Count);
        Assert.AreEqual(-113, reloaded.overrides[0].x);
        Assert.AreEqual(25, reloaded.overrides[0].y);
    }
}
