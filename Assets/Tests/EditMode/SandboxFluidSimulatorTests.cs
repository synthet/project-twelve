using NUnit.Framework;
using ProjectTwelve.Sandbox.Fluid;
using UnityEngine;

/// <summary>
/// P2-FLUID-001 fixtures on handcrafted grids: constant/equilibrium math, mass conservation,
/// settling, U-tube pressure equalization, determinism, wake-on-edit, the per-tick budget cap, and
/// the unloaded-chunk boundary. All tests run against the pure <see cref="SandboxFluidSimulator"/>
/// through the in-memory <see cref="FluidTestGrid"/> — no Unity scene required.
/// </summary>
public sealed class SandboxFluidSimulatorTests
{
    [Test]
    public void Constants_MatchP2Fluid001SpecTable()
    {
        Assert.AreEqual(1.0f, SandboxFluidConstants.MaxFill);
        Assert.AreEqual(0.02f, SandboxFluidConstants.MaxCompress);
        Assert.AreEqual(0.01f, SandboxFluidConstants.MinFlow);
        Assert.AreEqual(1.0f, SandboxFluidConstants.MaxTransferPerTick);
        Assert.AreEqual(1e-4f, SandboxFluidConstants.SettleEpsilon);
        Assert.AreEqual(0.02f, SandboxFluidConstants.MinVisibleFill);
        Assert.AreEqual(2, SandboxFluidConstants.IterationsPerFrame);
        Assert.AreEqual(4096, SandboxFluidConstants.MaxActiveCellsPerTick);
    }

    [Test]
    public void StableStateBelow_MatchesEquilibriumModel()
    {
        // At or below a full cell, the lower cell takes everything (clamped to what exists upstream).
        Assert.AreEqual(1.0f, SandboxFluidSimulator.StableStateBelow(0.5f), 1e-6f);
        Assert.AreEqual(1.0f, SandboxFluidSimulator.StableStateBelow(1.0f), 1e-6f);
        // Between full and 2*full+compress: compression lets the lower cell hold slightly over full.
        Assert.AreEqual(1.03f / 1.02f, SandboxFluidSimulator.StableStateBelow(1.5f), 1e-5f);
        // Well above: the pair splits the excess evenly (plus the compression term).
        Assert.AreEqual((3.0f + 0.02f) / 2f, SandboxFluidSimulator.StableStateBelow(3.0f), 1e-5f);
    }

    [Test]
    public void Conservation_RandomTerrainAndDrops_MassConstantWithinEpsilon()
    {
        var grid = new FluidTestGrid(20, 20);
        for (int x = 0; x < 20; x++)
        {
            grid.SetSolid(x, 0, true); // floor
        }

        var rng = new System.Random(12345);
        var sim = new SandboxFluidSimulator();
        for (int i = 0; i < 6; i++)
        {
            int x = 3 + rng.Next(14);
            int y = 2 + rng.Next(6);
            grid.SetSolid(x, y, true); // scattered pillars
        }

        for (int i = 0; i < 30; i++)
        {
            int x = 1 + rng.Next(18);
            int y = 6 + rng.Next(13);
            if (!grid.IsSolid(x, y))
            {
                sim.AddFluid(grid, x, y, 0.5f + (float)rng.NextDouble());
            }
        }

        float before = grid.TotalMass();
        for (int t = 0; t < 200; t++)
        {
            sim.Step(grid);
        }

        float after = grid.TotalMass();
        Assert.AreEqual(before, after, 1e-3f, "fluid mass must be conserved across ticks");
    }

