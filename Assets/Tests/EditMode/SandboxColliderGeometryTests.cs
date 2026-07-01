using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// EditMode unit coverage for <see cref="SandboxColliderGeometry"/> — the pure run-merge and
/// rect math behind the run-merged chunk terrain colliders specified in
/// <c>docs/wiki/rendering-and-collision.md</c> (ticket P1-COLL-001). The integrated physics
/// behavior (standing, jumping, not tunneling) is covered separately by the PlayMode suite.
/// </summary>
public sealed class SandboxColliderGeometryTests
{
    [Test]
    public void BuildSolidRuns_NullChunk_ReturnsEmpty()
    {
        Assert.IsEmpty(SandboxColliderGeometry.BuildSolidRuns(null));
    }

    [Test]
    public void BuildSolidRuns_EmptyChunk_ReturnsNoRuns()
    {
        SandboxChunk chunk = new SandboxChunk(Vector2Int.zero);

        Assert.IsEmpty(SandboxColliderGeometry.BuildSolidRuns(chunk));
    }

    [Test]
    public void BuildSolidRuns_SingleTile_ProducesOneLengthOneRun()
    {
        SandboxChunk chunk = new SandboxChunk(Vector2Int.zero);
        chunk.SetLocalTile(5, 3, new SandboxTile(SandboxTileIds.Stone));

        List<SandboxColliderGeometry.SolidRun> runs = SandboxColliderGeometry.BuildSolidRuns(chunk);

        Assert.AreEqual(1, runs.Count);
        Assert.AreEqual(3, runs[0].Row);
        Assert.AreEqual(5, runs[0].Start);
        Assert.AreEqual(1, runs[0].Length);
    }

    [Test]
    public void BuildSolidRuns_FullRow_MergesIntoSingleRun()
    {
        SandboxChunk chunk = new SandboxChunk(Vector2Int.zero);
        for (int x = 0; x < SandboxChunk.Size; x++)
        {
            chunk.SetLocalTile(x, 0, new SandboxTile(SandboxTileIds.Dirt));
        }

        List<SandboxColliderGeometry.SolidRun> runs = SandboxColliderGeometry.BuildSolidRuns(chunk);

        Assert.AreEqual(1, runs.Count);
        Assert.AreEqual(0, runs[0].Start);
        Assert.AreEqual(SandboxChunk.Size, runs[0].Length);
    }

    [Test]
    public void BuildSolidRuns_RowWithGaps_SplitsIntoSeparateRuns()
    {
        SandboxChunk chunk = new SandboxChunk(Vector2Int.zero);
        // Row 2: solid at x 0..3 and 6..7, air elsewhere.
        for (int x = 0; x <= 3; x++)
        {
            chunk.SetLocalTile(x, 2, new SandboxTile(SandboxTileIds.Stone));
        }

        for (int x = 6; x <= 7; x++)
        {
            chunk.SetLocalTile(x, 2, new SandboxTile(SandboxTileIds.Stone));
        }

        List<SandboxColliderGeometry.SolidRun> runs = SandboxColliderGeometry.BuildSolidRuns(chunk);

        Assert.AreEqual(2, runs.Count);
        Assert.AreEqual(0, runs[0].Start);
        Assert.AreEqual(4, runs[0].Length);
        Assert.AreEqual(6, runs[1].Start);
        Assert.AreEqual(2, runs[1].Length);
    }

    [Test]
    public void BuildSolidRuns_RunTouchingRightEdge_ClosesCorrectly()
    {
        SandboxChunk chunk = new SandboxChunk(Vector2Int.zero);
        chunk.SetLocalTile(SandboxChunk.Size - 1, 4, new SandboxTile(SandboxTileIds.Stone));

        List<SandboxColliderGeometry.SolidRun> runs = SandboxColliderGeometry.BuildSolidRuns(chunk);

        Assert.AreEqual(1, runs.Count);
        Assert.AreEqual(SandboxChunk.Size - 1, runs[0].Start);
        Assert.AreEqual(1, runs[0].Length);
    }

    [Test]
    public void BuildSolidRuns_SolidColumn_DoesNotMergeAcrossRows()
    {
        SandboxChunk chunk = new SandboxChunk(Vector2Int.zero);
        for (int y = 0; y < SandboxChunk.Size; y++)
        {
            chunk.SetLocalTile(10, y, new SandboxTile(SandboxTileIds.Stone));
        }

        List<SandboxColliderGeometry.SolidRun> runs = SandboxColliderGeometry.BuildSolidRuns(chunk);

        Assert.AreEqual(SandboxChunk.Size, runs.Count, "A vertical column must produce one run per row (no cross-row merge).");
        foreach (SandboxColliderGeometry.SolidRun run in runs)
        {
            Assert.AreEqual(10, run.Start);
            Assert.AreEqual(1, run.Length);
        }
    }

    [Test]
    public void BuildSolidRuns_EnumeratesRowsInAscendingOrder()
    {
        SandboxChunk chunk = new SandboxChunk(Vector2Int.zero);
        chunk.SetLocalTile(0, 5, new SandboxTile(SandboxTileIds.Stone));
        chunk.SetLocalTile(0, 1, new SandboxTile(SandboxTileIds.Stone));

        List<SandboxColliderGeometry.SolidRun> runs = SandboxColliderGeometry.BuildSolidRuns(chunk);

        Assert.AreEqual(2, runs.Count);
        Assert.AreEqual(1, runs[0].Row);
        Assert.AreEqual(5, runs[1].Row);
    }

    [Test]
    public void GetColliderRect_CentersRunAndSpansTiles()
    {
        SandboxColliderGeometry.SolidRun run = new SandboxColliderGeometry.SolidRun(5, 2, 3);

        SandboxColliderGeometry.GetColliderRect(run, 1f, out Vector2 offset, out Vector2 size);

        // Run covers local x [2,5) x y [5,6): center (3.5, 5.5), size (3, 1).
        Assert.AreEqual(new Vector2(3.5f, 5.5f), offset);
        Assert.AreEqual(new Vector2(3f, 1f), size);
    }

    [Test]
    public void GetColliderRect_ScalesWithTileSize()
    {
        SandboxColliderGeometry.SolidRun run = new SandboxColliderGeometry.SolidRun(5, 2, 3);

        SandboxColliderGeometry.GetColliderRect(run, 0.5f, out Vector2 offset, out Vector2 size);

        Assert.AreEqual(new Vector2(1.75f, 2.75f), offset);
        Assert.AreEqual(new Vector2(1.5f, 0.5f), size);
    }
}
