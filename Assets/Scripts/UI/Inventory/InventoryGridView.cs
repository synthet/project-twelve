using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ProjectTwelve.UI.Controls;
using ProjectTwelve.UI.Theme;

namespace ProjectTwelve.UI.Inventory
{
    /// <summary>Directional navigation input for grid focus traversal.</summary>
    public enum GridDirection { Up, Down, Left, Right }

    /// <summary>Configurable inventory grid dimensions/spacing, so grids of any shape build without code changes.</summary>
    [Serializable]
    public struct InventoryGridConfig
    {
        public int columns;
        public float slotSize;
        public float spacing;

        public static InventoryGridConfig Default => new InventoryGridConfig { columns = 5, slotSize = 48f, spacing = 4f };
    }

    /// <summary>
    /// A configurable, pooled grid of inventory slots bound to an <see cref="IInventoryViewModel"/>. It
    /// rebuilds on demand (not per-frame) and, crucially, refreshes a <b>single</b> slot in response to the
    /// model's per-slot <c>SlotChanged</c> event instead of re-reading every slot. Slot interactions route
    /// through an <see cref="IInventoryCommandHandler"/>; a rejected move restores the affected slots.
    /// </summary>
    public sealed class InventoryGridView : MonoBehaviour, IUiThemeConsumer
    {
        [SerializeField] private InventoryGridConfig config = InventoryGridConfig.Default;

        private readonly List<InventorySlotView> pool = new List<InventorySlotView>();

        private IInventoryViewModel viewModel;
        private IInventoryCommandHandler commandHandler;
        private UiThemeProvider provider;
        private TooltipService tooltipService;
        private int selectedIndex = -1;

        /// <summary>Raised when a slot is clicked/selected (presentation-state; owner decides what it means).</summary>
        public event Action<int> SlotSelected;

        /// <summary>Number of active slot views currently shown.</summary>
        public int ActiveSlotCount => viewModel != null ? viewModel.SlotCount : 0;

        /// <summary>Grid column count.</summary>
        public int Columns => Mathf.Max(1, config.columns);

        // ---- Pure layout / navigation helpers (unit-testable without a scene) --------------------------

        /// <summary>Row of a slot index for the given column count.</summary>
        public static int IndexToRow(int index, int columns) => columns > 0 ? index / columns : 0;

        /// <summary>Column of a slot index for the given column count.</summary>
        public static int IndexToColumn(int index, int columns) => columns > 0 ? index % columns : 0;

        /// <summary>Slot index for a (row, column) pair.</summary>
        public static int CellToIndex(int row, int column, int columns) => row * columns + column;

        /// <summary>
        /// Deterministic focus navigation: returns the target index for a move, or the same index when the
        /// move would leave the grid (focus stays put). Never returns an out-of-range index.
        /// </summary>
        public static int Navigate(int index, GridDirection direction, int columns, int count)
        {
            if (count <= 0 || columns <= 0)
            {
                return index;
            }

            int row = IndexToRow(index, columns);
            int col = IndexToColumn(index, columns);
            switch (direction)
            {
                case GridDirection.Right:
                    return (col < columns - 1 && index + 1 < count) ? index + 1 : index;
                case GridDirection.Left:
                    return col > 0 ? index - 1 : index;
                case GridDirection.Down:
                    return index + columns < count ? index + columns : index;
                case GridDirection.Up:
                    return row > 0 ? index - columns : index;
                default:
                    return index;
            }
        }

        // ---- Binding / lifecycle -----------------------------------------------------------------------

        public void Bind(UiThemeProvider themeProvider)
        {
            provider = themeProvider;
            for (int i = 0; i < pool.Count; i++)
            {
                pool[i].Bind(provider);
            }
        }

        public void Refresh() => RebuildAll();

        /// <summary>Wires the grid to its data, command handler, and shared services, then builds the slots.</summary>
        public void Initialize(
            IInventoryViewModel model,
            IInventoryCommandHandler handler,
            UiThemeProvider themeProvider = null,
            TooltipService tooltip = null,
            InventoryGridConfig? gridConfig = null)
        {
            if (viewModel != null)
            {
                viewModel.SlotChanged -= OnSlotChanged;
                viewModel.Rebuilt -= RebuildAll;
            }

            viewModel = model;
            commandHandler = handler;
            provider = themeProvider ?? provider;
            tooltipService = tooltip ?? tooltipService;
            if (gridConfig.HasValue)
            {
                config = gridConfig.Value;
            }

            if (viewModel != null)
            {
                viewModel.SlotChanged += OnSlotChanged;
                viewModel.Rebuilt += RebuildAll;
            }

            RebuildAll();
        }

