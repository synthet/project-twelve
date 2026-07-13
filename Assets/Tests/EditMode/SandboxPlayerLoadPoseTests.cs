using NUnit.Framework;
using UnityEngine;

/// <summary>
/// EditMode coverage for <see cref="SandboxPlayerLoadPose"/> overlap detection and upward lift.
/// </summary>
public sealed class SandboxPlayerLoadPoseTests
{
    private static readonly Vector2 DefaultOffset = Vector2.zero;
    private static readonly Vector2 DefaultSize = new Vector2(1f, 1f);
    private const float TileSize = 1f;

    [Test]
    public void OverlapsSolid_FalseWhenAllCoveredTilesAreAir()
    {
        Assert.IsFalse(SandboxPlayerLoadPose.OverlapsSolid(
            new Vector2(0.5f, 5.5f),
            DefaultOffset,
            DefaultSize,
            TileSize,
            (x, y) => false));
    }

    [Test]
    public void OverlapsSolid_TrueWhenColliderCoversSolidTile()
    {
        Assert.IsTrue(SandboxPlayerLoadPose.OverlapsSolid(
            new Vector2(0.5f, 2.5f),
            DefaultOffset,
            DefaultSize,
            TileSize,
            (x, y) => x == 0 && y == 2));
    }

    [Test]
    public void TryResolveStandingPose_ReturnsFalseWhenAlreadyClear()
    {
        Vector2 center = new Vector2(0.5f, 5.5f);
        bool resolved = SandboxPlayerLoadPose.TryResolveStandingPose(
            center,
            DefaultOffset,
            DefaultSize,
            TileSize,
            (x, y) => y < 3,
            out Vector2 result);

        Assert.IsFalse(resolved);
        Assert.AreEqual(center, result);
    }

    [Test]
    public void TryResolveStandingPose_LiftsToAirAboveSolidWithSupport()
    {
        // Solid fill y <= 10. Player buried at y=8 center (covers tile y=8).
        // First clear standing pose: body in air at y=11 center, feet supported by y=10.
        Vector2 buried = new Vector2(0.5f, 8.5f);
        bool resolved = SandboxPlayerLoadPose.TryResolveStandingPose(
            buried,
            DefaultOffset,
            DefaultSize,
            TileSize,
            (x, y) => y <= 10,
            out Vector2 result);

        Assert.IsTrue(resolved);
        Assert.AreEqual(0.5f, result.x, 0.0001f);
        Assert.AreEqual(11.5f, result.y, 0.0001f);
        Assert.IsFalse(SandboxPlayerLoadPose.OverlapsSolid(
            result, DefaultOffset, DefaultSize, TileSize, (x, y) => y <= 10));
    }

    [Test]
    public void TryResolveStandingPose_ReturnsFalseWhenNoAirWithinLiftBudget()
    {
        Vector2 buried = new Vector2(0.5f, 2.5f);
        bool resolved = SandboxPlayerLoadPose.TryResolveStandingPose(
            buried,
            DefaultOffset,
            DefaultSize,
            TileSize,
            (x, y) => true,
            out Vector2 result);

        Assert.IsFalse(resolved);
        Assert.AreEqual(buried, result);
    }

    [Test]
    public void TryResolveStandingPose_FallsBackToFirstAirPoseWhenNoSupportExists()
    {
        // Tiny collider: one tile up clears the solid and the support probe misses it too.
        Vector2 tiny = new Vector2(1f, 0.1f);
        Vector2 buried = new Vector2(0.5f, 2.5f);
        bool resolved = SandboxPlayerLoadPose.TryResolveStandingPose(
            buried,
            DefaultOffset,
            tiny,
            TileSize,
            (x, y) => y == 2,
            out Vector2 result);

        Assert.IsTrue(resolved);
        Assert.AreEqual(3.5f, result.y, 0.0001f);
    }
}