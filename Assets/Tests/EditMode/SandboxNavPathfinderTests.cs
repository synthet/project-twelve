using System.Collections.Generic;
using NUnit.Framework;
using ProjectTwelve.Sandbox.Nav;
using UnityEngine;

/// <summary>
/// P2-AI-001 pathfinding fixtures on handcrafted grids: walk, jump, fall, blocked routes,
/// budgets, loaded-world boundaries, determinism, and nav-dirty invalidation.
/// </summary>
public sealed class SandboxNavPathfinderTests
{
    [Test]
    public void Constants_MatchP2Ai001SpecTable()
    {
        Assert.AreEqual(3, SandboxNavConstants.MaxJumpHeight);
        Assert.AreEqual(4, SandboxNavConstants.MaxJumpGap);
        Assert.AreEqual(12, SandboxNavConstants.MaxFallDistance);
        Assert.AreEqual(2048, SandboxNavConstants.MaxExpansionsPerRequest);
        Assert.AreEqual(4, SandboxNavConstants.MaxRequestsPerTick);
        Assert.AreEqual(24, SandboxNavConstants.MinSpawnDistance);
        Assert.AreEqual(64, SandboxNavConstants.MaxSpawnDistance);
        Assert.AreEqual(3, SandboxNavConstants.SpawnLightThreshold);
        Assert.AreEqual(8, SandboxNavConstants.PopulationCap);
        Assert.AreEqual(6f, SandboxNavConstants.SpawnInterval);
        Assert.AreEqual(10f, SandboxNavConstants.DespawnGraceSeconds);
    }

    [Test]
    public void FlatGround_WalksStraightToGoal()
    {
        NavTestGrid grid = NavTestGrid.Flat(12, 6, 0);

        SandboxNavPath path = SandboxNavPathfinder.FindPath(grid, new Vector2Int(1, 1), new Vector2Int(8, 1));

        Assert.AreEqual(SandboxNavStatus.Found, path.Status);
        Assert.AreEqual(7, path.Steps.Count);
        foreach (SandboxNavStep step in path.Steps)
        {
            Assert.AreEqual(SandboxNavMove.Walk, step.Move);
        }

        Assert.AreEqual(new Vector2Int(8, 1), path.Steps[path.Steps.Count - 1].Cell);
    }

    [TestCase(1)]
    [TestCase(2)]
    [TestCase(3)]
    public void JumpUp_WithinMaxJumpHeight_ReachesLedge(int rise)
    {
        NavTestGrid grid = BuildLedgeGrid(rise);

        SandboxNavPath path = SandboxNavPathfinder.FindPath(grid, new Vector2Int(1, 1), new Vector2Int(8, rise + 1));

        Assert.AreEqual(SandboxNavStatus.Found, path.Status);
        Assert.IsTrue(ContainsMove(path, SandboxNavMove.Jump), "expected a jump edge onto the ledge");
    }

    [Test]
    public void JumpUp_AboveMaxJumpHeight_ReturnsNoPath()
    {
        int rise = SandboxNavConstants.MaxJumpHeight + 1;
        NavTestGrid grid = BuildLedgeGrid(rise);

        SandboxNavPath path = SandboxNavPathfinder.FindPath(grid, new Vector2Int(1, 1), new Vector2Int(8, rise + 1));

        Assert.AreEqual(SandboxNavStatus.NoPath, path.Status);
    }

    [TestCase(4, true)]
    [TestCase(5, false)]
    public void GapCross_RespectsMaxJumpGap(int gapWidth, bool expectPath)
    {
        NavTestGrid grid = BuildGapGrid(gapWidth);
        Vector2Int goal = new Vector2Int(5 + gapWidth + 2, 1);

        SandboxNavPath path = SandboxNavPathfinder.FindPath(grid, new Vector2Int(2, 1), goal);

        Assert.AreEqual(expectPath ? SandboxNavStatus.Found : SandboxNavStatus.NoPath, path.Status);
        if (expectPath)
        {
            Assert.IsTrue(ContainsMove(path, SandboxNavMove.Jump), "expected a jump edge across the gap");
        }
    }

    [TestCase(12, true)]
    [TestCase(13, false)]
    public void FallFromLedge_RespectsMaxFallDistance(int drop, bool expectPath)
    {
        NavTestGrid grid = BuildDropGrid(drop);

        SandboxNavPath path = SandboxNavPathfinder.FindPath(grid, new Vector2Int(1, drop + 1), new Vector2Int(5, 1));

        Assert.AreEqual(expectPath ? SandboxNavStatus.Found : SandboxNavStatus.NoPath, path.Status);
        if (expectPath)
        {
            Assert.IsTrue(ContainsMove(path, SandboxNavMove.Fall), "expected a fall edge off the ledge");
        }
    }

