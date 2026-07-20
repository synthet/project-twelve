using UnityEngine;

namespace ProjectTwelve.UI.Inventory
{
    /// <summary>
    /// The view-facing snapshot of a single inventory slot. Deliberately exposes only what the UI needs to
    /// draw and describe a slot — no domain types leak across the boundary, so the inventory UI stays
    /// independent of the concrete inventory implementation.
    /// </summary>
    public readonly struct InventorySlotViewData
    {
        public InventorySlotViewData(
            string itemId,
            Sprite icon,
            int count,
            int maxStack,
            bool isSelected = false,
            bool isLocked = false,
            bool isUsable = true,
            string tooltipTitle = null,
            string tooltipBody = null,
            float fillFraction = -1f)
        {
            ItemId = itemId;
            Icon = icon;
            Count = count;
            MaxStack = maxStack;
            IsSelected = isSelected;
            IsLocked = isLocked;
            IsUsable = isUsable;
            TooltipTitle = tooltipTitle;
            TooltipBody = tooltipBody;
            FillFraction = fillFraction;
        }

        /// <summary>Stable item identifier, or null/empty when the slot is empty.</summary>
        public string ItemId { get; }

        /// <summary>Icon sprite to draw, or null when empty.</summary>
        public Sprite Icon { get; }

        /// <summary>Stack count. Only shown when greater than one.</summary>
        public int Count { get; }

        /// <summary>Maximum stack size for this item (used by split/merge presentation).</summary>
        public int MaxStack { get; }

        /// <summary>True when this slot is the current selection.</summary>
        public bool IsSelected { get; }

        /// <summary>True when the slot is locked and cannot receive focus or items.</summary>
        public bool IsLocked { get; }

        /// <summary>True when the item can be used/placed (drives greyed presentation when false).</summary>
        public bool IsUsable { get; }

        /// <summary>Optional tooltip title (item name).</summary>
        public string TooltipTitle { get; }

        /// <summary>Optional tooltip body (description).</summary>
        public string TooltipBody { get; }

        /// <summary>Optional 0..1 durability/cooldown fill; negative means "none".</summary>
        public float FillFraction { get; }

        /// <summary>True when the slot holds no item.</summary>
        public bool IsEmpty => string.IsNullOrEmpty(ItemId) || Count <= 0;

        /// <summary>An explicit empty slot.</summary>
        public static InventorySlotViewData Empty => new InventorySlotViewData(null, null, 0, 0);

        /// <summary>Returns a copy with the selection flag overridden (selection is presentation state).</summary>
        public InventorySlotViewData WithSelected(bool selected)
        {
            return new InventorySlotViewData(
                ItemId, Icon, Count, MaxStack, selected, IsLocked, IsUsable,
                TooltipTitle, TooltipBody, FillFraction);
        }
    }
}
