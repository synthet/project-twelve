using System;
using NUnit.Framework;
using ProjectTwelve.Sandbox.Fluid;
using UnityEngine;

/// <summary>
/// P2-FLUID-001 fixtures on handcrafted grids: constants, mass conservation, settling, U-tube
/// pressure equalization, determinism, edit-driven wake, and the per-tick budget. All pure data
/// tests over <see cref="FluidTestGrid"/> — no scenes.
/// </summary>
public sealed class SandboxFluidSimulatorTests
{
    private const float Epsilon = 1e-3f;

    [Test]
    public void Constants_MatchP2Fluid001SpecTable()
    {
        Assert.AreEqual(1.0f, SandboxFluidConstants.MaxFill);
        Assert.AreEqual(0.02f, SandboxFluidConstants.MaxCompression);
        Assert.AreEqual(1.0f, SandboxFluidConstants.MaxTransferPerTick);
        Assert.AreEqual(0.0001f, SandboxFluidConstants.SettleEpsilon);
        Assert.AreEqual(0.05f, SandboxFluidConstants.MinVisibleFill);
        Assert.AreEqual(4096, SandboxFluidConstants.MaxActiveCellsPerTick);
    }

    [Test]
    public void StableLower_IsContinuousAndMonotonic()
    {
        // Below full: the lower cell wants all of it (top drains to 0).
        Assert.AreEqual(0.5f, SandboxFluidSimulator.StableLower(0.5f), 1e-6f);
        Assert.AreEqual(1.0f, SandboxFluidSimulator.StableLower(1.0f), 1e-6f);

        // Continuous across the compression boundary at total = MaxFill.
        Assert.AreEqual(
            SandboxFluidSimulator.StableLower(1.0f),
            SandboxFluidSimulator.StableLower(1.0f + 1e-5f),
            1e-3f);

        // Two full cells compress: the lower holds more than half so pressure rises.
        float lowerOfTwo = SandboxFluidSimulator.StableLower(2.0f);
        Assert.Greater(lowerOfTwo, 1.0f, "a stacked pair compresses the lower cell above MaxFill");
        Assert.Less(lowerOfTwo, 2.0f);
    }

    [Test]
    public void FluidFalls_ThroughAirToTheFloor()
    {
        // A one-wide well so the water has nowhere to spread — it must land on the floor row.
        FluidTestGrid grid = FluidTestGrid.Flat(1, 8);
        var sim = new SandboxFluidSimulator(grid);
        sim.AddFluid(0, 6, 1.0f);

        SettleUntilAsleep(sim, 500);

        Assert.AreEqual(1.0f, grid.GetFluid(0, 1), Epsilon, "water should rest on the floor row");
        Assert.AreEqual(0f, grid.GetFluid(0, 6), Epsilon, "the source cell should have drained");
    }

    [Test]
    public void MassConservation_RandomDrops_ConservedWithinEpsilon()
    {
        FluidTestGrid grid = BuildTerrainWithPillars();
        var sim = new SandboxFluidSimulator(grid, seed: 12345);

        var rng = new System.Random(9001);
        for (int i = 0; i < 40; i++)
        {
            int x = rng.Next(0, grid.Width);
            int y = rng.Next(1, grid.Height);
            if (!grid.IsSolid(x, y))
            {
                sim.AddFluid(x, y, (float)rng.NextDouble());
            }
        }

        float before = grid.TotalFluid();
        for (int t = 0; t < 300; t++)
        {
            sim.ProcessTick();
        }

        Assert.AreEqual(before, grid.TotalFluid(), Epsilon, "mass must not leak or be created by flow");
    }

