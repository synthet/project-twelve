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

[Serializable]
public sealed class SandboxVisualOverrideEntrySaveData
{
    public int x;
    public int y;
    public string visualKey;
    public int variant;

    public Vector2Int Coord => new Vector2Int(x, y);
}
