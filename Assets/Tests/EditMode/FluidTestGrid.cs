using ProjectTwelve.Sandbox.Fluid;
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Pure in-memory fluid grid for EditMode fixtures, implementing the same solidity + loaded rules
/// as the world without a Unity scene. Solidity is built from top-down string rows ('#' solid, '.'
/// air); world origin is the bottom-left cell (0, 0). Out-of-bounds cells report unloaded (walls),
/// chunks can be marked unloaded explicitly, and fluid amounts live in a parallel float grid.
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

    public FluidTestGrid(params string[] topDownRows)
    {
        height = topDownRows.Length;
        width = topDownRows[0].Length;
        solid = new bool[width, height];
        fluid = new float[width, height];
        for (int row = 0; row < height; row++)
        {
            for (int x = 0; x < width; x++)
            {
                solid[x, height - 1 - row] = topDownRows[row][x] == '#';
            }
        }
    }

    /// <summary>Solid floor at y = 0, open air above, no side walls (bounds act as walls).</summary>
    public static FluidTestGrid Flat(int width, int height)
    {
        var rows = new string[height];
        for (int row = 0; row < height; row++)
        {
            int y = height - 1 - row;
            rows[row] = new string(y == 0 ? '#' : '.', width);
        }

        return new FluidTestGrid(rows);
    }

    public bool IsSolid(int x, int y)
    {
        if (!InBounds(x, y))
        {
            return true;
        }

        return solid[x, y];
    }

    public bool IsLoaded(int x, int y)
    {
        if (!InBounds(x, y))
        {
            return false;
        }

        return !unloadedChunks.Contains(SandboxWorld.WorldToChunkCoord(x, y));
    }

    public float GetFluid(int x, int y)
    {
        return InBounds(x, y) ? fluid[x, y] : 0f;
    }

    public void SetFluid(int x, int y, float amount)
    {
        if (InBounds(x, y))
        {
            fluid[x, y] = amount;
        }
    }

    /// <summary>Directly seeds a cell's fluid for fixture setup (not a simulation transfer).</summary>
    public void PlaceFluid(int x, int y, float amount)
    {
        SetFluid(x, y, amount);
    }

    /// <summary>Flips a cell's solidity, e.g. to carve a floor out from under a settled pool.</summary>
    public void SetSolid(int x, int y, bool isSolid)
    {
        if (InBounds(x, y))
        {
            solid[x, y] = isSolid;
        }
    }

    public void MarkChunkUnloaded(Vector2Int chunkCoord)
    {
        unloadedChunks.Add(chunkCoord);
    }

    /// <summary>Total fluid across every cell — the mass-conservation invariant.</summary>
    public float TotalFluid()
    {
        float total = 0f;
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                total += fluid[x, y];
            }
        }

        return total;
    }

    /// <summary>Total fluid in a single column, used to compare surface levels after settling.</summary>
    public float ColumnFluid(int x)
    {
        float total = 0f;
        for (int y = 0; y < height; y++)
        {
            total += GetFluid(x, y);
        }

        return total;
    }

    /// <summary>Highest y whose fluid is at least <paramref name="threshold"/>, or -1 if none.</summary>
    public int TopFluidRow(int x, float threshold)
    {
        for (int y = height - 1; y >= 0; y--)
        {
            if (GetFluid(x, y) >= threshold)
            {
                return y;
            }
        }

        return -1;
    }

    private bool InBounds(int x, int y)
    {
        return x >= 0 && x < width && y >= 0 && y < height;
    }
}
