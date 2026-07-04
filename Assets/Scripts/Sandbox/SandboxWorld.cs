using System.Collections.Generic;
using System.IO;
using ProjectTwelve.Sandbox.Registry;
using UnityEngine;

[DefaultExecutionOrder(-100)]
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
    [SerializeField] private SandboxTileVisualCatalog tileVisualCatalog;
    [SerializeField] private float tileSize = 1f;

    private readonly Dictionary<Vector2Int, SandboxChunk> chunks = new Dictionary<Vector2Int, SandboxChunk>();
    private readonly Dictionary<Vector2Int, SandboxChunkRenderer> renderers = new Dictionary<Vector2Int, SandboxChunkRenderer>();
    private readonly List<Vector2Int> rebuildScratch = new List<Vector2Int>();
    private float nextChunkRefreshTime;

    public float TileSize => tileSize;
    public int Seed => seed;

    /// <summary>Visual catalog used for autotile mesh rendering and debug tooling.</summary>
    public SandboxTileVisualCatalog TileVisualCatalog => tileVisualCatalog;

    /// <summary>Number of chunks with active renderers.</summary>
    public int LoadedChunkCount => renderers.Count;

    /// <summary>Returns the current player world position when a player target is assigned.</summary>
    public bool TryGetPlayerWorldPosition(out Vector2 position)
    {
        if (playerTarget == null)
        {
            position = default;
            return false;
        }

        position = playerTarget.position;
        return true;
    }

    public void SetPlayerTarget(Transform target)
    {
        playerTarget = target;
    }

    /// <summary>The transform chunk loading follows; also the chase target for enemies.</summary>
    public Transform PlayerTarget => playerTarget;

    private void Start()
    {
        RefreshLoadedChunks();
        RebuildDirtyChunks();
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
        MarkBorderNeighborsDirty(chunks, chunkCoord, local.x, local.y);
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
            MarkChunkDirtyIfLoaded(loadedChunks, neighborCoord);
        }
    }

    /// <summary>
    /// Returns the four face-adjacent chunk coordinates sharing a border with <paramref name="chunkCoord"/>.
    /// </summary>
    public static IEnumerable<Vector2Int> GetFaceNeighborChunks(Vector2Int chunkCoord)
    {
        yield return new Vector2Int(chunkCoord.x - 1, chunkCoord.y);
        yield return new Vector2Int(chunkCoord.x + 1, chunkCoord.y);
        yield return new Vector2Int(chunkCoord.x, chunkCoord.y - 1);
        yield return new Vector2Int(chunkCoord.x, chunkCoord.y + 1);
    }

    /// <summary>
    /// Flags rendered face-adjacent neighbors for rebuild when a chunk becomes visible or is
    /// unloaded. Autotile masks sample neighbor tiles across chunk borders, so a neighbor that
    /// rendered before this chunk loaded (or after it unloads) keeps stale edge geometry until it
    /// rebuilds.
    /// </summary>
    public static void MarkRenderedFaceNeighborsDirty(
        Vector2Int chunkCoord,
        IReadOnlyDictionary<Vector2Int, SandboxChunk> loadedChunks,
        ICollection<Vector2Int> renderedChunkCoords)
    {
        foreach (Vector2Int neighborCoord in GetFaceNeighborChunks(chunkCoord))
        {
            if (!renderedChunkCoords.Contains(neighborCoord))
            {
                continue;
            }

            MarkChunkDirtyIfLoaded(loadedChunks, neighborCoord);
        }
    }

    private static void MarkChunkDirtyIfLoaded(
        IReadOnlyDictionary<Vector2Int, SandboxChunk> loadedChunks,
        Vector2Int chunkCoord)
    {
        if (loadedChunks.TryGetValue(chunkCoord, out SandboxChunk chunk))
        {
            chunk.NeedsRenderRebuild = true;
            chunk.NeedsColliderRebuild = true;
            chunk.BumpNavVersion();
        }
    }

    /// <summary>
    /// Whether the chunk is in the streamed (renderer-backed) window. This is the loaded-chunk
    /// set navigation and spawning are bounded by: enemies never path into or spawn in chunks
    /// outside it.
    /// </summary>
    public bool IsChunkLoaded(Vector2Int chunkCoord)
    {
        return renderers.ContainsKey(chunkCoord);
    }

    /// <summary>
    /// Current navigation version of the chunk (see <see cref="SandboxChunk.NavVersion"/>), or 0
    /// when the chunk has never been generated.
    /// </summary>
    public int GetNavVersion(Vector2Int chunkCoord)
    {
        return chunks.TryGetValue(chunkCoord, out SandboxChunk chunk) ? chunk.NavVersion : 0;
    }


    public void SaveToPath(string path)
    {
        SandboxSaveData saveData = new SandboxSaveData
        {
            seed = seed,
            tilePalette = RegistryPalette.Capture(SandboxRegistries.Tiles)
        };
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

        if (playerTarget != null)
        {
            saveData.hasPlayerPosition = true;
            saveData.playerX = playerTarget.position.x;
            saveData.playerY = playerTarget.position.y;
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

        // Palette saves remap saved runtime indices to the live registry; version-1 prototype
        // saves carry no palette and store the fixed legacy numbering instead.
        int[] paletteRemap = saveData.HasTilePalette
            ? saveData.tilePalette.BuildRemap(SandboxRegistries.Tiles)
            : null;

        foreach (SandboxChunkSaveData chunkData in saveData.chunks)
        {
            SandboxChunk chunk = GenerateChunk(chunkData.Coord);
            foreach (SandboxTileEditData edit in chunkData.edits)
            {
                SandboxTile tile = edit.tile;
                tile.id = ResolveSavedTileId(tile.id, paletteRemap, chunkData, edit);
                chunk.SetLocalTile(edit.localX, edit.localY, tile, false);
            }

            chunk.MarkHasEdits();
            chunk.MarkClean();
            chunks.Add(chunkData.Coord, chunk);
        }

        if (saveData.hasPlayerPosition)
        {
            RestorePlayerPosition(new Vector2(saveData.playerX, saveData.playerY));
            RefreshLoadedChunks();
        }

        MarkAllLoadedRenderersDirty();
    }

    /// <summary>
    /// Maps a saved tile id to a live registry runtime index: through the save's palette remap
    /// when present, otherwise through the fixed legacy numbering of version-1 prototype saves.
    /// Fails loudly on ids outside either table rather than silently corrupting tiles.
    /// </summary>
    private static int ResolveSavedTileId(
        int savedId,
        int[] paletteRemap,
        SandboxChunkSaveData chunkData,
        SandboxTileEditData edit)
    {
        if (paletteRemap != null)
        {
            if (savedId < 0 || savedId >= paletteRemap.Length)
            {
                throw new System.InvalidOperationException(
                    $"Saved tile id {savedId} at chunk ({chunkData.x}, {chunkData.y}) local ({edit.localX}, {edit.localY}) is outside the save palette.");
            }

            return paletteRemap[savedId];
        }

        IReadOnlyList<string> legacy = SandboxCoreContent.LegacyTileIdToStringId;
        if (savedId < 0 || savedId >= legacy.Count)
        {
            throw new System.InvalidOperationException(
                $"Legacy tile id {savedId} at chunk ({chunkData.x}, {chunkData.y}) local ({edit.localX}, {edit.localY}) is outside the legacy tile-id table.");
        }

        return SandboxRegistries.Tiles.GetIndex(legacy[savedId]);
    }

    private void RestorePlayerPosition(Vector2 position)
    {
        if (playerTarget == null)
        {
            return;
        }

        if (playerTarget.TryGetComponent(out Rigidbody2D body))
        {
            body.position = position;
            body.linearVelocity = Vector2.zero;
            return;
        }

        Vector3 current = playerTarget.position;
        playerTarget.position = new Vector3(position.x, position.y, current.z);
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
            MarkRenderedFaceNeighborsDirty(coord, chunks, renderers.Keys);
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
            renderers[coord].Rebuild(
                chunks[coord],
                tileSize,
                tileMaterial,
                tileVisualCatalog,
                GetTile);
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
        MarkRenderedFaceNeighborsDirty(chunkCoord, chunks, renderers.Keys);
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
