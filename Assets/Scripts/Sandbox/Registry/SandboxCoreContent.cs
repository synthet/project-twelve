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
        /// the string ID of legacy tile ID N (air=0, dirt=1, grass=2, stone=3, then the four
        /// brick variants that were originally misnamed "ores", followed by the additional
        /// licensed ground materials). Existing numeric slots are fixed
        /// forever; the strings track renames of the same tile (see
        /// <see cref="RenamedTileIdAliases"/>). Version-1 saves load through this table. Note
        /// <see cref="SandboxTileIds"/> no longer carries this numbering — its fields are
        /// registry runtime indices now.
        /// </summary>
        public static readonly IReadOnlyList<string> LegacyTileIdToStringId = new[]
        {
            AirTileId,
            "core:dirt",
            "core:grass",
            "core:stone",
            "core:bricks_a",
            "core:bricks_b",
            "core:bricks_c",
            "core:bricks_d",
            "core:frozen",
            "core:magma",
            "core:sand",
        };

        /// <summary>
        /// Retired string IDs → canonical replacements, registered as registry aliases so
        /// palette saves and inventories written before the rename keep loading. The four
        /// "ore" tiles never had ore art — they always rendered with the vendor BricksA–D
        /// tilesets — so they were renamed to match the vendor art (the source of truth).
        /// </summary>
        public static readonly IReadOnlyDictionary<string, string> RenamedTileIdAliases =
            new Dictionary<string, string>
            {
                ["core:copper_ore"] = "core:bricks_a",
                ["core:iron_ore"] = "core:bricks_b",
                ["core:silver_ore"] = "core:bricks_c",
                ["core:gold_ore"] = "core:bricks_d",
            };

        /// <summary>Builds and freezes the core tile registry (11 defs, air pinned to index 0).</summary>
        public static ContentRegistry<TileDefinition> CreateTileRegistry()
        {
            ContentRegistry<TileDefinition> tiles = new ContentRegistry<TileDefinition>(AirTileId);
            tiles.Register(new TileDefinition(AirTileId, solid: false, opaque: false, hardness: 0f));
            tiles.Register(new TileDefinition("core:dirt", solid: true, opaque: true, lightAttenuation: 3, atlasSprite: "Humus", dropItemId: "core:dirt", hardness: 1f));
            tiles.Register(new TileDefinition("core:grass", solid: true, opaque: true, lightAttenuation: 3, atlasSprite: "Humus", dropItemId: "core:dirt", hardness: 1f));
            tiles.Register(new TileDefinition("core:stone", solid: true, opaque: true, lightAttenuation: 3, atlasSprite: "Rocks", dropItemId: "core:stone", hardness: 2f));
            tiles.Register(new TileDefinition("core:bricks_a", solid: true, opaque: true, lightAttenuation: 3, atlasSprite: "BricksA", dropItemId: "core:bricks_a", hardness: 2f));
            tiles.Register(new TileDefinition("core:bricks_b", solid: true, opaque: true, lightAttenuation: 3, atlasSprite: "BricksB", dropItemId: "core:bricks_b", hardness: 2f));
            tiles.Register(new TileDefinition("core:bricks_c", solid: true, opaque: true, lightAttenuation: 3, atlasSprite: "BricksC", dropItemId: "core:bricks_c", hardness: 2f));
            tiles.Register(new TileDefinition("core:bricks_d", solid: true, opaque: true, lightAttenuation: 3, atlasSprite: "BricksD", dropItemId: "core:bricks_d", hardness: 2f));
            tiles.Register(new TileDefinition("core:frozen", solid: true, opaque: true, lightAttenuation: 3, atlasSprite: "Frozen", dropItemId: "core:frozen", hardness: 2f));
            tiles.Register(new TileDefinition("core:magma", solid: true, opaque: true, lightEmission: 12, lightAttenuation: 3, atlasSprite: "Magma", dropItemId: "core:magma", hardness: 2f));
            tiles.Register(new TileDefinition("core:sand", solid: true, opaque: true, lightAttenuation: 3, atlasSprite: "Sand", dropItemId: "core:sand", hardness: 1f));
            RegisterRenamedAliases(tiles);
            tiles.Freeze();
            return tiles;
        }

        /// <summary>Builds and freezes the core item registry (one placeable item per solid tile).</summary>
        public static ContentRegistry<ItemDefinition> CreateItemRegistry()
        {
            ContentRegistry<ItemDefinition> items = new ContentRegistry<ItemDefinition>();
            items.Register(new ItemDefinition("core:dirt", placesTileId: "core:dirt"));
            items.Register(new ItemDefinition("core:grass", placesTileId: "core:grass"));
            items.Register(new ItemDefinition("core:stone", placesTileId: "core:stone"));
            items.Register(new ItemDefinition("core:bricks_a", placesTileId: "core:bricks_a"));
            items.Register(new ItemDefinition("core:bricks_b", placesTileId: "core:bricks_b"));
            items.Register(new ItemDefinition("core:bricks_c", placesTileId: "core:bricks_c"));
            items.Register(new ItemDefinition("core:bricks_d", placesTileId: "core:bricks_d"));
            items.Register(new ItemDefinition("core:frozen", placesTileId: "core:frozen"));
            items.Register(new ItemDefinition("core:magma", placesTileId: "core:magma"));
            items.Register(new ItemDefinition("core:sand", placesTileId: "core:sand"));
            RegisterRenamedAliases(items);
            items.Freeze();
            return items;
        }

        /// <summary>
        /// Registers the retired "ore" string IDs as aliases so pre-rename palette saves and
        /// inventories keep resolving. Tile and item IDs share the same rename history.
        /// </summary>
        private static void RegisterRenamedAliases<TDef>(ContentRegistry<TDef> registry)
            where TDef : class, IContentDefinition
        {
            foreach (KeyValuePair<string, string> alias in RenamedTileIdAliases)
            {
                registry.RegisterAlias(alias.Key, alias.Value);
            }
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

                if (tile.LightAttenuation < 1 || tile.LightAttenuation > 15)
                {
                    throw new InvalidOperationException(
                        $"Tile '{tile.Id}' light attenuation {tile.LightAttenuation} must be in the range 1-15.");
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
