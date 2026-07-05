using System;
using System.Collections.Generic;
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
