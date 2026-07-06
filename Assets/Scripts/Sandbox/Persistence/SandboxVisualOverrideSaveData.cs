using System;
using UnityEngine;

/// <summary>
/// JSON-facing sidecar DTO for a single sandbox visual override.
/// Keep coordinates as top-level integers so saved JSON has machine-readable x/y fields.
/// </summary>
[Serializable]
public sealed class SandboxVisualOverrideSaveData
{
    public int x;
    public int y;
    public int spriteId;
    public bool flipX;

    public Vector2Int ToCoord()
    {
        return new Vector2Int(x, y);
    }

    public static SandboxVisualOverrideSaveData FromRuntime(Vector2Int coord, int spriteId, bool flipX = false)
    {
        return new SandboxVisualOverrideSaveData
        {
            x = coord.x,
            y = coord.y,
            spriteId = spriteId,
            flipX = flipX
        };
    }
}
