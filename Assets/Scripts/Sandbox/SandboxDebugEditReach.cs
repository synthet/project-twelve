using UnityEngine;

/// <summary>
/// Shared reach checks for debug tile editing and visual override selection.
/// </summary>
public static class SandboxDebugEditReach
{
    /// <summary>
    /// True when the tile center is within player range or visible in the active camera.
    /// </summary>
    internal static bool IsWithinReach(
        Vector3 tileWorldCenter,
        Vector3 editorOrigin,
        Camera camera,
        float playerRange,
        bool allowVisibleOnScreen)
    {
        if (Vector2.Distance(editorOrigin, tileWorldCenter) <= playerRange)
        {
            return true;
        }

        if (!allowVisibleOnScreen || camera == null)
        {
            return false;
        }

        Vector3 viewport = camera.WorldToViewportPoint(tileWorldCenter);
        return viewport.x >= 0f
            && viewport.x <= 1f
            && viewport.y >= 0f
            && viewport.y <= 1f;
    }
}