    [Test]
    public void Settling_PouredColumnInBasin_FlattensAndActiveSetEmpties()
    {
        const int w = 9;
        const int h = 12;
        var grid = new FluidTestGrid(w, h);
        for (int x = 0; x < w; x++)
        {
            grid.SetSolid(x, 0, true); // floor
        }

        for (int y = 0; y < h; y++)
        {
            grid.SetSolid(0, y, true);
            grid.SetSolid(w - 1, y, true); // walls
        }

        var sim = new SandboxFluidSimulator();
        for (int y = 1; y < 10; y++)
        {
            sim.AddFluid(grid, 4, y, 1.0f); // tall central column
        }

        float before = grid.TotalMass();
        int ticks = RunUntilSettled(sim, grid, 3000);
        float after = grid.TotalMass();

        Assert.AreEqual(0, sim.ActiveCount, $"settled fluid must empty the active set (ticks={ticks})");
        Assert.AreEqual(before, after, 1e-3f, "mass must be conserved while settling");

        float min = float.MaxValue;
        float max = float.MinValue;
        for (int x = 1; x < w - 1; x++)
        {
            float col = grid.ColumnMass(x);
            min = Mathf.Min(min, col);
            max = Mathf.Max(max, col);
        }

        Assert.Less(max - min, 0.05f, $"surface must be flat (min={min}, max={max})");
    }

    [Test]
    public void StillWater_SettledLake_CostsZeroPerTick()
    {
        const int w = 8;
        const int h = 8;
        var grid = new FluidTestGrid(w, h);
        for (int x = 0; x < w; x++)
        {
            grid.SetSolid(x, 0, true);
        }

        for (int y = 0; y < h; y++)
        {
            grid.SetSolid(0, y, true);
            grid.SetSolid(w - 1, y, true);
        }

        var sim = new SandboxFluidSimulator();
        for (int y = 1; y < 4; y++)
        {
            for (int x = 1; x < w - 1; x++)
            {
                sim.AddFluid(grid, x, y, 1.0f);
            }
        }

        RunUntilSettled(sim, grid, 2000);
        Assert.AreEqual(0, sim.ActiveCount);
        Assert.AreEqual(0, sim.Step(grid), "a settled lake processes no cells");
    }

    [Test]
    public void UTube_ConnectedColumns_EqualizeViaPressure()
    {
        // Two shafts connected by a channel at y=1; central divider (x=3) from y>=2 up.
        const int w = 7;
        const int h = 12;
        var grid = new FluidTestGrid(w, h);
        for (int x = 0; x < w; x++)
        {
            grid.SetSolid(x, 0, true); // floor
        }

        for (int y = 0; y < h; y++)
        {
            grid.SetSolid(0, y, true);
            grid.SetSolid(w - 1, y, true); // outer walls
            if (y >= 2)
            {
                grid.SetSolid(3, y, true); // divider, leaving y=1 as the connecting channel
            }
        }

        var sim = new SandboxFluidSimulator();
        for (int y = 1; y < 10; y++)
        {
            sim.AddFluid(grid, 1, y, 1.0f);
            sim.AddFluid(grid, 2, y, 1.0f); // fill only the LEFT shaft
        }

        float before = grid.TotalMass();
        RunUntilSettled(sim, grid, 8000);
        float after = grid.TotalMass();

        float left = grid.ColumnMass(1) + grid.ColumnMass(2);
        float right = grid.ColumnMass(4) + grid.ColumnMass(5);

        Assert.AreEqual(before, after, 1e-3f, "mass conserved");
        Assert.Greater(right, 0.5f * left, "pressure must push water up the empty shaft");
        Assert.Less(Mathf.Abs(left - right), 0.2f * before, $"columns must roughly equalize (left={left}, right={right})");
    }

    [Test]
    public void Determinism_SameInitialState_IdenticalFieldAfterNTicks()
    {
        FluidTestGrid a = BuildDeterminismGrid(out SandboxFluidSimulator simA);
        FluidTestGrid b = BuildDeterminismGrid(out SandboxFluidSimulator simB);

        for (int t = 0; t < 100; t++)
        {
            simA.Step(a);
            simB.Step(b);
        }

        for (int x = 0; x < a.Width; x++)
        {
            for (int y = 0; y < a.Height; y++)
            {
                Assert.AreEqual(a.GetFluid(x, y), b.GetFluid(x, y), 0f, $"field must be identical at ({x},{y})");
            }
        }
    }

