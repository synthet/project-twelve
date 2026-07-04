using System.Collections.Generic;
using ProjectTwelve.Sandbox.Fluid;
using UnityEngine;

/// <summary>
/// Pure in-memory fluid grid for EditMode fixtures, implementing the same solidity/loaded contract
/// as <see cref="SandboxWorldFluidGrid"/> without a Unity scene. Solidity can be built from top-down
/// string rows ('#' solid, '.' air); world origin is the bottom-left cell (0, 0). Out-of-bounds and
/// unloaded cells report solid so flow never crosses the boundary, exactly like the world adapter.
/// </summary>
public sealed class FluidTestGrid : ISandboxFluidGrid
{
    private readonly bool[,] solid;
    private readonly float[,] fluid;
    private readonly int width;
    private readonly int height;
    private readonly HashSet<Vector2Int> unloadedChunks = new HashSet<Vector2Int>();

    public int Width => width;
    public int Height => height;

    public FluidTestGrid(int width, int height)
    {
        this.width = width;
        this.height = height;
        solid = new bool[width, height];
        fluid = new float[width, height];
    }

    public FluidTestGrid(params string[] topDownRows)
        : this(topDownRows[0].Length, topDownRows.Length)
    {
        for (int row = 0; row < height; row++)
        {
            for (int x = 0; x < width; x++)
            {
                solid[x, height - 1 - row] = topDownRows[row][x] == '#';
            }
        }
    }

    public bool IsSolid(int x, int y)
    {
        if (!IsLoaded(x, y))
        {
            return true; // OOB and unloaded cells block flow, matching the world adapter.
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

    public float GetFluid(int x, int y)
    {
        if (x < 0 || x >= width || y < 0 || y >= height)
        {
            return 0f;
        }

        return fluid[x, y];
    }

    public void SetFluid(int x, int y, float amount)
    {
        if (x < 0 || x >= width || y < 0 || y >= height)
        {
            return;
        }

        fluid[x, y] = amount;
    }

    /// <summary>Directly sets solidity of a cell (e.g. simulating a tile edit) without waking it.</summary>
    public void SetSolid(int x, int y, bool isSolid)
    {
        solid[x, y] = isSolid;
    }

    public void MarkChunkUnloaded(Vector2Int chunkCoord)
    {
        unloadedChunks.Add(chunkCoord);
    }

    public float TotalMass()
    {
        double total = 0;
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                total += fluid[x, y];
            }
        }

        return (float)total;
    }

    /// <summary>Total fluid mass in a single column (all rows at world x).</summary>
    public float ColumnMass(int x)
    {
        double total = 0;
        for (int y = 0; y < height; y++)
        {
            total += fluid[x, y];
        }

        return (float)total;
    }
}
