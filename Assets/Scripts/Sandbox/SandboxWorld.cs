using System;
using System.Collections.Generic;
using System.IO;
using ProjectTwelve.Sandbox.Debug;
using ProjectTwelve.Sandbox.Inventory;
using ProjectTwelve.Sandbox.Lighting;
using ProjectTwelve.Sandbox.Registry;
using ProjectTwelve.Visual.AutotileDebug;
using ProjectTwelve.Visual.Tiles;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

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

    [Header("Debug")]
    [SerializeField] private GroundAutotileDebugMode groundAutotileDebugMode;
    [SerializeField] private bool debugOverrideModeEnabled;
    [SerializeField] private int autotileExposureFloorY = AutotileExposure.DefaultSandboxFloorY;

    [Header("Grass")]
    [Tooltip("Probability (0–1) that each persisted grass tile reverts to dirt when a save is loaded.")]
    [SerializeField, Range(0f, 1f)] private float loadGrassLossChance = 0.1f;

    private System.Random loadGrassRng;

    private readonly Dictionary<Vector2Int, SandboxChunk> chunks = new Dictionary<Vector2Int, SandboxChunk>();
    private readonly Dictionary<Vector2Int, SandboxChunkRenderer> renderers = new Dictionary<Vector2Int, SandboxChunkRenderer>();
    private readonly Dictionary<Vector2Int, SandboxGroundAutotileDebugOverlay> debugOverlays =
        new Dictionary<Vector2Int, SandboxGroundAutotileDebugOverlay>();
    private readonly List<Vector2Int> rebuildScratch = new List<Vector2Int>();
    private readonly AutotileVisualOverrideMap autotileVisualOverrides = new AutotileVisualOverrideMap();
    private float nextChunkRefreshTime;
    private SandboxWorldLightGrid lightGrid;
    private SandboxInventory playerInventory;

    public float TileSize => tileSize;
    public int Seed => seed;

    /// <summary>Visual catalog used for autotile mesh rendering and debug tooling.</summary>
    public SandboxTileVisualCatalog TileVisualCatalog => tileVisualCatalog;

    /// <summary>Rendering-only autotile sprite overrides, keyed outside tile identity and generation.</summary>
    public AutotileVisualOverrideMap AutotileVisualOverrides => autotileVisualOverrides;

    /// <summary>Whether any visual override entries are loaded or active.</summary>
    public bool HasVisualOverrides => autotileVisualOverrides.HasOverrides;

    /// <summary>Play Mode ground autotile debug mode (F3 cycles; F8 toggles <see cref="GroundAutotileDebugMode.VisualOverrideEdit"/>).</summary>
    public GroundAutotileDebugMode GroundAutotileDebugMode => groundAutotileDebugMode;

    /// <summary>True when interactive visual override editing is active.</summary>
    public bool IsVisualOverrideEditActive => GroundAutotileDebugModes.IsVisualOverrideEdit(groundAutotileDebugMode);

    /// <summary>Inspector flag for debug override features (tile edit, visual override mode, MCP writes).</summary>
    public bool DebugOverrideModeEnabled => debugOverrideModeEnabled;

    /// <summary>True only when debug override mode is explicitly enabled in an Editor or development build.</summary>
    public bool IsDebugOverrideModeEnabled => CanUseDebugOverrides(debugOverrideModeEnabled);

    /// <summary>
    /// World Y below which autotile masks treat neighbors as air (tile-viz off-space parity).
    /// </summary>
    public int AutotileExposureFloorY => autotileExposureFloorY;

    /// <summary>Number of chunks with active renderers.</summary>
    public int LoadedChunkCount => renderers.Count;

    /// <summary>
    /// Coordinates of chunks in the streamed (renderer-backed) window. The grass simulation scans
    /// only these so growth is bounded to the loaded set and never generates chunks on query.
    /// </summary>
    public IEnumerable<Vector2Int> LoadedChunkCoords => renderers.Keys;

    /// <summary>Build/runtime gate for tile-edit overrides, debug persistence shortcuts, and MCP writes.</summary>
    public static bool CanUseDebugOverrides(bool requested)
    {
        if (!requested)
        {
            return false;
        }

#if UNITY_EDITOR
        return true;
#else
        return Debug.isDebugBuild;
#endif
    }

    /// <summary>Whether loaded override metadata is affecting rendering.</summary>
    public bool ShouldApplyDebugVisualOverrides => autotileVisualOverrides.HasOverrides;

    /// <summary>Returns the current player world position when a player target is assigned.</summary>
    public bool TryGetPlayerWorldPosition(out Vector2 position)
    {
        if (playerTarget == null)
        {
            position = default;
            return false;
        }

        position = ReadPlayerPose();
        return true;
    }

    public void SetPlayerTarget(Transform target)
    {
        playerTarget = target;
    }

    /// <summary>Registers the player inventory that joins this world's save payload.</summary>
    public void SetPlayerInventory(SandboxInventory inventory)
    {
        playerInventory = inventory;
    }

    /// <summary>The transform chunk loading follows; also the chase target for enemies.</summary>
    public Transform PlayerTarget => playerTarget;

    private void Start()
    {
        groundAutotileDebugMode = GroundAutotileDebugModes.Normalize(groundAutotileDebugMode);
        if (!TryGetComponent(out SandboxGroundAutotileDebugHud _))
        {
            gameObject.AddComponent<SandboxGroundAutotileDebugHud>();
        }

#if UNITY_EDITOR
        if (!debugOverrideModeEnabled)
        {
            Debug.LogWarning(
                "SandboxWorld: debugOverrideModeEnabled is off — mouse tile editing, F8 visual overrides, and MCP writes are disabled. Enable it on SandboxWorld in the Inspector.");
        }
#endif

        RefreshLoadedChunks();
        RebuildDirtyChunks();
    }

    private void Update()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && Keyboard.current.f3Key.wasPressedThisFrame)
        {
            CycleGroundAutotileDebugMode();
        }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetKeyDown(KeyCode.F3))
        {
            CycleGroundAutotileDebugMode();
        }