    [Test]
    public void Wake_OpeningTileUnderSettledPool_ReactivatesFlow()
    {
        const int w = 9;
        const int h = 12;
        var grid = new FluidTestGrid(w, h);
        for (int x = 0; x < w; x++)
        {
            grid.SetSolid(x, 0, true);
        }

        for (int y = 0; y < h; y++)
        {
            grid.SetSolid(0, y, true);
            grid.SetSolid(w - 1, y, true);
        }

        var sim = new SandboxFluidSimulator();
        for (int y = 1; y < 6; y++)
        {
            for (int x = 1; x < w - 1; x++)
            {
                sim.AddFluid(grid, x, y, 1.0f);
            }
        }

        RunUntilSettled(sim, grid, 2000);
        Assert.AreEqual(0, sim.ActiveCount, "pool must settle before the edit");
        float before = grid.TotalMass();

        // Dig a hole in the floor: the edit flow opens the cell and wakes it + neighbours.
        grid.SetSolid(4, 0, false);
        sim.WakeWithNeighbors(grid, 4, 0);
        Assert.Greater(sim.ActiveCount, 0, "the edit must re-wake the affected cells");

        int ticks = RunUntilSettled(sim, grid, 2000);
        float after = grid.TotalMass();

        Assert.AreEqual(before, after, 1e-3f, "mass conserved across the edit-driven reflow");
        Assert.Greater(grid.GetFluid(4, 0), SandboxFluidConstants.MinVisibleFill, $"fluid must drain into the opened cell (ticks={ticks})");
    }

    [Test]
    public void Budget_SmallCap_StillConservesMass()
    {
        const int w = 15;
        const int h = 15;
        var grid = new FluidTestGrid(w, h);
        for (int x = 0; x < w; x++)
        {
            grid.SetSolid(x, 0, true);
        }

        var sim = new SandboxFluidSimulator();
        for (int x = 2; x < 13; x++)
        {
            for (int y = 5; y < 12; y++)
            {
                sim.AddFluid(grid, x, y, 1.0f);
            }
        }

        float before = grid.TotalMass();
        for (int t = 0; t < 300; t++)
        {
            sim.Step(grid, 10); // tiny per-tick budget
        }

        float after = grid.TotalMass();
        Assert.AreEqual(before, after, 1e-3f, "the budget cap defers cells but never loses mass");
    }

    [Test]
    public void UnloadedChunk_FluidDoesNotCrossBoundary()
    {
        // A wide grid spanning two chunks; the right chunk is unloaded.
        int w = SandboxChunk.Size * 2;
        int h = 8;
        var grid = new FluidTestGrid(w, h);
        for (int x = 0; x < w; x++)
        {
            grid.SetSolid(x, 0, true);
        }

        grid.MarkChunkUnloaded(new Vector2Int(1, 0)); // right half unloaded

        var sim = new SandboxFluidSimulator();
        // Pour against the seam in the loaded chunk.
        for (int y = 1; y < 5; y++)
        {
            sim.AddFluid(grid, SandboxChunk.Size - 1, y, 1.0f);
        }

        float before = grid.TotalMass();
        for (int t = 0; t < 300; t++)
        {
            sim.Step(grid);
        }

        float after = grid.TotalMass();
        Assert.AreEqual(before, after, 1e-3f, "mass conserved");
        Assert.AreEqual(0f, grid.GetFluid(SandboxChunk.Size, 1), 1e-6f, "no fluid may enter the unloaded chunk");
    }

    private static FluidTestGrid BuildDeterminismGrid(out SandboxFluidSimulator sim)
    {
        const int w = 12;
        const int h = 12;
        var grid = new FluidTestGrid(w, h);
        for (int x = 0; x < w; x++)
        {
            grid.SetSolid(x, 0, true);
        }

        for (int y = 0; y < 4; y++)
        {
            grid.SetSolid(6, y, true); // a partial divider
        }

        sim = new SandboxFluidSimulator();
        for (int y = 1; y < 8; y++)
        {
            sim.AddFluid(grid, 2, y, 1.0f);
        }

        for (int y = 1; y < 5; y++)
        {
            sim.AddFluid(grid, 9, y, 0.7f);
        }

        return grid;
    }

    private static int RunUntilSettled(SandboxFluidSimulator sim, FluidTestGrid grid, int maxTicks)
    {
        int ticks = 0;
        while (sim.ActiveCount > 0 && ticks < maxTicks)
        {
            sim.Step(grid);
            ticks++;
        }

        return ticks;
    }
}
