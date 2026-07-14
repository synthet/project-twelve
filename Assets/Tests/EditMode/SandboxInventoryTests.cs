using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using ProjectTwelve.Sandbox.Inventory;
using ProjectTwelve.Sandbox.Registry;
using UnityEngine;

public sealed class SandboxInventoryTests
{
    private static readonly string[] PrototypeHotbarItems =
    {
        "core:dirt",
        "core:grass",
        "core:stone",
        "core:bricks_a",
        "core:bricks_b",
        "core:bricks_c",
        "core:bricks_d",
        "core:frozen",
        "core:magma",
        "core:sand",
    };

    [TearDown]
    public void TearDown()
    {
        SandboxRegistries.ResetForTests();
    }

    [Test]
    public void Add_MergesMatchingStacksBeforeOpeningEmptySlots_AndHonorsMaxStack()
    {
        ContentRegistry<ItemDefinition> items = BuildItems(new ItemDefinition("test:block", maxStack: 5));
        SandboxInventory inventory = new SandboxInventory(items, 10);
        inventory.SetSlot(0, "test:block", 4);
        inventory.SetSlot(2, "test:block", 2);

        int remainder = inventory.Add("test:block", 8);

        Assert.Zero(remainder);
        Assert.AreEqual(5, inventory.GetSlot(0).Count);
        Assert.AreEqual(5, inventory.GetSlot(2).Count);
        Assert.AreEqual(4, inventory.GetSlot(1).Count,
            "Only after all matching stacks are full should the first empty slot be used.");
    }

    [Test]
    public void PrototypeLoadout_FillsHotbarWithEveryGroundMaterial()
    {
        SandboxInventory inventory = SandboxInventory.CreatePrototypeLoadout(SandboxRegistries.Items);

        for (int i = 0; i < PrototypeHotbarItems.Length; i++)
        {
            SandboxInventory.Slot slot = inventory.GetSlot(i);
            Assert.AreEqual(PrototypeHotbarItems[i], slot.ItemId, $"Unexpected item in hotbar slot {i}.");
            Assert.AreEqual(SandboxInventoryConstants.PrototypeStartingStack, slot.Count);
        }
    }

    [Test]
    public void Add_FullInventoryReturnsRemainderWithoutLosingDrop()
    {
        SandboxInventory inventory = new SandboxInventory(SandboxRegistries.Items, 10);
        for (int i = 0; i < inventory.SlotCount; i++)
        {
            inventory.SetSlot(i, "core:stone", SandboxRegistries.Items.Get("core:stone").MaxStack);
        }

        Assert.AreEqual(1, inventory.Add("core:dirt", 1));
        Assert.Zero(inventory.CountItem("core:dirt"));
    }

    [Test]
    public void Break_WithFullInventoryLeavesRegistryDropUnconsumed()
    {
        SandboxInventory inventory = new SandboxInventory(SandboxRegistries.Items, 10);
        for (int i = 0; i < inventory.SlotCount; i++)
        {
            inventory.SetSlot(i, "core:stone", SandboxRegistries.Items.Get("core:stone").MaxStack);
        }

        FakeInventoryWorld world = new FakeInventoryWorld();
        world.Seed(3, 4, SandboxRegistries.Tiles.GetIndex("core:dirt"));

        Assert.AreEqual(
            SandboxInventoryEditResult.Success,
            CreateService().TryBreak(world, 3, 4, out SandboxItemStack drop));
        Assert.AreEqual("core:dirt", drop.ItemId);
        Assert.AreEqual(1, inventory.Add(drop.ItemId, drop.Count),
            "The pickup remainder must stay in the world when every slot is full.");
        Assert.AreEqual(SandboxRegistries.AirIndex, world.GetTileId(3, 4));
    }

    [Test]
    public void Place_ConsumesExactlyOneOnlyAfterSuccessfulWorldMutation()
    {
        SandboxInventory inventory = new SandboxInventory(SandboxRegistries.Items, 10);
        inventory.SetSlot(0, "core:dirt", 2);
        FakeInventoryWorld world = new FakeInventoryWorld();
        SandboxInventoryEditService service = CreateService();

        Assert.AreEqual(SandboxInventoryEditResult.Success, service.TryPlace(world, inventory, 0, 2, 3));
        Assert.AreEqual(1, inventory.GetSlot(0).Count);
        Assert.AreEqual(SandboxRegistries.Tiles.GetIndex("core:dirt"), world.GetTileId(2, 3));

        Assert.AreEqual(SandboxInventoryEditResult.Occupied, service.TryPlace(world, inventory, 0, 2, 3));
        Assert.AreEqual(1, inventory.GetSlot(0).Count, "Occupied placement must not consume an item.");

        world.RejectWrites = true;
        Assert.AreEqual(SandboxInventoryEditResult.WorldRejected, service.TryPlace(world, inventory, 0, 4, 3));
        Assert.AreEqual(1, inventory.GetSlot(0).Count, "Rejected world mutation must not consume an item.");
    }

