using System;
using System.Collections.Generic;
using NUnit.Framework;
using ProjectTwelve.Sandbox.Lighting;
using UnityEngine;

public sealed class SandboxLightSolverTests
{
    [Test]
    public void RelightAll_FallsOffByManhattanDistanceAndUsesOpaqueCost()
    {
        TestLightGrid grid = TestLightGrid.Rectangle(-4, -4, 4, 4);
        grid.SetSource(0, 0, 15);
        grid.SetAttenuation(1, 0, 3);

        SandboxLightSolver.RelightAll(grid, grid.Bounds);

        Assert.AreEqual(15, grid.LightAt(0, 0));
        Assert.AreEqual(14, grid.LightAt(0, 1));
        Assert.AreEqual(13, grid.LightAt(0, 2));
        Assert.AreEqual(12, grid.LightAt(1, 0), "Entering an opaque tile costs three light levels.");
        Assert.AreEqual(11, grid.LightAt(2, 0));
    }

    [Test]
    public void RelightAll_OverlappingSourcesTakeMaximum()
    {
        TestLightGrid grid = TestLightGrid.Rectangle(-5, -2, 5, 2);
        grid.SetSource(-2, 0, 15);
        grid.SetSource(2, 0, 12);

        SandboxLightSolver.RelightAll(grid, grid.Bounds);

        Assert.AreEqual(13, grid.LightAt(0, 0));
        Assert.AreEqual(12, grid.LightAt(2, 0));
    }

    [Test]
    public void RelightAfterEdit_SealingAndReopeningSkylightClearsAndRestoresLight()
    {
        TestLightGrid grid = TestLightGrid.Rectangle(-20, -20, 20, 20);
        grid.SetSource(0, 0, 15);
        SandboxLightSolver.RelightAll(grid, grid.Bounds);
        byte before = grid.LightAt(7, 0);

        grid.SetSource(0, 0, 0);
        grid.SetAttenuation(0, 0, 3);
        SandboxLightSolver.RelightAfterEdit(grid, 0, 0);
        Assert.AreEqual(0, grid.LightAt(7, 0), "A sealed skylight must not leave stale brightness.");

        grid.SetAttenuation(0, 0, 1);
        grid.SetSource(0, 0, 15);
        SandboxLightSolver.RelightAfterEdit(grid, 0, 0);
        Assert.AreEqual(before, grid.LightAt(7, 0));
    }

    [Test]
    public void RelightAfterEdit_MatchesFullRelightAfterDeterministicEdits()
    {
        TestLightGrid incremental = TestLightGrid.Rectangle(-20, -20, 20, 20);
        TestLightGrid full = TestLightGrid.Rectangle(-20, -20, 20, 20);
        ConfigureDeterministicFixture(incremental);
        ConfigureDeterministicFixture(full);
        SandboxLightSolver.RelightAll(incremental, incremental.Bounds);
        SandboxLightSolver.RelightAll(full, full.Bounds);

        incremental.SetSource(0, 0, 0);
        full.SetSource(0, 0, 0);
        incremental.SetAttenuation(2, 1, 3);
        full.SetAttenuation(2, 1, 3);

        SandboxLightSolver.RelightAfterEdit(incremental, 0, 0);
        SandboxLightSolver.RelightAll(full, full.Bounds);

        foreach (Vector2Int cell in incremental.Cells)
        {
            Assert.AreEqual(full.LightAt(cell.x, cell.y), incremental.LightAt(cell.x, cell.y),
                $"Dirty-window mismatch at {cell}.");
        }
    }

    [Test]
    public void RelightAfterEdit_DoesNotProbeOrCreateUnavailableCells()
    {
        TestLightGrid grid = TestLightGrid.Rectangle(-2, -2, 2, 2);
        grid.ThrowOnUnavailableAccess = true;
        grid.SetSource(-2, 0, 15);

        Assert.DoesNotThrow(() => SandboxLightSolver.RelightAfterEdit(grid, -2, 0));
        Assert.AreEqual(14, grid.LightAt(-1, 0));
    }

