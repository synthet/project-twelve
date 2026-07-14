using System;
using System.Collections.Generic;
using ProjectTwelve.Sandbox.Registry;

namespace ProjectTwelve.Sandbox.Inventory
{
    /// <summary>
    /// Fixed-size ordered inventory. Matching non-full stacks are filled before empty slots and
    /// every item is validated against the frozen item registry.
    /// </summary>
    public sealed class SandboxInventory
    {
        public readonly struct Slot
        {
            public Slot(string itemId, int count)
            {
                ItemId = itemId;
                Count = count;
            }

            public string ItemId { get; }
            public int Count { get; }
            public bool IsEmpty => string.IsNullOrEmpty(ItemId) || Count <= 0;
        }

        private readonly ContentRegistry<ItemDefinition> registry;
        private readonly string[] itemIds;
        private readonly int[] counts;

        public SandboxInventory(ContentRegistry<ItemDefinition> registry, int slotCount = SandboxInventoryConstants.SlotCount)
        {
            this.registry = registry ?? throw new ArgumentNullException(nameof(registry));
            if (!registry.IsFrozen)
            {
                throw new ArgumentException("Inventory requires a frozen item registry.", nameof(registry));
            }

            if (slotCount < SandboxInventoryConstants.HotbarSlotCount)
            {
                throw new ArgumentOutOfRangeException(nameof(slotCount), slotCount,
                    $"Inventory must contain at least {SandboxInventoryConstants.HotbarSlotCount} hotbar slots.");
            }

            itemIds = new string[slotCount];
            counts = new int[slotCount];
        }

        public event Action Changed;

        public int SlotCount => itemIds.Length;

        public Slot GetSlot(int index)
        {
            RequireSlot(index);
            return new Slot(itemIds[index], counts[index]);
        }

        public int CountItem(string itemId)
        {
            if (!registry.TryGet(itemId, out ItemDefinition definition))
            {
                return 0;
            }

            string canonicalId = definition.Id;
            int total = 0;
            for (int i = 0; i < itemIds.Length; i++)
            {
                if (string.Equals(itemIds[i], canonicalId, StringComparison.Ordinal))
                {
                    total += counts[i];
                }
            }

            return total;
        }

        public void SetSlot(int index, string itemId, int count)
        {
            RequireSlot(index);
            if (string.IsNullOrEmpty(itemId) || count <= 0)
            {
                itemIds[index] = null;
                counts[index] = 0;
                Changed?.Invoke();
                return;
            }

            ItemDefinition definition = registry.Get(itemId);
            if (count > definition.MaxStack)
            {
                throw new ArgumentOutOfRangeException(nameof(count), count,
                    $"Item '{itemId}' stack exceeds max {definition.MaxStack}.");
            }

            // Store the canonical ID so retired aliases from old saves normalize on load.
            itemIds[index] = definition.Id;
            counts[index] = count;
            Changed?.Invoke();
        }

        /// <summary>Adds as much as possible and returns the amount that did not fit.</summary>
        public int Add(string itemId, int count)
        {
            if (count <= 0)
            {
                return 0;
            }

            ItemDefinition definition = registry.Get(itemId);
            string canonicalId = definition.Id;
            int remaining = count;
            bool changed = false;

            for (int i = 0; i < itemIds.Length && remaining > 0; i++)
            {
                if (!string.Equals(itemIds[i], canonicalId, StringComparison.Ordinal)
                    || counts[i] >= definition.MaxStack)
                {
                    continue;
                }

                int moved = Math.Min(remaining, definition.MaxStack - counts[i]);
                counts[i] += moved;
                remaining -= moved;
                changed = true;
            }

            for (int i = 0; i < itemIds.Length && remaining > 0; i++)
            {
                if (counts[i] > 0)
                {
                    continue;
                }

                int moved = Math.Min(remaining, definition.MaxStack);
                itemIds[i] = canonicalId;
                counts[i] = moved;
                remaining -= moved;
                changed = true;
            }

            if (changed)
            {
                Changed?.Invoke();
            }

            return remaining;
        }