    [Test]
    public void Place_EmptySlotFailsWithoutMutation()
    {
        SandboxInventory inventory = new SandboxInventory(SandboxRegistries.Items, 10);
        FakeInventoryWorld world = new FakeInventoryWorld();

        Assert.AreEqual(
            SandboxInventoryEditResult.EmptySlot,
            CreateService().TryPlace(world, inventory, 0, 1, 1));
        Assert.AreEqual(SandboxRegistries.AirIndex, world.GetTileId(1, 1));
    }

    [Test]
    public void Break_ProducesRegistryDefinedDropAndRoutesTileToAir()
    {
        FakeInventoryWorld world = new FakeInventoryWorld();
        int stone = SandboxRegistries.Tiles.GetIndex("core:stone");
        world.Seed(5, -2, stone);

        SandboxInventoryEditResult result = CreateService().TryBreak(world, 5, -2, out SandboxItemStack drop);

        Assert.AreEqual(SandboxInventoryEditResult.Success, result);
        Assert.AreEqual("core:stone", drop.ItemId);
        Assert.AreEqual(1, drop.Count);
        Assert.AreEqual(SandboxRegistries.AirIndex, world.GetTileId(5, -2));
    }

    [Test]
    public void PlaceThenBreak_ConservesItemAndTileCountsAfterPickup()
    {
        SandboxInventory inventory = new SandboxInventory(SandboxRegistries.Items, 10);
        inventory.SetSlot(0, "core:dirt", 3);
        FakeInventoryWorld world = new FakeInventoryWorld();
        SandboxInventoryEditService service = CreateService();

        Assert.AreEqual(SandboxInventoryEditResult.Success, service.TryPlace(world, inventory, 0, 0, 0));
        Assert.AreEqual(2, inventory.CountItem("core:dirt"));
        Assert.AreEqual(SandboxInventoryEditResult.Success, service.TryBreak(world, 0, 0, out SandboxItemStack drop));
        Assert.Zero(inventory.Add(drop.ItemId, drop.Count));

        Assert.AreEqual(3, inventory.CountItem("core:dirt"));
        Assert.AreEqual(SandboxRegistries.AirIndex, world.GetTileId(0, 0));
    }

    [TestCase(0f, 0f, 6f, 0f, 6f, true)]
    [TestCase(0f, 0f, 6.01f, 0f, 6f, false)]
    [TestCase(-2f, -2f, 1f, 2f, 5f, true)]
    public void ReachValidation_IsDeterministicAtBoundary(
        float playerX, float playerY, float targetX, float targetY, float range, bool expected)
    {
        Assert.AreEqual(expected,
            SandboxInventoryEditService.IsWithinReach(playerX, playerY, targetX, targetY, range));
    }

    [Test]
    public void OutOfRangePlacementRequest_DoesNotConsumeOrMutate()
    {
        SandboxInventory inventory = new SandboxInventory(SandboxRegistries.Items, 10);
        inventory.SetSlot(0, "core:dirt", 2);
        FakeInventoryWorld world = new FakeInventoryWorld();

        SandboxInventoryEditResult validation = SandboxInventoryEditService.ValidateControllerRequest(
            0f, 0f, 6.1f, 0f, SandboxInventoryConstants.EditRange, playerOccluded: false);

        Assert.AreEqual(SandboxInventoryEditResult.OutOfRange, validation);
        Assert.AreEqual(2, inventory.GetSlot(0).Count);
        Assert.AreEqual(SandboxRegistries.AirIndex, world.GetTileId(6, 0));
    }

    [Test]
    public void InventorySaveData_RoundTripsExactSlotOrderAndCounts()
    {
        SandboxInventory original = new SandboxInventory(SandboxRegistries.Items, 10);
        original.SetSlot(0, "core:dirt", 17);
        original.SetSlot(7, "core:bricks_d", 3);

        string json = JsonUtility.ToJson(original.ToSaveData());
        SandboxInventorySaveData serialized = JsonUtility.FromJson<SandboxInventorySaveData>(json);
        SandboxInventory loaded = new SandboxInventory(SandboxRegistries.Items, 10);
        loaded.LoadFromSaveData(serialized);

        Assert.AreEqual("core:dirt", loaded.GetSlot(0).ItemId);
        Assert.AreEqual(17, loaded.GetSlot(0).Count);
        Assert.IsTrue(loaded.GetSlot(1).IsEmpty);
        Assert.AreEqual("core:bricks_d", loaded.GetSlot(7).ItemId);
        Assert.AreEqual(3, loaded.GetSlot(7).Count);
    }

