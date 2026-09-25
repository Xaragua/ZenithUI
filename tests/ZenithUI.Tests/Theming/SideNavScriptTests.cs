namespace ZenithUI.Tests.Theming;

/// <summary>
/// The collapsed side nav keeps its state in two places that must agree: the inline script
/// ZenSideNav renders to restore it before first paint, and <c>zen-sidenav.js</c>, which writes it.
/// </summary>
/// <remarks>
/// Neither can import the other - the inline copy exists precisely to avoid a request - and a
/// mismatch is silent: the toggle works, and the rail springs open again on every full page load.
/// </remarks>
public class SideNavScriptTests
{
    private static readonly string Component = File.ReadAllText(
        Path.Combine(AppContext.BaseDirectory, "Theming", "ZenSideNav.razor"));

    private static readonly string Module = File.ReadAllText(
        Path.Combine(AppContext.BaseDirectory, "Theming", "zen-sidenav.js"));

    private static readonly string Initializer = File.ReadAllText(
        Path.Combine(AppContext.BaseDirectory, "Theming", "ZenithUI.lib.module.js"));

    [Theory]
    [InlineData("'zen-sidenav'")]
    [InlineData("'data-zen-sidenav'")]
    [InlineData("'collapsed'")]
    public void TheInlineScriptAndTheModule_ShareTheirContract(string fragment)
    {
        Component.ShouldContain(fragment);
        Module.ShouldContain(fragment);
    }

    [Fact]
    public void EnhancedNavigation_ReappliesTheCollapsedState()
    {
        // Enhanced navigation merges <html>'s attributes back to the server's, which drops the
        // collapsed state exactly as it drops the theme. Without this the rail re-expands on every
        // link followed.
        Initializer.ShouldContain("applyStoredSideNav()");
        Initializer.ShouldContain("./js/zen-sidenav.js");
    }

    [Fact]
    public void TheListeners_AreInstalledInBothHostingModels()
    {
        // afterWebStarted for a Blazor Web App, afterStarted for standalone WebAssembly. Only the
        // first existed before, and the collapse button would have done nothing in the second.
        Initializer.ShouldContain("export function afterWebStarted");
        Initializer.ShouldContain("export function afterStarted");
    }
}
