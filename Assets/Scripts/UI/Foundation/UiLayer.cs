namespace ProjectTwelve.UI
{
    /// <summary>
    /// Explicit HUD layers, ordered back-to-front. Centralising the bands here removes the scattered
    /// per-prefab sorting-order constants that let a tooltip render behind a panel or a dropdown clip
    /// inside its parent scroll area. Numeric ordering matches visual stacking (higher = in front).
    /// </summary>
    public enum UiLayer
    {
        WorldIndicators = 0,
        PersistentHud = 1,
        Windows = 2,
        DropdownsContextMenus = 3,
        DragPreview = 4,
        Tooltips = 5,
        Modal = 6,
        Debug = 7,
    }

    /// <summary>Helpers mapping <see cref="UiLayer"/> bands onto concrete canvas sorting orders.</summary>
    public static class UiLayers
    {
        /// <summary>Sorting-order spacing between adjacent layer bands, leaving room within each band.</summary>
        public const int BandStride = 100;

        /// <summary>Base <c>Canvas.sortingOrder</c> for a layer band.</summary>
        public static int SortingOrder(UiLayer layer)
        {
            return (int)layer * BandStride;
        }

        /// <summary>Total number of layer bands.</summary>
        public const int Count = 8;
    }
}