    [Test]
    public void Settling_PouredColumnInBasin_ReachesFlatSurfaceAndSleeps()
    {
        FluidTestGrid grid = BuildClosedBasin(width: 8, height: 10);
        var sim = new SandboxFluidSimulator(grid, seed: 7);

        // Pour into one interior column; a uniform basin should equalize to a flat surface.
        sim.AddFluid(1, 8, 6.0f);

        int ticks = SettleUntilAsleep(sim, 5000);

        Assert.AreEqual(0, sim.ActiveCount, "a settled basin must have an empty active set");
        Assert.Less(ticks, 5000, "the basin should settle well within the tick cap");

        float min = float.MaxValue;
        float max = float.MinValue;
        for (int x = 1; x <= 6; x++)
        {
            float column = grid.ColumnFluid(x);
            min = Mathf.Min(min, column);
            max = Mathf.Max(max, column);
        }

        Assert.Less(max - min, 0.05f, "interior columns should hold near-equal water (flat surface)");
    }

    [Test]
    public void StillWater_SettledBasin_CostsNothing()
    {
        FluidTestGrid grid = BuildClosedBasin(width: 8, height: 10);
        var sim = new SandboxFluidSimulator(grid, seed: 7);
        sim.AddFluid(1, 8, 6.0f);
        SettleUntilAsleep(sim, 5000);

        Assert.AreEqual(0, sim.ActiveCount);
        Assert.AreEqual(0, sim.ProcessTick(), "ticking settled water must process no cells");
    }

    [Test]
    public void UTube_ConnectedShafts_EqualizeViaPressure()
    {
        FluidTestGrid grid = BuildUTube();
        var sim = new SandboxFluidSimulator(grid, seed: 3);

        // Pour only into the LEFT shaft; pressure must lift water up the right shaft to match.
        // Enough volume that the level clears the bottom channel and rises into both shafts.
        sim.AddFluid(0, 7, 12.0f);
        SettleUntilAsleep(sim, 20000);

        int leftTop = grid.TopFluidRow(0, SandboxFluidConstants.MinVisibleFill);
        int rightTop = grid.TopFluidRow(6, SandboxFluidConstants.MinVisibleFill);

        Assert.Greater(rightTop, 1, "water must rise up the far shaft, not just sit in the channel");
        Assert.LessOrEqual(Mathf.Abs(leftTop - rightTop), 1, "both shafts must equalize to the same level");
    }

    [Test]
    public void Determinism_SameStateAndSeed_IdenticalFieldAfterNTicks()
    {
        FluidTestGrid a = BuildClosedBasin(width: 8, height: 10);
        FluidTestGrid b = BuildClosedBasin(width: 8, height: 10);
        var simA = new SandboxFluidSimulator(a, seed: 42);
        var simB = new SandboxFluidSimulator(b, seed: 42);

        // Asymmetric pour so the result is not trivially mirror-symmetric.
        simA.AddFluid(1, 8, 4.0f);
        simB.AddFluid(1, 8, 4.0f);

        for (int t = 0; t < 200; t++)
        {
            simA.ProcessTick();
            simB.ProcessTick();
        }

        for (int x = 0; x < a.Width; x++)
        {
            for (int y = 0; y < a.Height; y++)
            {
                Assert.AreEqual(a.GetFluid(x, y), b.GetFluid(x, y), 0f,
                    $"same seed must produce identical fluid at ({x}, {y})");
            }
        }
    }

    [Test]
    public void Wake_RemovingTileUnderSettledPool_ResumesFlow()
    {
        FluidTestGrid grid = BuildWalledColumnOnShelf();
        var sim = new SandboxFluidSimulator(grid);

        // A single confined cell of water on a shelf is already at rest.
        grid.PlaceFluid(2, 2, 1.0f);
        sim.Wake(2, 2);
        SettleUntilAsleep(sim, 50);
        Assert.AreEqual(0, sim.ActiveCount, "the confined pool starts settled");
        Assert.AreEqual(0f, grid.GetFluid(2, 0), Epsilon);

        // Carve the shelf out from under it — the edit flow wakes the affected cells.
        grid.SetSolid(2, 1, false);
        sim.Wake(2, 1);
        Assert.Greater(sim.ActiveCount, 0, "removing a tile under the pool must wake cells");

        SettleUntilAsleep(sim, 200);

        Assert.AreEqual(1.0f, grid.GetFluid(2, 0), Epsilon, "water should have drained to the new floor");
        Assert.AreEqual(0f, grid.GetFluid(2, 2), Epsilon, "the old cell should be empty");
    }

