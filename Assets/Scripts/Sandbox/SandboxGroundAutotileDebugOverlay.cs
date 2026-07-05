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
    private const string DefaultBaselineName = "sandbox-scene-mountain";

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
        Func<int, int, SandboxTile> tileLookup)
    {
        EnsureComponents();

        if (mode == GroundAutotileDebugMode.Off
            || visualCatalog == null
            || !visualCatalog.HasAutotileSources
            || tileLookup == null)
        {
            ClearMesh();
            meshRenderer.enabled = false;
            return;
        }

        IReadOnlyDictionary<Vector2Int, BaselineCell> baseline =
            mode == GroundAutotileDebugMode.MismatchBaseline
                ? AutotileBaselineStore.TryLoad(DefaultBaselineName)
                : null;

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
                        out AutotileGroundResolveResult resolve))
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
                    baseline);
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
        IReadOnlyDictionary<Vector2Int, BaselineCell> baseline)
    {
        if (mode == GroundAutotileDebugMode.MismatchBaseline)
        {
            if (baseline == null
                || !baseline.TryGetValue(worldCoord, out BaselineCell expected)
                || AutotileBaselineCompare.GroundMatches(
                    AutotileBaselineCompare.ToLegacyTileId(tile.id),
                    resolve,
                    expected))
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
                AutotileDebugPalette.MismatchColor);
            return;
        }

        if (mode == GroundAutotileDebugMode.CoverSpriteIdLabel)
        {
            if (!TryResolveCover(visualCatalog, tileLookup, tile, worldCoord, out CoverResolveResult cover))
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
                AutotileDebugPalette.ColorForCoverSpriteId(cover.SpriteId));

            AutotileDebugMeshBuilder.AppendSpriteIdLabel(
                vertices,
                triangles,
                uvs,
                colors,
                localX,
                localY,
                tileSize,
                cover.SpriteId,
                cover.FlipX);
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
            }

            return;
        }

        Color markerColor = mode == GroundAutotileDebugMode.ColorByTileId
            ? AutotileDebugPalette.ColorForTileId(tile.id)
            : AutotileDebugPalette.ColorForSpriteId(resolve.SpriteId);

        AutotileDebugMeshBuilder.AppendTileMarker(
            vertices,
            triangles,
            uvs,
            colors,
            localX,
            localY,
            tileSize,
            markerColor);

        if (mode == GroundAutotileDebugMode.SpriteIdLabel && resolve.Resolved)
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
