namespace ProjectTwelve.Sandbox.Registry
{
    /// <summary>
    /// Pure-data item definition contract for stack limits and optional tile placement.
    /// </summary>
    public sealed class ItemDefinition : IContentDefinition
    {
        public ItemDefinition(
            string id,
            int maxStack = ProjectTwelve.Sandbox.Inventory.SandboxInventoryConstants.DefaultMaxStack,
            string placesTileId = null)
        {
            if (maxStack < 1)
            {
                throw new System.ArgumentOutOfRangeException(nameof(maxStack), maxStack, "Max stack must be at least 1.");
            }

            Id = id;
            MaxStack = maxStack;
            PlacesTileId = placesTileId;
        }

        public string Id { get; }

        /// <summary>Maximum quantity per inventory slot (P2-INV-001).</summary>
        public int MaxStack { get; }

        /// <summary>Tile string ID this item places, or null for non-placeable items.</summary>
        public string PlacesTileId { get; }
    }
}
