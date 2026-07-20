using System;
using ProjectTwelve.Sandbox.Inventory;
using ProjectTwelve.Sandbox.Registry;
using ProjectTwelve.UI.Inventory;
using UnityEngine;

namespace ProjectTwelve.Sandbox.UI
{
    /// <summary>
    /// Adapts the domain <see cref="SandboxInventory"/> to the framework's <see cref="IInventoryViewModel"/>.
    /// The domain inventory only raises a coarse "something changed" event, so this adapter keeps a snapshot
    /// and <b>diffs</b> it on each coarse event, raising <see cref="SlotChanged"/> only for slots that
    /// actually changed. That is what lets the grid refresh a single slot instead of re-reading all of them.
    /// The adapter is presentation-facing: it reads the inventory but never mutates it.
    /// </summary>
    public sealed class SandboxInventoryViewModel : IInventoryViewModel, IDisposable
    {
        private readonly SandboxInventory inventory;
        private readonly Func<string, Sprite> iconResolver;
        private readonly string[] lastItemIds;
        private readonly int[] lastCounts;

        public SandboxInventoryViewModel(SandboxInventory inventory, Func<string, Sprite> iconResolver = null)
        {
            this.inventory = inventory ?? throw new ArgumentNullException(nameof(inventory));
            this.iconResolver = iconResolver;
            lastItemIds = new string[inventory.SlotCount];
            lastCounts = new int[inventory.SlotCount];
            SnapshotAll();
            inventory.Changed += OnInventoryChanged;
        }

        public event Action<int> SlotChanged;
        public event Action Rebuilt;

        public int SlotCount => inventory.SlotCount;

        public InventorySlotViewData GetSlot(int index)
        {
            SandboxInventory.Slot slot = inventory.GetSlot(index);
            if (slot.IsEmpty)
            {
                return InventorySlotViewData.Empty;
            }

            int maxStack = SandboxRegistries.Items.TryGet(slot.ItemId, out ItemDefinition item)
                ? item.MaxStack
                : slot.Count;
            string name = FormatItemName(slot.ItemId);
            Sprite icon = iconResolver != null ? iconResolver(slot.ItemId) : null;

            return new InventorySlotViewData(
                slot.ItemId,
                icon,
                slot.Count,
                maxStack,
                tooltipTitle: name,
                tooltipBody: $"x{slot.Count}");
        }

        public void Dispose()
        {
            inventory.Changed -= OnInventoryChanged;
        }

        private void OnInventoryChanged()
        {
            for (int i = 0; i < inventory.SlotCount; i++)
            {
                SandboxInventory.Slot slot = inventory.GetSlot(i);
                if (!string.Equals(lastItemIds[i], slot.ItemId, StringComparison.Ordinal) || lastCounts[i] != slot.Count)
                {
                    lastItemIds[i] = slot.ItemId;
                    lastCounts[i] = slot.Count;
                    SlotChanged?.Invoke(i);
                }
            }
        }

        private void SnapshotAll()
        {
            for (int i = 0; i < inventory.SlotCount; i++)
            {
                SandboxInventory.Slot slot = inventory.GetSlot(i);
                lastItemIds[i] = slot.ItemId;
                lastCounts[i] = slot.Count;
            }
        }

        /// <summary>Turns "core:bricks_a" into "Bricks A" for display.</summary>
        public static string FormatItemName(string itemId)
        {
            if (string.IsNullOrEmpty(itemId))
            {
                return string.Empty;
            }

            int colon = itemId.IndexOf(':');
            string value = colon >= 0 ? itemId.Substring(colon + 1) : itemId;
            string[] words = value.Split('_');
            for (int i = 0; i < words.Length; i++)
            {
                if (words[i].Length > 0)
                {
                    words[i] = char.ToUpperInvariant(words[i][0]) + words[i].Substring(1);
                }
            }

            return string.Join(" ", words);
        }
    }
}
