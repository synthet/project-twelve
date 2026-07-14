using NUnit.Framework;
using UnityEngine;

public sealed class SingleAudioListenerEnforcerTests
{
    [Test]
    public void Enforce_KeepsMainCameraListener_WhenDuplicateExists()
    {
        GameObject mainCameraObject = new GameObject("Main Camera");
        GameObject extraObject = new GameObject("Extra Listener");
        try
        {
            mainCameraObject.tag = "MainCamera";
            mainCameraObject.AddComponent<Camera>();
            AudioListener mainListener = mainCameraObject.AddComponent<AudioListener>();
            AudioListener extraListener = extraObject.AddComponent<AudioListener>();

            SingleAudioListenerEnforcer.Enforce(new[] { mainListener, extraListener });

            Assert.IsNotNull(mainListener);
            Assert.IsTrue(extraListener == null, "The duplicate listener component must be removed.");
        }
        finally
        {
            Object.DestroyImmediate(extraObject);
            Object.DestroyImmediate(mainCameraObject);
        }
    }

    [Test]
    public void Enforce_NoOp_WhenOnlyOneListenerExists()
    {
        GameObject cameraObject = new GameObject("Camera");
        try
        {
            AudioListener listener = cameraObject.AddComponent<AudioListener>();

            SingleAudioListenerEnforcer.Enforce(new[] { listener });

            Assert.IsNotNull(listener);
        }
        finally
        {
            Object.DestroyImmediate(cameraObject);
        }
    }
}
