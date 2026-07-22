using System;
using ProjectTwelve.Sandbox.Inventory;
using ProjectTwelve.Sandbox.Registry;

public readonly struct SandboxInventorySlotViewData : IEquatable<SandboxInventorySlotViewData>
{
    public SandboxInventorySlotViewData(int index, string itemId, int count, int maximumCount)
    {
        Index = index;
        ItemId = itemId;
        Count = count;
        MaximumCount = maximumCount;
    }

    public int Index { get; }
    public string ItemId { get; }
    public int Count { get; }
    public int MaximumCount { get; }
    public bool IsEmpty => string.IsNullOrEmpty(ItemId) || Count <= 0;

    public bool Equals(SandboxInventorySlotViewData other)
    {
        return Index == other.Index && Count == other.Count && MaximumCount == other.MaximumCount &&
            string.Equals(ItemId, other.ItemId, StringComparison.Ordinal);
    }

    public override bool Equals(object obj)
    {
        return obj is SandboxInventorySlotViewData other && Equals(other);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = Index;
            hash = (hash * 397) ^ Count;
            hash = (hash * 397) ^ MaximumCount;
            hash = (hash * 397) ^ (ItemId != null ? ItemId.GetHashCode() : 0);
            return hash;
        }
    }
}

public sealed class SandboxInventoryViewAdapter : IDisposable
{
    private readonly SandboxInventory inventory;
    private readonly ContentRegistry<ItemDefinition> registry;
    private readonly SandboxInventorySlotViewData[] cache;
    private bool disposed;

    public SandboxInventoryViewAdapter(SandboxInventory inventory, ContentRegistry<ItemDefinition> registry)
    {
        this.inventory = inventory ?? throw new ArgumentNullException(nameof(inventory));
        this.registry = registry ?? throw new ArgumentNullException(nameof(registry));
        cache = new SandboxInventorySlotViewData[inventory.SlotCount];
        for (int i = 0; i < cache.Length; i++)
        {
            cache[i] = ReadSlot(i);
        }

        inventory.Changed += OnInventoryChanged;
    }

    public event Action<int> SlotChanged;

    public int SlotCount => cache.Length;

    public SandboxInventorySlotViewData GetSlot(int index)
    {
        if (index < 0 || index >= cache.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        return cache[index];
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        inventory.Changed -= OnInventoryChanged;
    }

    private void OnInventoryChanged()
    {
        for (int i = 0; i < cache.Length; i++)
        {
            SandboxInventorySlotViewData current = ReadSlot(i);
            if (current.Equals(cache[i]))
            {
                continue;
            }

            cache[i] = current;
            SlotChanged?.Invoke(i);
        }
    }

    private SandboxInventorySlotViewData ReadSlot(int index)
    {
        SandboxInventory.Slot slot = inventory.GetSlot(index);
        int maximum = SandboxInventoryConstants.DefaultMaxStack;
        if (!slot.IsEmpty && registry.TryGet(slot.ItemId, out ItemDefinition definition))
        {
            maximum = definition.MaxStack;
        }

        return new SandboxInventorySlotViewData(index, slot.ItemId, slot.Count, maximum);
    }
}