#endif

        if (Time.time >= nextChunkRefreshTime)
        {
            RefreshLoadedChunks();
            nextChunkRefreshTime = Time.time + chunkRefreshInterval;
        }

        RebuildDirtyChunks();
    }

    /// <summary>
    /// F8 shortcut: toggle interactive visual override editing (first F3 mode after Off).
    /// </summary>
    public void ToggleVisualOverrideEditMode()
    {
        if (GroundAutotileDebugModes.IsVisualOverrideEdit(groundAutotileDebugMode))
        {
            SetGroundAutotileDebugMode(GroundAutotileDebugMode.Off);
            return;
        }

        if (!IsDebugOverrideModeEnabled)
        {
            Debug.Log(VisualOverrideModeLog.UnavailableMessage);
            return;
        }

        SetGroundAutotileDebugMode(GroundAutotileDebugMode.VisualOverrideEdit);
    }

    private void CycleGroundAutotileDebugMode()
    {
        GroundAutotileDebugMode next = GroundAutotileDebugModes.Cycle(groundAutotileDebugMode);
        if (next == GroundAutotileDebugMode.VisualOverrideEdit && !IsDebugOverrideModeEnabled)
        {
            next = GroundAutotileDebugModes.Cycle(next);
        }

        SetGroundAutotileDebugMode(next);
    }

    private void SetGroundAutotileDebugMode(GroundAutotileDebugMode mode)
    {
        mode = GroundAutotileDebugModes.Normalize(mode);
        if (groundAutotileDebugMode == mode)
        {
            return;
        }

        groundAutotileDebugMode = mode;
        Debug.Log(GroundAutotileDebugModes.FormatLogLine(groundAutotileDebugMode));
        MarkAllLoadedRenderersDirty();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        groundAutotileDebugMode = GroundAutotileDebugModes.Normalize(groundAutotileDebugMode);
        if (Application.isPlaying)
        {
            MarkAllLoadedRenderersDirty();
        }
    }