        public bool TryConsumeAt(int index, int count = 1)
        {
            RequireSlot(index);
            if (count <= 0 || counts[index] < count)
            {
                return false;
            }

            counts[index] -= count;
            if (counts[index] == 0)
            {
                itemIds[index] = null;
            }

            Changed?.Invoke();
            return true;
        }

        public SandboxInventorySaveData ToSaveData()
        {
            SandboxInventorySaveData data = new SandboxInventorySaveData();
            for (int i = 0; i < itemIds.Length; i++)
            {
                if (counts[i] > 0)
                {
                    data.slots.Add(new SandboxInventorySlotSaveData(i, itemIds[i], counts[i]));
                }
            }

            return data;
        }

        public void LoadFromSaveData(SandboxInventorySaveData data)
        {
            Array.Clear(itemIds, 0, itemIds.Length);
            Array.Clear(counts, 0, counts.Length);
            if (data?.slots != null)
            {
                HashSet<int> populated = new HashSet<int>();
                foreach (SandboxInventorySlotSaveData slot in data.slots)
                {
                    if (slot.index < 0 || slot.index >= itemIds.Length || !populated.Add(slot.index))
                    {
                        throw new InvalidOperationException($"Invalid or duplicate saved inventory slot {slot.index}.");
                    }

                    ItemDefinition definition = registry.Get(slot.itemId);
                    if (slot.count <= 0 || slot.count > definition.MaxStack)
                    {
                        throw new InvalidOperationException(
                            $"Saved stack '{slot.itemId}' count {slot.count} is outside 1-{definition.MaxStack}.");
                    }

                    itemIds[slot.index] = definition.Id;
                    counts[slot.index] = slot.count;
                }
            }

            Changed?.Invoke();
        }

        public static SandboxInventory CreatePrototypeLoadout(ContentRegistry<ItemDefinition> registry)
        {
            SandboxInventory inventory = new SandboxInventory(registry);
            inventory.SetSlot(0, "core:dirt", SandboxInventoryConstants.PrototypeStartingStack);
            inventory.SetSlot(1, "core:grass", SandboxInventoryConstants.PrototypeStartingStack);
            inventory.SetSlot(2, "core:stone", SandboxInventoryConstants.PrototypeStartingStack);
            inventory.SetSlot(3, "core:bricks_a", SandboxInventoryConstants.PrototypeStartingStack);
            inventory.SetSlot(4, "core:bricks_b", SandboxInventoryConstants.PrototypeStartingStack);
            inventory.SetSlot(5, "core:bricks_c", SandboxInventoryConstants.PrototypeStartingStack);
            inventory.SetSlot(6, "core:bricks_d", SandboxInventoryConstants.PrototypeStartingStack);
            inventory.SetSlot(7, "core:frozen", SandboxInventoryConstants.PrototypeStartingStack);
            inventory.SetSlot(8, "core:magma", SandboxInventoryConstants.PrototypeStartingStack);
            inventory.SetSlot(9, "core:sand", SandboxInventoryConstants.PrototypeStartingStack);
            return inventory;
        }

        private void RequireSlot(int index)
        {
            if (index < 0 || index >= itemIds.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(index), index, $"Slot must be in 0-{itemIds.Length - 1}.");
            }
        }
    }

    [Serializable]
    public sealed class SandboxInventorySaveData
    {
        public List<SandboxInventorySlotSaveData> slots = new List<SandboxInventorySlotSaveData>();
    }

    [Serializable]
    public struct SandboxInventorySlotSaveData
    {
        public int index;
        public string itemId;
        public int count;

        public SandboxInventorySlotSaveData(int index, string itemId, int count)
        {
            this.index = index;
            this.itemId = itemId;
            this.count = count;
        }
    }
}