    [Test]
    public void InventorySlots_CanonicalizeRetiredItemIdsFromOldSaves()
    {
        // Inventories saved before the ore → bricks rename carry the retired IDs;
        // every inventory ingress resolves them through the registry alias.
        SandboxInventory inventory = new SandboxInventory(SandboxRegistries.Items, 10);
        inventory.SetSlot(0, "core:gold_ore", 5);
        Assert.Zero(inventory.Add("core:gold_ore", 2));
        Assert.AreEqual("core:bricks_d", inventory.GetSlot(0).ItemId);
        Assert.AreEqual(7, inventory.CountItem("core:gold_ore"));

        SandboxInventorySaveData legacy = new SandboxInventorySaveData();
        legacy.slots.Add(new SandboxInventorySlotSaveData(1, "core:copper_ore", 3));
        inventory.LoadFromSaveData(legacy);

        Assert.AreEqual("core:bricks_a", inventory.GetSlot(1).ItemId);
        Assert.AreEqual(3, inventory.GetSlot(1).Count);
        Assert.AreEqual(3, inventory.CountItem("core:copper_ore"));
        Assert.AreEqual(3, inventory.CountItem("core:bricks_a"));
    }

    [Test]
    public void WorldSaveLoad_RoundTripsRegisteredPlayerInventory()
    {
        string path = Path.Combine(Path.GetTempPath(), $"project-twelve-inventory-{Guid.NewGuid():N}.json");
        string sidecar = SandboxWorld.GetVisualOverrideSidecarPath(path);
        GameObject sourceObject = new GameObject("InventorySaveWorld");
        GameObject targetObject = new GameObject("InventoryLoadWorld");
        try
        {
            SandboxInventory source = new SandboxInventory(SandboxRegistries.Items);
            source.SetSlot(0, "core:dirt", 12);
            source.SetSlot(11, "core:bricks_d", 4);
            SandboxWorld sourceWorld = sourceObject.AddComponent<SandboxWorld>();
            sourceWorld.SetPlayerInventory(source);
            sourceWorld.SaveToPath(path);

            SandboxInventory target = new SandboxInventory(SandboxRegistries.Items);
            target.SetSlot(0, "core:stone", 99);
            SandboxWorld targetWorld = targetObject.AddComponent<SandboxWorld>();
            targetWorld.SetPlayerInventory(target);
            targetWorld.LoadFromPath(path);

            Assert.AreEqual("core:dirt", target.GetSlot(0).ItemId);
            Assert.AreEqual(12, target.GetSlot(0).Count);
            Assert.AreEqual("core:bricks_d", target.GetSlot(11).ItemId);
            Assert.AreEqual(4, target.GetSlot(11).Count);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(sourceObject);
            UnityEngine.Object.DestroyImmediate(targetObject);
            if (File.Exists(path)) File.Delete(path);
            if (File.Exists(sidecar)) File.Delete(sidecar);
        }
    }

    [Test]
    public void CoreRegistry_ProvidesGrassItemAndValidStackLimits()
    {
        ItemDefinition grass = SandboxRegistries.Items.Get("core:grass");
        Assert.AreEqual("core:grass", grass.PlacesTileId);
        Assert.AreEqual(SandboxInventoryConstants.DefaultMaxStack, grass.MaxStack);
        Assert.Throws<ArgumentOutOfRangeException>(() => new ItemDefinition("test:invalid", maxStack: 0));
    }

    private static SandboxInventoryEditService CreateService()
    {
        return new SandboxInventoryEditService(SandboxRegistries.Tiles, SandboxRegistries.Items);
    }

    private static ContentRegistry<ItemDefinition> BuildItems(params ItemDefinition[] definitions)
    {
        ContentRegistry<ItemDefinition> registry = new ContentRegistry<ItemDefinition>();
        foreach (ItemDefinition definition in definitions)
        {
            registry.Register(definition);
        }

        registry.Freeze();
        return registry;
    }

    private sealed class FakeInventoryWorld : ISandboxInventoryWorld
    {
        private readonly Dictionary<(int x, int y), int> tiles = new Dictionary<(int x, int y), int>();

        public bool RejectWrites { get; set; }

        public int GetTileId(int x, int y)
        {
            return tiles.TryGetValue((x, y), out int tile) ? tile : SandboxRegistries.AirIndex;
        }

        public bool TrySetTile(int x, int y, int tileId)
        {
            if (RejectWrites)
            {
                return false;
            }

            tiles[(x, y)] = tileId;
            return true;
        }

        public void Seed(int x, int y, int tileId)
        {
            tiles[(x, y)] = tileId;
        }
    }
}