    [Test]
    public void BlockedRoute_ReturnsNoPathWithinDefaultBudget()
    {
        NavTestGrid grid = BuildWalledGrid();

        SandboxNavPath path = SandboxNavPathfinder.FindPath(grid, new Vector2Int(1, 1), new Vector2Int(10, 1));

        Assert.AreEqual(SandboxNavStatus.NoPath, path.Status);
    }

    [Test]
    public void ExpansionBudget_ExhaustionReturnsBudgetExhausted()
    {
        NavTestGrid grid = NavTestGrid.Flat(30, 6, 0);

        SandboxNavPath path = SandboxNavPathfinder.FindPath(
            grid, new Vector2Int(1, 1), new Vector2Int(28, 1), maxExpansions: 3);

        Assert.AreEqual(SandboxNavStatus.BudgetExhausted, path.Status);
        Assert.AreEqual(0, path.Steps.Count);
    }

    [Test]
    public void UnloadedChunk_IsNeverEnteredOrTargeted()
    {
        NavTestGrid grid = NavTestGrid.Flat(40, 6, 0);
        grid.MarkChunkUnloaded(new Vector2Int(1, 0));

        SandboxNavPath path = SandboxNavPathfinder.FindPath(grid, new Vector2Int(1, 1), new Vector2Int(35, 1));

        Assert.AreEqual(SandboxNavStatus.NoPath, path.Status);
    }

    [Test]
    public void SameRequest_ReturnsIdenticalPathBothTimes()
    {
        NavTestGrid grid = BuildGapGrid(4);
        Vector2Int start = new Vector2Int(2, 1);
        Vector2Int goal = new Vector2Int(11, 1);

        SandboxNavPath first = SandboxNavPathfinder.FindPath(grid, start, goal);
        SandboxNavPath second = SandboxNavPathfinder.FindPath(grid, start, goal);

        Assert.AreEqual(SandboxNavStatus.Found, first.Status);
        Assert.AreEqual(first.Steps.Count, second.Steps.Count);
        for (int i = 0; i < first.Steps.Count; i++)
        {
            Assert.AreEqual(first.Steps[i].Cell, second.Steps[i].Cell);
            Assert.AreEqual(first.Steps[i].Move, second.Steps[i].Move);
        }
    }

    [Test]
    public void CarvingTunnel_MakesBlockedRouteSucceedAfterNavDirtyUpdate()
    {
        NavTestGrid grid = BuildWalledGrid();
        Vector2Int start = new Vector2Int(1, 1);
        Vector2Int goal = new Vector2Int(10, 1);

        Assert.AreEqual(SandboxNavStatus.NoPath, SandboxNavPathfinder.FindPath(grid, start, goal).Status);

        int versionBeforeCarve = grid.GetNavVersion(Vector2Int.zero);
        grid.SetCell(6, 1, false); // carve a walker-sized tunnel through the wall

        Assert.AreEqual(versionBeforeCarve + 1, grid.GetNavVersion(Vector2Int.zero),
            "carving must mark the chunk nav-dirty");
        Assert.AreEqual(SandboxNavStatus.Found, SandboxNavPathfinder.FindPath(grid, start, goal).Status);
    }

    [Test]
    public void SealingTunnel_InvalidatesInFlightPathAndRecomputeFindsNoPath()
    {
        NavTestGrid grid = BuildWalledGrid();
        grid.SetCell(6, 1, false);
        Vector2Int start = new Vector2Int(1, 1);
        Vector2Int goal = new Vector2Int(10, 1);

        SandboxNavPath path = SandboxNavPathfinder.FindPath(grid, start, goal, grid);
        Assert.AreEqual(SandboxNavStatus.Found, path.Status);
        Assert.IsFalse(path.IsStale(grid), "freshly computed path must not be stale");

        grid.SetCell(6, 1, true); // seal the tunnel again

        Assert.IsTrue(path.IsStale(grid), "sealing a crossed cell must invalidate the path");
        Assert.AreEqual(SandboxNavStatus.NoPath, SandboxNavPathfinder.FindPath(grid, start, goal, grid).Status);
    }

