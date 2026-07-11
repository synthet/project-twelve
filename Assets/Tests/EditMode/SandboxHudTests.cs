using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public sealed class SandboxHudTests
{
    [Test]
    public void CreativeHotbar_DefaultsToFourCreativeTilesAndDirtSelected()
    {
        SandboxCreativeHotbarState state = new SandboxCreativeHotbarState();

        Assert.AreEqual(SandboxCreativeHotbarState.SlotCount, state.Slots.Count);
        Assert.AreEqual(0, state.SelectedIndex);
        Assert.AreEqual("core:dirt", state.SelectedTileId);
        Assert.AreEqual("core:grass", state.Slots[1].TileId);
        Assert.AreEqual("core:stone", state.Slots[2].TileId);
        Assert.AreEqual("core:copper_ore", state.Slots[3].TileId);
        Assert.IsFalse(state.Slots[4].IsPopulated);
    }

    [Test]
    public void CreativeHotbar_DirectSelectionAllowsEmptySlot()
    {
        SandboxCreativeHotbarState state = new SandboxCreativeHotbarState();

        Assert.IsTrue(state.Select(9));
        Assert.AreEqual(9, state.SelectedIndex);
        Assert.IsNull(state.SelectedTileId);
        Assert.IsFalse(state.Select(10));
        Assert.AreEqual(9, state.SelectedIndex);
    }

    [Test]
    public void CreativeHotbar_CycleSkipsEmptySlotsAndWraps()
    {
        SandboxCreativeHotbarState state = new SandboxCreativeHotbarState();

        state.Select(3);
        Assert.IsTrue(state.CyclePopulated(1));
        Assert.AreEqual(0, state.SelectedIndex);
        Assert.IsTrue(state.CyclePopulated(-1));
        Assert.AreEqual(3, state.SelectedIndex);

        state.Select(8);
        Assert.IsTrue(state.CyclePopulated(1));
        Assert.AreEqual(0, state.SelectedIndex);
    }

    [Test]
    public void PlayerVitals_ClampDamageHealAndRestore()
    {
        GameObject go = new GameObject("VitalsTest");
        try
        {
            SandboxPlayerVitals vitals = go.AddComponent<SandboxPlayerVitals>();
            int notificationCount = 0;
            vitals.Changed += (_, _) => notificationCount++;

            Assert.AreEqual(100, vitals.CurrentHealth);
            Assert.AreEqual(100, vitals.MaxHealth);
            vitals.ApplyDamage(125);
            Assert.AreEqual(0, vitals.CurrentHealth);
            vitals.Heal(35);
            Assert.AreEqual(35, vitals.CurrentHealth);
            vitals.Heal(-5);
            Assert.AreEqual(35, vitals.CurrentHealth);
            vitals.RestoreFull();
            Assert.AreEqual(100, vitals.CurrentHealth);
            Assert.AreEqual(3, notificationCount);
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void PlayerController_ValidatesAndClearsCreativePlacementSelection()
    {
        GameObject go = new GameObject("PlayerControllerTest");
        try
        {
            SandboxPlayerController controller = go.AddComponent<SandboxPlayerController>();

            Assert.IsTrue(controller.TrySetActivePlacementTile("core:stone"));
            Assert.AreEqual("core:stone", controller.ActivePlacementTileId);
            Assert.IsFalse(controller.TrySetActivePlacementTile("core:air"));
            Assert.IsFalse(controller.TrySetActivePlacementTile("missing:tile"));
            Assert.AreEqual("core:stone", controller.ActivePlacementTileId);

            controller.ClearActivePlacementTile();
            Assert.IsNull(controller.ActivePlacementTileId);
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    [TestCase(0, 100, 0, 0f)]
    [TestCase(5, 100, 0, 0.5f)]
    [TestCase(10, 100, 0, 1f)]
    [TestCase(15, 100, 1, 0.5f)]
    [TestCase(95, 95, 9, 1f)]
    public void HeartFill_RepresentsFullAndPartialHearts(int current, int maximum, int index, float expected)
    {
        Assert.AreEqual(expected, SandboxHudController.GetHeartFill(current, maximum, index), 0.001f);
    }

    [Test]
    public void WorldInfo_UsesCanonicalNegativeChunkCoordinates()
    {
        Vector2Int tile = new Vector2Int(-1, 64);
        Vector2Int chunk = SandboxWorld.WorldToChunkCoord(tile.x, tile.y);

        Assert.AreEqual(new Vector2Int(-1, 2), chunk);
        Assert.AreEqual("SEED  1337\nTILE  -1, 64\nCHUNK -1, 2",
            SandboxHudController.FormatWorldInfo(1337, tile, chunk));
    }

    [Test]
    public void HudPrefab_HasThemeReferencesCanvasScalerAndTenSlotViews()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/SandboxHUD.prefab");
        Assert.IsNotNull(prefab);
        Assert.IsNotNull(prefab.GetComponent<Canvas>());
        CanvasScaler scaler = prefab.GetComponent<CanvasScaler>();
        Assert.IsNotNull(scaler);
        Assert.AreEqual(new Vector2(1280f, 720f), scaler.referenceResolution);

        GameObject instance = Object.Instantiate(prefab);
        try
        {
            SandboxHudController hud = instance.GetComponent<SandboxHudController>();
            Assert.IsNotNull(hud);
            if (hud.SlotViewCount == 0)
            {
                hud.SendMessage("Awake");
            }

            Assert.AreEqual(10, hud.SlotViewCount);
            SerializedObject serialized = new SerializedObject(hud);
            Assert.IsNotNull(serialized.FindProperty("panelSprite").objectReferenceValue);
            Assert.IsNotNull(serialized.FindProperty("pixelFont").objectReferenceValue);
        }
        finally
        {
            Object.DestroyImmediate(instance);
        }
    }
}
