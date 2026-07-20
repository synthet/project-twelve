using System.Collections.Generic;
using NUnit.Framework;
using ProjectTwelve.Sandbox.Inventory;
using ProjectTwelve.Sandbox.Registry;
using ProjectTwelve.Sandbox.UI;
using ProjectTwelve.UI.Inventory;

namespace ProjectTwelve.Tests.UI
{
    /// <summary>
    /// The domain-side bridge: the view-model turns the inventory's coarse Changed event into per-slot
    /// SlotChanged events by diffing, and the command handler applies move/split/swap/clear through the
    /// inventory API and reports typed results without leaking rules into the UI.
    /// </summary>
    public sealed class SandboxInventoryBridgeTests
    {
        [TearDown]
        public void TearDown()
        {
            SandboxRegistries.ResetForTests();
        }

        [Test]
        public void ViewModel_CoarseChange_RaisesSlotChangedOnlyForTheChangedSlot()
        {
            SandboxInventory inventory = SandboxInventory.CreatePrototypeLoadout(SandboxRegistries.Items);
            SandboxInventoryViewModel viewModel = new SandboxInventoryViewModel(inventory);
            List<int> changed = new List<int>();
            viewModel.SlotChanged += changed.Add;

            inventory.SetSlot(3, "core:dirt", 5); // slot 3 held bricks_a in the prototype loadout.

            Assert.AreEqual(1, changed.Count, "Exactly one slot changed, so exactly one event.");
            Assert.AreEqual(3, changed[0]);
            Assert.AreEqual("core:dirt", viewModel.GetSlot(3).ItemId);
            viewModel.Dispose();
        }

        [Test]
        public void CommandHandler_MoveIntoEmptySlot_MovesTheStack()
        {
            SandboxInventory inventory = SandboxInventory.CreatePrototypeLoadout(SandboxRegistries.Items);
            SandboxInventoryCommandHandler handler = new SandboxInventoryCommandHandler(inventory);
            string movedItem = inventory.GetSlot(0).ItemId;
            int movedCount = inventory.GetSlot(0).Count;

            InventoryOperationResult result = handler.TryMove(0, 20, int.MaxValue);

            Assert.AreEqual(InventoryOperationResult.Success, result);
            Assert.IsTrue(inventory.GetSlot(0).IsEmpty);
            Assert.AreEqual(movedItem, inventory.GetSlot(20).ItemId);
            Assert.AreEqual(movedCount, inventory.GetSlot(20).Count);
        }

        [Test]
        public void CommandHandler_MoveOntoDifferentItem_SwapsWholeStacks()
        {
            SandboxInventory inventory = SandboxInventory.CreatePrototypeLoadout(SandboxRegistries.Items);
            SandboxInventoryCommandHandler handler = new SandboxInventoryCommandHandler(inventory);
            string a = inventory.GetSlot(0).ItemId;
            string b = inventory.GetSlot(1).ItemId;

            Assert.AreEqual(InventoryOperationResult.Success, handler.TryMove(0, 1, int.MaxValue));
            Assert.AreEqual(b, inventory.GetSlot(0).ItemId);
            Assert.AreEqual(a, inventory.GetSlot(1).ItemId);
        }

        [Test]
        public void CommandHandler_SplitStack_IntoEmptySlot()
        {
            SandboxInventory inventory = SandboxInventory.CreatePrototypeLoadout(SandboxRegistries.Items);
            SandboxInventoryCommandHandler handler = new SandboxInventoryCommandHandler(inventory);
            string item = inventory.GetSlot(0).ItemId;
            int total = inventory.GetSlot(0).Count;

            Assert.AreEqual(InventoryOperationResult.Success, handler.TrySplit(0, 21, 10));
            Assert.AreEqual(total - 10, inventory.GetSlot(0).Count);
            Assert.AreEqual(item, inventory.GetSlot(21).ItemId);
            Assert.AreEqual(10, inventory.GetSlot(21).Count);
        }

        [Test]
        public void CommandHandler_MoveFromEmptySlot_IsRejectedAndLeavesInventoryUnchanged()
        {
            SandboxInventory inventory = SandboxInventory.CreatePrototypeLoadout(SandboxRegistries.Items);
            SandboxInventoryCommandHandler handler = new SandboxInventoryCommandHandler(inventory);
            string destItemBefore = inventory.GetSlot(0).ItemId;

            InventoryOperationResult result = handler.TryMove(25, 0, int.MaxValue); // slot 25 is empty.

            Assert.AreEqual(InventoryOperationResult.Rejected, result);
            Assert.AreEqual(destItemBefore, inventory.GetSlot(0).ItemId, "Destination is untouched on rejection.");
            Assert.IsTrue(inventory.GetSlot(25).IsEmpty);
        }

        [Test]
        public void CommandHandler_Clear_EmptiesTheSlot()
        {
            SandboxInventory inventory = SandboxInventory.CreatePrototypeLoadout(SandboxRegistries.Items);
            SandboxInventoryCommandHandler handler = new SandboxInventoryCommandHandler(inventory);

            Assert.AreEqual(InventoryOperationResult.Success, handler.TryClear(0));
            Assert.IsTrue(inventory.GetSlot(0).IsEmpty);
        }

        [Test]
        public void CommandHandler_InvalidIndex_ReturnsInvalidSlot()
        {
            SandboxInventory inventory = SandboxInventory.CreatePrototypeLoadout(SandboxRegistries.Items);
            SandboxInventoryCommandHandler handler = new SandboxInventoryCommandHandler(inventory);

            Assert.AreEqual(InventoryOperationResult.InvalidSlot, handler.TryMove(-1, 0, 1));
            Assert.AreEqual(InventoryOperationResult.InvalidSlot, handler.TryMove(0, 9999, 1));
        }
    }
}
