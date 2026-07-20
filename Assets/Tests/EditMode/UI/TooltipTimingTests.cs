using NUnit.Framework;
using ProjectTwelve.UI.Controls;

namespace ProjectTwelve.Tests.UI
{
    /// <summary>Tooltip appears only after the configured hover delay elapses.</summary>
    public sealed class TooltipTimingTests
    {
        [Test]
        public void ReadyToShow_OnlyAfterDelay()
        {
            Assert.IsFalse(TooltipService.ReadyToShow(0.0f, 0.4f));
            Assert.IsFalse(TooltipService.ReadyToShow(0.39f, 0.4f));
            Assert.IsTrue(TooltipService.ReadyToShow(0.4f, 0.4f));
            Assert.IsTrue(TooltipService.ReadyToShow(1.2f, 0.4f));
        }

        [Test]
        public void ReadyToShow_ZeroDelayShowsImmediately()
        {
            Assert.IsTrue(TooltipService.ReadyToShow(0f, 0f));
        }
    }
}
