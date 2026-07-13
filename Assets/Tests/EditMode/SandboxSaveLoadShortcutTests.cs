using NUnit.Framework;

public sealed class SandboxSaveLoadShortcutTests
{
    [Test]
    public void VisualOverrideMode_F5SavesSidecarAndDoesNotLoadWorld()
    {
        SandboxSaveLoadShortcutRouter.ShortcutCommand command = SandboxSaveLoadShortcutRouter.Resolve(
            savePressed: true,
            loadPressed: false,
            visualOverrideModeActive: true);

        Assert.AreEqual(SandboxSaveLoadShortcutRouter.ShortcutCommand.SaveWorldAndVisualOverrideSidecar, command);
        Assert.AreNotEqual(SandboxSaveLoadShortcutRouter.ShortcutCommand.LoadWorld, command,
            "Visual Override Mode save shortcut must not route to SandboxWorld.LoadFromPath.");
    }

    [Test]
    public void NormalMode_F5SavesAndF6LoadsUnchanged()
    {
        Assert.AreEqual(
            SandboxSaveLoadShortcutRouter.ShortcutCommand.SaveWorld,
            SandboxSaveLoadShortcutRouter.Resolve(savePressed: true, loadPressed: false, visualOverrideModeActive: false));
        Assert.AreEqual(
            SandboxSaveLoadShortcutRouter.ShortcutCommand.LoadWorld,
            SandboxSaveLoadShortcutRouter.Resolve(savePressed: false, loadPressed: true, visualOverrideModeActive: false));
    }
}
