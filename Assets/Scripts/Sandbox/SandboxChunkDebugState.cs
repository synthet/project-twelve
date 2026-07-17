using UnityEngine;

/// <summary>
/// Read-only snapshot of a generated chunk's bookkeeping flags for debug tooling (P2-TOOL-001).
/// A copy-by-value struct instead of the live <see cref="SandboxChunk"/> so inspectors and MCP
/// tools can never flip flags or write tiles — debug reads must not create a second edit path.
/// </summary>
public readonly struct SandboxChunkDebugState
{
    /// <summary>Chunk coordinate this snapshot was taken from.</summary>
    public Vector2Int Coord { get; }

    /// <summary>True when the chunk is in the streamed (renderer-backed) window.</summary>
    public bool RendererLoaded { get; }

    /// <summary>Pending mesh rebuild flag at snapshot time.</summary>
    public bool NeedsRenderRebuild { get; }

    /// <summary>Pending collider rebuild flag at snapshot time.</summary>
    public bool NeedsColliderRebuild { get; }

    /// <summary>True when the chunk has unsaved edits (save-diff status).</summary>
    public bool IsSaveDirty { get; }

    /// <summary>True when the chunk has ever been edited (joins the save payload).</summary>
    public bool HasEdits { get; }

    /// <summary>Monotonic navigation version (see <see cref="SandboxChunk.NavVersion"/>).</summary>
    public int NavVersion { get; }

    public SandboxChunkDebugState(
        Vector2Int coord,
        bool rendererLoaded,
        bool needsRenderRebuild,
        bool needsColliderRebuild,
        bool isSaveDirty,
        bool hasEdits,
        int navVersion)
    {
        Coord = coord;
        RendererLoaded = rendererLoaded;
        NeedsRenderRebuild = needsRenderRebuild;
        NeedsColliderRebuild = needsColliderRebuild;
        IsSaveDirty = isSaveDirty;
        HasEdits = hasEdits;
        NavVersion = navVersion;
    }
}
