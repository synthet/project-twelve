using System;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectTwelve.UI
{
    /// <summary>
    /// Drives an <b>integer</b> <see cref="CanvasScaler.scaleFactor"/> (ConstantPixelSize) so point-filtered
    /// pixel-art never resamples at fractional scales, and layers a user-selectable scale preference on top
    /// while staying integer. Generalises the HUD-only <c>SandboxHudPixelPerfectScaler</c> into a reusable
    /// framework control that also fires an event when the effective scale changes (for persistence / UI).
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CanvasScaler))]
    public sealed class UiScaleController : MonoBehaviour
    {
        [Tooltip("User scale preference. 0 = auto (largest integer that fits the reference layout).")]
        [SerializeField] private int userScale;

        private CanvasScaler scaler;
        private int lastScreenWidth;
        private int lastScreenHeight;
        private int appliedScale;

        /// <summary>Raised whenever the applied integer scale changes (resolution or preference change).</summary>
        public event Action<int> ScaleChanged;

        /// <summary>The integer scale currently applied to the canvas.</summary>
        public int AppliedScale => appliedScale;

        /// <summary>Largest integer scale at which the reference layout still fits the current screen.</summary>
        public int MaxScaleForScreen =>
            scaler != null ? ComputeMaxIntegerScale(Screen.width, Screen.height, scaler.referenceResolution) : 1;

        /// <summary>
        /// Largest integer scale at which <paramref name="referenceResolution"/> fits the screen. Identical
        /// math to the original HUD scaler so existing pixel-perfect behaviour is preserved.
        /// </summary>
        public static int ComputeMaxIntegerScale(float screenWidth, float screenHeight, Vector2 referenceResolution)
        {
            if (referenceResolution.x <= 0f || referenceResolution.y <= 0f)
            {
                return 1;
            }

            float fit = Mathf.Min(screenWidth / referenceResolution.x, screenHeight / referenceResolution.y);
            return Mathf.Max(1, Mathf.FloorToInt(fit));
        }

        /// <summary>
        /// Resolves the effective integer scale from a user preference. <paramref name="userScale"/> of 0 (or
        /// negative) means "auto" and yields the largest fitting integer; otherwise it is clamped to
        /// [1, maxIntegerScale] so the layout can never be scaled off-screen.
        /// </summary>
        public static int ComputeEffectiveScale(int userScale, int maxIntegerScale)
        {
            int max = Mathf.Max(1, maxIntegerScale);
            if (userScale <= 0)
            {
                return max;
            }

            return Mathf.Clamp(userScale, 1, max);
        }

        /// <summary>Sets the user scale preference (0 = auto) and reapplies immediately.</summary>
        public void SetUserScale(int scale)
        {
            userScale = Mathf.Max(0, scale);
            Apply();
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
            if (scaler == null)
            {
                return;
            }

            lastScreenWidth = Screen.width;
            lastScreenHeight = Screen.height;
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;

            int max = ComputeMaxIntegerScale(lastScreenWidth, lastScreenHeight, scaler.referenceResolution);
            int effective = ComputeEffectiveScale(userScale, max);
            scaler.scaleFactor = effective;

            if (effective != appliedScale)
            {
                appliedScale = effective;
                ScaleChanged?.Invoke(effective);
            }
        }
    }
}
