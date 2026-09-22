using System.Text.RegularExpressions;

namespace ZenithUI.Tests.Theming;

/// <summary>
/// Guards the geometry of the border-emphasis utilities.
/// </summary>
/// <remarks>
/// <para>
/// These assert against CSS text, which is not how most component behaviour should be tested. They
/// exist because the alternative is nothing: bUnit has no layout engine, so no rendering test can
/// measure how thick a focus indicator actually appears, and the rule at stake is pure arithmetic
/// that reads as correct while being wrong.
/// </para>
/// <para>
/// The bug this was written for: <c>outline-offset: -1px</c> laid a 1px outline directly on top of
/// the 1px border rather than beside it, so the band stayed 1px no matter what
/// <c>--zen-ring-width</c> said. Everything built, every test passed, and the indicator was half
/// the intended thickness.
/// </para>
/// <para>
/// A CSS outline paints <i>outward</i> from the outline edge, which sits at <c>outline-offset</c>
/// from the border-box edge. For a band spanning <c>[0, W]</c> inward where a 1px border already
/// covers <c>[0, 1]</c>, the offset must be <c>-W</c> and the width <c>W - 1px</c>.
/// </para>
/// </remarks>
public partial class BorderEmphasisTests
{
    private static readonly string Css = File.ReadAllText(
        Path.Combine(AppContext.BaseDirectory, "Styles", "base.css"));

    private static readonly string[] Utilities =
    [
        "zen-focus-border",
        "zen-focus-border-within",
        "zen-interactive-border",
    ];

    [Theory]
    [InlineData("zen-focus-border")]
    [InlineData("zen-focus-border-within")]
    [InlineData("zen-interactive-border")]
    public void EmphasisUtility_OffsetsByTheFullRingWidth(string utility)
    {
        var body = ExtractUtility(utility);

        // -1px is the wrong-but-plausible value that halves the band.
        body.Contains("outline-offset: -1px", StringComparison.Ordinal).ShouldBeFalse(
            $"{utility}: an outline offset of -1px sits on top of the 1px border instead of beside " +
            "it, leaving a 1px indicator regardless of --zen-ring-width.");

        body.Contains("outline-offset: calc(-1 * var(--zen-ring-width))", StringComparison.Ordinal).ShouldBeTrue(
            $"{utility}: the outline edge has to sit a full --zen-ring-width inside the border-box " +
            "edge for the outline to paint back out into the space beside the border.");
    }

    [Theory]
    [InlineData("zen-focus-border")]
    [InlineData("zen-focus-border-within")]
    [InlineData("zen-interactive-border")]
    public void EmphasisUtility_WidthMakesUpTheRemainderOfTheBand(string utility) =>
        // The border supplies 1px; the outline supplies the rest.
        ExtractUtility(utility).Contains("calc(var(--zen-ring-width) - 1px)", StringComparison.Ordinal).ShouldBeTrue(
            $"{utility}: the outline covers the band minus the 1px the border already draws.");

    [Theory]
    [InlineData("zen-focus-border")]
    [InlineData("zen-focus-border-within")]
    [InlineData("zen-interactive-border")]
    public void EmphasisUtility_RecoloursTheBorderToo(string utility) =>
        // Without this the band is two colours: a grey outer pixel and a ring-coloured inner one.
        ExtractUtility(utility).Contains("border-color: var(--zen-ring)", StringComparison.Ordinal).ShouldBeTrue(
            $"{utility}: the border itself has to take the ring colour, or the band reads as two " +
            "different lines.");

    [Fact]
    public void EmphasisUtilities_DoNotUseBoxShadow()
    {
        // box-shadow is deliberately left free so this composes with ZenCard's elevation. An inset
        // shadow would silently replace the elevation on any raised surface.
        foreach (var utility in Utilities)
        {
            ExtractUtility(utility).Contains("box-shadow", StringComparison.Ordinal).ShouldBeFalse(
                $"{utility}: box-shadow is reserved for elevation.");
        }
    }

    [Fact]
    public void InvalidFieldsKeepTheirDangerColourWhileFocused()
    {
        // An error outranks a focus hint. Turning a failing field primary the moment it is focused
        // hides the state the user is trying to fix.
        var body = ExtractUtility("zen-focus-border");

        body.Contains("aria-invalid", StringComparison.Ordinal).ShouldBeTrue();
        body.Contains("var(--zen-danger)", StringComparison.Ordinal).ShouldBeTrue();
    }

    /// <summary>
    /// Returns the body of an <c>@utility</c> block, brace-matched so nested rules come along.
    /// </summary>
    private static string ExtractUtility(string name)
    {
        var match = Regex.Match(Css, $@"@utility\s+{Regex.Escape(name)}\s*\{{");

        match.Success.ShouldBeTrue($"@utility {name} was not found in components/base.css.");

        var start = match.Index + match.Length - 1;
        var depth = 0;

        for (var i = start; i < Css.Length; i++)
        {
            if (Css[i] == '{')
            {
                depth++;
            }
            else if (Css[i] == '}')
            {
                depth--;

                if (depth == 0)
                {
                    return Css[(start + 1)..i];
                }
            }
        }

        throw new InvalidOperationException($"@utility {name} is unterminated.");
    }
}
