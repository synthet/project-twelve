using System;
using UnityEngine;

namespace ProjectTwelve.Visual.Tiles
{
    /// <summary>
    /// Ground autotile mask and sprite resolution for a single world tile.
    /// </summary>
    public readonly struct AutotileGroundResolveResult
    {
        public bool HasGroundTileset { get; }
        public bool Resolved { get; }
        public string SpriteId { get; }
        public bool FlipX { get; }
        public string TilesetName { get; }
        public int[,] Mask { get; }

        public AutotileGroundResolveResult(
            bool hasGroundTileset,
            bool resolved,
            string spriteId,
            bool flipX,
            string tilesetName,
            int[,] mask)
        {
            HasGroundTileset = hasGroundTileset;
            Resolved = resolved;
            SpriteId = spriteId;
            FlipX = flipX;
            TilesetName = tilesetName;
            Mask = mask;
        }
    }

    /// <summary>
    /// Shared ground autotile mask build and sprite resolve (chunk mesh, MCP, debug overlay).
    /// </summary>
    public static class AutotileGroundResolve
    {
        /// <summary>
        /// Builds the ground mask and resolves the sprite for a solid tile at world coordinates.
        /// </summary>
        public static bool TryResolve(
            SandboxTileVisualCatalog catalog,
            Func<int, int, SandboxTile> tileLookup,
            SandboxTile tile,
            int worldX,
            int worldY,
            out AutotileGroundResolveResult result)
        {
            result = default;
            if (catalog == null || tileLookup == null || !tile.IsSolid)
            {
                return false;
            }

            if (!catalog.TryGetGroundTileset(tile.id, out AutotileTileset tileset))
            {
                result = new AutotileGroundResolveResult(false, false, null, false, null, null);
                return false;
            }

            int[,] mask = BuildGroundMask(catalog, tileLookup, tile, worldX, worldY);
            Sprite sprite = AutotileResolver.ResolveSprite(tileset, mask, out bool flipX);
            result = new AutotileGroundResolveResult(
                hasGroundTileset: true,
                resolved: sprite != null,
                spriteId: sprite != null ? sprite.name : null,
                flipX: flipX,
                tilesetName: tileset.Name,
                mask: mask);
            return true;
        }

        /// <summary>
        /// Builds the ground neighbor mask using the same predicates as chunk rendering.
        /// </summary>
        public static int[,] BuildGroundMask(
            SandboxTileVisualCatalog catalog,
            Func<int, int, SandboxTile> tileLookup,
            SandboxTile tile,
            int worldX,
            int worldY)
        {
            return AutotileMaskBuilder.BuildGroundMask(
                (x, y) =>
                {
                    SandboxTile neighbor = tileLookup(x, y);
                    return catalog.SharesGroundAutotileGroup(tile.id, neighbor.id);
                },
                (x, y) => tileLookup(x, y).IsSolid,
                worldX,
                worldY,
                (x, y) =>
                {
                    SandboxTile t = tileLookup(x, y);
                    return t.id == SandboxTileIds.Grass && !tileLookup(x, y + 1).IsSolid;
                });
        }
    }
}
