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
}
