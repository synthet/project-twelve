using System;
using System.Collections.Generic;
using ProjectTwelve.Visual.Tiles;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public sealed class SandboxChunkRenderer : MonoBehaviour
{
    private const int TileAtlasColumns = 4;

    private static readonly Rect DirtUv = GetLegacyAtlasUv(0);
    private static readonly Rect GrassUv = GetLegacyAtlasUv(1);
    private static readonly Rect StoneUv = GetLegacyAtlasUv(2);
    private static readonly Rect CopperOreUv = GetLegacyAtlasUv(3);

    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private readonly List<Material> rebuildMaterials = new List<Material>();

    private void Awake()
    {
        EnsureComponents();
    }

    public void Rebuild(
        SandboxChunk chunk,
        float tileSize,
        Material material,
        SandboxTileVisualCatalog visualCatalog,
        Func<int, int, SandboxTile> tileLookup)
    {
        EnsureComponents();

        if (visualCatalog != null && visualCatalog.HasAutotileSources && tileLookup != null)
        {
            RebuildAutotileMesh(chunk, tileSize, material, visualCatalog, tileLookup);
        }
        else
        {
            RebuildLegacyAtlasTiles(chunk, tileSize, material);
        }

        RebuildColliders(chunk, tileSize);
        chunk.NeedsRenderRebuild = false;
        chunk.NeedsColliderRebuild = false;
    }

    public void Rebuild(SandboxChunk chunk, float tileSize, Material material)
    {
        Rebuild(chunk, tileSize, material, null, null);
    }

    private void RebuildLegacyAtlasTiles(SandboxChunk chunk, float tileSize, Material material)
    {
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

                AddTileQuad(
                    vertices,
                    triangles,
                    uvs,
                    colors,
                    x,
                    y,
                    tileSize,
                    GetLegacyTileUv(tile),
                    GetLegacyTileColor(tile));
            }
        }

        ApplySingleSubmesh(vertices, triangles, uvs, colors, ResolveMaterial(material), chunk);
    }

    private void RebuildAutotileMesh(
        SandboxChunk chunk,
        float tileSize,
        Material material,
        SandboxTileVisualCatalog visualCatalog,
        Func<int, int, SandboxTile> tileLookup)
    {
        Dictionary<Texture2D, MeshLayer> groundLayers = new Dictionary<Texture2D, MeshLayer>();
        Dictionary<Texture2D, MeshLayer> coverLayers = new Dictionary<Texture2D, MeshLayer>();

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
                AddGroundTile(groundLayers, visualCatalog, tileLookup, tile, localX, localY, worldCoord, tileSize);
                AddCoverTile(coverLayers, visualCatalog, tileLookup, tile, localX, localY, worldCoord, tileSize);
            }
        }

        ApplyLayeredMesh(groundLayers, coverLayers, material, chunk, tileSize);
    }

    private static void AddGroundTile(
        Dictionary<Texture2D, MeshLayer> layers,
        SandboxTileVisualCatalog visualCatalog,
        Func<int, int, SandboxTile> tileLookup,
        SandboxTile tile,
        int localX,
        int localY,
        Vector2Int worldCoord,
        float tileSize)
    {
        if (!visualCatalog.TryGetGroundTileset(tile.id, out AutotileTileset tileset))
        {
            return;
        }

        int[,] mask = AutotileMaskBuilder.BuildGroundMask(
            (x, y) =>
            {
                SandboxTile neighbor = tileLookup(x, y);
                return visualCatalog.SharesGroundAutotileGroup(tile.id, neighbor.id);
            },
            worldCoord.x,
            worldCoord.y);

        Sprite sprite = AutotileResolver.ResolveSprite(tileset, mask, out bool flipX);
        if (sprite == null)
        {
            return;
        }

        MeshLayer layer = GetOrCreateLayer(layers, sprite.texture);
        AddSpriteQuad(layer, localX, localY, tileSize, sprite, flipX, GetTileLightColor(tile), zOffset: 0f);
    }

    private static void AddCoverTile(
        Dictionary<Texture2D, MeshLayer> layers,
        SandboxTileVisualCatalog visualCatalog,
        Func<int, int, SandboxTile> tileLookup,
        SandboxTile tile,
        int localX,
        int localY,
        Vector2Int worldCoord,
        float tileSize)
    {
        SandboxTile tileAbove = tileLookup(worldCoord.x, worldCoord.y + 1);
        if (!visualCatalog.ShouldRenderGrassCover(tile.id, tileAbove)
            || !visualCatalog.TryGetCoverTileset(tile.id, out AutotileTileset tileset))
        {
            return;
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
            return;
        }

        MeshLayer layer = GetOrCreateLayer(layers, sprite.texture);
        AddSpriteQuad(layer, localX, localY, tileSize, sprite, flipX, GetTileLightColor(tile), zOffset: -0.01f);
    }

    private void ApplyLayeredMesh(
        Dictionary<Texture2D, MeshLayer> groundLayers,
        Dictionary<Texture2D, MeshLayer> coverLayers,
        Material material,
        SandboxChunk chunk,
        float tileSize)
    {
        if (groundLayers.Count == 0 && coverLayers.Count == 0)
        {
            RebuildLegacyAtlasTiles(chunk, tileSize, material);
            return;
        }

        Mesh mesh = GetOrCreateMesh(chunk);
        mesh.Clear();
        rebuildMaterials.Clear();

        List<Vector3> vertices = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();
        List<Color> colors = new List<Color>();
        List<List<int>> submeshTriangles = new List<List<int>>();

        AppendMeshLayers(groundLayers, material, vertices, uvs, colors, submeshTriangles, rebuildMaterials);
        AppendMeshLayers(coverLayers, material, vertices, uvs, colors, submeshTriangles, rebuildMaterials);

        mesh.SetVertices(vertices);
        mesh.SetUVs(0, uvs);
        mesh.SetColors(colors);
        mesh.subMeshCount = submeshTriangles.Count;
        for (int submeshIndex = 0; submeshIndex < submeshTriangles.Count; submeshIndex++)
        {
            mesh.SetTriangles(submeshTriangles[submeshIndex], submeshIndex);
        }

        mesh.RecalculateBounds();
        meshRenderer.sharedMaterials = rebuildMaterials.ToArray();
    }

    private void AppendMeshLayers(
        Dictionary<Texture2D, MeshLayer> layers,
        Material material,
        List<Vector3> vertices,
        List<Vector2> uvs,
        List<Color> colors,
        List<List<int>> submeshTriangles,
        List<Material> materials)
    {
        foreach (KeyValuePair<Texture2D, MeshLayer> entry in layers)
        {
            MeshLayer layer = entry.Value;
            int vertexOffset = vertices.Count;
            vertices.AddRange(layer.Vertices);
            uvs.AddRange(layer.Uvs);
            colors.AddRange(layer.Colors);

            List<int> triangles = new List<int>(layer.Triangles.Count);
            for (int i = 0; i < layer.Triangles.Count; i++)
            {
                triangles.Add(layer.Triangles[i] + vertexOffset);
            }

            submeshTriangles.Add(triangles);
            materials.Add(CreateMaterialForTexture(material, entry.Key));
        }
    }

    private void ApplySingleSubmesh(
        List<Vector3> vertices,
        List<int> triangles,
        List<Vector2> uvs,
        List<Color> colors,
        Material resolvedMaterial,
        SandboxChunk chunk)
    {
        Mesh mesh = GetOrCreateMesh(chunk);
        mesh.Clear();
        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.SetUVs(0, uvs);
        mesh.SetColors(colors);
        mesh.RecalculateBounds();
        meshRenderer.sharedMaterial = resolvedMaterial;
    }

    private Mesh GetOrCreateMesh(SandboxChunk chunk)
    {
        Mesh mesh = meshFilter.sharedMesh;
        if (mesh == null)
        {
            mesh = new Mesh { name = $"ChunkMesh_{chunk.Coord.x}_{chunk.Coord.y}" };
            meshFilter.sharedMesh = mesh;
        }

        return mesh;
    }

    private static MeshLayer GetOrCreateLayer(Dictionary<Texture2D, MeshLayer> layers, Texture2D texture)
    {
        if (!layers.TryGetValue(texture, out MeshLayer layer))
        {
            layer = new MeshLayer();
            layers.Add(texture, layer);
        }

        return layer;
    }

    private static void AddSpriteQuad(
        MeshLayer layer,
        int x,
        int y,
        float tileSize,
        Sprite sprite,
        bool flipX,
        Color color,
        float zOffset)
    {
        Rect uv = GetSpriteUv(sprite, flipX);
        AddSpriteBoundsQuad(layer, x, y, tileSize, sprite, uv, color, zOffset);
    }

    private static void AddSpriteBoundsQuad(
        MeshLayer layer,
        int x,
        int y,
        float tileSize,
        Sprite sprite,
        Rect uv,
        Color color,
        float zOffset)
    {
        Bounds bounds = sprite.bounds;
        float anchorX = x * tileSize - bounds.min.x;
        float anchorY = y * tileSize - bounds.min.y;

        float left = anchorX + bounds.min.x;
        float right = anchorX + bounds.max.x;
        float bottom = anchorY + bounds.min.y;
        float top = anchorY + bounds.max.y;

        int start = layer.Vertices.Count;
        layer.Vertices.Add(new Vector3(left, bottom, zOffset));
        layer.Vertices.Add(new Vector3(right, bottom, zOffset));
        layer.Vertices.Add(new Vector3(right, top, zOffset));
        layer.Vertices.Add(new Vector3(left, top, zOffset));

        layer.Uvs.Add(new Vector2(uv.xMin, uv.yMin));
        layer.Uvs.Add(new Vector2(uv.xMax, uv.yMin));
        layer.Uvs.Add(new Vector2(uv.xMax, uv.yMax));
        layer.Uvs.Add(new Vector2(uv.xMin, uv.yMax));

        layer.Triangles.Add(start);
        layer.Triangles.Add(start + 2);
        layer.Triangles.Add(start + 1);
        layer.Triangles.Add(start);
        layer.Triangles.Add(start + 3);
        layer.Triangles.Add(start + 2);

        layer.Colors.Add(color);
        layer.Colors.Add(color);
        layer.Colors.Add(color);
        layer.Colors.Add(color);
    }

    private static Rect GetSpriteUv(Sprite sprite, bool flipX)
    {
        Rect rect = sprite.textureRect;
        Texture2D texture = sprite.texture;
        float uMin = rect.x / texture.width;
        float uMax = (rect.x + rect.width) / texture.width;
        float vMin = rect.y / texture.height;
        float vMax = (rect.y + rect.height) / texture.height;

        if (flipX)
        {
            float swap = uMin;
            uMin = uMax;
            uMax = swap;
        }

        return new Rect(uMin, vMin, uMax - uMin, vMax - vMin);
    }

    private static Material CreateMaterialForTexture(Material template, Texture2D texture)
    {
        Material material = template != null && template.shader != null && template.shader.isSupported
            ? new Material(template)
            : GetDefaultMaterial();

        if (material.HasProperty("_MainTex"))
        {
            material.SetTexture("_MainTex", texture);
        }

        return material;
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

        foreach (SandboxColliderGeometry.SolidRun run in SandboxColliderGeometry.BuildSolidRuns(chunk))
        {
            SandboxColliderGeometry.GetColliderRect(run, tileSize, out Vector2 offset, out Vector2 size);
            BoxCollider2D collider = gameObject.AddComponent<BoxCollider2D>();
            collider.sharedMaterial = SandboxPhysicsMaterials.ZeroFriction;
            collider.offset = offset;
            collider.size = size;
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

    private static Rect GetLegacyTileUv(SandboxTile tile)
    {
        switch (tile.id)
        {
            case SandboxTileIds.Grass:
                return GrassUv;
            case SandboxTileIds.Stone:
                return StoneUv;
            case SandboxTileIds.CopperOre:
            case SandboxTileIds.IronOre:
            case SandboxTileIds.SilverOre:
            case SandboxTileIds.GoldOre:
                return CopperOreUv;
            default:
                return DirtUv;
        }
    }

    private static Color GetLegacyTileColor(SandboxTile tile)
    {
        Color color = GetTileLightColor(tile);
        switch (tile.id)
        {
            case SandboxTileIds.IronOre:
                return new Color(color.r * 0.82f, color.g * 0.82f, color.b * 0.88f, color.a);
            case SandboxTileIds.SilverOre:
                return new Color(
                    Mathf.Min(1f, color.r * 1.08f),
                    Mathf.Min(1f, color.g * 1.08f),
                    Mathf.Min(1f, color.b * 1.12f),
                    color.a);
            case SandboxTileIds.GoldOre:
                return new Color(
                    Mathf.Min(1f, color.r * 1.12f),
                    Mathf.Min(1f, color.g * 1.06f),
                    color.b * 0.88f,
                    color.a);
            default:
                return color;
        }
    }

    private static Color GetTileLightColor(SandboxTile tile)
    {
        float light = Mathf.Clamp01(tile.light / 15f);
        float brightness = Mathf.Lerp(0.35f, 1f, light);
        return new Color(brightness, brightness, brightness, 1f);
    }

    private static Rect GetLegacyAtlasUv(int column)
    {
        float tileWidth = 1f / TileAtlasColumns;
        return new Rect(column * tileWidth, 0f, tileWidth, 1f);
    }

    private static Material ResolveMaterial(Material material)
    {
        if (material != null && material.shader != null && material.shader.isSupported)
        {
            return material;
        }

        Material fallback = GetDefaultMaterial();
        if (material != null && material.HasProperty("_MainTex"))
        {
            Texture mainTex = material.GetTexture("_MainTex");
            if (mainTex != null && fallback.HasProperty("_MainTex"))
            {
                fallback.SetTexture("_MainTex", mainTex);
            }
        }

        return fallback;
    }

    private static Material GetDefaultMaterial()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default")
            ?? Shader.Find("Sprites/Default");
        return new Material(shader != null ? shader : Shader.Find("Unlit/Color"));
    }

    private sealed class MeshLayer
    {
        public List<Vector3> Vertices { get; } = new List<Vector3>();
        public List<int> Triangles { get; } = new List<int>();
        public List<Vector2> Uvs { get; } = new List<Vector2>();
        public List<Color> Colors { get; } = new List<Color>();
    }
}
