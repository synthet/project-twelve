using NUnit.Framework;
using ProjectTwelve.UI.Inventory;

namespace ProjectTwelve.Tests.UI
{
    /// <summary>
    /// Pure grid geometry and focus-navigation rules (no scene needed). Deterministic navigation is a hard
    /// requirement for controller support in inventory grids.
    /// </summary>
    public sealed class InventoryGridLogicTests
    {
        [Test]
        public void IndexMapping_RoundTripsThroughRowColumn()
        {
            const int columns = 5;
            for (int index = 0; index < 23; index++)
            {
                int row = InventoryGridView.IndexToRow(index, columns);
                int col = InventoryGridView.IndexToColumn(index, columns);
                Assert.AreEqual(index, InventoryGridView.CellToIndex(row, col, columns));
            }
        }

        [Test]
        public void Navigate_MovesWithinBoundsDeterministically()
        {
            // 5x4 grid (20 slots). Index 6 = row 1, col 1.
            Assert.AreEqual(7, InventoryGridView.Navigate(6, GridDirection.Right, 5, 20));
            Assert.AreEqual(5, InventoryGridView.Navigate(6, GridDirection.Left, 5, 20));
            Assert.AreEqual(11, InventoryGridView.Navigate(6, GridDirection.Down, 5, 20));
            Assert.AreEqual(1, InventoryGridView.Navigate(6, GridDirection.Up, 5, 20));
        }

        [Test]
        public void Navigate_StaysPutAtEdges()
        {
            // Top-left corner: up and left are no-ops.
            Assert.AreEqual(0, InventoryGridView.Navigate(0, GridDirection.Up, 5, 20));
            Assert.AreEqual(0, InventoryGridView.Navigate(0, GridDirection.Left, 5, 20));
            // Right edge of a row does not wrap to the next row.
            Assert.AreEqual(4, InventoryGridView.Navigate(4, GridDirection.Right, 5, 20));
        }

        [Test]
        public void Navigate_DownDoesNotLeaveTheGridWhenLastRowIsPartial()
        {
            // 5 columns, 13 slots -> last row is 10,11,12. Index 9 down would be 14 (out of range) -> stays.
            Assert.AreEqual(9, InventoryGridView.Navigate(9, GridDirection.Down, 5, 13));
            // Index 7 down -> 12 (valid).
            Assert.AreEqual(12, InventoryGridView.Navigate(7, GridDirection.Down, 5, 13));
        }
    }
}
