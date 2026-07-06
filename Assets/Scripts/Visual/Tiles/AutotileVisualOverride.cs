using System;
using UnityEngine;

/// <summary>
/// Runtime representation of a persisted autotile visual override for a single world tile.
/// </summary>
[Serializable]
public sealed class AutotileVisualOverride
{
    public Vector2Int coord;
    public int spriteId;
    public bool flipX;

    public AutotileVisualOverride()
    {
    }

    public AutotileVisualOverride(Vector2Int coord, int spriteId, bool flipX = false)
    {
        this.coord = coord;
        this.spriteId = spriteId;
        this.flipX = flipX;
    }

    public SandboxVisualOverrideEntrySaveData ToSaveData()
    {
        return SandboxVisualOverrideEntrySaveData.FromRuntime(coord, spriteId, flipX);
    }

    public static AutotileVisualOverride FromSaveData(SandboxVisualOverrideEntrySaveData saveData)
    {
        return new AutotileVisualOverride(saveData.ToCoord(), saveData.spriteId, saveData.flipX);
    }
}
