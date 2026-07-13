using System;
using UnityEngine;

/// <summary>
/// Pure helpers for save/load player pose: overlap detection and upward lift out of solid tiles.
/// </summary>
public static class SandboxPlayerLoadPose
{
    /// <summary>Maximum tiles to scan upward when resolving an overlapping load pose.</summary>
    public const int MaxLiftTiles = 32;

    private const float BoundsEpsilon = 0.001f;
    private const float SupportProbeInset = 0.05f;

    /// <summary>
    /// Returns true when any tile intersecting the collider AABB at <paramref name="center"/> is solid.
    /// </summary>
    public static bool OverlapsSolid(
        Vector2 center,
        Vector2 colliderOffset,
        Vector2 colliderSize,
        float tileSize,
        Func<int, int, bool> isSolid)
    {
        if (isSolid == null || tileSize <= 0f)
        {
            return false;
        }

        GetColliderBounds(center, colliderOffset, colliderSize, out Vector2 min, out Vector2 max);
        EnumerateCoveredTiles(min, max, tileSize, out int xMin, out int xMax, out int yMin, out int yMax);
        for (int x = xMin; x <= xMax; x++)
        {
            for (int y = yMin; y <= yMax; y++)
            {
                if (isSolid(x, y))
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// When <paramref name="center"/> overlaps solid tiles, finds the nearest upward pose with an
    /// air body. Prefers a pose with solid support under the feet; otherwise returns the first
    /// fully-air pose within <see cref="MaxLiftTiles"/>.
    /// </summary>
    public static bool TryResolveStandingPose(
        Vector2 center,
        Vector2 colliderOffset,
        Vector2 colliderSize,
        float tileSize,
        Func<int, int, bool> isSolid,
        out Vector2 resolved)
    {
        resolved = center;
        if (isSolid == null || tileSize <= 0f)
        {
            return false;
        }

        if (!OverlapsSolid(center, colliderOffset, colliderSize, tileSize, isSolid))
        {
            return false;
        }

        Vector2? firstAirPose = null;
        for (int step = 1; step <= MaxLiftTiles; step++)
        {
            Vector2 candidate = new Vector2(center.x, center.y + step * tileSize);
            if (OverlapsSolid(candidate, colliderOffset, colliderSize, tileSize, isSolid))
            {
                continue;
            }

            firstAirPose ??= candidate;
            if (HasSolidSupport(candidate, colliderOffset, colliderSize, tileSize, isSolid))
            {
                resolved = candidate;
                return true;
            }
        }

        if (firstAirPose.HasValue)
        {
            resolved = firstAirPose.Value;
            return true;
        }

        return false;
    }

    /// <summary>Builds the world-space AABB for a box collider at the given center.</summary>
    public static void GetColliderBounds(
        Vector2 center,
        Vector2 colliderOffset,
        Vector2 colliderSize,
        out Vector2 min,
        out Vector2 max)
    {
        Vector2 half = colliderSize * 0.5f;
        Vector2 worldCenter = center + colliderOffset;
        min = worldCenter - half;
        max = worldCenter + half;
    }

    private static bool HasSolidSupport(
        Vector2 center,
        Vector2 colliderOffset,
        Vector2 colliderSize,
        float tileSize,
        Func<int, int, bool> isSolid)
    {
        GetColliderBounds(center, colliderOffset, colliderSize, out Vector2 min, out Vector2 max);
        float probeY = min.y - SupportProbeInset;
        int y = Mathf.FloorToInt(probeY / tileSize);
        int xMin = Mathf.FloorToInt((min.x + BoundsEpsilon) / tileSize);
        int xMax = Mathf.FloorToInt((max.x - BoundsEpsilon) / tileSize);
        if (xMax < xMin)
        {
            xMax = xMin;
        }

        for (int x = xMin; x <= xMax; x++)
        {
            if (isSolid(x, y))
            {
                return true;
            }
        }

        return false;
    }

    private static void EnumerateCoveredTiles(
        Vector2 min,
        Vector2 max,
        float tileSize,
        out int xMin,
        out int xMax,
        out int yMin,
        out int yMax)
    {
        xMin = Mathf.FloorToInt((min.x + BoundsEpsilon) / tileSize);
        xMax = Mathf.FloorToInt((max.x - BoundsEpsilon) / tileSize);
        yMin = Mathf.FloorToInt((min.y + BoundsEpsilon) / tileSize);
        yMax = Mathf.FloorToInt((max.y - BoundsEpsilon) / tileSize);
        if (xMax < xMin)
        {
            xMax = xMin;
        }

        if (yMax < yMin)
        {
            yMax = yMin;
        }
    }
}
