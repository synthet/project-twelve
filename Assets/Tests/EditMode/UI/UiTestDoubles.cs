using System;
using System.Collections.Generic;
using ProjectTwelve.UI;
using ProjectTwelve.UI.Inventory;

namespace ProjectTwelve.Tests.UI
{
    /// <summary>In-memory view-model whose slots and change events are driven directly by tests.</summary>
    internal sealed class FakeInventoryViewModel : IInventoryViewModel
    {
        private readonly InventorySlotViewData[] slots;

        public FakeInventoryViewModel(int slotCount)
        {
            slots = new InventorySlotViewData[slotCount];
        }

        public event Action<int> SlotChanged;
        public event Action Rebuilt;

        public int SlotCount => slots.Length;

        public InventorySlotViewData GetSlot(int index) => slots[index];

        public void SetSlot(int index, InventorySlotViewData data) => slots[index] = data;

        public void RaiseSlotChanged(int index) => SlotChanged?.Invoke(index);

        public void RaiseRebuilt() => Rebuilt?.Invoke();
    }

    /// <summary>Command handler that returns a fixed result and records the calls it received.</summary>
    internal sealed class FakeInventoryCommandHandler : IInventoryCommandHandler
    {
        public InventoryOperationResult NextResult = InventoryOperationResult.Success;
        public readonly List<(int source, int destination, int amount)> Moves = new List<(int, int, int)>();

        public InventoryOperationResult TryMove(int source, int destination, int amount)
        {
            Moves.Add((source, destination, amount));
            return NextResult;
        }

        public InventoryOperationResult TrySplit(int source, int destination, int amount) => NextResult;
        public InventoryOperationResult TryAssignHotbar(int source, int hotbarIndex) => NextResult;
        public InventoryOperationResult TryClear(int slot) => NextResult;
    }

    /// <summary>Minimal screen for exercising the modal stack's input-blocking logic.</summary>
    internal sealed class FakeScreen : IUiScreen
    {
        public bool BlocksGameplayInput { get; set; }
        public UiLayer Layer { get; set; } = UiLayer.Windows;
        public int ShowCount { get; private set; }
        public int HideCount { get; private set; }

        public void Show() => ShowCount++;
        public void Hide() => HideCount++;
    }
}
