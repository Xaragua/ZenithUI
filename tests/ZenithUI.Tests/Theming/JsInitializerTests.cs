using System.Text.RegularExpressions;

namespace ZenithUI.Tests.Theming;

/// <summary>
/// Guards the Blazor JS initializer, which is what keeps the palette across an enhanced
/// navigation.
/// </summary>
/// <remarks>
/// <para>
/// The bug it exists for: enhanced navigation merges the server's document into the live DOM,
/// attributes on <c>&lt;html&gt;</c> included. The server cannot know the viewer's stored theme, so
/// every response carries an <c>&lt;html&gt;</c> without <c>data-zen-theme</c> and the merge removes
/// the one the theme service set. The switcher then appears to theme only the current page.
/// </para>
/// <para>
/// None of that is reachable from bUnit — there is no navigation, no Blazor startup and no
/// JavaScript. What can be asserted is the contract that makes the fix load at all: the file has
/// to be named for Blazor's auto-discovery, export the hook Blazor calls, and listen to the right
/// event. Each of those is silent when wrong: the theme simply goes back to resetting.
/// </para>
/// </remarks>
public partial class JsInitializerTests
{
    private static readonly string Initializer = File.ReadAllText(
        Path.Combine(AppContext.BaseDirectory, "Theming", "ZenithUI.lib.module.js"));

    [Fact]
    public void TheInitializer_ExportsTheHookBlazorCalls()
    {
        // Blazor Web App calls afterWebStarted on each auto-discovered initializer. A differently
        // named export is never invoked and never complained about.
        Initializer.ShouldContain("export function afterWebStarted");
    }

    [Fact]
    public void TheInitializer_ListensForEnhancedLoad()
    {
        // 'enhancedload' fires after every enhanced navigation, enhanced form post and streaming
        // update - each one a merge that can drop the attribute.
        Initializer.ShouldContain("'enhancedload'");
    }

    [Fact]
    public void TheInitializer_ReusesTheThemeModule_RatherThanCopyingItsLogic()
    {
        Initializer.ShouldContain("./js/zen-theme.js");

        // There are already two copies of the "system means no attribute" rule, kept in lockstep
        // by ThemeInitScriptTests because each has a reason to exist. This file has no such
        // reason: it runs after startup, when importing the real module costs nothing. A third
        // copy here would be the one that drifts.
        //
        // Comments are stripped first: this file explains the storage rule at length, and matching
        // the prose would be testing the documentation rather than the code.
        var code = StripComments(Initializer);

        code.ShouldNotContain("localStorage");
        code.ShouldNotContain("setAttribute");
    }

    private static string StripComments(string source) =>
        LineComment().Replace(BlockComment().Replace(source, string.Empty), string.Empty);

    [GeneratedRegex(@"/\*.*?\*/", RegexOptions.Singleline)]
    private static partial Regex BlockComment();

    [GeneratedRegex(@"//[^\n]*")]
    private static partial Regex LineComment();
}
