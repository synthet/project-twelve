using NUnit.Framework;
using UnityEngine;

public sealed class SingleAudioListenerEnforcerTests
{
    [Test]
    public void Enforce_KeepsMainCameraListener_WhenDuplicateExists()
    {
        GameObject mainCameraObject = new GameObject("Main Camera");
        mainCameraObject.tag = "MainCamera";
        mainCameraObject.AddComponent<Camera>();
        AudioListener mainListener = mainCameraObject.AddComponent<AudioListener>();

        GameObject extraObject = new GameObject("Extra Listener");
        extraObject.AddComponent<AudioListener>();

        SingleAudioListenerEnforcer.Enforce();

        AudioListener[] listeners = Object.FindObjectsByType<AudioListener>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        Assert.AreEqual(1, listeners.Length);
        Assert.AreSame(mainListener, listeners[0]);

        Object.DestroyImmediate(extraObject);
        Object.DestroyImmediate(mainCameraObject);
    }

    [Test]
    public void Enforce_NoOp_WhenOnlyOneListenerExists()
    {
        GameObject cameraObject = new GameObject("Camera");
        AudioListener listener = cameraObject.AddComponent<AudioListener>();

        SingleAudioListenerEnforcer.Enforce();

        AudioListener[] listeners = Object.FindObjectsByType<AudioListener>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        Assert.AreEqual(1, listeners.Length);
        Assert.AreSame(listener, listeners[0]);

        Object.DestroyImmediate(cameraObject);
    }
}