    [Test]
    public void RelightAfterChunkLoad_NegativeChunkCoordinatesUseFloorAlignedBounds()
    {
        TestLightGrid grid = TestLightGrid.Rectangle(-40, -2, -20, 2);
        grid.SetSource(-32, 0, 15);

        SandboxLightSolver.RelightAfterChunkLoad(grid, -1, 0, SandboxChunk.Size);

        Assert.AreEqual(15, grid.LightAt(-32, 0));
        Assert.AreEqual(14, grid.LightAt(-31, 0));
        Assert.AreEqual(14, grid.LightAt(-33, 0));
    }

    [Test]
    public void RelightAfterChunkLoad_ConvergesRegardlessOfChunkLoadOrder()
    {
        TestLightGrid leftFirst = TestLightGrid.Rectangle(-32, -2, 31, 2);
        TestLightGrid rightFirst = TestLightGrid.Rectangle(-32, -2, 31, 2);
        leftFirst.SetSource(-1, 0, 15);
        rightFirst.SetSource(-1, 0, 15);

        SandboxLightSolver.RelightAfterChunkLoad(leftFirst, -1, 0, SandboxChunk.Size);
        SandboxLightSolver.RelightAfterChunkLoad(leftFirst, 0, 0, SandboxChunk.Size);
        SandboxLightSolver.RelightAfterChunkLoad(rightFirst, 0, 0, SandboxChunk.Size);
        SandboxLightSolver.RelightAfterChunkLoad(rightFirst, -1, 0, SandboxChunk.Size);

        foreach (Vector2Int cell in leftFirst.Cells)
        {
            Assert.AreEqual(leftFirst.LightAt(cell.x, cell.y), rightFirst.LightAt(cell.x, cell.y),
                $"Load-order mismatch at {cell}.");
        }
    }

    [Test]
    public void RelightAll_SourceAcrossChunkBorderMarksBothChunkMeshesDirty()
    {
        ChunkPairLightGrid grid = new ChunkPairLightGrid();

        SandboxLightSolver.RelightAll(grid, new SandboxLightBounds(0, 0, 63, 0));

        Assert.AreEqual(15, grid.Left.GetLocalTile(31, 0).light);
        Assert.AreEqual(14, grid.Right.GetLocalTile(0, 0).light);
        Assert.IsTrue(grid.Left.NeedsRenderRebuild);
        Assert.IsTrue(grid.Right.NeedsRenderRebuild);
        Assert.IsFalse(grid.Left.NeedsColliderRebuild);
        Assert.IsFalse(grid.Right.NeedsColliderRebuild);
    }

    [Test]
    public void SetLocalLight_MarksOnlyRenderDirty()
    {
        SandboxChunk chunk = new SandboxChunk(new Vector2Int(-1, 0));
        chunk.NeedsRenderRebuild = false;
        chunk.NeedsColliderRebuild = false;
        int navVersion = chunk.NavVersion;

        chunk.SetLocalLight(0, 0, 9);

        Assert.AreEqual(9, chunk.GetLocalTile(0, 0).light);
        Assert.IsTrue(chunk.NeedsRenderRebuild);
        Assert.IsFalse(chunk.NeedsColliderRebuild);
        Assert.IsFalse(chunk.IsDirty);
        Assert.IsFalse(chunk.HasEdits);
        Assert.AreEqual(navVersion, chunk.NavVersion);
    }

    [Test]
    public void RendererLightColor_PreservesConfiguredDarknessFloorAndFullBrightness()
    {
        Color dark = SandboxChunkRenderer.GetTileLightColor(new SandboxTile(SandboxTileIds.Stone, 0));
        Color bright = SandboxChunkRenderer.GetTileLightColor(new SandboxTile(SandboxTileIds.Stone, 15));

        Assert.AreEqual(0.35f, dark.r, 0.0001f);
        Assert.AreEqual(0.35f, dark.g, 0.0001f);
        Assert.AreEqual(1f, bright.r, 0.0001f);
        Assert.AreEqual(1f, bright.g, 0.0001f);
    }

    private static void ConfigureDeterministicFixture(TestLightGrid grid)
    {
        grid.SetSource(0, 0, 15);
        grid.SetSource(8, -3, 12);
        System.Random random = new System.Random(1337);
        for (int i = 0; i < 40; i++)
        {
            grid.SetAttenuation(random.Next(-10, 11), random.Next(-10, 11), 3);
        }
    }

