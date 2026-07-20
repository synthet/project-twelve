using System.Collections.Generic;
using NUnit.Framework;
using ProjectTwelve.UI.Inventory;
using UnityEngine;

namespace ProjectTwelve.Tests.UI
{
    /// <summary>
    /// Behavioural tests for the grid view: a single-slot model event refreshes only that slot, and a
    /// rejected drop is reported (and the affected slots re-read from the model, restoring their visuals).
    /// </summary>
    public sealed class InventoryGridViewTests
    {
        private readonly List<GameObject> spawned = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject go in spawned)
            {
                if (go != null)
                {
                    Object.DestroyImmediate(go);
                }
            }

            spawned.Clear();
        }

        private InventoryGridView NewGrid(out FakeInventoryViewModel model, out FakeInventoryCommandHandler handler,
            int slotCount = 10)
        {
            GameObject go = new GameObject("Grid", typeof(RectTransform));
            spawned.Add(go);
            InventoryGridView grid = go.AddComponent<InventoryGridView>();
            model = new FakeInventoryViewModel(slotCount);
            for (int i = 0; i < slotCount; i++)
            {
                model.SetSlot(i, new InventorySlotViewData($"item:{i}", null, i + 1, 99));
            }

            handler = new FakeInventoryCommandHandler();
            grid.Initialize(model, handler, null, null,
                new InventoryGridConfig { columns = 5, slotSize = 48f, spacing = 4f });
            return grid;
        }

        [Test]
        public void SlotChanged_RefreshesOnlyTheAffectedSlot()
        {
            InventoryGridView grid = NewGrid(out FakeInventoryViewModel model, out _);

            // Capture the data all slots currently show.
            InventorySlotViewData[] before = new InventorySlotViewData[grid.ActiveSlotCount];
            for (int i = 0; i < before.Length; i++)
            {
                before[i] = grid.GetSlotView(i).Data;
            }

            // Change only slot 3 in the model and raise the single-slot event.
            model.SetSlot(3, new InventorySlotViewData("item:changed", null, 42, 99));
            model.RaiseSlotChanged(3);

            Assert.AreEqual("item:changed", grid.GetSlotView(3).Data.ItemId, "Slot 3 must refresh.");
            Assert.AreEqual(42, grid.GetSlotView(3).Data.Count);

            for (int i = 0; i < before.Length; i++)
            {
                if (i == 3)
                {
                    continue;
                }

                Assert.AreEqual(before[i].ItemId, grid.GetSlotView(i).Data.ItemId,
                    $"Slot {i} must be untouched by a slot-3 change.");
            }
        }

        [Test]
        public void HandleDrop_RejectedResult_IsReportedAndSlotsRestored()
        {
            InventoryGridView grid = NewGrid(out FakeInventoryViewModel model, out FakeInventoryCommandHandler handler);
            handler.NextResult = InventoryOperationResult.Rejected;

            InventoryOperationResult result = grid.HandleDrop(1, 4);

            Assert.AreEqual(InventoryOperationResult.Rejected, result);
            Assert.AreEqual(1, handler.Moves.Count);
            Assert.AreEqual((1, 4, int.MaxValue), handler.Moves[0]);
            // On rejection the slots are re-read from the (unchanged) model — visuals restored.
            Assert.AreEqual(model.GetSlot(1).ItemId, grid.GetSlotView(1).Data.ItemId);
            Assert.AreEqual(model.GetSlot(4).ItemId, grid.GetSlotView(4).Data.ItemId);
        }

        [Test]
        public void HandleDrop_SuccessResult_IsReported()
        {
            InventoryGridView grid = NewGrid(out _, out FakeInventoryCommandHandler handler);
            handler.NextResult = InventoryOperationResult.Success;

            Assert.AreEqual(InventoryOperationResult.Success, grid.HandleDrop(0, 2));
        }

        [Test]
        public void SetSelected_MarksSelectionOnTheChosenSlotOnly()
        {
            InventoryGridView grid = NewGrid(out _, out _);
            grid.SetSelected(2);

            Assert.IsTrue(grid.GetSlotView(2).Data.IsSelected);
            Assert.IsFalse(grid.GetSlotView(0).Data.IsSelected);

            grid.SetSelected(5);
            Assert.IsFalse(grid.GetSlotView(2).Data.IsSelected, "Previous selection cleared.");
            Assert.IsTrue(grid.GetSlotView(5).Data.IsSelected);
        }
    }
}
