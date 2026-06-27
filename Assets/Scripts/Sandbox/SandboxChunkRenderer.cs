using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public sealed class SandboxChunkRenderer : MonoBehaviour
{
    private const int TileAtlasColumns = 4;

    private static readonly Rect DirtUv = GetAtlasUv(0);
    private static readonly Rect GrassUv = GetAtlasUv(1);
    private static readonly Rect StoneUv = GetAtlasUv(2);
    private static readonly Rect CopperOreUv = GetAtlasUv(3);

    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;

    private void Awake()
    {
        EnsureComponents();
    }

    public void Rebuild(SandboxChunk chunk, float tileSize, Material material)
    {
        EnsureComponents();

        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector2> uvs = new List<Vector2>();
        List<Color> colors = new List<Color>();

        for (int x = 0; x < SandboxChunk.Size; x++)
        {
            for (int y = 0; y < SandboxChunk.Size; y++)
            {
                SandboxTile tile = chunk.GetLocalTile(x, y);
                if (!tile.IsSolid)
                {
                    continue;
                }

                AddTileQuad(vertices, triangles, uvs, colors, x, y, tileSize, GetTileUv(tile), GetTileLightColor(tile));
            }
        }

        Mesh mesh = meshFilter.sharedMesh;
        if (mesh == null)
        {
            mesh = new Mesh { name = $"ChunkMesh_{chunk.Coord.x}_{chunk.Coord.y}" };
            meshFilter.sharedMesh = mesh;
        }

        mesh.Clear();
        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.SetUVs(0, uvs);
        mesh.SetColors(colors);
        mesh.RecalculateBounds();

        meshRenderer.sharedMaterial = material != null ? material : GetDefaultMaterial();
        RebuildColliders(chunk, tileSize);
        chunk.NeedsRenderRebuild = false;
        chunk.NeedsColliderRebuild = false;
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


    private void RebuildColliders(SandboxChunk chunk, float tileSize)
    {
        BoxCollider2D[] existingColliders = GetComponents<BoxCollider2D>();
        for (int i = existingColliders.Length - 1; i >= 0; i--)
        {
            if (Application.isPlaying)
            {
                Destroy(existingColliders[i]);
            }
            else
            {
                DestroyImmediate(existingColliders[i]);
            }
        }

        for (int y = 0; y < SandboxChunk.Size; y++)
        {
            int runStart = -1;
            for (int x = 0; x <= SandboxChunk.Size; x++)
            {
                bool isSolid = x < SandboxChunk.Size && chunk.GetLocalTile(x, y).IsSolid;
                if (isSolid && runStart < 0)
                {
                    runStart = x;
                }

                if ((!isSolid || x == SandboxChunk.Size) && runStart >= 0)
                {
                    int runLength = x - runStart;
                    BoxCollider2D collider = gameObject.AddComponent<BoxCollider2D>();
                    collider.offset = new Vector2((runStart + runLength * 0.5f) * tileSize, (y + 0.5f) * tileSize);
                    collider.size = new Vector2(runLength * tileSize, tileSize);
                    runStart = -1;
                }
            }
        }
    }

    private static void AddTileQuad(
        List<Vector3> vertices,
        List<int> triangles,
        List<Vector2> uvs,
        List<Color> colors,
        int x,
        int y,
        float tileSize,
        Rect uv,
        Color color)
    {
        int start = vertices.Count;
        float left = x * tileSize;
        float right = left + tileSize;
        float bottom = y * tileSize;
        float top = bottom + tileSize;

        vertices.Add(new Vector3(left, bottom, 0f));
        vertices.Add(new Vector3(right, bottom, 0f));
        vertices.Add(new Vector3(right, top, 0f));
        vertices.Add(new Vector3(left, top, 0f));

        uvs.Add(new Vector2(uv.xMin, uv.yMin));
        uvs.Add(new Vector2(uv.xMax, uv.yMin));
        uvs.Add(new Vector2(uv.xMax, uv.yMax));
        uvs.Add(new Vector2(uv.xMin, uv.yMax));

        triangles.Add(start);
        triangles.Add(start + 2);
        triangles.Add(start + 1);
        triangles.Add(start);
        triangles.Add(start + 3);
        triangles.Add(start + 2);

        colors.Add(color);
        colors.Add(color);
        colors.Add(color);
        colors.Add(color);
    }

    private static Rect GetTileUv(SandboxTile tile)
    {
        switch (tile.id)
        {
            case SandboxTileIds.Grass:
                return GrassUv;
            case SandboxTileIds.Stone:
                return StoneUv;
            case SandboxTileIds.CopperOre:
                return CopperOreUv;
            default:
                return DirtUv;
        }
    }

    private static Color GetTileLightColor(SandboxTile tile)
    {
        float light = Mathf.Clamp01(tile.light / 15f);
        float brightness = Mathf.Lerp(0.35f, 1f, light);
        return new Color(brightness, brightness, brightness, 1f);
    }

    private static Rect GetAtlasUv(int column)
    {
        float tileWidth = 1f / TileAtlasColumns;
        return new Rect(column * tileWidth, 0f, tileWidth, 1f);
    }

    private static Material GetDefaultMaterial()
    {
        Shader shader = Shader.Find("Sprites/Default");
        return new Material(shader != null ? shader : Shader.Find("Unlit/Color"));
    }
}
