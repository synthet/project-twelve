using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Sidecar-only persistence contract for presentation metadata that must not affect simulation
/// save/load: tile ids, palette remapping, chunk edits, and player position remain owned by
/// SandboxSaveData.
/// </summary>
[Serializable]
public sealed class SandboxVisualOverrideSaveData
{
    public int version = 1;
    public List<SandboxVisualOverrideEntrySaveData> overrides = new List<SandboxVisualOverrideEntrySaveData>();

    public bool HasOverrides => overrides != null && overrides.Count > 0;
}

/// <summary>
/// JSON-facing sidecar entry for a single sandbox visual override.
/// Keep coordinates as top-level integers so saved JSON has machine-readable x/y fields.
/// </summary>
[Serializable]
public sealed class SandboxVisualOverrideEntrySaveData
{
    public int x;
    public int y;
    public int spriteId;
    public bool flipX;

    public Vector2Int ToCoord()
    {
        return new Vector2Int(x, y);
    }

    public static SandboxVisualOverrideEntrySaveData FromRuntime(Vector2Int coord, int spriteId, bool flipX = false)
    {
        return new SandboxVisualOverrideEntrySaveData
        {
            x = coord.x,
            y = coord.y,
            spriteId = spriteId,
            flipX = flipX
        };
    }
}
