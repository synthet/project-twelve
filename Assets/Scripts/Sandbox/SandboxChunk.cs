using UnityEngine;

public sealed class SandboxChunk
{
    public const int Size = 32;

    private readonly SandboxTile[,] tiles = new SandboxTile[Size, Size];

    public Vector2Int Coord { get; }
    public bool NeedsRenderRebuild { get; set; }
    public bool NeedsColliderRebuild { get; set; }
    public bool IsDirty { get; set; }
    public bool HasEdits { get; private set; }

    /// <summary>
    /// Monotonic navigation version, bumped on every tile mutation. Agents snapshot the versions
    /// of the chunks their path crosses and treat a version change as nav-dirty (recompute lazily
    /// on their next step). A counter replaces a clearable dirty bool because multiple agents
    /// observe the same edit and must not race to clear a shared flag.
    /// </summary>
    public int NavVersion { get; private set; }

    public SandboxTile[,] Tiles => tiles;

    public SandboxChunk(Vector2Int coord)
    {
        Coord = coord;
        NeedsRenderRebuild = true;
        NeedsColliderRebuild = true;
    }

    public SandboxTile GetLocalTile(int x, int y)
    {
        if (!IsLocalInBounds(x, y))
        {
            return default;
        }

        return tiles[x, y];
    }

    public void SetLocalTile(int x, int y, SandboxTile tile)
    {
        SetLocalTile(x, y, tile, true);
    }

    public void SetLocalTile(int x, int y, SandboxTile tile, bool markDirty)
    {
        if (!IsLocalInBounds(x, y))
        {
            return;
        }

        tiles[x, y] = tile;
        NeedsRenderRebuild = true;
        NeedsColliderRebuild = true;
        NavVersion++;
        IsDirty |= markDirty;
        HasEdits |= markDirty;
    }

    /// <summary>
    /// Marks the chunk nav-dirty without a tile write. Used for border edits in neighbor chunks
    /// (jump/fall arcs cross chunk borders) and for load/unload transitions that change the
    /// loaded-set membership paths depend on.
    /// </summary>
    public void BumpNavVersion()
    {
        NavVersion++;
    }

    public void MarkClean()
    {
        IsDirty = false;
    }

    public void MarkHasEdits()
    {
        HasEdits = true;
    }

    public static bool IsLocalInBounds(int x, int y)
    {
        return x >= 0 && x < Size && y >= 0 && y < Size;
    }
}