    private sealed class TestLightGrid : ISandboxLightGrid
    {
        private readonly Dictionary<Vector2Int, SandboxTile> tiles = new Dictionary<Vector2Int, SandboxTile>();
        private readonly Dictionary<Vector2Int, byte> sources = new Dictionary<Vector2Int, byte>();
        private readonly Dictionary<Vector2Int, byte> attenuation = new Dictionary<Vector2Int, byte>();

        public SandboxLightBounds Bounds { get; private set; }
        public IEnumerable<Vector2Int> Cells => tiles.Keys;
        public bool ThrowOnUnavailableAccess { get; set; }

        public static TestLightGrid Rectangle(int minX, int minY, int maxX, int maxY)
        {
            TestLightGrid grid = new TestLightGrid
            {
                Bounds = new SandboxLightBounds(minX, minY, maxX, maxY)
            };
            for (int x = minX; x <= maxX; x++)
            {
                for (int y = minY; y <= maxY; y++)
                {
                    grid.tiles.Add(new Vector2Int(x, y), new SandboxTile(SandboxTileIds.Air));
                }
            }

            return grid;
        }

        public bool IsLoaded(int x, int y)
        {
            return tiles.ContainsKey(new Vector2Int(x, y));
        }

        public SandboxTile GetTile(int x, int y)
        {
            Vector2Int cell = new Vector2Int(x, y);
            if (tiles.TryGetValue(cell, out SandboxTile tile))
            {
                return tile;
            }

            if (ThrowOnUnavailableAccess)
            {
                throw new InvalidOperationException($"Unavailable cell was read: {cell}");
            }

            return default;
        }

        public byte GetSourceLight(int x, int y)
        {
            return sources.TryGetValue(new Vector2Int(x, y), out byte source) ? source : (byte)0;
        }

        public byte GetAttenuation(int x, int y)
        {
            return attenuation.TryGetValue(new Vector2Int(x, y), out byte cost) ? cost : (byte)1;
        }

        public void SetLight(int x, int y, byte light)
        {
            Vector2Int cell = new Vector2Int(x, y);
            if (!tiles.TryGetValue(cell, out SandboxTile tile))
            {
                if (ThrowOnUnavailableAccess)
                {
                    throw new InvalidOperationException($"Unavailable cell was written: {cell}");
                }

                return;
            }

            tile.light = light;
            tiles[cell] = tile;
        }

        public void SetSource(int x, int y, byte light)
        {
            sources[new Vector2Int(x, y)] = light;
        }

        public void SetAttenuation(int x, int y, byte cost)
        {
            attenuation[new Vector2Int(x, y)] = cost;
        }

        public byte LightAt(int x, int y)
        {
            return GetTile(x, y).light;
        }
    }

    private sealed class ChunkPairLightGrid : ISandboxLightGrid
    {
        public ChunkPairLightGrid()
        {
            Left = new SandboxChunk(Vector2Int.zero);
            Right = new SandboxChunk(Vector2Int.right);
            Left.NeedsRenderRebuild = false;
            Left.NeedsColliderRebuild = false;
            Right.NeedsRenderRebuild = false;
            Right.NeedsColliderRebuild = false;
        }

        public SandboxChunk Left { get; }
        public SandboxChunk Right { get; }

        public bool IsLoaded(int x, int y)
        {
            return x >= 0 && x < SandboxChunk.Size * 2 && y == 0;
        }

        public SandboxTile GetTile(int x, int y)
        {
            SandboxChunk chunk = x < SandboxChunk.Size ? Left : Right;
            return chunk.GetLocalTile(x % SandboxChunk.Size, y);
        }

        public byte GetSourceLight(int x, int y)
        {
            return x == SandboxChunk.Size - 1 && y == 0 ? (byte)15 : (byte)0;
        }

        public byte GetAttenuation(int x, int y)
        {
            return 1;
        }

        public void SetLight(int x, int y, byte light)
        {
            SandboxChunk chunk = x < SandboxChunk.Size ? Left : Right;
            chunk.SetLocalLight(x % SandboxChunk.Size, y, light);
        }
    }
}