        private void OnDestroy()
        {
            if (viewModel != null)
            {
                viewModel.SlotChanged -= OnSlotChanged;
                viewModel.Rebuilt -= RebuildAll;
            }
        }

        // ---- View updates ------------------------------------------------------------------------------

        /// <summary>Full rebuild: sizes the pool, positions every slot, and binds current data.</summary>
        public void RebuildAll()
        {
            if (viewModel == null)
            {
                return;
            }

            int count = viewModel.SlotCount;
            EnsurePool(count);

            for (int i = 0; i < pool.Count; i++)
            {
                bool active = i < count;
                pool[i].gameObject.SetActive(active);
                if (active)
                {
                    PositionSlot(pool[i].GetComponent<RectTransform>(), i);
                    pool[i].SetData(i, ComposeSlotData(i));
                }
            }
        }

        /// <summary>Refreshes exactly one slot view. This is the single-slot event path.</summary>
        public void RefreshSlot(int index)
        {
            if (viewModel == null || index < 0 || index >= viewModel.SlotCount || index >= pool.Count)
            {
                return;
            }

            pool[index].SetData(index, ComposeSlotData(index));
        }

        /// <summary>Sets the presentation selection and refreshes only the two affected slots.</summary>
        public void SetSelected(int index)
        {
            int previous = selectedIndex;
            selectedIndex = index;
            if (previous >= 0)
            {
                RefreshSlot(previous);
            }

            if (index >= 0)
            {
                RefreshSlot(index);
            }
        }

        /// <summary>The slot view at an index (or null), for wiring/tests. Does not allocate.</summary>
        public InventorySlotView GetSlotView(int index)
        {
            return index >= 0 && index < pool.Count ? pool[index] : null;
        }

        /// <summary>
        /// Routes a drop through the command handler. On a non-success result the source and destination are
        /// re-bound from the model (restoring their visuals), so a rejected/invalid drop never leaves a
        /// half-applied view. Returns the handler's result.
        /// </summary>
        public InventoryOperationResult HandleDrop(int source, int destination, int amount = int.MaxValue)
        {
            if (commandHandler == null)
            {
                return InventoryOperationResult.Rejected;
            }

            InventoryOperationResult result = commandHandler.TryMove(source, destination, amount);
            if (!result.IsSuccess())
            {
                RefreshSlot(source);
                RefreshSlot(destination);
            }

            return result;
        }

        private InventorySlotViewData ComposeSlotData(int index)
        {
            InventorySlotViewData model = viewModel.GetSlot(index);
            return index == selectedIndex ? model.WithSelected(true) : model.WithSelected(false);
        }

        private void OnSlotChanged(int index) => RefreshSlot(index);

        private void EnsurePool(int count)
        {
            while (pool.Count < count)
            {
                pool.Add(CreateSlot(pool.Count));
            }
        }

        private InventorySlotView CreateSlot(int index)
        {
            Image image = UiFactory.CreateImage($"Slot{index}", transform, null, Color.white, raycastTarget: true);
            InventorySlotView slot = image.gameObject.AddComponent<InventorySlotView>();
            slot.Build();
            slot.Bind(provider);
            if (tooltipService != null && slot.Tooltip != null)
            {
                slot.Tooltip.Bind(tooltipService);
            }

            slot.Clicked += OnSlotClicked;
            slot.Dropped += OnSlotDropped;
            return slot;
        }

        private void OnSlotClicked(int index)
        {
            SetSelected(index);
            SlotSelected?.Invoke(index);
        }

        private void OnSlotDropped(int source, int destination)
        {
            HandleDrop(source, destination);
        }

        private void PositionSlot(RectTransform rect, int index)
        {
            int columns = Columns;
            int row = IndexToRow(index, columns);
            int col = IndexToColumn(index, columns);
            float step = config.slotSize + config.spacing;
            UiFactory.SetRect(rect,
                new Vector2(col * step, -row * step),
                new Vector2(config.slotSize, config.slotSize),
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f));
        }
    }
}
