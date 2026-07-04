using System.Collections.Generic;
using ProjectTwelve.Sandbox.Nav;
using UnityEngine;

/// <summary>
/// Pure in-memory nav grid for EditMode fixtures, implementing the same solidity rules as the
/// world without a Unity scene. Built from top-down string rows ('#' solid, '.' air); world
/// origin is the bottom-left cell (0, 0). Out-of-bounds cells report unloaded, chunks can be
/// marked unloaded explicitly, and cell mutations bump the owning chunk's nav version exactly
/// like <see cref="SandboxChunk.NavVersion"/>.
/// </summary>
public sealed class NavTestGrid : ISandboxNavGrid, ISandboxNavVersionSource
{
    private readonly bool[,] solid;
    private readonly int width;
    private readonly int height;
    private readonly Dictionary<Vector2Int, int> navVersions = new Dictionary<Vector2Int, int>();
    private readonly HashSet<Vector2Int> unloadedChunks = new HashSet<Vector2Int>();

    public NavTestGrid(params string[] topDownRows)
    {
        height = topDownRows.Length;
        width = topDownRows[0].Length;
        solid = new bool[width, height];
        for (int row = 0; row < height; row++)
        {
            for (int x = 0; x < width; x++)
            {
                solid[x, height - 1 - row] = topDownRows[row][x] == '#';
            }
        }
    }

    /// <summary>Creates a flat world: solid at and below <paramref name="groundY"/>, air above.</summary>
    public static NavTestGrid Flat(int width, int height, int groundY)
    {
        var rows = new string[height];
        for (int row = 0; row < height; row++)
        {
            int y = height - 1 - row;
            rows[row] = new string(y <= groundY ? '#' : '.', width);
        }

        return new NavTestGrid(rows);
    }

    public bool IsSolid(int x, int y)
    {
        if (x < 0 || x >= width || y < 0 || y >= height)
        {
            return true;
        }

        return solid[x, y];
    }

    public bool IsLoaded(int x, int y)
    {
        if (x < 0 || x >= width || y < 0 || y >= height)
        {
            return false;
        }

        return !unloadedChunks.Contains(SandboxWorld.WorldToChunkCoord(x, y));
    }

    public int GetNavVersion(Vector2Int chunkCoord)
    {
        return navVersions.TryGetValue(chunkCoord, out int version) ? version : 0;
    }

    /// <summary>Mutates a cell and bumps the owning chunk's nav version, like a SetTile edit.</summary>
    public void SetCell(int x, int y, bool isSolid)
    {
        solid[x, y] = isSolid;
        Vector2Int chunkCoord = SandboxWorld.WorldToChunkCoord(x, y);
        navVersions[chunkCoord] = GetNavVersion(chunkCoord) + 1;
    }

    public void MarkChunkUnloaded(Vector2Int chunkCoord)
    {
        unloadedChunks.Add(chunkCoord);
    }
}
