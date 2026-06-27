using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class SandboxSaveData
{
    public int version = 1;
    public int seed;
    public List<SandboxChunkSaveData> chunks = new List<SandboxChunkSaveData>();
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
