using System;
using System.Collections.Generic;

/// <summary>
/// Pure selection/presentation state for the ten-slot hotbar. Quantities live in the player's
/// registry-backed SandboxInventory so this type stays focused on input order and display labels.
/// </summary>
public sealed class SandboxCreativeHotbarState
{
    public const int SlotCount = 10;

    public readonly struct Slot
    {
        public Slot(string tileId, string displayName)
        {
            TileId = tileId;
            DisplayName = displayName ?? string.Empty;
        }

        public string TileId { get; }
        public string DisplayName { get; }
        public bool IsPopulated => !string.IsNullOrEmpty(TileId);
    }

    private static readonly Slot[] DefaultSlots =
    {
        new Slot("core:dirt", "Dirt"),
        new Slot("core:grass", "Grass"),
        new Slot("core:stone", "Stone"),
        new Slot("core:bricks_a", "Bricks A"),
        new Slot("core:bricks_b", "Bricks B"),
        new Slot("core:bricks_c", "Bricks C"),
        new Slot("core:bricks_d", "Bricks D"),
        new Slot("core:frozen", "Frozen"),
        new Slot("core:magma", "Magma"),
        new Slot("core:sand", "Sand"),
    };

    private readonly Slot[] slots;
    private int selectedIndex;

    public SandboxCreativeHotbarState()
        : this(DefaultSlots)
    {
    }

    public SandboxCreativeHotbarState(IReadOnlyList<Slot> initialSlots)
    {
        if (initialSlots == null)
        {
            throw new ArgumentNullException(nameof(initialSlots));
        }

        if (initialSlots.Count != SlotCount)
        {
            throw new ArgumentException($"Creative hotbar requires exactly {SlotCount} slots.", nameof(initialSlots));
        }

        slots = new Slot[SlotCount];
        for (int i = 0; i < SlotCount; i++)
        {
            slots[i] = initialSlots[i];
        }
    }

    public event Action<int, Slot> SelectionChanged;

    public IReadOnlyList<Slot> Slots => slots;
    public int SelectedIndex => selectedIndex;
    public Slot SelectedSlot => slots[selectedIndex];
    public string SelectedTileId => SelectedSlot.TileId;

    public void SetSlot(int index, Slot slot)
    {
        if (index < 0 || index >= SlotCount)
        {
            throw new ArgumentOutOfRangeException(nameof(index), index, $"Slot must be in 0-{SlotCount - 1}.");
        }

        if (slots[index].TileId == slot.TileId && slots[index].DisplayName == slot.DisplayName)
        {
            return;
        }

        slots[index] = slot;
        if (index == selectedIndex)
        {
            SelectionChanged?.Invoke(selectedIndex, SelectedSlot);
        }
    }

    public bool Select(int index)
    {
        if (index < 0 || index >= SlotCount)
        {
            return false;
        }

        if (selectedIndex == index)
        {
            return true;
        }

        selectedIndex = index;
        SelectionChanged?.Invoke(selectedIndex, SelectedSlot);
        return true;
    }

    /// <summary>Cycles in the requested direction, skipping empty slots and wrapping.</summary>
    public bool CyclePopulated(int direction)
    {
        int step = Math.Sign(direction);
        if (step == 0)
        {
            return false;
        }

        for (int offset = 1; offset <= SlotCount; offset++)
        {
            int candidate = PositiveModulo(selectedIndex + offset * step, SlotCount);
            if (slots[candidate].IsPopulated)
            {
                Select(candidate);
                return true;
            }
        }

        return false;
    }

    private static int PositiveModulo(int value, int modulus)
    {
        int remainder = value % modulus;
        return remainder < 0 ? remainder + modulus : remainder;
    }
}
