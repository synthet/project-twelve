using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Drives an integer <see cref="CanvasScaler.scaleFactor"/> so point-filtered
/// pixel-art HUD sprites are never resampled at fractional scales, which drops
/// or doubles individual pixel rows and makes frames look jagged or damaged.
/// Uses <see cref="CanvasScaler.referenceResolution"/> as the design size and
/// floors the fit ratio so the reference layout always fits on screen.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(CanvasScaler))]
public sealed class SandboxHudPixelPerfectScaler : MonoBehaviour
{
    private CanvasScaler scaler;
    private int lastScreenWidth;
    private int lastScreenHeight;

    public static int ComputeScaleFactor(float screenWidth, float screenHeight, Vector2 referenceResolution)
    {
        if (referenceResolution.x <= 0f || referenceResolution.y <= 0f)
        {
            return 1;
        }

        float fit = Mathf.Min(screenWidth / referenceResolution.x, screenHeight / referenceResolution.y);
        return Mathf.Max(1, Mathf.FloorToInt(fit));
    }

    private void Awake()
    {
        scaler = GetComponent<CanvasScaler>();
        Apply();
    }

    private void Update()
    {
        if (Screen.width != lastScreenWidth || Screen.height != lastScreenHeight)
        {
            Apply();
        }
    }

    private void Apply()
    {
        lastScreenWidth = Screen.width;
        lastScreenHeight = Screen.height;
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
        scaler.scaleFactor = ComputeScaleFactor(lastScreenWidth, lastScreenHeight, scaler.referenceResolution);
    }
}
