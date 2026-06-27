using System.Collections.Generic;
using System.IO;
using UnityEngine;

public sealed class SandboxWorld : MonoBehaviour
{
    [Header("World Generation")]
    [SerializeField] private int seed = 1337;
    [SerializeField] private int surfaceHeight = 28;
    [SerializeField] private int terrainAmplitude = 8;
    [SerializeField] private float terrainFrequency = 0.06f;
    [SerializeField] private int dirtDepth = 8;

    [Header("Chunk Loading")]
    [SerializeField] private Transform playerTarget;
    [SerializeField] private int loadRadiusInChunks = 3;
    [SerializeField] private int unloadPaddingInChunks = 1;
    [SerializeField] private float chunkRefreshInterval = 0.5f;

    [Header("Rendering")]
    [SerializeField] private SandboxChunkRenderer chunkRendererPrefab;
    [SerializeField] private Material tileMaterial;
    [SerializeField] private float tileSize = 1f;

    private readonly Dictionary<Vector2Int, SandboxChunk> chunks = new Dictionary<Vector2Int, SandboxChunk>();
    private readonly Dictionary<Vector2Int, SandboxChunkRenderer> renderers = new Dictionary<Vector2Int, SandboxChunkRenderer>();
    private float nextChunkRefreshTime;

    public float TileSize => tileSize;
    public int Seed => seed;

    public void SetPlayerTarget(Transform target)
    {
        playerTarget = target;
    }

    private void Start()
    {
        RefreshLoadedChunks();
    }

    private void Update()
    {
        if (Time.time >= nextChunkRefreshTime)
        {
            RefreshLoadedChunks();
            nextChunkRefreshTime = Time.time + chunkRefreshInterval;
        }

        RebuildDirtyChunks();
    }

    public SandboxTile GetTile(int x, int y)
    {
        Vector2Int chunkCoord = WorldToChunkCoord(x, y);
        if (!chunks.TryGetValue(chunkCoord, out SandboxChunk chunk))
        {
            chunk = GenerateChunk(chunkCoord);
            chunks.Add(chunkCoord, chunk);
        }

        Vector2Int local = WorldToLocalCoord(x, y);
        return chunk.GetLocalTile(local.x, local.y);
    }

    public void SetTile(int x, int y, int tileId)
    {
        Vector2Int chunkCoord = WorldToChunkCoord(x, y);
        SandboxChunk chunk = GetOrCreateChunk(chunkCoord);
        Vector2Int local = WorldToLocalCoord(x, y);
        chunk.SetLocalTile(local.x, local.y, new SandboxTile(tileId));
        EnsureRenderer(chunkCoord);
    }


    public void SaveToPath(string path)
    {
        SandboxSaveData saveData = new SandboxSaveData { seed = seed };
        foreach (KeyValuePair<Vector2Int, SandboxChunk> pair in chunks)
        {
            if (!pair.Value.HasEdits)
            {
                continue;
            }

            SandboxChunkSaveData chunkData = new SandboxChunkSaveData { x = pair.Key.x, y = pair.Key.y };
            for (int x = 0; x < SandboxChunk.Size; x++)
            {
                for (int y = 0; y < SandboxChunk.Size; y++)
                {
                    chunkData.edits.Add(new SandboxTileEditData(x, y, pair.Value.GetLocalTile(x, y)));
                }
            }

            saveData.chunks.Add(chunkData);
            pair.Value.MarkClean();
        }

        string directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(path, JsonUtility.ToJson(saveData, true));
    }

    public void LoadFromPath(string path)
    {
        if (!File.Exists(path))
        {
            Debug.LogWarning($"Sandbox save file not found: {path}");
            return;
        }

        SandboxSaveData saveData = JsonUtility.FromJson<SandboxSaveData>(File.ReadAllText(path));
        seed = saveData.seed;
        chunks.Clear();

        foreach (SandboxChunkSaveData chunkData in saveData.chunks)
        {
            SandboxChunk chunk = GenerateChunk(chunkData.Coord);
            foreach (SandboxTileEditData edit in chunkData.edits)
            {
                chunk.SetLocalTile(edit.localX, edit.localY, edit.tile, false);
            }

            chunk.MarkHasEdits();
            chunk.MarkClean();
            chunks.Add(chunkData.Coord, chunk);
        }

        MarkAllLoadedRenderersDirty();
    }

    public bool IsSolidAtWorldPosition(Vector2 worldPosition)
    {
        Vector2Int tile = WorldPositionToTile(worldPosition);
        return GetTile(tile.x, tile.y).IsSolid;
    }

    public Vector2Int WorldPositionToTile(Vector2 worldPosition)
    {
        return new Vector2Int(Mathf.FloorToInt(worldPosition.x / tileSize), Mathf.FloorToInt(worldPosition.y / tileSize));
    }

    public Vector3 TileToWorldCenter(int x, int y)
    {
        return new Vector3((x + 0.5f) * tileSize, (y + 0.5f) * tileSize, 0f);
    }

    public static Vector2Int WorldToChunkCoord(int x, int y)
    {
        return new Vector2Int(FloorDiv(x, SandboxChunk.Size), FloorDiv(y, SandboxChunk.Size));
    }

    public static Vector2Int WorldToLocalCoord(int x, int y)
    {
        return new Vector2Int(Mod(x, SandboxChunk.Size), Mod(y, SandboxChunk.Size));
    }