    [Test]
    public void Scheduler_ProcessesAtMostMaxRequestsPerTick()
    {
        NavTestGrid grid = NavTestGrid.Flat(12, 6, 0);
        var scheduler = new SandboxNavRequestScheduler();
        var results = new List<SandboxNavResult>();
        for (int agentId = 0; agentId < 6; agentId++)
        {
            Assert.IsTrue(scheduler.Enqueue(new SandboxNavRequest(agentId, new Vector2Int(1, 1), new Vector2Int(8, 1))));
        }

        Assert.AreEqual(SandboxNavConstants.MaxRequestsPerTick, scheduler.ProcessTick(grid, results));
        Assert.AreEqual(2, scheduler.ProcessTick(grid, results));
        Assert.AreEqual(0, scheduler.ProcessTick(grid, results));
        Assert.AreEqual(6, results.Count);
        foreach (SandboxNavResult result in results)
        {
            Assert.AreEqual(SandboxNavStatus.Found, result.Path.Status);
        }
    }

    [Test]
    public void Scheduler_IgnoresDuplicateRequestWhilePending()
    {
        var scheduler = new SandboxNavRequestScheduler();

        Assert.IsTrue(scheduler.Enqueue(new SandboxNavRequest(7, new Vector2Int(1, 1), new Vector2Int(3, 1))));
        Assert.IsFalse(scheduler.Enqueue(new SandboxNavRequest(7, new Vector2Int(1, 1), new Vector2Int(4, 1))));
        Assert.AreEqual(1, scheduler.PendingCount);
    }

    [Test]
    public void SandboxChunk_SetLocalTileBumpsNavVersion()
    {
        SandboxChunk chunk = new SandboxChunk(Vector2Int.zero);
        int before = chunk.NavVersion;

        chunk.SetLocalTile(3, 4, new SandboxTile(SandboxTileIds.Stone));

        Assert.AreEqual(before + 1, chunk.NavVersion);
    }

    [Test]
    public void SandboxWorld_BorderEditBumpsNeighborNavVersion()
    {
        var chunks = new Dictionary<Vector2Int, SandboxChunk>
        {
            [new Vector2Int(1, 0)] = new SandboxChunk(new Vector2Int(1, 0)),
        };

        SandboxWorld.MarkBorderNeighborsDirty(chunks, Vector2Int.zero, SandboxChunk.Size - 1, 5);

        Assert.AreEqual(1, chunks[new Vector2Int(1, 0)].NavVersion,
            "a border edit must mark the adjacent chunk nav-dirty (arcs cross chunk borders)");
    }

    private static bool ContainsMove(SandboxNavPath path, SandboxNavMove move)
    {
        foreach (SandboxNavStep step in path.Steps)
        {
            if (step.Move == move)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Flat floor with a raised ledge (columns 6+) whose top sits <paramref name="rise"/> tiles up.</summary>
    private static NavTestGrid BuildLedgeGrid(int rise)
    {
        int height = rise + 6;
        var rows = new string[height];
        for (int row = 0; row < height; row++)
        {
            int y = height - 1 - row;
            var chars = new char[12];
            for (int x = 0; x < 12; x++)
            {
                bool isSolid = y == 0 || (x >= 6 && y >= 1 && y <= rise);
                chars[x] = isSolid ? '#' : '.';
            }

            rows[row] = new string(chars);
        }

        return new NavTestGrid(rows);
    }

    /// <summary>Floor with a pit of <paramref name="gapWidth"/> open columns starting at x = 5.</summary>
    private static NavTestGrid BuildGapGrid(int gapWidth)
    {
        int width = 5 + gapWidth + 5;
        var rows = new string[6];
        for (int row = 0; row < 6; row++)
        {
            int y = 5 - row;
            var chars = new char[width];
            for (int x = 0; x < width; x++)
            {
                bool isSolid = y == 0 && (x < 5 || x >= 5 + gapWidth);
                chars[x] = isSolid ? '#' : '.';
            }

            rows[row] = new string(chars);
        }

        return new NavTestGrid(rows);
    }

    /// <summary>Tower (columns 0-2) standing <paramref name="drop"/> tiles above the floor to its right.</summary>
    private static NavTestGrid BuildDropGrid(int drop)
    {
        int height = drop + 4;
        var rows = new string[height];
        for (int row = 0; row < height; row++)
        {
            int y = height - 1 - row;
            var chars = new char[8];
            for (int x = 0; x < 8; x++)
            {
                bool isSolid = y == 0 || (x <= 2 && y <= drop);
                chars[x] = isSolid ? '#' : '.';
            }

            rows[row] = new string(chars);
        }

        return new NavTestGrid(rows);
    }

    /// <summary>Flat floor split by a full-height wall at x = 6.</summary>
    private static NavTestGrid BuildWalledGrid()
    {
        NavTestGrid grid = NavTestGrid.Flat(12, 8, 0);
        for (int y = 1; y < 8; y++)
        {
            grid.SetCell(6, y, true);
        }

        return grid;
    }
}
