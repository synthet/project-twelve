using System;

namespace ProjectTwelve.UI.Inventory
{
    /// <summary>
    /// Read-only, view-facing projection of an inventory. The grid renders from this and listens to
    /// <see cref="SlotChanged"/> for targeted, event-driven refreshes — never per-frame polling. Concrete
    /// adapters (e.g. over the Sandbox inventory) live on the domain side and diff their source so a
    /// single-slot change raises exactly one <see cref="SlotChanged"/>.
    /// </summary>
    public interface IInventoryViewModel
    {
        /// <summary>Number of slots exposed to the view.</summary>
        int SlotCount { get; }

        /// <summary>Returns the current view snapshot for a slot.</summary>
        InventorySlotViewData GetSlot(int index);

        /// <summary>Raised when a single slot changes; argument is the slot index.</summary>
        event Action<int> SlotChanged;

        /// <summary>Raised when the whole model changes shape and the view should rebuild.</summary>
        event Action Rebuilt;
    }
}
