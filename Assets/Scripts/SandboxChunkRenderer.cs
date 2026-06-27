using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public sealed class SandboxChunkRenderer : MonoBehaviour
{
    private static readonly Color DirtColor = new Color(0.45f, 0.26f, 0.12f);
    private static readonly Color GrassColor = new Color(0.22f, 0.58f, 0.18f);
    private static readonly Color StoneColor = new Color(0.42f, 0.42f, 0.45f);

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

                AddTileQuad(vertices, triangles, colors, x, y, tileSize, GetTileColor(tile));
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

    private static void AddTileQuad(List<Vector3> vertices, List<int> triangles, List<Color> colors, int x, int y, float tileSize, Color color)
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

    private static Color GetTileColor(SandboxTile tile)
    {
        Color baseColor;
        switch (tile.id)
        {
            case SandboxTileIds.Grass:
                baseColor = GrassColor;
                break;
            case SandboxTileIds.Stone:
                baseColor = StoneColor;
                break;
            default:
                baseColor = DirtColor;
                break;
        }

        float light = Mathf.Clamp01(tile.light / 15f);
        return baseColor * Mathf.Lerp(0.35f, 1f, light);
    }

    private static Material GetDefaultMaterial()
    {
        Shader shader = Shader.Find("Sprites/Default");
        return new Material(shader != null ? shader : Shader.Find("Unlit/Color"));
    }
}
