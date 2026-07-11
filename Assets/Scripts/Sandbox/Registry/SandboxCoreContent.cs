using System;
using System.Collections.Generic;

namespace ProjectTwelve.Sandbox.Registry
{
    /// <summary>
    /// First-party (<c>core:</c>) content bootstrap: builds and freezes the tile, item, and
    /// entity registries and validates cross-registry references before play. Also carries the
    /// legacy numeric tile ID ↔ string ID migration table for the P1 prototype and its saves.
    /// See docs/wiki/12-modding.md § "Prototype migration (legacy numeric IDs)".
    /// </summary>
    public static class SandboxCoreContent
    {
        public const string AirTileId = "core:air";
        public const string DirtTileId = "core:dirt";
        public const string GrassTileId = "core:grass";
        public const string StoneTileId = "core:stone";

        /// <summary>
        /// Legacy P1-prototype numeric tile ID → stable string ID, index-aligned: element N is
        /// the string ID of legacy tile ID N (air=0, dirt=1, grass=2, stone=3, then the ores).
        /// Fixed forever; version-1 saves load through it. Note <see cref="SandboxTileIds"/> no
        /// longer carries this numbering — its fields are registry runtime indices now.
        /// </summary>
        public static readonly IReadOnlyList<string> LegacyTileIdToStringId = new[]
        {
            AirTileId,
            "core:dirt",
            "core:grass",
            "core:stone",
            "core:copper_ore",
            "core:iron_ore",
            "core:silver_ore",
            "core:gold_ore",
        };

        /// <summary>Builds and freezes the core tile registry (8 defs, air pinned to index 0).</summary>
        public static ContentRegistry<TileDefinition> CreateTileRegistry()
        {
            ContentRegistry<TileDefinition> tiles = new ContentRegistry<TileDefinition>(AirTileId);
            tiles.Register(new TileDefinition(AirTileId, solid: false, opaque: false, hardness: 0f));
            tiles.Register(new TileDefinition("core:dirt", solid: true, opaque: true, atlasSprite: "Humus", dropItemId: "core:dirt", hardness: 1f));
            tiles.Register(new TileDefinition("core:grass", solid: true, opaque: true, atlasSprite: "Humus", dropItemId: "core:dirt", hardness: 1f));
            tiles.Register(new TileDefinition("core:stone", solid: true, opaque: true, atlasSprite: "Rocks", dropItemId: "core:stone", hardness: 2f));
            tiles.Register(new TileDefinition("core:copper_ore", solid: true, opaque: true, atlasSprite: "BricksA", dropItemId: "core:copper_ore", hardness: 2f));
            tiles.Register(new TileDefinition("core:iron_ore", solid: true, opaque: true, atlasSprite: "BricksB", dropItemId: "core:iron_ore", hardness: 2f));
            tiles.Register(new TileDefinition("core:silver_ore", solid: true, opaque: true, atlasSprite: "BricksC", dropItemId: "core:silver_ore", hardness: 2f));
            tiles.Register(new TileDefinition("core:gold_ore", solid: true, opaque: true, atlasSprite: "BricksD", dropItemId: "core:gold_ore", hardness: 2f));
            tiles.Freeze();
            return tiles;
        }

        /// <summary>Builds and freezes the core item registry (one placeable item per solid tile).</summary>
        public static ContentRegistry<ItemDefinition> CreateItemRegistry()
        {
            ContentRegistry<ItemDefinition> items = new ContentRegistry<ItemDefinition>();
            items.Register(new ItemDefinition("core:dirt", placesTileId: "core:dirt"));
            items.Register(new ItemDefinition("core:stone", placesTileId: "core:stone"));
            items.Register(new ItemDefinition("core:copper_ore", placesTileId: "core:copper_ore"));
            items.Register(new ItemDefinition("core:iron_ore", placesTileId: "core:iron_ore"));
            items.Register(new ItemDefinition("core:silver_ore", placesTileId: "core:silver_ore"));
            items.Register(new ItemDefinition("core:gold_ore", placesTileId: "core:gold_ore"));
            items.Freeze();
            return items;
        }

        /// <summary>Builds and freezes the core entity registry (player only for now).</summary>
        public static ContentRegistry<EntityDefinition> CreateEntityRegistry()
        {
            ContentRegistry<EntityDefinition> entities = new ContentRegistry<EntityDefinition>();
            entities.Register(new EntityDefinition("core:player", maxHealth: 100));
            entities.Freeze();
            return entities;
        }

        /// <summary>
        /// Validates cross-registry references after freeze, before entering play: every non-air
        /// tile must carry a visual key, declared drops must resolve to registered items, declared
        /// item placements must resolve to registered tiles, and light emission stays in 0–15.
        /// Throws on the first unresolved reference instead of failing silently at runtime.
        /// </summary>
        public static void ValidateTileReferences(
            ContentRegistry<TileDefinition> tiles,
            ContentRegistry<ItemDefinition> items)
        {
            if (tiles == null)
            {
                throw new ArgumentNullException(nameof(tiles));
            }

            if (items == null)
            {
                throw new ArgumentNullException(nameof(items));
            }

            IReadOnlyList<TileDefinition> allTiles = tiles.All;
            for (int i = 0; i < allTiles.Count; i++)
            {
                TileDefinition tile = allTiles[i];
                if (tile.Id != AirTileId && string.IsNullOrEmpty(tile.AtlasSprite))
                {
                    throw new InvalidOperationException($"Tile '{tile.Id}' has no visual key (AtlasSprite).");
                }

                if (tile.DropItemId != null && !items.TryGet(tile.DropItemId, out _))
                {
                    throw new InvalidOperationException($"Tile '{tile.Id}' drops unknown item '{tile.DropItemId}'.");
                }

                if (tile.LightEmission > 15)
                {
                    throw new InvalidOperationException($"Tile '{tile.Id}' light emission {tile.LightEmission} exceeds 15.");
                }
            }

            IReadOnlyList<ItemDefinition> allItems = items.All;
            for (int i = 0; i < allItems.Count; i++)
            {
                ItemDefinition item = allItems[i];
                if (item.PlacesTileId != null && !tiles.TryGet(item.PlacesTileId, out _))
                {
                    throw new InvalidOperationException($"Item '{item.Id}' places unknown tile '{item.PlacesTileId}'.");
                }
            }
        }
    }
}
