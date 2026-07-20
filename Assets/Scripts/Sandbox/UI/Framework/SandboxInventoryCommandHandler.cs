using System;
using ProjectTwelve.Sandbox.Inventory;
using ProjectTwelve.Sandbox.Registry;
using ProjectTwelve.UI.Inventory;

namespace ProjectTwelve.Sandbox.UI
{
    /// <summary>
    /// Routes framework inventory commands to the domain <see cref="SandboxInventory"/>. This is the domain
    /// side of the UI write boundary: the move/split/merge/swap rules live here (over the inventory's own
    /// <c>SetSlot</c> API), not in any view component, so the UI stays free of inventory rules. Every method
    /// validates inputs and returns a typed result the view uses purely for presentation.
    /// </summary>
    public sealed class SandboxInventoryCommandHandler : IInventoryCommandHandler
    {
        private readonly SandboxInventory inventory;

        public SandboxInventoryCommandHandler(SandboxInventory inventory)
        {
            this.inventory = inventory ?? throw new ArgumentNullException(nameof(inventory));
        }

        public InventoryOperationResult TryMove(int source, int destination, int amount)
        {
            if (!InRange(source) || !InRange(destination))
            {
                return InventoryOperationResult.InvalidSlot;
            }

            if (source == destination || amount <= 0)
            {
                return InventoryOperationResult.Rejected;
            }

            SandboxInventory.Slot src = inventory.GetSlot(source);
            if (src.IsEmpty)
            {
                return InventoryOperationResult.Rejected;
            }

            SandboxInventory.Slot dst = inventory.GetSlot(destination);
            int moveAmount = Math.Min(amount, src.Count);

            if (dst.IsEmpty)
            {
                inventory.SetSlot(destination, src.ItemId, moveAmount);
                WriteRemainder(source, src.ItemId, src.Count - moveAmount);
                return InventoryOperationResult.Success;
            }

            if (string.Equals(dst.ItemId, src.ItemId, StringComparison.Ordinal))
            {
                int maxStack = MaxStack(src.ItemId);
                int space = maxStack - dst.Count;
                if (space <= 0)
                {
                    return InventoryOperationResult.Full;
                }

                int moved = Math.Min(moveAmount, space);
                inventory.SetSlot(destination, dst.ItemId, dst.Count + moved);
                WriteRemainder(source, src.ItemId, src.Count - moved);
                return InventoryOperationResult.Success;
            }

            // Different items: only a whole-stack swap is allowed (no partial merge onto another item).
            if (moveAmount < src.Count)
            {
                return InventoryOperationResult.Rejected;
            }

            inventory.SetSlot(destination, src.ItemId, src.Count);
            inventory.SetSlot(source, dst.ItemId, dst.Count);
            return InventoryOperationResult.Success;
        }

        public InventoryOperationResult TrySplit(int source, int destination, int amount)
        {
            if (!InRange(source) || !InRange(destination))
            {
                return InventoryOperationResult.InvalidSlot;
            }

            if (source == destination)
            {
                return InventoryOperationResult.Rejected;
            }

            SandboxInventory.Slot src = inventory.GetSlot(source);
            SandboxInventory.Slot dst = inventory.GetSlot(destination);
            if (src.IsEmpty || !dst.IsEmpty || amount <= 0 || amount >= src.Count)
            {
                return InventoryOperationResult.Rejected;
            }

            inventory.SetSlot(destination, src.ItemId, amount);
            inventory.SetSlot(source, src.ItemId, src.Count - amount);
            return InventoryOperationResult.Success;
        }

        public InventoryOperationResult TryAssignHotbar(int source, int hotbarIndex)
        {
            if (hotbarIndex < 0 || hotbarIndex >= SandboxInventoryConstants.HotbarSlotCount)
            {
                return InventoryOperationResult.InvalidSlot;
            }

            return TryMove(source, hotbarIndex, int.MaxValue);
        }

        public InventoryOperationResult TryClear(int slot)
        {
            if (!InRange(slot))
            {
                return InventoryOperationResult.InvalidSlot;
            }

            inventory.SetSlot(slot, null, 0);
            return InventoryOperationResult.Success;
        }

        private void WriteRemainder(int slot, string itemId, int remaining)
        {
            if (remaining > 0)
            {
                inventory.SetSlot(slot, itemId, remaining);
            }
            else
            {
                inventory.SetSlot(slot, null, 0);
            }
        }

        private static int MaxStack(string itemId)
        {
            return SandboxRegistries.Items.TryGet(itemId, out ItemDefinition item)
                ? item.MaxStack
                : SandboxInventoryConstants.DefaultMaxStack;
        }

        private bool InRange(int index) => index >= 0 && index < inventory.SlotCount;
    }
}
