using System.Text.RegularExpressions;

namespace ZenithUI.Tests.Theming;

/// <summary>
/// Asserts that the two copies of the anti-flash initialiser stay in lockstep.
/// </summary>
/// <remarks>
/// <para>
/// There are two on purpose. <c>ZenThemeScript</c> inlines the logic into HTML the server is
/// already sending, which is the cheapest possible form and the only one that costs no request;
/// <c>wwwroot/js/zen-theme-init.js</c> exists because a standalone WebAssembly app's host page is
/// a static <c>index.html</c> that no Razor component can write into.
/// </para>
/// <para>
/// Duplication is the right call — importing a module would reintroduce the round-trip the inline
/// copy exists to avoid — but drift between them would be invisible: each is correct on its own,
/// and the failure would be a palette flash in one hosting model and not the other. So the
/// comparison is mechanical, on the statements rather than the formatting.
/// </para>
/// </remarks>
public partial class ThemeInitScriptTests
{
    private static readonly string Component = File.ReadAllText(
        Path.Combine(AppContext.BaseDirectory, "Theming", "ZenThemeScript.razor"));

    private static readonly string Standalone = File.ReadAllText(
        Path.Combine(AppContext.BaseDirectory, "Theming", "zen-theme-init.js"));

    [Fact]
    public void TheTwoCopies_AreTheSameProgram()
    {
        var inlined = Normalize(InlineScript());
        var standalone = Normalize(Standalone);

        inlined.ShouldNotBeNullOrWhiteSpace();

        standalone.ShouldBe(inlined,
            "wwwroot/js/zen-theme-init.js and Components/Theming/ZenThemeScript.razor have drifted " +
            "apart. Both set data-zen-theme from the 'zen-theme' storage key before first paint; a " +
            "difference between them is a theme flash in one hosting model and not the other.");
    }

    [Theory]
    [InlineData("'zen-theme'")]
    [InlineData("'data-zen-theme'")]
    [InlineData("removeAttribute")]
    public void BothCopies_KeepTheContractSharedWithZenThemeJs(string fragment)
    {
        // removeAttribute is listed because it is the rule that is easiest to "simplify" away:
        // writing the resolved value for System mode freezes the page against later OS changes.
        InlineScript().ShouldContain(fragment);
        Standalone.ShouldContain(fragment);
    }

    /// <summary>The body of the single &lt;script&gt; element in ZenThemeScript.razor.</summary>
    private static string InlineScript()
    {
        var match = ScriptElement().Match(Component);
        match.Success.ShouldBeTrue("ZenThemeScript.razor no longer contains a <script> element.");
        return match.Groups[1].Value;
    }

    private static string Normalize(string script) =>
        Whitespace().Replace(LineComment().Replace(BlockComment().Replace(script, " "), " "), string.Empty);

    [GeneratedRegex(@"<script>(.*?)</script>", RegexOptions.Singleline)]
    private static partial Regex ScriptElement();

    [GeneratedRegex(@"/\*.*?\*/", RegexOptions.Singleline)]
    private static partial Regex BlockComment();

    [GeneratedRegex(@"//[^\n]*")]
    private static partial Regex LineComment();

    [GeneratedRegex(@"\s+")]
    private static partial Regex Whitespace();
}
