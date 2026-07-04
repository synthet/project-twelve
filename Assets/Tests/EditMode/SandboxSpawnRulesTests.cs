using NUnit.Framework;
using ProjectTwelve.Sandbox.Nav;
using UnityEngine;

/// <summary>
/// P2-AI-001 spawn-rule table tests: distance band, air/support, camera exclusion, light
/// threshold, population cap, and the loaded-chunk boundary — all on a pure grid.
/// </summary>
public sealed class SandboxSpawnRulesTests
{
    // Flat world: ground top at y = 10, so foot cells sit at y = 11. Player foot at (10, 11).
    private static readonly Vector2Int PlayerFoot = new Vector2Int(10, 11);
    private static readonly RectInt CameraRect = new RectInt(5, 8, 10, 8); // around the player

    private static NavTestGrid CreateFlatWorld()
    {
        return NavTestGrid.Flat(80, 30, 10);
    }

    private static int SurfaceAtFootLevel(int x) => 11;

    private static bool Validate(NavTestGrid grid, Vector2Int cell, System.Func<Vector2Int, int> lightAt = null,
        System.Func<int, int> surfaceHeightAt = null)
    {
        surfaceHeightAt ??= SurfaceAtFootLevel;
        lightAt ??= cell2 => SandboxSpawnRules.InterimLightAt(cell2, surfaceHeightAt);
        return SandboxSpawnRules.IsValidSpawnCell(grid, cell, PlayerFoot, CameraRect, lightAt, surfaceHeightAt);
    }

    [TestCase(33, 11, false, TestName = "TooClose_Chebyshev23_Rejected")]
    [TestCase(34, 11, true, TestName = "BandLowerEdge_Chebyshev24_Accepted")]
    [TestCase(74, 11, true, TestName = "BandUpperEdge_Chebyshev64_Accepted")]
    [TestCase(75, 11, false, TestName = "TooFar_Chebyshev65_Rejected")]
    public void DistanceBand_IsEnforced(int x, int y, bool expected)
    {
        Assert.AreEqual(expected, Validate(CreateFlatWorld(), new Vector2Int(x, y)));
    }

    [Test]
    public void CellWithoutSupport_IsRejected()
    {
        Assert.IsFalse(Validate(CreateFlatWorld(), new Vector2Int(40, 15)), "mid-air cell must be rejected");
    }

    [Test]
    public void SolidCell_IsRejected()
    {
        Assert.IsFalse(Validate(CreateFlatWorld(), new Vector2Int(40, 10)), "cell inside the ground must be rejected");
    }

    [Test]
    public void CellInsidePaddedCameraRect_IsRejected()
    {
        // Camera rect spans x in [5, 15); with 1 tile padding the exclusion covers x in [4, 16).
        NavTestGrid grid = CreateFlatWorld();
        RectInt hugeCamera = new RectInt(5, 8, 29, 8); // x in [5, 34) -> padded [4, 35)

        bool insidePadding = SandboxSpawnRules.IsValidSpawnCell(
            grid, new Vector2Int(34, 11), PlayerFoot, hugeCamera,
            cell => 15, SurfaceAtFootLevel);
        bool justOutside = SandboxSpawnRules.IsValidSpawnCell(
            grid, new Vector2Int(35, 11), PlayerFoot, hugeCamera,
            cell => 15, SurfaceAtFootLevel);

        Assert.IsFalse(insidePadding, "cell inside camera rect + padding must be rejected");
        Assert.IsTrue(justOutside, "cell one tile beyond the padded rect must be accepted");
    }

    [TestCase(0, true, TestName = "UndergroundDark_Light0_Accepted")]
    [TestCase(3, true, TestName = "UndergroundAtThreshold_Light3_Accepted")]
    [TestCase(4, false, TestName = "UndergroundLit_Light4_Rejected")]
    public void UndergroundSpawns_RespectLightThreshold(int light, bool expected)
    {
        // Surface reported well above the cell, so the candidate counts as underground.
        Assert.AreEqual(expected, Validate(CreateFlatWorld(), new Vector2Int(40, 11),
            lightAt: cell => light, surfaceHeightAt: x => 20));
    }

    [Test]
    public void SurfaceSpawn_IgnoresLightThreshold()
    {
        // Full daylight (15) at foot level is fine because the cell is inside the surface band.
        Assert.IsTrue(Validate(CreateFlatWorld(), new Vector2Int(40, 11), lightAt: cell => 15));
    }

    [Test]
    public void UnloadedChunk_IsRejected()
    {
        NavTestGrid grid = CreateFlatWorld();
        grid.MarkChunkUnloaded(new Vector2Int(1, 0)); // covers x in [32, 64)

        Assert.IsFalse(Validate(grid, new Vector2Int(40, 11)), "never spawn in unloaded chunks");
    }

    [TestCase(0, true)]
    [TestCase(7, true)]
    [TestCase(8, false)]
    [TestCase(9, false)]
    public void PopulationCap_IsEnforced(int liveCount, bool expected)
    {
        Assert.AreEqual(expected, SandboxSpawnRules.CanSpawn(liveCount));
    }

    [Test]
    public void InterimLightModel_ReadsDarkUndergroundAndLitSurface()
    {
        System.Func<int, int> surfaceAt = x => 20;

        Assert.AreEqual(0, SandboxSpawnRules.InterimLightAt(new Vector2Int(0, 10), surfaceAt), "underground reads unlit");
        Assert.AreEqual(15, SandboxSpawnRules.InterimLightAt(new Vector2Int(0, 19), surfaceAt), "surface band reads full light");
    }

    [Test]
    public void CameraWorldRect_ConvertsToCoveringTileRect()
    {
        RectInt tileRect = SandboxSpawnRules.CameraWorldRectToTileRect(new Rect(0.5f, 0.5f, 3f, 2f), 1f);

        Assert.AreEqual(new RectInt(0, 0, 4, 3), tileRect);
    }
}
