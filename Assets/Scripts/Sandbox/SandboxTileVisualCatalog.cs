using ProjectTwelve.Sandbox.Registry;
using ProjectTwelve.Visual.Tiles;
using UnityEngine;

/// <summary>
/// Maps sandbox tile IDs to project autotile ground/cover tilesets. Ground tileset names come
/// from <see cref="TileDefinition.AtlasSprite"/> in the frozen tile registry (P2-DATA-002);
/// only the grass cover overlay remains catalog-configured, as cover selection is not part of
/// the tile definition contract.
/// </summary>
[CreateAssetMenu(
    fileName = "SandboxTileVisualCatalog",
    menuName = "ProjectTwelve/Sandbox Tile Visual Catalog")]
public sealed class SandboxTileVisualCatalog : ScriptableObject
{
    [SerializeField] private AutotileCatalog autotileCatalog;

    [Header("Cover Tilesets")]
    [SerializeField] private string grassCoverTileset = "GrassA";

    public AutotileCatalog AutotileCatalog => autotileCatalog;

    /// <summary>
    /// True when an autotile catalog with at least one tileset is assigned.
    /// </summary>
    public bool HasAutotileSources =>
        autotileCatalog != null
        && (autotileCatalog.GroundTilesets.Count > 0 || autotileCatalog.CoverTilesets.Count > 0);

    /// <summary>
    /// Returns the ground tileset used to render the given registry runtime tile index.
    /// </summary>
    public bool TryGetGroundTileset(int tileId, out AutotileTileset tileset)
    {
        tileset = null;
        if (autotileCatalog == null || !TryGetGroundTilesetName(tileId, out string tilesetName))
        {
            return false;
        }

        return autotileCatalog.TryGetGroundTileset(tilesetName, out tileset);
    }

    /// <summary>
    /// Returns the cover tileset rendered on top of grass surface tiles.
    /// </summary>
    public bool TryGetCoverTileset(int tileId, out AutotileTileset tileset)
    {
        tileset = null;
        if (autotileCatalog == null
            || tileId != SandboxRegistries.GrassIndex
            || string.IsNullOrEmpty(grassCoverTileset))
        {
            return false;
        }

        return autotileCatalog.TryGetCoverTileset(grassCoverTileset, out tileset);
    }

    /// <summary>
    /// Returns whether two sandbox tile IDs share the same ground autotile group.
    /// </summary>
    public bool SharesGroundAutotileGroup(int tileIdA, int tileIdB)
    {
        if (tileIdA == SandboxRegistries.AirIndex || tileIdB == SandboxRegistries.AirIndex)
        {
            return false;
        }

        if (!TryGetGroundTilesetName(tileIdA, out string nameA)
            || !TryGetGroundTilesetName(tileIdB, out string nameB))
        {
            return false;
        }

        return nameA == nameB;
    }

    /// <summary>
    /// Returns whether a grass cover overlay should render on the given tile.
    /// </summary>
    public bool ShouldRenderGrassCover(int tileId, SandboxTile tileAbove)
    {
        return tileId == SandboxRegistries.GrassIndex && !tileAbove.IsSolid;
    }

    /// <summary>
    /// Returns whether cover visual overrides can be edited at the given cell (exposed grass surface).
    /// </summary>
    public bool CanEditCoverAt(int tileId, SandboxTile tileAbove, out AutotileTileset tileset)
    {
        tileset = null;
        return TryGetCoverTileset(tileId, out tileset) && ShouldRenderGrassCover(tileId, tileAbove);
    }

    /// <summary>
    /// Returns whether cover visual overrides can be edited at the given world coordinate.
    /// </summary>
    public bool CanEditCoverAt(System.Func<int, int, SandboxTile> tileLookup, int x, int y, out AutotileTileset tileset)
    {
        tileset = null;
        SandboxTile tile = tileLookup(x, y);
        if (!tile.IsSolid)
        {
            return false;
        }

        return CanEditCoverAt(tile.id, tileLookup(x, y + 1), out tileset);
    }

    /// <summary>
    /// Returns whether two grass tiles should connect for cover autotiling.
    /// </summary>
    public bool SharesCoverAutotileGroup(int tileIdA, int tileIdB)
    {
        return tileIdA == SandboxRegistries.GrassIndex && tileIdB == SandboxRegistries.GrassIndex;
    }

    private static bool TryGetGroundTilesetName(int tileId, out string tilesetName)
    {
        ContentRegistry<TileDefinition> tiles = SandboxRegistries.Tiles;
        if (tileId <= 0 || tileId >= tiles.Count)
        {
            tilesetName = null;
            return false;
        }

        tilesetName = tiles.Get(tileId).AtlasSprite;
        return !string.IsNullOrEmpty(tilesetName);
    }
}
