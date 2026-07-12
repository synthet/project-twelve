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
        Assert.AreEqual("DEBUG  SEED 1337\nTILE  -1, 64\nCHUNK -1, 2",
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
            Assert.IsNotNull(serialized.FindProperty("emptyHeartSprite").objectReferenceValue);
            Assert.IsNotNull(serialized.FindProperty("pixelFont").objectReferenceValue);

            Sprite slotSprite = serialized.FindProperty("slotSprite").objectReferenceValue as Sprite;
            Assert.IsNotNull(slotSprite);
            Assert.LessOrEqual(slotSprite.border.x / slotSprite.pixelsPerUnit * 100f, 12f);
            Assert.LessOrEqual(slotSprite.border.y / slotSprite.pixelsPerUnit * 100f, 12f);

            RectTransform vitals = instance.transform.Find("Vitals") as RectTransform;
            RectTransform hotbar = instance.transform.Find("CreativeHotbar") as RectTransform;
            RectTransform telemetry = instance.transform.Find("WorldInfo") as RectTransform;
            Assert.IsNotNull(vitals);
            Assert.IsNotNull(hotbar);
            Assert.IsNotNull(telemetry);
            Assert.LessOrEqual(vitals.rect.width, 210f);
            Assert.LessOrEqual(vitals.rect.height, 70f);
            Assert.LessOrEqual(hotbar.rect.width, 612f);
            Assert.LessOrEqual(hotbar.rect.height, 60f);
            Assert.LessOrEqual(telemetry.rect.width, 160f);
            Assert.LessOrEqual(telemetry.rect.height, 62f);

            RectTransform firstSlot = instance.transform.Find("CreativeHotbar/Slot1") as RectTransform;
            RectTransform secondSlot = instance.transform.Find("CreativeHotbar/Slot2") as RectTransform;
            Assert.IsNotNull(firstSlot);
            Assert.IsNotNull(secondSlot);
            float firstSelectedY = firstSlot.anchoredPosition.y;
            Assert.IsTrue(hud.Hotbar.Select(1));
            Assert.AreEqual(firstSelectedY - 2f, firstSlot.anchoredPosition.y, 0.001f);
            Assert.AreEqual(firstSelectedY, secondSlot.anchoredPosition.y, 0.001f);
            RectTransform selectedItem = secondSlot.Find("SelectedItem") as RectTransform;
            Assert.IsNotNull(selectedItem);
            Assert.AreEqual(secondSlot, selectedItem.parent);
            Assert.AreEqual(8f, selectedItem.anchoredPosition.y, 0.001f);
            Transform selectedFrame = secondSlot.Find("Selected");
            Assert.IsNotNull(selectedFrame);
            Assert.IsNotNull(selectedFrame.Find("MarkerTop"));
            Assert.IsNotNull(selectedFrame.Find("MarkerBottom"));
            Assert.IsNotNull(selectedFrame.Find("MarkerLeft"));
            Assert.IsNotNull(selectedFrame.Find("MarkerRight"));

            Assert.IsNull(instance.transform.Find("Vitals/HealthValue"));

            hud.SetDebugTelemetryVisible(false);
            Assert.IsFalse(hud.DebugTelemetryVisible);
            hud.SetDebugTelemetryVisible(true);
            Assert.IsTrue(hud.DebugTelemetryVisible);
        }
        finally
        {
            Object.DestroyImmediate(instance);
        }
    }

    [TestCase(1f, 2f, false)]
    [TestCase(2f, 2f, true)]
    [TestCase(3f, 2f, true)]
    public void SelectedItemLabel_ExpiresAtConfiguredTime(float currentTime, float hideAt, bool expected)
    {
        Assert.AreEqual(expected, SandboxHudController.IsSelectedItemLabelExpired(currentTime, hideAt));
        Assert.That(SandboxHudController.SelectedItemLabelDurationSeconds, Is.InRange(1.5f, 2f));
    }

    [TestCase(1920f, 1080f)]
    [TestCase(2560f, 1440f)]
    [TestCase(1920f, 1200f)]
    [TestCase(2560f, 1080f)]
    [TestCase(1024f, 768f)]
    public void HudLayout_FitsRepresentativeAspectRatios(float screenWidth, float screenHeight)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/SandboxHUD.prefab");
        GameObject instance = Object.Instantiate(prefab);
        try
        {
            SandboxHudController hud = instance.GetComponent<SandboxHudController>();
            if (hud.SlotViewCount == 0)
            {
                hud.SendMessage("Awake");
            }

            CanvasScaler scaler = instance.GetComponent<CanvasScaler>();
            float widthScale = screenWidth / scaler.referenceResolution.x;
            float heightScale = screenHeight / scaler.referenceResolution.y;
            float scale = Mathf.Sqrt(widthScale * heightScale);
            float canvasWidth = screenWidth / scale;
            float canvasHeight = screenHeight / scale;

            RectTransform vitals = instance.transform.Find("Vitals") as RectTransform;
            RectTransform hotbar = instance.transform.Find("CreativeHotbar") as RectTransform;
            RectTransform telemetry = instance.transform.Find("WorldInfo") as RectTransform;
            Assert.GreaterOrEqual(canvasWidth, hotbar.rect.width + 32f);
            Assert.GreaterOrEqual(canvasWidth, vitals.rect.width + telemetry.rect.width + 48f);
            Assert.GreaterOrEqual(canvasHeight, vitals.rect.height + hotbar.rect.height + 96f);
        }
        finally
        {
            Object.DestroyImmediate(instance);
        }
    }
}
