namespace ZenithUI.Tests.Theming;

/// <summary>
/// Guards the rule that hides the focus ring on a heading focused from code.
/// </summary>
/// <remarks>
/// Blazor's <c>FocusOnNavigate</c> gives the page's h1 <c>tabindex="-1"</c> and focuses it after
/// every navigation. Browsers treat that focus as <c>:focus-visible</c>, so without this rule every
/// page opens with a box drawn around its title. The rule has to stay narrow in two directions, and
/// those are what these tests pin down: widened to any <c>tabindex="-1"</c>, it would hide the ring
/// on roving-tabindex items (tree rows, calendar days) during the instant between the focus call
/// and the re-render that moves <c>tabindex="0"</c>; unscoped from <c>.zen-root</c>, it would style
/// documents the library was never invited into.
/// </remarks>
public class ProgrammaticFocusTests
{
    private const string Selector = ".zen-root :is(h1, h2, h3, h4, h5, h6)[tabindex=\"-1\"]:focus";

    private static readonly string Css = File.ReadAllText(
        Path.Combine(AppContext.BaseDirectory, "Styles", "base.css"));

    [Fact]
    public void ProgrammaticallyFocusedHeading_HasNoRing()
    {
        var start = Css.IndexOf(Selector, StringComparison.Ordinal);
        start.ShouldBeGreaterThanOrEqualTo(0, $"base.css no longer contains `{Selector}`.");

        var body = Css[start..Css.IndexOf('}', start)];
        body.ShouldContain("outline: none");
    }

    [Fact]
    public void OnlyHeadingsLoseTheRing() =>
        // Any other `[tabindex="-1"]` outline suppression would reach roving-tabindex items.
        Css.Split('\n')
            .Where(line => line.Contains("[tabindex=\"-1\"]", StringComparison.Ordinal) && line.TrimStart().StartsWith('.'))
            .ShouldAllBe(line => line.Trim().StartsWith(Selector, StringComparison.Ordinal));
}
