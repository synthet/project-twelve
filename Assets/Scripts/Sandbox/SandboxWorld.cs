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
    private readonly List<Vector2Int> rebuildScratch = new List<Vector2Int>();
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

    /// <summary>
    /// Single choke point for every gameplay tile edit. Placing and breaking both route here:
    /// breaking is an edit to <see cref="SandboxTileIds.Air"/>. The edit resolves the owning chunk
    /// (generating it on demand), applies the change through <see cref="ApplyTileEdit"/> so tile
    /// data, dirty flags, and loaded border-neighbor chunks all update together, then ensures a
    /// renderer exists for the owning chunk so the change becomes visible.
    /// </summary>
    public void SetTile(int x, int y, int tileId)
    {
        Vector2Int chunkCoord = WorldToChunkCoord(x, y);
        SandboxChunk chunk = GetOrCreateChunk(chunkCoord);
        Vector2Int local = WorldToLocalCoord(x, y);
        ApplyTileEdit(chunks, chunk, local.x, local.y, new SandboxTile(tileId));
        EnsureRenderer(chunkCoord);
    }

    /// <summary>
    /// Applies a single tile edit to an already-resolved owning chunk and propagates its
    /// consequences. Writing the tile through <see cref="SandboxChunk.SetLocalTile(int,int,SandboxTile)"/>
    /// stores the new tile data, raises that chunk's render and collider dirty flags, and records the
    /// chunk as edited for saving; <see cref="MarkBorderNeighborsDirty"/> then dirties any loaded
    /// face-adjacent neighbor when the edit lands on a chunk border. The logic is a pure function of
    /// the supplied chunk set, so it is covered directly by EditMode tests without instantiating
    /// renderers. Edits that fall outside the chunk's local bounds are ignored by
    /// <see cref="SandboxChunk.SetLocalTile(int,int,SandboxTile)"/> and dirty no neighbors.
    /// </summary>
    public static void ApplyTileEdit(
        IReadOnlyDictionary<Vector2Int, SandboxChunk> loadedChunks,
        SandboxChunk owningChunk,
        int localX,
        int localY,
        SandboxTile tile)
    {
        if (!SandboxChunk.IsLocalInBounds(localX, localY))
        {
            return;
        }

        owningChunk.SetLocalTile(localX, localY, tile);
        MarkBorderNeighborsDirty(loadedChunks, owningChunk.Coord, localX, localY);
    }

    /// <summary>
    /// Returns the face-adjacent chunk coordinates whose border is touched by an edit at the
    /// given local coordinate. Interior edits touch no neighbor; an edge edit touches one neighbor
    /// and a corner edit touches two (the orthogonal chunks sharing that tile's exposed faces).
    /// </summary>
    public static IEnumerable<Vector2Int> GetBorderNeighborChunks(Vector2Int chunkCoord, int localX, int localY)
    {
        if (localX == 0)
        {
            yield return new Vector2Int(chunkCoord.x - 1, chunkCoord.y);
        }
        else if (localX == SandboxChunk.Size - 1)
        {
            yield return new Vector2Int(chunkCoord.x + 1, chunkCoord.y);
        }

        if (localY == 0)
        {
            yield return new Vector2Int(chunkCoord.x, chunkCoord.y - 1);
        }
        else if (localY == SandboxChunk.Size - 1)
        {
            yield return new Vector2Int(chunkCoord.x, chunkCoord.y + 1);
        }
    }

    /// <summary>
    /// Flags every loaded face-adjacent neighbor of a border edit for render and collider rebuilds.
    /// Unloaded neighbors are skipped so editing never forces speculative chunk generation; they
    /// rebuild from current data when they next load.
    /// </summary>
    public static void MarkBorderNeighborsDirty(
        IReadOnlyDictionary<Vector2Int, SandboxChunk> loadedChunks,
        Vector2Int chunkCoord,
        int localX,
        int localY)
    {
        foreach (Vector2Int neighborCoord in GetBorderNeighborChunks(chunkCoord, localX, localY))
        {
            if (loadedChunks.TryGetValue(neighborCoord, out SandboxChunk neighbor))
            {
                neighbor.NeedsRenderRebuild = true;
                neighbor.NeedsColliderRebuild = true;
            }
        }
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

    /// <summary>
    /// Maps a world tile coordinate to the chunk that owns it using floor division by
    /// <see cref="SandboxChunk.Size"/>. Floor (not truncating) division keeps the chunk index
    /// monotonic across the origin so negative world coordinates resolve to the correct chunk
    /// (for example world x = -1 belongs to chunk x = -1, not chunk x = 0).
    /// </summary>
    public static Vector2Int WorldToChunkCoord(int x, int y)
    {
        return new Vector2Int(FloorDiv(x, SandboxChunk.Size), FloorDiv(y, SandboxChunk.Size));
    }

    /// <summary>
    /// Maps a world tile coordinate to its local coordinate inside the owning chunk using positive
    /// modulo by <see cref="SandboxChunk.Size"/>. The result is always in the range
    /// [0, <see cref="SandboxChunk.Size"/> - 1] even for negative world coordinates, so it is always
    /// a valid array index (for example world x = -1 maps to local x = Size - 1).
    /// </summary>
    public static Vector2Int WorldToLocalCoord(int x, int y)
    {
        return new Vector2Int(Mod(x, SandboxChunk.Size), Mod(y, SandboxChunk.Size));
    }

    /// <summary>
    /// Inverse of <see cref="WorldToChunkCoord"/> and <see cref="WorldToLocalCoord"/>: reconstructs
    /// the world tile coordinate from a chunk coordinate and an in-chunk local coordinate. For any
    /// world coordinate the round trip is exact, i.e.
    /// <c>ChunkLocalToWorld(WorldToChunkCoord(p), WorldToLocalCoord(p)) == p</c>. Callers are
    /// expected to pass a local coordinate in [0, <see cref="SandboxChunk.Size"/> - 1]; values
    /// outside that range still map linearly but no longer correspond to the given chunk.
    /// </summary>
    public static Vector2Int ChunkLocalToWorld(Vector2Int chunkCoord, int localX, int localY)
    {
        return new Vector2Int(chunkCoord.x * SandboxChunk.Size + localX, chunkCoord.y * SandboxChunk.Size + localY);
    }

    private void RefreshLoadedChunks()
    {
        Vector2Int centerChunk = GetCenterChunk();
        foreach (Vector2Int coord in GetChunksInLoadRange(centerChunk, loadRadiusInChunks))
        {
            EnsureRenderer(coord);
        }

        UnloadDistantRenderers(centerChunk);
    }

    /// <summary>
    /// Enumerates the chunk coordinates that should be loaded and visible around the player's chunk.
    /// The load window is a square of side (2 * loadRadius + 1) chunks centered on the player, so
    /// chunk streaming is bounded to a predictable neighborhood regardless of world size. A negative
    /// radius is clamped to zero, which loads only the center chunk.
    /// </summary>
    public static IEnumerable<Vector2Int> GetChunksInLoadRange(Vector2Int centerChunk, int loadRadius)
    {
        int radius = Mathf.Max(0, loadRadius);
        for (int x = centerChunk.x - radius; x <= centerChunk.x + radius; x++)
        {
            for (int y = centerChunk.y - radius; y <= centerChunk.y + radius; y++)
            {
                yield return new Vector2Int(x, y);
            }
        }
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
        List<Vector2Int> renderersToUnload = new List<Vector2Int>(
            GetRenderersToUnload(renderers.Keys, centerChunk, loadRadiusInChunks, unloadPaddingInChunks));

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

    /// <summary>
    /// Selects the currently loaded chunk coordinates that fall outside the unload window and should
    /// be released. The unload window is intentionally larger than the load window by an unload
    /// padding, creating hysteresis so chunks at the load edge are not thrashed (loaded and unloaded)
    /// by small player movements. Distance uses the Chebyshev (square) metric to match the square
    /// load window; negative radii and padding are clamped to zero.
    /// </summary>
    public static IEnumerable<Vector2Int> GetRenderersToUnload(
        IEnumerable<Vector2Int> loadedCoords,
        Vector2Int centerChunk,
        int loadRadius,
        int unloadPadding)
    {
        int unloadRadius = Mathf.Max(0, loadRadius) + Mathf.Max(0, unloadPadding);
        foreach (Vector2Int coord in loadedCoords)
        {
            int distanceX = Mathf.Abs(coord.x - centerChunk.x);
            int distanceY = Mathf.Abs(coord.y - centerChunk.y);
            if (distanceX > unloadRadius || distanceY > unloadRadius)
            {
                yield return coord;
            }
        }
    }

    private void RebuildDirtyChunks()
    {
        rebuildScratch.Clear();
        rebuildScratch.AddRange(GetChunksNeedingRebuild(renderers.Keys, chunks));
        foreach (Vector2Int coord in rebuildScratch)
        {
            renderers[coord].Rebuild(chunks[coord], tileSize, tileMaterial);
        }
    }

    /// <summary>
    /// Selects the visible chunks that need a render or collider rebuild this frame.
    /// Only currently visible (renderer-backed) coordinates are considered, and a chunk is
    /// returned solely when its own dirty flags are set, so a rebuild never touches an
    /// unrelated clean chunk or a loaded-but-not-visible chunk. This bounds rebuild cost to
    /// the chunks a player can actually see and has actually edited.
    /// </summary>
    public static IEnumerable<Vector2Int> GetChunksNeedingRebuild(
        IEnumerable<Vector2Int> visibleChunks,
        IReadOnlyDictionary<Vector2Int, SandboxChunk> loadedChunks)
    {
        foreach (Vector2Int coord in visibleChunks)
        {
            if (loadedChunks.TryGetValue(coord, out SandboxChunk chunk)
                && (chunk.NeedsRenderRebuild || chunk.NeedsColliderRebuild))
            {
                yield return coord;
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

    /// <summary>
    /// Builds the deterministic generator from the current world generation settings.
    /// Generation depends only on these inputs and the chunk coordinate, so the same
    /// settings reproduce identical chunks across runs.
    /// </summary>
    public SandboxTerrainGenerator CreateTerrainGenerator()
    {
        return new SandboxTerrainGenerator(seed, surfaceHeight, terrainAmplitude, terrainFrequency, dirtDepth);
    }

    private SandboxChunk GenerateChunk(Vector2Int chunkCoord)
    {
        return CreateTerrainGenerator().GenerateChunk(chunkCoord);
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