#endif

    public SandboxTile GetTile(int x, int y)
    {
        Vector2Int chunkCoord = WorldToChunkCoord(x, y);
        SandboxChunk chunk = GetOrCreateChunk(chunkCoord);
        Vector2Int local = WorldToLocalCoord(x, y);
        return chunk.GetLocalTile(local.x, local.y);
    }

    /// <summary>
    /// Raised for every <see cref="SetTile"/> edit with the edited world tile coordinate, so the
    /// liquid simulation can re-wake the affected fluid cells (P2-FLUID-001 FR3). Waking is done
    /// here, in the single edit choke point, rather than synchronously simulating flow inside the
    /// edit. No subscribers means no cost.
    /// </summary>
    public event System.Action<int, int> TileFluidWakeRequested;

    public bool TryGetVisualOverride(
        int x,
        int y,
        AutotileVisualLayer layer,
        string tilesetName,
        out AutotileVisualOverride visualOverride)
    {
        return autotileVisualOverrides.TryGetOverride(new Vector2Int(x, y), layer, tilesetName, out visualOverride);
    }

    public bool TryGetVisualOverride(int x, int y, out AutotileVisualOverride visualOverride)
    {
        SandboxTile tile = GetTile(x, y);
        if (AutotileGroundResolve.TryResolve(
                tileVisualCatalog,
                GetTile,
                tile,
                x,
                y,
                out AutotileGroundResolveResult ground,
                autotileExposureFloorY)
            && ground.HasGroundTileset
            && autotileVisualOverrides.TryGetOverride(
                new Vector2Int(x, y),
                AutotileVisualLayer.Ground,
                ground.TilesetName,
                out visualOverride))
        {
            return true;
        }

        visualOverride = null;
        return false;
    }

    public void SetVisualOverride(AutotileVisualOverride entry)
    {
        if (entry == null)
        {
            throw new ArgumentNullException(nameof(entry));
        }

        autotileVisualOverrides.SetOverride(entry);
        MarkTileVisualDirty(entry.x, entry.y);
    }

    public void SetVisualOverride(
        int x,
        int y,
        AutotileVisualLayer layer,
        string tilesetName,
        string overrideSpriteId,
        bool overrideFlipX = false,
        bool overrideFlipY = false,
        int rotationDegrees = 0,
        string note = null,
        bool captureAutoSnapshot = true)
    {
        string autoSpriteId = string.Empty;
        bool autoFlipX = false;
        if (captureAutoSnapshot && TryResolveAutoVisual(x, y, layer, out string resolvedSpriteId, out bool resolvedFlipX, out _))
        {
            autoSpriteId = resolvedSpriteId ?? string.Empty;
            autoFlipX = resolvedFlipX;
        }

        SetVisualOverride(new AutotileVisualOverride(
            new Vector2Int(x, y),
            layer,
            tilesetName,
            autoSpriteId,
            autoFlipX,
            overrideSpriteId,
            overrideFlipX,
            overrideFlipY,
            rotationDegrees,
            note));
    }

    public void SetVisualOverride(int x, int y, string spriteId, bool flipX)
    {
        SandboxTile tile = GetTile(x, y);
        if (!tileVisualCatalog.TryGetGroundTileset(tile.id, out AutotileTileset tileset))
        {
            return;
        }

        SetVisualOverride(x, y, AutotileVisualLayer.Ground, tileset.Name, spriteId, flipX);
    }

    public bool ClearVisualOverride(int x, int y, AutotileVisualLayer layer, string tilesetName)
    {
        bool removed = autotileVisualOverrides.ClearOverride(x, y, AutotileVisualLayerNames.ToName(layer), tilesetName);
        if (removed)
        {
            MarkTileVisualDirty(x, y);
        }

        return removed;
    }

    public bool ClearVisualOverride(int x, int y)
    {
        bool hadAny = false;
        SandboxTile tile = GetTile(x, y);
        if (tileVisualCatalog.TryGetGroundTileset(tile.id, out AutotileTileset groundTileset))
        {
            hadAny |= ClearVisualOverride(x, y, AutotileVisualLayer.Ground, groundTileset.Name);
        }

        if (tileVisualCatalog.TryGetCoverTileset(tile.id, out AutotileTileset coverTileset))
        {
            hadAny |= ClearVisualOverride(x, y, AutotileVisualLayer.Cover, coverTileset.Name);
        }

        return hadAny;
    }

    public string SaveVisualOverridesToPath(string path)
    {
        VisualOverridePersistence.WriteToPath(path, autotileVisualOverrides);
        return path;
    }

    public bool TryResolveAutoVisual(
        int x,
        int y,
        AutotileVisualLayer layer,
        out string spriteId,
        out bool flipX,
        out string tilesetName)
    {
        spriteId = null;
        flipX = false;
        tilesetName = null;
        SandboxTile tile = GetTile(x, y);
        if (!tile.IsSolid || tileVisualCatalog == null)
        {
            return false;
        }

        if (layer == AutotileVisualLayer.Ground)
        {
            if (!AutotileGroundResolve.TryResolve(
                    tileVisualCatalog,
                    GetTile,
                    tile,
                    x,
                    y,
                    out AutotileGroundResolveResult ground,
                    autotileExposureFloorY)
                || !ground.Resolved)
            {
                return false;
            }

            spriteId = ground.SpriteId;
            flipX = ground.FlipX;
            tilesetName = ground.TilesetName;
            return true;
        }

        SandboxTile tileAbove = GetTile(x, y + 1);
        if (!tileVisualCatalog.ShouldRenderGrassCover(tile.id, tileAbove)
            || !tileVisualCatalog.TryGetCoverTileset(tile.id, out AutotileTileset coverTileset))
        {
            return false;
        }

        int[,] mask = AutotileMaskBuilder.BuildCoverMask(
            (nx, ny) => GetTile(nx, ny).IsSolid,
            x,
            y);
        spriteId = AutotileResolver.ResolveSpriteId(coverTileset, mask, out flipX);
        tilesetName = coverTileset.Name;
        return !string.IsNullOrEmpty(spriteId);
    }

    public bool TrySetDebugOverrideTile(int x, int y, int tileId)
    {
        if (!IsDebugOverrideModeEnabled)
        {
            return false;
        }

        SetTile(x, y, tileId);
        return true;
    }

    /// <summary>
    /// Single choke point for every gameplay tile edit. Placing and breaking both route here:
    /// breaking is an edit to air. The edit resolves the owning chunk (generating it on demand),
    /// applies the change through <see cref="ApplyTileEdit"/> so tile data, dirty flags, and loaded
    /// border-neighbor chunks all update together, then ensures a renderer exists for the owning
    /// chunk so the change becomes visible.
    /// </summary>
    public void SetTile(int x, int y, int tileId)
    {
        Vector2Int chunkCoord = WorldToChunkCoord(x, y);
        SandboxChunk chunk = GetOrCreateChunk(chunkCoord);
        Vector2Int local = WorldToLocalCoord(x, y);
        ApplyTileEdit(chunks, chunk, local.x, local.y, new SandboxTile(tileId));
        EnsureRenderer(chunkCoord);
        SandboxLightSolver.RelightAfterEdit(GetLightGrid(), x, y);
        TileFluidWakeRequested?.Invoke(x, y);
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
    /// Rolls the load-time grass-loss chance for a persisted tile: with probability
    /// <see cref="loadGrassLossChance"/> a grass tile reverts to dirt as a save is loaded, so lawns
    /// recede a little between sessions. Non-grass tiles pass through unchanged. The roll uses a
    /// plain PRNG (not a position hash) so each load is independent. Note this only touches persisted
    /// (edited) chunks — untouched chunks regenerate fresh surface grass from the seed.
    /// </summary>
    private SandboxTile ApplyLoadGrassLoss(SandboxTile tile)
    {
        if (loadGrassLossChance <= 0f || tile.id != SandboxRegistries.GrassIndex)
        {
            return tile;
        }

        loadGrassRng ??= new System.Random();
        if (loadGrassRng.NextDouble() >= loadGrassLossChance)
        {
            return tile;
        }

        return new SandboxTile(SandboxRegistries.DirtIndex, tile.light, tile.fluid, tile.metadata);
    }

    /// <summary>Current fluid amount (0.0–1.0+ under pressure) at a world tile coordinate.</summary>
    public float GetTileFluid(int x, int y)
    {
        return GetTile(x, y).fluid;
    }

    /// <summary>
    /// Writes the fluid amount at a world tile coordinate, leaving the tile id untouched. Used by
    /// <see cref="ProjectTwelve.Sandbox.Fluid.SandboxWorldFluidGrid"/>; see
    /// <see cref="SandboxChunk.SetLocalFluid"/> for why fluid writes do not mark chunks dirty.
    /// </summary>
    public void SetTileFluid(int x, int y, float amount)
    {
        Vector2Int chunkCoord = WorldToChunkCoord(x, y);
        SandboxChunk chunk = GetOrCreateChunk(chunkCoord);
        Vector2Int local = WorldToLocalCoord(x, y);
        chunk.SetLocalFluid(local.x, local.y, amount);
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
            tilePalette = RegistryPalette.Capture(SandboxRegistries.Tiles),
            inventory = playerInventory?.ToSaveData()
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
                    SandboxTile savedTile = pair.Value.GetLocalTile(x, y);
                    savedTile.light = 0;
                    chunkData.edits.Add(new SandboxTileEditData(x, y, savedTile));
                }
            }

            saveData.chunks.Add(chunkData);
            pair.Value.MarkClean();
        }

        if (playerTarget != null)
        {
            Vector2 pose = ReadPlayerPose();
            saveData.hasPlayerPosition = true;
            saveData.playerX = pose.x;
            saveData.playerY = pose.y;
        }

        string directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(path, JsonUtility.ToJson(saveData, true));
        SaveVisualOverrideSidecar(path);
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

        if (saveData.inventory != null && playerInventory != null)
        {
            playerInventory.LoadFromSaveData(saveData.inventory);
        }

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
                tile.light = 0;
                tile = ApplyLoadGrassLoss(tile);
                chunk.SetLocalTile(edit.localX, edit.localY, tile, false);
            }

            chunk.MarkHasEdits();
            chunk.MarkClean();
            chunks.Add(chunkData.Coord, chunk);
        }

        RelightAllKnownChunks();

        if (saveData.hasPlayerPosition)
        {
            RestorePlayerPosition(new Vector2(saveData.playerX, saveData.playerY));
            RefreshLoadedChunks();
        }

        MarkAllLoadedRenderersDirty();
        RebuildDirtyChunks();
        if (saveData.hasPlayerPosition)
        {
            ResolvePlayerPoseIfOverlappingSolid();
        }

        autotileVisualOverrides.Clear();
        LoadVisualOverrideSidecar(path);
    }

    public static string GetVisualOverrideSidecarPath(string savePath)
    {
        string directory = Path.GetDirectoryName(savePath);
        string fileName = Path.GetFileNameWithoutExtension(savePath);
        string extension = Path.GetExtension(savePath);
        string sidecarName = $"{fileName}.visual-overrides{extension}";
        return string.IsNullOrEmpty(directory) ? sidecarName : Path.Combine(directory, sidecarName);
    }

    private void SaveVisualOverrideSidecar(string savePath)
    {
        VisualOverridePersistence.WriteToPath(GetVisualOverrideSidecarPath(savePath), autotileVisualOverrides);
    }

    private void LoadVisualOverrideSidecar(string savePath)
    {
        string sidecarPath = GetVisualOverrideSidecarPath(savePath);
        VisualOverridePersistence.ReadFromPath(sidecarPath, autotileVisualOverrides);
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
        WritePlayerPose(position);
    }

    /// <summary>
    /// Reads the authoritative player pose: Rigidbody2D.position when present, else Transform.
    /// </summary>
    private Vector2 ReadPlayerPose()
    {
        if (playerTarget != null && playerTarget.TryGetComponent(out Rigidbody2D body))
        {
            return body.position;
        }

        return playerTarget != null ? (Vector2)playerTarget.position : Vector2.zero;
    }

    /// <summary>
    /// Writes the player pose to both Rigidbody2D and Transform so chunk streaming sees the same
    /// position immediately (Interpolate leaves Transform lagging behind body.position alone).
    /// </summary>
    private void WritePlayerPose(Vector2 position)
    {
        if (playerTarget == null)
        {
            return;
        }

        float z = playerTarget.position.z;
        if (playerTarget.TryGetComponent(out Rigidbody2D body))
        {
            body.position = position;
            body.linearVelocity = Vector2.zero;
        }

        playerTarget.position = new Vector3(position.x, position.y, z);
        Physics2D.SyncTransforms();
    }

    /// <summary>
    /// Lifts the player upward out of solid tiles after load so a one-frame collider gap or a
    /// slightly-penetrating save cannot leave them underground.
    /// </summary>
    private void ResolvePlayerPoseIfOverlappingSolid()
    {
        if (playerTarget == null)
        {
            return;
        }

        Vector2 offset = Vector2.zero;
        Vector2 size = new Vector2(tileSize, tileSize);
        if (playerTarget.TryGetComponent(out BoxCollider2D box))
        {
            offset = box.offset;
            size = box.size;
        }

        Vector2 center = ReadPlayerPose();
        if (!SandboxPlayerLoadPose.TryResolveStandingPose(
                center,
                offset,
                size,
                tileSize,
                (x, y) => GetTile(x, y).IsSolid,
                out Vector2 resolved))
        {
            return;
        }

        WritePlayerPose(resolved);
        RefreshLoadedChunks();
        MarkAllLoadedRenderersDirty();
        RebuildDirtyChunks();
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

        Vector2 pose = ReadPlayerPose();
        return WorldToChunkCoord(
            Mathf.FloorToInt(pose.x / tileSize),
            Mathf.FloorToInt(pose.y / tileSize));
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
            debugOverlays.Remove(coord);
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
            SandboxChunk chunk = chunks[coord];
            SandboxChunkRenderer renderer = renderers[coord];
            renderer.Rebuild(
                chunk,
                tileSize,
                tileMaterial,
                tileVisualCatalog,
                GetTile,
                autotileVisualOverrides,
                autotileExposureFloorY);

            if (debugOverlays.TryGetValue(coord, out SandboxGroundAutotileDebugOverlay overlay))
            {
                overlay.Rebuild(
                    chunk,
                    tileSize,
                    groundAutotileDebugMode,
                    tileVisualCatalog,
                    GetTile,
                    autotileVisualOverrides,
                    autotileExposureFloorY);
            }
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


    private void MarkTileVisualDirty(int x, int y)
    {
        Vector2Int chunkCoord = WorldToChunkCoord(x, y);
        if (chunks.TryGetValue(chunkCoord, out SandboxChunk chunk))
        {
            chunk.NeedsRenderRebuild = true;
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
            RelightChunkAndNeighbors(chunkCoord);
        }

        return chunk;
    }

    internal bool TryGetExistingTileForLighting(int x, int y, out SandboxTile tile)
    {
        Vector2Int chunkCoord = WorldToChunkCoord(x, y);
        if (!chunks.TryGetValue(chunkCoord, out SandboxChunk chunk))
        {
            tile = default;
            return false;
        }

        Vector2Int local = WorldToLocalCoord(x, y);
        tile = chunk.GetLocalTile(local.x, local.y);
        return true;
    }

    internal void SetTileLightForLighting(int x, int y, byte light)
    {
        Vector2Int chunkCoord = WorldToChunkCoord(x, y);
        if (!chunks.TryGetValue(chunkCoord, out SandboxChunk chunk))
        {
            return;
        }

        Vector2Int local = WorldToLocalCoord(x, y);
        chunk.SetLocalLight(local.x, local.y, light);
    }

    /// <summary>
    /// Sunlight enters at the first air cell above the deterministic terrain surface. Known
    /// player-built blockers above that cell suppress the source; probing never creates chunks.
    /// </summary>
    internal bool IsSkySourceForLighting(int x, int y)
    {
        SandboxTerrainGenerator generator = CreateTerrainGenerator();
        if (y != generator.GetSurfaceHeight(x) + 1)
        {
            return false;
        }

        int columnChunkX = WorldToChunkCoord(x, y).x;
        int highestKnownY = y;
        foreach (Vector2Int coord in chunks.Keys)
        {
            if (coord.x == columnChunkX)
            {
                highestKnownY = Mathf.Max(highestKnownY, coord.y * SandboxChunk.Size + SandboxChunk.Size - 1);
            }
        }

        for (int scanY = y + 1; scanY <= highestKnownY; scanY++)
        {
            if (TryGetExistingTileForLighting(x, scanY, out SandboxTile above)
                && SandboxRegistries.Tiles.Get(above.id).Opaque)
            {
                return false;
            }
        }

        return true;
    }

    private SandboxWorldLightGrid GetLightGrid()
    {
        lightGrid ??= new SandboxWorldLightGrid(this);
        return lightGrid;
    }

    private void RelightChunkAndNeighbors(Vector2Int chunkCoord)
    {
        SandboxLightSolver.RelightAfterChunkLoad(
            GetLightGrid(),
            chunkCoord.x,
            chunkCoord.y,
            SandboxChunk.Size);
    }

    private void RelightAllKnownChunks()
    {
        List<Vector2Int> coords = new List<Vector2Int>(chunks.Keys);
        coords.Sort((a, b) => a.x != b.x ? a.x.CompareTo(b.x) : a.y.CompareTo(b.y));
        foreach (Vector2Int coord in coords)
        {
            RelightChunkAndNeighbors(coord);
        }
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
        debugOverlays[chunkCoord] = EnsureDebugOverlay(chunkRenderer);
        chunk.NeedsRenderRebuild = true;
        chunk.NeedsColliderRebuild = true;
        MarkRenderedFaceNeighborsDirty(chunkCoord, chunks, renderers.Keys);
    }

    private static SandboxGroundAutotileDebugOverlay EnsureDebugOverlay(SandboxChunkRenderer chunkRenderer)
    {
        Transform existing = chunkRenderer.transform.Find("GroundAutotileDebug");
        if (existing != null && existing.TryGetComponent(out SandboxGroundAutotileDebugOverlay overlay))
        {
            return overlay;
        }

        GameObject debugObject = new GameObject("GroundAutotileDebug");
        debugObject.transform.SetParent(chunkRenderer.transform, false);
        return debugObject.AddComponent<SandboxGroundAutotileDebugOverlay>();
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
