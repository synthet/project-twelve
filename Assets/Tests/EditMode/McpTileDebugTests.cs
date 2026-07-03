using NUnit.Framework;
using ProjectTwelve.RuntimeMcp;

public sealed class McpTileDebugTests
{
    [Test]
    public void TryResolveCenteredBounds_RadiusMode_UsesInclusiveCenter()
    {
        Assert.IsTrue(McpTileDebug.TryResolveCenteredBounds(
            10,
            20,
            radiusX: 1,
            radiusY: 1,
            maxCells: 25,
            out int xMin,
            out int yMin,
            out int xMax,
            out int yMax,
            out string error));
        Assert.IsNull(error);
        Assert.AreEqual(9, xMin);
        Assert.AreEqual(11, xMax);
        Assert.AreEqual(19, yMin);
        Assert.AreEqual(21, yMax);
    }

    [Test]
    public void TryResolveExplicitBounds_RejectsOversizedArea()
    {
        Assert.IsFalse(McpTileDebug.TryResolveExplicitBounds(0, 0, 99, 99, maxCells: 16, out string error));
        StringAssert.Contains("maximum is 16", error);
    }

    [Test]
    public void GetTileName_MapsKnownIds()
    {
        Assert.AreEqual("Grass", McpTileDebug.GetTileName(SandboxTileIds.Grass));
        Assert.AreEqual("Stone", McpTileDebug.GetTileName(SandboxTileIds.Stone));
        Assert.AreEqual("Air", McpTileDebug.GetTileName(SandboxTileIds.Air));
    }
}
