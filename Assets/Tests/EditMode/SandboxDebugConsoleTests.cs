using NUnit.Framework;
using ProjectTwelve.Sandbox.Debug;
using UnityEngine;

public sealed class SandboxDebugConsoleTests
{
    [Test]
    public void OverlayCommand_SetsAndTogglesSharedState()
    {
        SandboxDebugOverlayState state = new SandboxDebugOverlayState();

        string enabled = SandboxDebugConsole.ExecuteCommand("overlay light_heatmap on", state, null, null);
        string toggled = SandboxDebugConsole.ExecuteCommand("overlay light_heatmap toggle", state, null, null);

        StringAssert.Contains("light_heatmap: on", enabled);
        StringAssert.Contains("light_heatmap: off", toggled);
        Assert.IsFalse(state.IsEnabled(SandboxDebugOverlay.LightHeatmap));
    }

    [TestCase("slot-1", true)]
    [TestCase("test_slot", true)]
    [TestCase("../escape", false)]
    [TestCase("with space", false)]
    [TestCase("", false)]
    public void SlotNameValidation_BlocksPathTraversal(string name, bool expected)
    {
        Assert.AreEqual(expected, SandboxDebugConsole.IsValidSlotName(name));
    }

    [Test]
    public void UnknownCommand_ReturnsActionableError()
    {
        string result = SandboxDebugConsole.ExecuteCommand(
            "explode everything",
            new SandboxDebugOverlayState(),
            null,
            null);

        StringAssert.Contains("Unknown command", result);
        StringAssert.Contains("help", result);
    }

    [Test]
    public void Help_ListsRequiredMinimumCommands()
    {
        string result = SandboxDebugConsole.ExecuteCommand(
            "help",
            new SandboxDebugOverlayState(),
            null,
            null);

        StringAssert.Contains("teleport", result);
        StringAssert.Contains("set_tile", result);
        StringAssert.Contains("get_tile", result);
        StringAssert.Contains("generate_chunk", result);
        StringAssert.Contains("dump_chunk", result);
        StringAssert.Contains("save", result);
        StringAssert.Contains("load", result);
    }

    [Test]
    public void GetTile_UngeneratedChunk_DoesNotGenerateIt()
    {
        GameObject worldObject = new GameObject("World");
        try
        {
            SandboxWorld world = worldObject.AddComponent<SandboxWorld>();

            string result = SandboxDebugConsole.ExecuteCommand(
                "get_tile 3200 3200",
                new SandboxDebugOverlayState(),
                world,
                null);

            StringAssert.Contains("ungenerated", result);
            Assert.IsFalse(world.TryGetExistingTile(3200, 3200, out _));
        }
        finally
        {
            Object.DestroyImmediate(worldObject);
        }
    }

    [Test]
    public void SetTile_WhenDebugOverridesDisabled_DoesNotMutateWorld()
    {
        GameObject worldObject = new GameObject("World");
        try
        {
            SandboxWorld world = worldObject.AddComponent<SandboxWorld>();

            string result = SandboxDebugConsole.ExecuteCommand(
                "set_tile 0 0 3",
                new SandboxDebugOverlayState(),
                world,
                null);

            StringAssert.Contains("disabled", result);
            Assert.IsFalse(world.TryGetExistingTile(0, 0, out _));
        }
        finally
        {
            Object.DestroyImmediate(worldObject);
        }
    }
}
