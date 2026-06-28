using ProjectTwelve.Visual.Tiles;
using UnityEngine;

/// <summary>
/// Maps sandbox tile IDs to project autotile ground/cover tilesets.
/// </summary>
[CreateAssetMenu(
    fileName = "SandboxTileVisualCatalog",
    menuName = "ProjectTwelve/Sandbox Tile Visual Catalog")]
public sealed class SandboxTileVisualCatalog : ScriptableObject
{
    [SerializeField] private AutotileCatalog autotileCatalog;

    [Header("Ground Tilesets")]
    [SerializeField] private string dirtGroundTileset = "Humus";
    [SerializeField] private string grassGroundTileset = "Humus";
    [SerializeField] private string stoneGroundTileset = "Rocks";
    [SerializeField] private string copperOreGroundTileset = "BricksA";
    [SerializeField] private string ironOreGroundTileset = "BricksB";
    [SerializeField] private string silverOreGroundTileset = "BricksC";
    [SerializeField] private string goldOreGroundTileset = "BricksD";

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
    /// Returns the ground tileset used to render the given sandbox tile ID.
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
            || tileId != SandboxTileIds.Grass
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
        if (tileIdA == SandboxTileIds.Air || tileIdB == SandboxTileIds.Air)
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
        return tileId == SandboxTileIds.Grass && !tileAbove.IsSolid;
    }

    /// <summary>
    /// Returns whether two grass tiles should connect for cover autotiling.
    /// </summary>
    public bool SharesCoverAutotileGroup(int tileIdA, int tileIdB)
    {
        return tileIdA == SandboxTileIds.Grass && tileIdB == SandboxTileIds.Grass;
    }

    private bool TryGetGroundTilesetName(int tileId, out string tilesetName)
    {
        switch (tileId)
        {
            case SandboxTileIds.Dirt:
                tilesetName = dirtGroundTileset;
                return !string.IsNullOrEmpty(tilesetName);
            case SandboxTileIds.Grass:
                tilesetName = grassGroundTileset;
                return !string.IsNullOrEmpty(tilesetName);
            case SandboxTileIds.Stone:
                tilesetName = stoneGroundTileset;
                return !string.IsNullOrEmpty(tilesetName);
            case SandboxTileIds.CopperOre:
                tilesetName = copperOreGroundTileset;
                return !string.IsNullOrEmpty(tilesetName);
            case SandboxTileIds.IronOre:
                tilesetName = ironOreGroundTileset;
                return !string.IsNullOrEmpty(tilesetName);
            case SandboxTileIds.SilverOre:
                tilesetName = silverOreGroundTileset;
                return !string.IsNullOrEmpty(tilesetName);
            case SandboxTileIds.GoldOre:
                tilesetName = goldOreGroundTileset;
                return !string.IsNullOrEmpty(tilesetName);
            default:
                tilesetName = null;
                return false;
        }
    }
}
