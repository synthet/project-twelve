using NUnit.Framework;
using UnityEngine;

public sealed class SandboxWorldCoordinateTests
{
    [TestCase(0, 0, 0, 0, 0, 0)]
    [TestCase(31, 31, 0, 0, 31, 31)]
    [TestCase(32, 32, 1, 1, 0, 0)]
    [TestCase(-1, -1, -1, -1, 31, 31)]
    [TestCase(-32, -32, -1, -1, 0, 0)]
    [TestCase(-33, 33, -2, 1, 31, 1)]
    public void WorldCoordinatesMapToExpectedChunkAndLocalCoordinates(
        int worldX,
        int worldY,
        int expectedChunkX,
        int expectedChunkY,
        int expectedLocalX,
        int expectedLocalY)
    {
        Vector2Int chunk = SandboxWorld.WorldToChunkCoord(worldX, worldY);
        Vector2Int local = SandboxWorld.WorldToLocalCoord(worldX, worldY);

        Assert.AreEqual(new Vector2Int(expectedChunkX, expectedChunkY), chunk);
        Assert.AreEqual(new Vector2Int(expectedLocalX, expectedLocalY), local);
    }

    [TestCase(0, 0, true)]
    [TestCase(31, 31, true)]
    [TestCase(-1, 0, false)]
    [TestCase(0, -1, false)]
    [TestCase(32, 0, false)]
    [TestCase(0, 32, false)]
    public void LocalBoundsMatchChunkSize(int localX, int localY, bool expected)
    {
        Assert.AreEqual(expected, SandboxChunk.IsLocalInBounds(localX, localY));
    }
}