    private void RefreshLoadedChunks()
    {
        Vector2Int centerChunk = GetCenterChunk();
        for (int x = centerChunk.x - loadRadiusInChunks; x <= centerChunk.x + loadRadiusInChunks; x++)
        {
            for (int y = centerChunk.y - loadRadiusInChunks; y <= centerChunk.y + loadRadiusInChunks; y++)
            {
                EnsureRenderer(new Vector2Int(x, y));
            }
        }

        UnloadDistantRenderers(centerChunk);
    }

    private Vector2Int GetCenterChunk()
    {
        if (playerTarget == null)
        {
            return Vector2Int.zero;
        }

        return WorldToChunkCoord(
            Mathf.FloorToInt(playerTarget.position.x / tileSize),
            Mathf.FloorToInt(playerTarget.position.y / tileSize));
    }

    private void UnloadDistantRenderers(Vector2Int centerChunk)
    {
        int unloadRadius = loadRadiusInChunks + Mathf.Max(0, unloadPaddingInChunks);
        List<Vector2Int> renderersToUnload = new List<Vector2Int>();
        foreach (Vector2Int coord in renderers.Keys)
        {
            int distanceX = Mathf.Abs(coord.x - centerChunk.x);
            int distanceY = Mathf.Abs(coord.y - centerChunk.y);
            if (distanceX > unloadRadius || distanceY > unloadRadius)
            {
                renderersToUnload.Add(coord);
            }
        }

        foreach (Vector2Int coord in renderersToUnload)
        {
            SandboxChunkRenderer chunkRenderer = renderers[coord];
            renderers.Remove(coord);
            if (Application.isPlaying)
            {
                Destroy(chunkRenderer.gameObject);
            }
            else
            {
                DestroyImmediate(chunkRenderer.gameObject);
            }
        }
    }

    private void RebuildDirtyChunks()
    {
        foreach (KeyValuePair<Vector2Int, SandboxChunkRenderer> pair in renderers)
        {
            SandboxChunk chunk = GetOrCreateChunk(pair.Key);
            if (chunk.NeedsRenderRebuild || chunk.NeedsColliderRebuild)
            {
                pair.Value.Rebuild(chunk, tileSize, tileMaterial);
            }
        }
    }


    private void MarkAllLoadedRenderersDirty()
    {
        foreach (Vector2Int coord in renderers.Keys)
        {
            SandboxChunk chunk = GetOrCreateChunk(coord);
            chunk.NeedsRenderRebuild = true;
            chunk.NeedsColliderRebuild = true;
        }
    }

    private SandboxChunk GetOrCreateChunk(Vector2Int chunkCoord)
    {
        if (!chunks.TryGetValue(chunkCoord, out SandboxChunk chunk))
        {
            chunk = GenerateChunk(chunkCoord);
            chunks.Add(chunkCoord, chunk);
        }

        return chunk;
    }

    private void EnsureRenderer(Vector2Int chunkCoord)
    {
        SandboxChunk chunk = GetOrCreateChunk(chunkCoord);
        if (renderers.ContainsKey(chunkCoord))
        {
            return;
        }

        SandboxChunkRenderer chunkRenderer = chunkRendererPrefab == null
            ? new GameObject($"Chunk_{chunkCoord.x}_{chunkCoord.y}").AddComponent<SandboxChunkRenderer>()
            : Instantiate(chunkRendererPrefab);

        chunkRenderer.transform.SetParent(transform, false);
        chunkRenderer.transform.position = new Vector3(chunkCoord.x * SandboxChunk.Size * tileSize, chunkCoord.y * SandboxChunk.Size * tileSize, 0f);
        renderers.Add(chunkCoord, chunkRenderer);
        chunk.NeedsRenderRebuild = true;
        chunk.NeedsColliderRebuild = true;
    }

    private SandboxChunk GenerateChunk(Vector2Int chunkCoord)
    {
        SandboxChunk chunk = new SandboxChunk(chunkCoord);

        for (int localX = 0; localX < SandboxChunk.Size; localX++)
        {
            int worldX = chunkCoord.x * SandboxChunk.Size + localX;
            int height = GetSurfaceHeight(worldX);

            for (int localY = 0; localY < SandboxChunk.Size; localY++)
            {
                int worldY = chunkCoord.y * SandboxChunk.Size + localY;
                int tileId = GetGeneratedTileId(worldY, height);
                chunk.Tiles[localX, localY] = new SandboxTile(tileId, worldY >= height ? (byte)15 : (byte)4);
            }
        }

        return chunk;
    }

    private int GetSurfaceHeight(int worldX)
    {
        float noise = Mathf.PerlinNoise((worldX + seed) * terrainFrequency, seed * 0.001f);
        return surfaceHeight + Mathf.RoundToInt((noise - 0.5f) * terrainAmplitude * 2f);
    }

    private int GetGeneratedTileId(int worldY, int height)
    {
        if (worldY > height)
        {
            return SandboxTileIds.Air;
        }

        if (worldY == height)
        {
            return SandboxTileIds.Grass;
        }

        return worldY > height - dirtDepth ? SandboxTileIds.Dirt : SandboxTileIds.Stone;
    }

    private static int FloorDiv(int value, int divisor)
    {
        return Mathf.FloorToInt((float)value / divisor);
    }

    private static int Mod(int value, int divisor)
    {
        int result = value % divisor;
        return result < 0 ? result + divisor : result;
    }
}