    [Test]
    public void UnloadedChunk_NeverReceivesFluid()
    {
        FluidTestGrid grid = FluidTestGrid.Flat(SandboxChunk.Size * 2, 6);
        grid.MarkChunkUnloaded(new Vector2Int(1, 0));
        var sim = new SandboxFluidSimulator(grid);

        // Drop water right at the loaded/unloaded seam; it must not cross into the unloaded chunk.
        int seamX = SandboxChunk.Size - 1;
        sim.AddFluid(seamX, 3, 1.0f);
        SettleUntilAsleep(sim, 500);

        for (int y = 0; y < 6; y++)
        {
            Assert.AreEqual(0f, grid.GetFluid(SandboxChunk.Size, y), Epsilon,
                "fluid must never flow into an unloaded chunk");
        }
    }

    [Test]
    public void Budget_DefersExcessCellsToNextTick()
    {
        FluidTestGrid grid = FluidTestGrid.Flat(20, 4);
        var sim = new SandboxFluidSimulator(grid);
        for (int x = 1; x < 16; x++)
        {
            sim.AddFluid(x, 2, 1.0f);
        }

        int activeBefore = sim.ActiveCount;
        Assert.Greater(activeBefore, 4, "fixture should present more active cells than the test budget");

        int processed = sim.ProcessTick(maxCells: 2);

        Assert.AreEqual(2, processed, "only the budgeted number of cells is processed per tick");
        Assert.Greater(sim.ActiveCount, 2, "the overflow must be carried to the next tick, not dropped");
        Assert.AreEqual(2, sim.ProcessTick(maxCells: 2), "the deferred cells keep processing next tick");
    }

    private static int SettleUntilAsleep(SandboxFluidSimulator sim, int maxTicks)
    {
        for (int t = 0; t < maxTicks; t++)
        {
            sim.ProcessTick();
            if (sim.ActiveCount == 0)
            {
                return t + 1;
            }
        }

        return maxTicks;
    }

    /// <summary>Flat floor with a couple of solid pillars, for the conservation fixture.</summary>
    private static FluidTestGrid BuildTerrainWithPillars()
    {
        FluidTestGrid grid = FluidTestGrid.Flat(12, 10);
        for (int y = 1; y <= 4; y++)
        {
            grid.SetSolid(4, y, true);
            grid.SetSolid(8, y, true);
        }

        return grid;
    }

    /// <summary>Closed basin: solid floor and two solid side walls, open top.</summary>
    private static FluidTestGrid BuildClosedBasin(int width, int height)
    {
        var rows = new string[height];
        for (int row = 0; row < height; row++)
        {
            int y = height - 1 - row;
            var chars = new char[width];
            for (int x = 0; x < width; x++)
            {
                bool wall = y == 0 || x == 0 || x == width - 1;
                chars[x] = wall ? '#' : '.';
            }

            rows[row] = new string(chars);
        }

        return new FluidTestGrid(rows);
    }

    /// <summary>
    /// Two open vertical shafts (columns 0 and 6) joined by a one-tile channel at y = 1 over a
    /// solid floor; the interior between them is solid. A classic communicating-vessels shape.
    /// </summary>
    private static FluidTestGrid BuildUTube()
    {
        return new FluidTestGrid(
            ".......",  // y = 7
            ".#####.",  // y = 6
            ".#####.",  // y = 5
            ".#####.",  // y = 4
            ".#####.",  // y = 3
            ".#####.",  // y = 2
            ".......",  // y = 1  (bottom channel connects both shafts)
            "#######"); // y = 0  (floor)
    }

    /// <summary>
    /// A single air column (x = 2) walled on both sides, sitting on a shelf at y = 1, with open
    /// space below the shelf. Removing the shelf tile drains the column downward.
    /// </summary>
    private static FluidTestGrid BuildWalledColumnOnShelf()
    {
        return new FluidTestGrid(
            ".#.#.",  // y = 5
            ".#.#.",  // y = 4
            ".#.#.",  // y = 3
            ".#.#.",  // y = 2
            "#####",  // y = 1  (shelf)
            "....."); // y = 0  (space below the shelf)
    }
}
