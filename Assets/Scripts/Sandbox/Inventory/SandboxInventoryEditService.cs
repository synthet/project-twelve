using System;
using ProjectTwelve.Sandbox.Registry;

namespace ProjectTwelve.Sandbox.Inventory
{
    public interface ISandboxInventoryWorld
    {
        int GetTileId(int x, int y);
        bool TrySetTile(int x, int y, int tileId);
    }

    public readonly struct SandboxItemStack
    {
        public SandboxItemStack(string itemId, int count)
        {
            ItemId = itemId;
            Count = count;
        }

        public string ItemId { get; }
        public int Count { get; }
        public bool IsEmpty => string.IsNullOrEmpty(ItemId) || Count <= 0;
    }

    public enum SandboxInventoryEditResult
    {
        Success,
        OutOfRange,
        PlayerOccluded,
        EmptySlot,
        NotPlaceable,
        Occupied,
        NotBreakable,
        WorldRejected
    }

    /// <summary>Pure inventory-backed place/break transaction rules.</summary>
    public sealed class SandboxInventoryEditService
    {
        private readonly ContentRegistry<TileDefinition> tiles;
        private readonly ContentRegistry<ItemDefinition> items;

        public SandboxInventoryEditService(
            ContentRegistry<TileDefinition> tiles,
            ContentRegistry<ItemDefinition> items)
        {
            this.tiles = tiles ?? throw new ArgumentNullException(nameof(tiles));
            this.items = items ?? throw new ArgumentNullException(nameof(items));
        }

        public SandboxInventoryEditResult TryPlace(
            ISandboxInventoryWorld world,
            SandboxInventory inventory,
            int slotIndex,
            int x,
            int y)
        {
            SandboxInventory.Slot slot = inventory.GetSlot(slotIndex);
            if (slot.IsEmpty)
            {
                return SandboxInventoryEditResult.EmptySlot;
            }

            ItemDefinition item = items.Get(slot.ItemId);
            if (string.IsNullOrEmpty(item.PlacesTileId)
                || !tiles.TryGetIndex(item.PlacesTileId, out int tileIndex))
            {
                return SandboxInventoryEditResult.NotPlaceable;
            }

            if (world.GetTileId(x, y) != tiles.GetIndex(SandboxCoreContent.AirTileId))
            {
                return SandboxInventoryEditResult.Occupied;
            }

            if (!world.TrySetTile(x, y, tileIndex))
            {
                return SandboxInventoryEditResult.WorldRejected;
            }

            if (!inventory.TryConsumeAt(slotIndex))
            {
                throw new InvalidOperationException("Inventory changed during a synchronous placement transaction.");
            }

            return SandboxInventoryEditResult.Success;
        }

        public SandboxInventoryEditResult TryBreak(
            ISandboxInventoryWorld world,
            int x,
            int y,
            out SandboxItemStack drop)
        {
            drop = default;
            int tileIndex = world.GetTileId(x, y);
            TileDefinition tile = tiles.Get(tileIndex);
            if (!tile.Solid || tile.Hardness <= 0f || string.IsNullOrEmpty(tile.DropItemId))
            {
                return SandboxInventoryEditResult.NotBreakable;
            }

            if (!world.TrySetTile(x, y, tiles.GetIndex(SandboxCoreContent.AirTileId)))
            {
                return SandboxInventoryEditResult.WorldRejected;
            }

            drop = new SandboxItemStack(tile.DropItemId, 1);
            return SandboxInventoryEditResult.Success;
        }

        public static bool IsWithinReach(
            float playerX,
            float playerY,
            float targetX,
            float targetY,
            float maxRange)
        {
            if (maxRange < 0f)
            {
                return false;
            }

            float dx = targetX - playerX;
            float dy = targetY - playerY;
            return dx * dx + dy * dy <= maxRange * maxRange;
        }

        public static SandboxInventoryEditResult ValidateControllerRequest(
            float playerX,
            float playerY,
            float targetX,
            float targetY,
            float maxRange,
            bool playerOccluded)
        {
            if (!IsWithinReach(playerX, playerY, targetX, targetY, maxRange))
            {
                return SandboxInventoryEditResult.OutOfRange;
            }

            return playerOccluded
                ? SandboxInventoryEditResult.PlayerOccluded
                : SandboxInventoryEditResult.Success;
        }
    }
}
