namespace ProjectTwelve.UI.Inventory
{
    /// <summary>
    /// Outcome of an inventory command requested by the UI. The UI only reads this to decide presentation
    /// (apply the change or restore the source and show feedback); it never encodes gameplay rules itself.
    /// </summary>
    public enum InventoryOperationResult
    {
        Success = 0,
        Rejected = 1,
        OutOfRange = 2,
        Full = 3,
        InvalidSlot = 4,
    }

    /// <summary>Convenience helpers for interpreting an <see cref="InventoryOperationResult"/>.</summary>
    public static class InventoryOperationResults
    {
        /// <summary>True only for <see cref="InventoryOperationResult.Success"/>.</summary>
        public static bool IsSuccess(this InventoryOperationResult result)
        {
            return result == InventoryOperationResult.Success;
        }
    }
}
