namespace ProjectTwelve.Sandbox.Registry
{
    /// <summary>
    /// Minimal pure-data item definition contract; extended by P2-INV-001 (stack rules, tools).
    /// </summary>
    public sealed class ItemDefinition : IContentDefinition
    {
        public ItemDefinition(string id, int maxStack = 999, string placesTileId = null)
        {
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
