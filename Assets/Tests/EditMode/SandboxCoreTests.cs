using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public sealed class SandboxCoreTests
{
    [Test]
    public void SandboxTile_DefaultsToAirAndIsNotSolid()
    {
        SandboxTile tile = default;

        Assert.AreEqual(SandboxTileIds.Air, tile.id);
        Assert.IsFalse(tile.IsSolid);
    }

    [Test]
    public void SandboxTile_NonAirTileIsSolid()
    {
        SandboxTile tile = new SandboxTile(SandboxTileIds.Dirt);

        Assert.IsTrue(tile.IsSolid);
    }

    [Test]
    public void SandboxChunk_SetLocalTileStoresTileAndMarksChunkForRebuild()
    {
        SandboxChunk chunk = new SandboxChunk(Vector2Int.zero);
        chunk.NeedsRenderRebuild = false;
        chunk.NeedsColliderRebuild = false;

        chunk.SetLocalTile(2, 3, new SandboxTile(SandboxTileIds.Stone));

        Assert.AreEqual(SandboxTileIds.Stone, chunk.GetLocalTile(2, 3).id);
        Assert.IsTrue(chunk.NeedsRenderRebuild);
        Assert.IsTrue(chunk.NeedsColliderRebuild);
        Assert.IsTrue(chunk.IsDirty);
    }

    [Test]
    public void SandboxChunk_OutOfBoundsSetDoesNotModifyChunkState()
    {
        SandboxChunk chunk = new SandboxChunk(Vector2Int.zero);
        chunk.NeedsRenderRebuild = false;
        chunk.NeedsColliderRebuild = false;

        chunk.SetLocalTile(-1, 0, new SandboxTile(SandboxTileIds.Dirt));

        Assert.AreEqual(SandboxTileIds.Air, chunk.GetLocalTile(-1, 0).id);
        Assert.IsFalse(chunk.NeedsRenderRebuild);
        Assert.IsFalse(chunk.NeedsColliderRebuild);
        Assert.IsFalse(chunk.IsDirty);
    }

    [TestCase(0, 0, 0, 0)]
    [TestCase(31, 31, 0, 0)]
    [TestCase(32, 0, 1, 0)]
    [TestCase(-1, -1, -1, -1)]
    [TestCase(-32, -32, -1, -1)]
    [TestCase(-33, 64, -2, 2)]
    public void SandboxWorld_WorldToChunkCoordUsesFloorDivision(int x, int y, int expectedChunkX, int expectedChunkY)
    {
        Assert.AreEqual(new Vector2Int(expectedChunkX, expectedChunkY), SandboxWorld.WorldToChunkCoord(x, y));
    }

    [TestCase(0, 0, 0, 0)]
    [TestCase(31, 31, 31, 31)]
    [TestCase(32, 0, 0, 0)]
    [TestCase(-1, -1, 31, 31)]
    [TestCase(-32, -32, 0, 0)]
    [TestCase(-33, 64, 31, 0)]
    public void SandboxWorld_WorldToLocalCoordWrapsNegativeCoordinates(int x, int y, int expectedLocalX, int expectedLocalY)
    {
        Assert.AreEqual(new Vector2Int(expectedLocalX, expectedLocalY), SandboxWorld.WorldToLocalCoord(x, y));
    }

    [TestCase(0, 0, true)]
    [TestCase(31, 31, true)]
    [TestCase(-1, 0, false)]
    [TestCase(0, -1, false)]
    [TestCase(32, 0, false)]
    [TestCase(0, 32, false)]
    public void SandboxChunk_IsLocalInBoundsMatchesChunkSize(int localX, int localY, bool expected)
    {
        Assert.AreEqual(expected, SandboxChunk.IsLocalInBounds(localX, localY));
    }

    [TestCase(0, 0, 0, 0, 0, 0)]
    [TestCase(0, 0, 31, 31, 31, 31)]
    [TestCase(1, 0, 0, 0, 32, 0)]
    [TestCase(-1, -1, 31, 31, -1, -1)]
    [TestCase(-1, -1, 0, 0, -32, -32)]
    [TestCase(-2, 2, 31, 0, -33, 64)]
    public void SandboxWorld_ChunkLocalToWorldInvertsSplit(
        int chunkX, int chunkY, int localX, int localY, int expectedWorldX, int expectedWorldY)
    {
        Vector2Int world = SandboxWorld.ChunkLocalToWorld(new Vector2Int(chunkX, chunkY), localX, localY);

        Assert.AreEqual(new Vector2Int(expectedWorldX, expectedWorldY), world);
    }

    [TestCase(0, 0)]
    [TestCase(31, 31)]
    [TestCase(32, 0)]
    [TestCase(-1, -1)]
    [TestCase(-32, -32)]
    [TestCase(-33, 64)]
    [TestCase(-100000, 100000)]
    public void SandboxWorld_WorldChunkLocalRoundTripIsExact(int worldX, int worldY)
    {
        Vector2Int chunkCoord = SandboxWorld.WorldToChunkCoord(worldX, worldY);
        Vector2Int local = SandboxWorld.WorldToLocalCoord(worldX, worldY);

        Vector2Int roundTripped = SandboxWorld.ChunkLocalToWorld(chunkCoord, local.x, local.y);

        Assert.AreEqual(new Vector2Int(worldX, worldY), roundTripped);
    }

    [TestCase(-64, -1)]
    [TestCase(-33, 0)]
    [TestCase(-1, 31)]
    [TestCase(0, 0)]
    [TestCase(31, 31)]
    [TestCase(64, 0)]
    public void SandboxWorld_WorldToLocalCoordAlwaysWithinChunkBounds(int worldX, int worldY)
    {
        Vector2Int local = SandboxWorld.WorldToLocalCoord(worldX, worldY);

        Assert.IsTrue(SandboxChunk.IsLocalInBounds(local.x, local.y),
            $"Local coordinate {local} for world ({worldX},{worldY}) must be a valid chunk index.");
    }

    [Test]
    public void SandboxWorld_InteriorEditTouchesNoNeighborChunks()
    {
        CollectionAssert.IsEmpty(SandboxWorld.GetBorderNeighborChunks(Vector2Int.zero, 5, 5));
    }

    [TestCase(0, 5, -1, 0)]
    [TestCase(31, 5, 1, 0)]
    [TestCase(5, 0, 0, -1)]
    [TestCase(5, 31, 0, 1)]
    public void SandboxWorld_EdgeEditTouchesSingleNeighborChunk(int localX, int localY, int neighborX, int neighborY)
    {
        List<Vector2Int> neighbors = new List<Vector2Int>(
            SandboxWorld.GetBorderNeighborChunks(Vector2Int.zero, localX, localY));

        Assert.AreEqual(1, neighbors.Count);
        Assert.AreEqual(new Vector2Int(neighborX, neighborY), neighbors[0]);
    }

    [Test]
    public void SandboxWorld_CornerEditTouchesTwoOrthogonalNeighborChunks()
    {
        List<Vector2Int> neighbors = new List<Vector2Int>(
            SandboxWorld.GetBorderNeighborChunks(new Vector2Int(2, 3), 0, 0));

        CollectionAssert.AreEquivalent(
            new[] { new Vector2Int(1, 3), new Vector2Int(2, 2) },
            neighbors);
    }

    [Test]
    public void SandboxWorld_BorderEditMarksLoadedNeighborForRebuild()
    {
        Vector2Int editedCoord = Vector2Int.zero;
        Vector2Int neighborCoord = new Vector2Int(-1, 0);
        SandboxChunk neighbor = new SandboxChunk(neighborCoord);
        neighbor.NeedsRenderRebuild = false;
        neighbor.NeedsColliderRebuild = false;

        var loadedChunks = new Dictionary<Vector2Int, SandboxChunk>
        {
            { editedCoord, new SandboxChunk(editedCoord) },
            { neighborCoord, neighbor },
        };

        SandboxWorld.MarkBorderNeighborsDirty(loadedChunks, editedCoord, 0, 5);

        Assert.IsTrue(neighbor.NeedsRenderRebuild);
        Assert.IsTrue(neighbor.NeedsColliderRebuild);
    }

    [Test]
    public void SandboxWorld_InteriorEditLeavesNeighborsClean()
    {
        Vector2Int editedCoord = Vector2Int.zero;
        Vector2Int neighborCoord = new Vector2Int(-1, 0);
        SandboxChunk neighbor = new SandboxChunk(neighborCoord);
        neighbor.NeedsRenderRebuild = false;
        neighbor.NeedsColliderRebuild = false;

        var loadedChunks = new Dictionary<Vector2Int, SandboxChunk>
        {
            { editedCoord, new SandboxChunk(editedCoord) },
            { neighborCoord, neighbor },
        };

        SandboxWorld.MarkBorderNeighborsDirty(loadedChunks, editedCoord, 5, 5);

        Assert.IsFalse(neighbor.NeedsRenderRebuild);
        Assert.IsFalse(neighbor.NeedsColliderRebuild);
    }

    [Test]
    public void SandboxWorld_BorderEditDoesNotGenerateUnloadedNeighbor()
    {
        Vector2Int editedCoord = Vector2Int.zero;
        var loadedChunks = new Dictionary<Vector2Int, SandboxChunk>
        {
            { editedCoord, new SandboxChunk(editedCoord) },
        };

        SandboxWorld.MarkBorderNeighborsDirty(loadedChunks, editedCoord, 0, 5);

        Assert.AreEqual(1, loadedChunks.Count);
        Assert.IsFalse(loadedChunks.ContainsKey(new Vector2Int(-1, 0)));
    }

    private static SandboxChunk CleanChunk(Vector2Int coord)
    {
        SandboxChunk chunk = new SandboxChunk(coord);
        chunk.NeedsRenderRebuild = false;
        chunk.NeedsColliderRebuild = false;
        return chunk;
    }

    [Test]
    public void SandboxWorld_RebuildSelectionIncludesVisibleDirtyChunk()
    {
        Vector2Int coord = new Vector2Int(1, 2);
        SandboxChunk chunk = CleanChunk(coord);
        chunk.NeedsRenderRebuild = true;
        var loaded = new Dictionary<Vector2Int, SandboxChunk> { { coord, chunk } };

        List<Vector2Int> selected = new List<Vector2Int>(
            SandboxWorld.GetChunksNeedingRebuild(new[] { coord }, loaded));

        CollectionAssert.AreEqual(new[] { coord }, selected);
    }

    [Test]
    public void SandboxWorld_RebuildSelectionIncludesColliderOnlyDirtyChunk()
    {
        Vector2Int coord = new Vector2Int(3, 4);
        SandboxChunk chunk = CleanChunk(coord);
        chunk.NeedsColliderRebuild = true;
        var loaded = new Dictionary<Vector2Int, SandboxChunk> { { coord, chunk } };

        List<Vector2Int> selected = new List<Vector2Int>(
            SandboxWorld.GetChunksNeedingRebuild(new[] { coord }, loaded));

        CollectionAssert.AreEqual(new[] { coord }, selected);
    }

    [Test]
    public void SandboxWorld_RebuildSelectionSkipsVisibleCleanChunk()
    {
        Vector2Int coord = new Vector2Int(0, 0);
        var loaded = new Dictionary<Vector2Int, SandboxChunk> { { coord, CleanChunk(coord) } };

        CollectionAssert.IsEmpty(
            new List<Vector2Int>(SandboxWorld.GetChunksNeedingRebuild(new[] { coord }, loaded)));
    }

    [Test]
    public void SandboxWorld_RebuildSelectionSkipsDirtyChunkThatIsNotVisible()
    {
        Vector2Int visibleCoord = new Vector2Int(0, 0);
        Vector2Int offscreenCoord = new Vector2Int(9, 9);
        SandboxChunk offscreen = CleanChunk(offscreenCoord);
        offscreen.NeedsRenderRebuild = true;
        var loaded = new Dictionary<Vector2Int, SandboxChunk>
        {
            { visibleCoord, CleanChunk(visibleCoord) },
            { offscreenCoord, offscreen },
        };

        CollectionAssert.IsEmpty(
            new List<Vector2Int>(SandboxWorld.GetChunksNeedingRebuild(new[] { visibleCoord }, loaded)));
    }

    [Test]
    public void SandboxWorld_RebuildSelectionSkipsVisibleCoordWithNoLoadedChunk()
    {
        Vector2Int coord = new Vector2Int(2, 2);
        var loaded = new Dictionary<Vector2Int, SandboxChunk>();

        CollectionAssert.IsEmpty(
            new List<Vector2Int>(SandboxWorld.GetChunksNeedingRebuild(new[] { coord }, loaded)));
    }

    [Test]
    public void SandboxWorld_RebuildSelectionPicksOnlyDirtyVisibleChunksFromMixedSet()
    {
        Vector2Int dirtyA = new Vector2Int(0, 0);
        Vector2Int dirtyB = new Vector2Int(1, 0);
        Vector2Int clean = new Vector2Int(0, 1);
        SandboxChunk chunkA = CleanChunk(dirtyA);
        chunkA.NeedsRenderRebuild = true;
        SandboxChunk chunkB = CleanChunk(dirtyB);
        chunkB.NeedsColliderRebuild = true;
        var loaded = new Dictionary<Vector2Int, SandboxChunk>
        {
            { dirtyA, chunkA },
            { dirtyB, chunkB },
            { clean, CleanChunk(clean) },
        };

        List<Vector2Int> selected = new List<Vector2Int>(
            SandboxWorld.GetChunksNeedingRebuild(new[] { dirtyA, dirtyB, clean }, loaded));

        CollectionAssert.AreEquivalent(new[] { dirtyA, dirtyB }, selected);
    }

    [Test]
    public void SandboxWorld_LoadRangeCoversSquareWindowAroundCenter()
    {
        Vector2Int center = new Vector2Int(5, -3);

        List<Vector2Int> loaded = new List<Vector2Int>(
            SandboxWorld.GetChunksInLoadRange(center, 1));

        CollectionAssert.AreEquivalent(
            new[]
            {
                new Vector2Int(4, -4), new Vector2Int(5, -4), new Vector2Int(6, -4),
                new Vector2Int(4, -3), new Vector2Int(5, -3), new Vector2Int(6, -3),
                new Vector2Int(4, -2), new Vector2Int(5, -2), new Vector2Int(6, -2),
            },
            loaded);
    }

    [Test]
    public void SandboxWorld_LoadRangeWithZeroRadiusLoadsOnlyCenterChunk()
    {
        Vector2Int center = new Vector2Int(2, 7);

        List<Vector2Int> loaded = new List<Vector2Int>(
            SandboxWorld.GetChunksInLoadRange(center, 0));

        CollectionAssert.AreEqual(new[] { center }, loaded);
    }

    [Test]
    public void SandboxWorld_LoadRangeClampsNegativeRadiusToCenterOnly()
    {
        Vector2Int center = new Vector2Int(-1, -1);

        List<Vector2Int> loaded = new List<Vector2Int>(
            SandboxWorld.GetChunksInLoadRange(center, -4));

        CollectionAssert.AreEqual(new[] { center }, loaded);
    }

    [Test]
    public void SandboxWorld_UnloadSelectsOnlyChunksBeyondPaddedRange()
    {
        Vector2Int center = Vector2Int.zero;
        Vector2Int withinPadding = new Vector2Int(2, 0);
        Vector2Int onPaddingBoundary = new Vector2Int(2, 2);
        Vector2Int beyondPadding = new Vector2Int(3, 0);
        var loaded = new[] { center, withinPadding, onPaddingBoundary, beyondPadding };

        List<Vector2Int> unload = new List<Vector2Int>(
            SandboxWorld.GetRenderersToUnload(loaded, center, loadRadius: 1, unloadPadding: 1));

        CollectionAssert.AreEqual(new[] { beyondPadding }, unload);
    }

    [Test]
    public void SandboxWorld_UnloadHysteresisKeepsEdgeChunkLoaded()
    {
        // A chunk exactly at loadRadius + unloadPadding stays loaded; a small player move that
        // pushes it one chunk past the boundary is what finally unloads it.
        Vector2Int edgeChunk = new Vector2Int(0, 2);

        List<Vector2Int> keptAtBoundary = new List<Vector2Int>(
            SandboxWorld.GetRenderersToUnload(new[] { edgeChunk }, Vector2Int.zero, loadRadius: 1, unloadPadding: 1));
        List<Vector2Int> unloadedAfterMove = new List<Vector2Int>(
            SandboxWorld.GetRenderersToUnload(new[] { edgeChunk }, new Vector2Int(0, -1), loadRadius: 1, unloadPadding: 1));

        CollectionAssert.IsEmpty(keptAtBoundary);
        CollectionAssert.AreEqual(new[] { edgeChunk }, unloadedAfterMove);
    }

    [Test]
    public void SandboxWorld_UnloadKeepsAllChunksWhenNoneAreLoaded()
    {
        CollectionAssert.IsEmpty(new List<Vector2Int>(
            SandboxWorld.GetRenderersToUnload(new Vector2Int[0], Vector2Int.zero, 3, 1)));
    }

    [Test]
    public void SandboxChunk_NewlyLoadedChunkRequestsRenderAndColliderRebuild()
    {
        SandboxChunk chunk = new SandboxChunk(new Vector2Int(4, 4));

        Assert.IsTrue(chunk.NeedsRenderRebuild);
        Assert.IsTrue(chunk.NeedsColliderRebuild);
    }

    [Test]
    public void SandboxChunk_RenderAndColliderDirtyFlagsToggleIndependently()
    {
        SandboxChunk chunk = CleanChunk(Vector2Int.zero);

        chunk.NeedsRenderRebuild = true;
        Assert.IsTrue(chunk.NeedsRenderRebuild);
        Assert.IsFalse(chunk.NeedsColliderRebuild);

        chunk.NeedsRenderRebuild = false;
        chunk.NeedsColliderRebuild = true;
        Assert.IsFalse(chunk.NeedsRenderRebuild);
        Assert.IsTrue(chunk.NeedsColliderRebuild);
    }

    private static SandboxTerrainGenerator GoldenGenerator(int seed = 1337)
    {
        return new SandboxTerrainGenerator(
            seed: seed,
            surfaceHeight: 28,
            terrainAmplitude: 8,
            terrainFrequency: 0.06f,
            dirtDepth: 8);
    }

    [TestCase(0, 0)]
    [TestCase(2, 3)]
    [TestCase(-1, -1)]
    [TestCase(-4, 5)]
    public void SandboxTerrainGenerator_SameSeedAndCoordProduceIdenticalTiles(int chunkX, int chunkY)
    {
        Vector2Int coord = new Vector2Int(chunkX, chunkY);

        SandboxChunk first = GoldenGenerator().GenerateChunk(coord);
        SandboxChunk second = GoldenGenerator().GenerateChunk(coord);

        for (int x = 0; x < SandboxChunk.Size; x++)
        {
            for (int y = 0; y < SandboxChunk.Size; y++)
            {
                SandboxTile a = first.GetLocalTile(x, y);
                SandboxTile b = second.GetLocalTile(x, y);
                Assert.AreEqual(a.id, b.id, $"id mismatch at {x},{y}");
                Assert.AreEqual(a.light, b.light, $"light mismatch at {x},{y}");
            }
        }
    }

    [Test]
    public void SandboxTerrainGenerator_GeneratesColumnLayersFromSurfaceDownward()
    {
        SandboxTerrainGenerator generator = GoldenGenerator();
        const int worldX = 0;
        int height = generator.GetSurfaceHeight(worldX);

        Assert.AreEqual(SandboxTileIds.Air, generator.GetGeneratedTileId(height + 1, height));
        Assert.AreEqual(SandboxTileIds.Grass, generator.GetGeneratedTileId(height, height));
        Assert.AreEqual(SandboxTileIds.Dirt, generator.GetGeneratedTileId(height - 1, height));
        Assert.AreEqual(SandboxTileIds.Dirt, generator.GetGeneratedTileId(height - generator.DirtDepth + 1, height));
        Assert.AreEqual(SandboxTileIds.Stone, generator.GetGeneratedTileId(height - generator.DirtDepth, height));
        Assert.AreEqual(SandboxTileIds.Stone, generator.GetGeneratedTileId(height - 100, height));
    }

    [Test]
    public void SandboxTerrainGenerator_SurfaceTilesAreFullyLitAndUndergroundTilesAreDim()
    {
        SandboxTerrainGenerator generator = GoldenGenerator();
        int height = generator.GetSurfaceHeight(0);

        Assert.AreEqual((byte)15, generator.GenerateTile(height, height).light);
        Assert.AreEqual((byte)4, generator.GenerateTile(height - 1, height).light);
    }

    [Test]
    public void SandboxTerrainGenerator_DifferentSeedsProduceDifferentWorlds()
    {
        SandboxChunk a = GoldenGenerator(1337).GenerateChunk(Vector2Int.zero);
        SandboxChunk b = GoldenGenerator(9001).GenerateChunk(Vector2Int.zero);

        bool foundDifference = false;
        for (int x = 0; x < SandboxChunk.Size && !foundDifference; x++)
        {
            for (int y = 0; y < SandboxChunk.Size && !foundDifference; y++)
            {
                if (a.GetLocalTile(x, y).id != b.GetLocalTile(x, y).id)
                {
                    foundDifference = true;
                }
            }
        }

        Assert.IsTrue(foundDifference, "Distinct seeds should not generate identical chunks.");
    }
}
