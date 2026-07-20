using NUnit.Framework;
using ProjectTwelve.UI;
using UnityEngine;

namespace ProjectTwelve.Tests.UI
{
    /// <summary>
    /// Verifies the pixel-perfect scale math: the framework only ever applies integer scales (so pixel-art
    /// borders never resample), and the user preference is clamped so a layout can never scale off-screen.
    /// </summary>
    public sealed class UiFrameworkScaleTests
    {
        private static readonly Vector2 Reference = new Vector2(1280f, 720f);

        [Test]
        public void MaxIntegerScale_FloorsTheFitRatio()
        {
            Assert.AreEqual(1, UiScaleController.ComputeMaxIntegerScale(1280f, 720f, Reference));
            Assert.AreEqual(1, UiScaleController.ComputeMaxIntegerScale(1920f, 1080f, Reference), "1.5x floors to 1.");
            Assert.AreEqual(2, UiScaleController.ComputeMaxIntegerScale(2560f, 1440f, Reference));
            Assert.AreEqual(3, UiScaleController.ComputeMaxIntegerScale(3840f, 2160f, Reference));
        }

        [Test]
        public void MaxIntegerScale_UsesTheLimitingAxis_ForUltrawideAndTall()
        {
            // 2560x1080 (21:9): height limits to 1x even though width would allow 2x.
            Assert.AreEqual(1, UiScaleController.ComputeMaxIntegerScale(2560f, 1080f, Reference));
        }

        [Test]
        public void MaxIntegerScale_NeverBelowOne()
        {
            Assert.AreEqual(1, UiScaleController.ComputeMaxIntegerScale(640f, 360f, Reference));
            Assert.AreEqual(1, UiScaleController.ComputeMaxIntegerScale(100f, 100f, new Vector2(0f, 0f)));
        }

        [Test]
        public void EffectiveScale_AutoPreferenceReturnsMaxFit()
        {
            Assert.AreEqual(3, UiScaleController.ComputeEffectiveScale(0, 3));
            Assert.AreEqual(3, UiScaleController.ComputeEffectiveScale(-5, 3));
        }

        [Test]
        public void EffectiveScale_ClampsUserPreferenceIntoRange()
        {
            Assert.AreEqual(2, UiScaleController.ComputeEffectiveScale(2, 3), "In-range preference honored.");
            Assert.AreEqual(3, UiScaleController.ComputeEffectiveScale(9, 3), "Above max clamps down to max.");
            Assert.AreEqual(1, UiScaleController.ComputeEffectiveScale(1, 3), "Minimum is 1.");
        }
    }
}
