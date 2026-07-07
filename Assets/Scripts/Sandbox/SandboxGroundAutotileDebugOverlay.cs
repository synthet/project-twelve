using System;
using System.Collections.Generic;
using ProjectTwelve.Sandbox.Registry;
using ProjectTwelve.Visual.AutotileDebug;
using ProjectTwelve.Visual.Tiles;
using UnityEngine;

/// <summary>
/// Per-chunk Play Mode overlay showing ground autotile debug markers.
/// </summary>
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public sealed class SandboxGroundAutotileDebugOverlay : MonoBehaviour
{
    private const int SortingOrder = 40;

    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private Material overlayMaterial;

    /// <summary>
    /// Rebuilds the debug overlay mesh for the chunk, or clears it when mode is off.
    /// </summary>
    public void Rebuild(
        SandboxChunk chunk,
        float tileSize,
        GroundAutotileDebugMode mode,
        SandboxTileVisualCatalog visualCatalog,
        Func<int, int, SandboxTile> tileLookup,
        AutotileVisualOverrideMap visualOverrides = null,
        int autotileExposureFloorY = AutotileExposure.NoFloor)
    {
        EnsureComponents();
        mode = GroundAutotileDebugModes.Normalize(mode);

        if (mode == GroundAutotileDebugMode.Off
            || mode == GroundAutotileDebugMode.VisualOverrideEdit
            || visualCatalog == null
            || !visualCatalog.HasAutotileSources
            || tileLookup == null)
        {
            ClearMesh();
            meshRenderer.enabled = false;
            return;
        }

        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector2> uvs = new List<Vector2>();
        List<Color> colors = new List<Color>();

        for (int localX = 0; localX < SandboxChunk.Size; localX++)
        {
            for (int localY = 0; localY < SandboxChunk.Size; localY++)
            {
                SandboxTile tile = chunk.GetLocalTile(localX, localY);
                if (!tile.IsSolid)
                {
                    continue;
                }

                Vector2Int worldCoord = SandboxWorld.ChunkLocalToWorld(chunk.Coord, localX, localY);
                if (!AutotileGroundResolve.TryResolve(
                        visualCatalog,
                        tileLookup,
                        tile,
                        worldCoord.x,
                        worldCoord.y,
                        out AutotileGroundResolveResult resolve,
                        autotileExposureFloorY))
                {
                    continue;
                }

                AppendCellDebug(
                    vertices,
                    triangles,
                    uvs,
                    colors,
                    mode,
                    visualCatalog,
                    tileLookup,
                    tile,
                    localX,
                    localY,
                    worldCoord,
                    tileSize,
                    resolve,
                    visualOverrides);
            }
        }

        if (vertices.Count == 0)
        {
            ClearMesh();
            meshRenderer.enabled = false;
            return;
        }

        Mesh mesh = meshFilter.sharedMesh;
        if (mesh == null)
        {
            mesh = new Mesh { name = $"GroundAutotileDebug_{chunk.Coord.x}_{chunk.Coord.y}" };
            meshFilter.sharedMesh = mesh;
        }

        mesh.Clear();
        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.SetUVs(0, uvs);
        mesh.SetColors(colors);
        mesh.RecalculateBounds();

        meshRenderer.sharedMaterial = GetOverlayMaterial();
        meshRenderer.sortingOrder = SortingOrder;
        meshRenderer.enabled = true;
    }

    private static void AppendCellDebug(
        List<Vector3> vertices,
        List<int> triangles,
        List<Vector2> uvs,
        List<Color> colors,
        GroundAutotileDebugMode mode,
        SandboxTileVisualCatalog visualCatalog,
        Func<int, int, SandboxTile> tileLookup,
        SandboxTile tile,
        int localX,
        int localY,
        Vector2Int worldCoord,
        float tileSize,
        AutotileGroundResolveResult resolve,
        AutotileVisualOverrideMap visualOverrides)
    {
        if (mode == GroundAutotileDebugMode.VisualOverrideLabel)
        {
            AppendVisualOverrideLabels(
                vertices,
                triangles,
                uvs,
                colors,
                visualCatalog,
                tileLookup,
                tile,
                localX,
                localY,
                worldCoord,
                tileSize,
                resolve,
                visualOverrides);
            return;
        }

        if (mode == GroundAutotileDebugMode.GroundCoverSplit)
        {
            Color groundColor = AutotileDebugPalette.ColorForSpriteId(resolve.SpriteId);
            AutotileDebugMeshBuilder.AppendTileMarker(
                vertices,
                triangles,
                uvs,
                colors,
                localX,
                localY,
                tileSize,
                groundColor);

            if (resolve.Resolved)
            {
                AutotileDebugMeshBuilder.AppendSpriteIdLabel(
                    vertices,
                    triangles,
                    uvs,
                    colors,
                    localX,
                    localY,
                    tileSize,
                    resolve.SpriteId,
                    resolve.FlipX,
                    AutotileDebugPalette.LabelColor,
                    verticalOffsetTiles: -0.18f);
            }

            if (TryResolveCover(visualCatalog, tileLookup, tile, worldCoord, out CoverResolveResult cover))
            {
                AutotileDebugMeshBuilder.AppendHalfTileMarker(
                    vertices,
                    triangles,
                    uvs,
                    colors,
                    localX,
                    localY,
                    tileSize,
                    AutotileDebugPalette.ColorForCoverSpriteId(cover.SpriteId),
                    topHalf: true);

                AutotileDebugMeshBuilder.AppendSpriteIdLabel(
                    vertices,
                    triangles,
                    uvs,
                    colors,
                    localX,
                    localY,
                    tileSize,
                    cover.SpriteId,
                    cover.FlipX,
                    AutotileDebugPalette.LabelColor,
                    verticalOffsetTiles: 0.18f);
            }

            return;
        }

        if (mode != GroundAutotileDebugMode.SpriteIdLabel)
        {
            return;
        }

        AutotileDebugMeshBuilder.AppendTileMarker(
            vertices,
            triangles,
            uvs,
            colors,
            localX,
            localY,
            tileSize,
            AutotileDebugPalette.ColorForSpriteId(resolve.SpriteId));

        if (resolve.Resolved)
        {
            AutotileDebugMeshBuilder.AppendSpriteIdLabel(
                vertices,
                triangles,
                uvs,
                colors,
                localX,
                localY,
                tileSize,
                resolve.SpriteId,
                resolve.FlipX);
        }
    }


    private static void AppendVisualOverrideLabels(
        List<Vector3> vertices,
        List<int> triangles,
        List<Vector2> uvs,
        List<Color> colors,
        SandboxTileVisualCatalog visualCatalog,
        Func<int, int, SandboxTile> tileLookup,
        SandboxTile tile,
        int localX,
        int localY,
        Vector2Int worldCoord,
        float tileSize,
        AutotileGroundResolveResult ground,
        AutotileVisualOverrideMap visualOverrides)
    {
        if (visualOverrides == null || !visualOverrides.HasOverrides)
        {
            return;
        }

        if (visualCatalog.TryGetGroundTileset(tile.id, out AutotileTileset groundTileset)
            && visualOverrides.TryGetOverride(
                worldCoord,
                AutotileVisualLayer.Ground,
                groundTileset.Name,
                out AutotileVisualOverride groundOverride))
        {
            VisualOverrideResult groundDecision = VisualOverrideDecision.Apply(
                ground.SpriteId,
                ground.FlipX,
                groundOverride,
                groundTileset);
            Color color = GetOverrideLabelColor(groundTileset, groundDecision);
            AutotileDebugMeshBuilder.AppendSpriteIdLabel(
                vertices,
                triangles,
                uvs,
                colors,
                localX,
                localY,
                tileSize,
                groundDecision.SpriteId,
                groundDecision.FlipX,
                color,
                verticalOffsetTiles: -0.18f);
        }

        bool coverRendered = TryResolveCover(visualCatalog, tileLookup, tile, worldCoord, out CoverResolveResult cover);
        if (coverRendered
            && visualCatalog.TryGetCoverTileset(tile.id, out AutotileTileset coverTileset)
            && visualOverrides.TryGetOverride(
                worldCoord,
                AutotileVisualLayer.Cover,
                coverTileset.Name,
                out AutotileVisualOverride coverOverride))
        {
            VisualOverrideResult coverDecision = VisualOverrideDecision.Apply(
                cover.SpriteId,
                cover.FlipX,
                coverOverride,
                coverTileset);
            Color color = GetOverrideLabelColor(coverTileset, coverDecision);
            AutotileDebugMeshBuilder.AppendSpriteIdLabel(
                vertices,
                triangles,
                uvs,
                colors,
                localX,
                localY,
                tileSize,
                coverDecision.SpriteId,
                coverDecision.FlipX,
                color,
                verticalOffsetTiles: 0.18f);
        }
    }

    private static Color GetOverrideLabelColor(AutotileTileset tileset, VisualOverrideResult decision)
    {
        if (!TilesetHasSprite(tileset, decision.SpriteId))
        {
            return AutotileDebugPalette.MissingOverrideSpriteColor;
        }

        if (!decision.OverrideApplied)
        {
            return AutotileDebugPalette.AutoSnapshotMismatchColor;
        }

        return AutotileDebugPalette.ValidOverrideColor;
    }

    private static bool TilesetHasSprite(AutotileTileset tileset, string spriteId)
    {
        if (tileset?.Sprites == null || string.IsNullOrEmpty(spriteId))
        {
            return false;
        }

        for (int i = 0; i < tileset.Sprites.Count; i++)
        {
            Sprite sprite = tileset.Sprites[i];
            if (sprite != null && sprite.name == spriteId)
            {
                return true;
            }
        }

        return false;
    }

    private static bool TryResolveCover(
        SandboxTileVisualCatalog visualCatalog,
        Func<int, int, SandboxTile> tileLookup,
        SandboxTile tile,
        Vector2Int worldCoord,
        out CoverResolveResult result)
    {
        result = default;
        SandboxTile tileAbove = tileLookup(worldCoord.x, worldCoord.y + 1);
        if (!visualCatalog.ShouldRenderGrassCover(tile.id, tileAbove)
            || !visualCatalog.TryGetCoverTileset(tile.id, out AutotileTileset tileset))
        {
            return false;
        }

        int[,] mask = AutotileMaskBuilder.BuildCoverMask(
            (x, y) =>
            {
                SandboxTile neighbor = tileLookup(x, y);
                return visualCatalog.SharesCoverAutotileGroup(tile.id, neighbor.id);
            },
            (x, y) => tileLookup(x, y).IsSolid,
            worldCoord.x,
            worldCoord.y);

        Sprite sprite = AutotileResolver.ResolveSprite(tileset, mask, out bool flipX);
        if (sprite == null)
        {
            return false;
        }

        result = new CoverResolveResult(sprite.name, flipX);
        return true;
    }

    private readonly struct CoverResolveResult
    {
        public CoverResolveResult(string spriteId, bool flipX)
        {
            SpriteId = spriteId;
            FlipX = flipX;
        }

        public string SpriteId { get; }
        public bool FlipX { get; }
    }

    private void EnsureComponents()
    {
        if (meshFilter == null)
        {
            meshFilter = GetComponent<MeshFilter>();
        }

        if (meshRenderer == null)
        {
            meshRenderer = GetComponent<MeshRenderer>();
        }
    }

    private void ClearMesh()
    {
        if (meshFilter != null && meshFilter.sharedMesh != null)
        {
            meshFilter.sharedMesh.Clear();
        }
    }

    private Material GetOverlayMaterial()
    {
        if (overlayMaterial != null)
        {
            overlayMaterial.mainTexture = AutotileDebugMeshBuilder.GetDigitAtlas();
            return overlayMaterial;
        }

        Shader shader = Shader.Find("Sprites/Default") ?? Shader.Find("Unlit/Texture");
        overlayMaterial = new Material(shader != null ? shader : Shader.Find("Unlit/Color"))
        {
            mainTexture = AutotileDebugMeshBuilder.GetDigitAtlas(),
        };
        return overlayMaterial;
    }
}

/// <summary>
/// Top-left hover HUD with world/chunk/local tile coordinates
/// while any F3 ground autotile debug overlay mode is active.
/// </summary>
[DisallowMultipleComponent]
public sealed class SandboxGroundAutotileDebugHud : MonoBehaviour
{
    [SerializeField] private SandboxWorld world;
    [SerializeField] private Camera targetCamera;

    private void Awake()
    {
        if (world == null)
        {
            world = GetComponent<SandboxWorld>();
        }

        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }
    }

    private void OnGUI()
    {
        if (world == null || !GroundAutotileDebugModes.IsOverlayActive(world.GroundAutotileDebugMode))
        {
            return;
        }

        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        if (targetCamera == null)
        {
            return;
        }

        if (!SandboxScreenPointer.TryReadWorldTile(targetCamera, world, out Vector2Int tile))
        {
            return;
        }

        string text = GroundAutotileDebugCoordinates.FormatHoverCoordinates(tile.x, tile.y);
        GUI.Label(new Rect(12f, 12f, 480f, 48f), text);
    }
}
