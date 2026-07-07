using System;
using System.Collections.Generic;
using ProjectTwelve.Visual.Tiles;
using UnityEngine;

/// <summary>
/// Sidecar-only persistence contract for presentation metadata that must not affect simulation
/// save/load: tile ids, palette remapping, chunk edits, and player position remain owned by
/// SandboxSaveData.
/// </summary>
[Serializable]
public sealed class SandboxVisualOverrideSaveData
{
    public int visualOverridesVersion = 1;
    public List<SandboxVisualOverrideEntrySaveData> visualOverrides = new List<SandboxVisualOverrideEntrySaveData>();

    // Legacy JsonUtility field kept for migration reads only.
    public int version;
    public List<SandboxVisualOverrideEntrySaveData> overrides;

    public bool HasOverrides =>
        (visualOverrides != null && visualOverrides.Count > 0)
        || (overrides != null && overrides.Count > 0);
}

/// <summary>
/// JSON-facing sidecar entry for a single sandbox visual override.
/// </summary>
[Serializable]
public sealed class SandboxVisualOverrideEntrySaveData
{
    public int x;
    public int y;
    public string layer = AutotileVisualLayerNames.Ground;
    public string tileset;
    public string autoSpriteId;
    public bool autoFlipX;
    public string overrideSpriteId;

    // Legacy int sprite id (pre-unification sidecars).
    public int spriteId;

    public bool overrideFlipX;
    public bool overrideFlipY;
    public bool flipX;
    public int rotation;
    public string note;

    public Vector2Int ToCoord()
    {
        return new Vector2Int(x, y);
    }
}
