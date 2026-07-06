using System;
using System.Collections.Generic;
using ProjectTwelve.Sandbox.Registry;
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

    // Registry runtime indices resolved once; the per-tile rebuild loop stays free of
    // string lookups (P2-DATA-002 hot-path requirement).
    private static readonly int GrassTileIndex = SandboxRegistries.Tiles.GetIndex("core:grass");
    private static readonly int StoneTileIndex = SandboxRegistries.Tiles.GetIndex("core:stone");
    private static readonly int CopperOreTileIndex = SandboxRegistries.Tiles.GetIndex("core:copper_ore");
    private static readonly int IronOreTileIndex = SandboxRegistries.Tiles.GetIndex("core:iron_ore");
    private static readonly int SilverOreTileIndex = SandboxRegistries.Tiles.GetIndex("core:silver_ore");
    private static readonly int GoldOreTileIndex = SandboxRegistries.Tiles.GetIndex("core:gold_ore");
    private static readonly HashSet<string> MissingOverrideSpriteWarnings = new HashSet<string>();

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
        Func<int, int, SandboxTile> tileLookup,
        AutotileVisualOverrideMap visualOverrides = null,
        SandboxVisualOverrideLookup visualOverrideLookup = null)
    {
        EnsureComponents();

        if (visualCatalog != null && visualCatalog.HasAutotileSources && tileLookup != null)
        {
            RebuildAutotileMesh(chunk, tileSize, material, visualCatalog, tileLookup, visualOverrides, visualOverrideLookup);
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
        Func<int, int, SandboxTile> tileLookup,
        AutotileVisualOverrideMap visualOverrides,
        SandboxVisualOverrideLookup visualOverrideLookup)
    {
        Dictionary<Texture2D, MeshLayer> groundLayers = new Dictionary<Texture2D, MeshLayer>();
        Dictionary<Texture2D, MeshLayer> coverLayers = new Dictionary<Texture2D, MeshLayer>();
        bool skippedMissingSprite = false;

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
                skippedMissingSprite |= AddGroundTile(
                    groundLayers,
                    visualCatalog,
                    tileLookup,
                    visualOverrides,
                    visualOverrideLookup,
                    tile,
                    localX,
                    localY,
                    worldCoord,
                    tileSize);
                skippedMissingSprite |= AddCoverTile(
                    coverLayers,
                    visualCatalog,
                    tileLookup,
                    visualOverrides,
                    tile,
                    localX,
                    localY,
                    worldCoord,
                    tileSize);
            }
        }

        ApplyLayeredMesh(groundLayers, coverLayers, material, chunk, tileSize, skippedMissingSprite);
    }

    private static bool AddGroundTile(
        Dictionary<Texture2D, MeshLayer> layers,
        SandboxTileVisualCatalog visualCatalog,
        Func<int, int, SandboxTile> tileLookup,
        AutotileVisualOverrideMap visualOverrides,
        SandboxVisualOverrideLookup visualOverrideLookup,
        SandboxTile tile,
        int localX,
        int localY,
        Vector2Int worldCoord,
        float tileSize)
    {
        if (!visualCatalog.TryGetGroundTileset(tile.id, out AutotileTileset tileset))
        {
            return false;
        }

        int[,] mask = AutotileGroundResolve.BuildGroundMask(
            visualCatalog,
            tileLookup,
            tile,
            worldCoord.x,
            worldCoord.y);

        string spriteId;
        bool flipX;
        if (visualOverrideLookup != null
            && visualOverrideLookup(worldCoord.x, worldCoord.y, out SandboxVisualOverride visualOverride))
        {
            spriteId = visualOverride.SpriteId;
            flipX = visualOverride.FlipX;
        }
        else
        {
            spriteId = AutotileResolver.ResolveSpriteId(tileset, mask, out flipX);
        }

        if (!TryResolveAutotileSprite(tileset, spriteId, "ground", worldCoord, out Sprite sprite))
        {
            return true;
        }

        AddResolvedSpriteQuad(
            layers,
            visualOverrides,
            worldCoord,
            AutotileVisualOverrideMap.GroundLayer,
            tileset,
            localX,
            localY,
            tileSize,
            sprite,
            flipX,
            GetTileLightColor(tile),
            zOffset: 0f);
        return false;
    }

    private static bool AddCoverTile(
        Dictionary<Texture2D, MeshLayer> layers,
        SandboxTileVisualCatalog visualCatalog,
        Func<int, int, SandboxTile> tileLookup,
        AutotileVisualOverrideMap visualOverrides,
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

        string spriteId = AutotileResolver.ResolveSpriteId(tileset, mask, out bool flipX);
        if (!TryResolveAutotileSprite(tileset, spriteId, "cover", worldCoord, out Sprite sprite))
        {
            return true;
        }

        AddResolvedSpriteQuad(
            layers,
            visualOverrides,
            worldCoord,
            AutotileVisualOverrideMap.CoverLayer,
            tileset,
            localX,
            localY,
            tileSize,
            sprite,
            flipX,
            GetTileLightColor(tile),
            zOffset: -0.01f);
        return false;
    }

    private static bool TryResolveAutotileSprite(
        AutotileTileset tileset,
        string spriteId,
        string layer,
        Vector2Int worldCoord,
        out Sprite sprite)
    {
        if (AutotileResolver.TryGetSpriteById(tileset, spriteId, out sprite))
        {
            return true;
        }

        WarnMissingOverrideSpriteOnce(tileset, layer, spriteId, worldCoord);
        return false;
    }

    internal static void WarnMissingOverrideSpriteOnce(
        AutotileTileset tileset,
        string layer,
        string spriteId,
        Vector2Int worldCoord)
    {
        string tilesetName = tileset != null ? tileset.Name : "<null>";
        string key = $"{tilesetName}|{layer}|{spriteId}";
        if (!MissingOverrideSpriteWarnings.Add(key))
        {
            return;
        }

        Debug.LogWarning(
            "SandboxChunkRenderer: missing autotile override sprite; ignoring visual entry " +
            $"at cell ({worldCoord.x}, {worldCoord.y}), layer '{layer}', tileset '{tilesetName}', sprite id '{spriteId}'.");
    }

    internal static void ClearMissingOverrideSpriteWarningsForTests()
    {
        MissingOverrideSpriteWarnings.Clear();
    }

    private void ApplyLayeredMesh(
        Dictionary<Texture2D, MeshLayer> groundLayers,
        Dictionary<Texture2D, MeshLayer> coverLayers,
        Material material,
        SandboxChunk chunk,
        float tileSize,
        bool skippedMissingSprite)
    {
        if (groundLayers.Count == 0 && coverLayers.Count == 0)
        {
            if (skippedMissingSprite)
            {
                ApplySingleSubmesh(
                    new List<Vector3>(),
                    new List<int>(),
                    new List<Vector2>(),
                    new List<Color>(),
                    ResolveMaterial(material),
                    chunk);
                return;
            }

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


    private static void AddResolvedSpriteQuad(
        Dictionary<Texture2D, MeshLayer> layers,
        AutotileVisualOverrideMap visualOverrides,
        Vector2Int worldCoord,
        string layerName,
        AutotileTileset tileset,
        int x,
        int y,
        float tileSize,
        Sprite normalSprite,
        bool normalFlipX,
        Color color,
        float zOffset)
    {
        Sprite sprite = normalSprite;
        bool flipX = normalFlipX;
        bool useFixedCellQuad = false;

        if (visualOverrides != null
            && visualOverrides.TryGetOverride(worldCoord, layerName, tileset.Name, out string overrideSpriteId)
            && tileset.TryGetSprite(overrideSpriteId, out Sprite overrideSprite))
        {
            sprite = overrideSprite;
            flipX = false;
            useFixedCellQuad = true;
        }

        MeshLayer meshLayer = GetOrCreateLayer(layers, sprite.texture);
        if (useFixedCellQuad)
        {
            AutotileSpriteMeshBuilder.AppendFixedCellQuad(
                meshLayer.Vertices,
                meshLayer.Triangles,
                meshLayer.Uvs,
                meshLayer.Colors,
                x,
                y,
                tileSize,
                sprite,
                flipX,
                color,
                zOffset,
                flipY: false,
                rotationDegrees: 0);
            return;
        }

        AddSpriteQuad(meshLayer, x, y, tileSize, sprite, flipX, color, zOffset);
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
        AutotileSpriteMeshBuilder.AppendSprite(
            layer.Vertices,
            layer.Triangles,
            layer.Uvs,
            layer.Colors,
            x,
            y,
            tileSize,
            sprite,
            flipX,
            color,
            zOffset);
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
        if (tile.id == GrassTileIndex)
        {
            return GrassUv;
        }

        if (tile.id == StoneTileIndex)
        {
            return StoneUv;
        }

        if (tile.id == CopperOreTileIndex
            || tile.id == IronOreTileIndex
            || tile.id == SilverOreTileIndex
            || tile.id == GoldOreTileIndex)
        {
            return CopperOreUv;
        }

        return DirtUv;
    }

    private static Color GetLegacyTileColor(SandboxTile tile)
    {
        Color color = GetTileLightColor(tile);
        if (tile.id == IronOreTileIndex)
        {
            return new Color(color.r * 0.82f, color.g * 0.82f, color.b * 0.88f, color.a);
        }

        if (tile.id == SilverOreTileIndex)
        {
            return new Color(
                Mathf.Min(1f, color.r * 1.08f),
                Mathf.Min(1f, color.g * 1.08f),
                Mathf.Min(1f, color.b * 1.12f),
                color.a);
        }

        if (tile.id == GoldOreTileIndex)
        {
            return new Color(
                Mathf.Min(1f, color.r * 1.12f),
                Mathf.Min(1f, color.g * 1.06f),
                color.b * 0.88f,
                color.a);
        }

        return color;
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
