using System;
using System.Collections.Generic;
using ProjectTwelve.Sandbox.Registry;
using UnityEngine;

[Serializable]
public sealed class SandboxSaveData
{
    public int version = 1;
    public int seed;
    public bool hasPlayerPosition;
    public float playerX;
    public float playerY;
    public List<SandboxChunkSaveData> chunks = new List<SandboxChunkSaveData>();

    // Tile identity palette (string ID → runtime index at save time). Absent or empty on
    // version-1 prototype saves, whose tile ids are the fixed legacy numbering instead
    // (JsonUtility auto-instantiates this field, so "legacy" means entries is empty, not null).
    // The binary header slot for this palette is owned by P2-SAVE-001.
    public RegistryPalette tilePalette;

    /// <summary>True when the save carries a tile palette (post-P2-DATA-002 saves).</summary>
    public bool HasTilePalette => tilePalette != null && tilePalette.entries != null && tilePalette.entries.Count > 0;
}

[Serializable]
public sealed class SandboxChunkSaveData
{
    public int x;
    public int y;
    public List<SandboxTileEditData> edits = new List<SandboxTileEditData>();

    public Vector2Int Coord => new Vector2Int(x, y);
}

[Serializable]
public struct SandboxTileEditData
{
    public int localX;
    public int localY;
    public SandboxTile tile;

    public SandboxTileEditData(int localX, int localY, SandboxTile tile)
    {
        this.localX = localX;
        this.localY = localY;
        this.tile = tile;
    }
}
