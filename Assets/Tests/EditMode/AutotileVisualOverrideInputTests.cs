using NUnit.Framework;
using ProjectTwelve.Sandbox.Debug;
using ProjectTwelve.Visual.Tiles;
using UnityEngine;

public sealed class AutotileVisualOverrideInputTests
{
    [Test]
    public void FormatToggleMessage_On_IncludesControls()
    {
        string message = VisualOverrideModeLog.FormatToggleMessage(
            turnedOn: true,
            gateOpen: true);

        StringAssert.Contains("Visual Override Mode: ON", message);
        StringAssert.Contains("F5 save sidecar", message);
    }

    [Test]
    public void FormatToggleMessage_Off()
    {
        Assert.AreEqual(VisualOverrideModeLog.OffMessage, VisualOverrideModeLog.FormatToggleMessage(false, true));
    }

    [Test]
    public void FormatToggleMessage_UnavailableWhenGateClosed()
    {
        Assert.AreEqual(VisualOverrideModeLog.UnavailableMessage, VisualOverrideModeLog.FormatToggleMessage(true, false));
    }

    [Test]
    public void StepSpriteWrapsWithin0To31ForGround()
    {
        Assert.AreEqual("31", VisualOverrideSpriteStep.Step("0", -1, AutotileRuleTables.GroundSpriteCount));
        Assert.AreEqual("0", VisualOverrideSpriteStep.Step("31", 1, AutotileRuleTables.GroundSpriteCount));
    }

    [Test]
    public void StepSpriteWrapsWithin0To5ForCover()
    {
        Assert.AreEqual("5", VisualOverrideSpriteStep.Step("0", -1, 6));
        Assert.AreEqual("0", VisualOverrideSpriteStep.Step("5", 1, 6));
    }

    [Test]
    public void GetShiftStride_UsesSpriteCountWhenBelowEight()
    {
        Assert.AreEqual(6, VisualOverrideSpriteStep.GetShiftStride(6));
        Assert.AreEqual(8, VisualOverrideSpriteStep.GetShiftStride(32));
    }

    [Test]
    public void EditReach_AllowsVisibleTilesOutsidePlayerRange()
    {
        var cameraObject = new GameObject("ReachTestCamera");
        var camera = cameraObject.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 12f;
        camera.transform.position = new Vector3(0f, 28f, -10f);

        Vector3 tileCenter = new Vector3(10f, 28f, 0f);
        Vector3 playerOrigin = new Vector3(0f, 40f, 0f);

        Assert.IsFalse(SandboxDebugEditReach.IsWithinReach(
            tileCenter,
            playerOrigin,
            camera,
            playerRange: 6f,
            allowVisibleOnScreen: false));
        Assert.IsTrue(SandboxDebugEditReach.IsWithinReach(
            tileCenter,
            playerOrigin,
            camera,
            playerRange: 6f,
            allowVisibleOnScreen: true));

        Object.DestroyImmediate(cameraObject);
    }

    [Test]
    public void ScreenPointer_RoundTripsWorldToScreenToWorld()
    {
        var cameraObject = new GameObject("PointerTestCamera");
        var camera = cameraObject.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 12f;
        camera.transform.position = new Vector3(0f, 28f, -10f);

        Vector3 expected = new Vector3(4.5f, 30.5f, 0f);
        Vector3 screen = camera.WorldToScreenPoint(expected);
        Vector3 actual = SandboxScreenPointer.ScreenToWorld2D(camera, new Vector2(screen.x, screen.y));

        Assert.AreEqual(expected.x, actual.x, 0.01f);
        Assert.AreEqual(expected.y, actual.y, 0.01f);

        Object.DestroyImmediate(cameraObject);
    }

    [Test]
    public void ScreenPointer_RaycastHitsWorldPlaneAtZeroZ()
    {
        var cameraObject = new GameObject("PointerTestCamera");
        var camera = cameraObject.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 12f;
        camera.transform.position = new Vector3(5f, 28f, -10f);

        Vector3 world = SandboxScreenPointer.ScreenToWorld2D(camera, new Vector2(Screen.width * 0.5f, Screen.height * 0.5f));
        Assert.AreEqual(0f, world.z, 0.001f);

        Object.DestroyImmediate(cameraObject);
    }
}
