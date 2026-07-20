using NUnit.Framework;
using ProjectTwelve.UI.Theme;
using UnityEngine;

namespace ProjectTwelve.Tests.UI
{
    /// <summary>
    /// Theme-token fallback: an unconfigured theme still resolves every color role from the built-in
    /// defaults, and the provider falls back to a default theme when none is assigned — so controls never
    /// render "unthemed".
    /// </summary>
    public sealed class UiThemeTests
    {
        [Test]
        public void EmptyTheme_ResolvesRolesFromBuiltInDefaults()
        {
            UiTheme theme = ScriptableObject.CreateInstance<UiTheme>();
            try
            {
                Assert.AreEqual(UiTheme.DefaultColor(UiColorRole.Selected), theme.ResolveColor(UiColorRole.Selected));
                Assert.AreEqual(UiTheme.DefaultColor(UiColorRole.Destructive), theme.ResolveColor(UiColorRole.Destructive));
                Assert.AreEqual(theme.ResolveColor(UiColorRole.Focused), theme.ResolveStateColor(UiControlState.Focused));
            }
            finally
            {
                Object.DestroyImmediate(theme);
            }
        }

        [Test]
        public void EmptyTheme_HasSensibleDefaultTokens()
        {
            UiTheme theme = ScriptableObject.CreateInstance<UiTheme>();
            try
            {
                Assert.Greater(theme.SlotSize, 0);
                Assert.Greater(theme.FontSizeBody, 0);
                Assert.Greater(theme.TooltipDelay, 0f);
            }
            finally
            {
                Object.DestroyImmediate(theme);
            }
        }

        [Test]
        public void Provider_WithoutAssignedTheme_UsesFallback()
        {
            UiThemeProvider provider = new UiThemeProvider();
            Assert.IsFalse(provider.HasAssignedTheme);
            Assert.IsNotNull(provider.Active);
            Assert.AreEqual(UiTheme.DefaultColor(UiColorRole.Normal), provider.Active.ResolveColor(UiColorRole.Normal));
        }

        [Test]
        public void Provider_SetTheme_RaisesChanged()
        {
            UiThemeProvider provider = new UiThemeProvider();
            UiTheme theme = ScriptableObject.CreateInstance<UiTheme>();
            int raised = 0;
            provider.ThemeChanged += _ => raised++;
            try
            {
                provider.SetTheme(theme);
                Assert.IsTrue(provider.HasAssignedTheme);
                Assert.AreEqual(1, raised);
            }
            finally
            {
                Object.DestroyImmediate(theme);
            }
        }
    }
}
