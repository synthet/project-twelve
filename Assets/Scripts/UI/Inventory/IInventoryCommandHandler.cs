namespace ProjectTwelve.UI.Inventory
{
    /// <summary>
    /// The write boundary for inventory UI. Every mutation the player requests (move, split, hotbar bind,
    /// clear) is routed through this handler, which applies the concrete inventory's own rules and returns
    /// a result. The UI never edits item collections directly, so inventory/stacking rules stay out of the
    /// view. A rejected result is the signal for the view to restore the source slot.
    /// </summary>
    public interface IInventoryCommandHandler
    {
        /// <summary>Moves/swaps up to <paramref name="amount"/> items from source to destination.</summary>
        InventoryOperationResult TryMove(int source, int destination, int amount);

        /// <summary>Splits <paramref name="amount"/> from source into an (empty) destination.</summary>
        InventoryOperationResult TrySplit(int source, int destination, int amount);

        /// <summary>Assigns the item in <paramref name="source"/> to a hotbar index.</summary>
        InventoryOperationResult TryAssignHotbar(int source, int hotbarIndex);

        /// <summary>Clears the given slot.</summary>
        InventoryOperationResult TryClear(int slot);
    }
}
