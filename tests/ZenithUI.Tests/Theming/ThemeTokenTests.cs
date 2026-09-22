using ZenithUI.Tests.Theming;

namespace ZenithUI.Tests;

/// <summary>
/// Guards the design tokens themselves: completeness, cascade correctness, sRGB gamut, and the
/// WCAG contrast promises documented in <c>tokens/base.css</c>.
/// </summary>
/// <remarks>
/// Each test collects <i>every</i> violation before failing rather than stopping at the first.
/// Palette work is iterative - nudging one lightness value shifts several ratios at once - and a
/// report that names only the first offender turns a single tuning pass into a dozen test runs.
/// </remarks>
public class ThemeTokenTests
{
    private const double AaNormalText = 4.5;
    private const double EnhancedBodyText = 7.0;

    private static readonly string[] Intents =
    [
        "primary", "secondary", "accent", "success", "warning", "danger", "info",
    ];

    private static readonly string[] Surfaces =
    [
        "zen-surface", "zen-surface-raised", "zen-surface-sunken", "zen-surface-overlay",
    ];

    private static readonly TokenSheet Light = TokenSheet.Load(TokenPaths.Base);

    private static readonly TokenSheet DarkOverrides = TokenSheet.Parse(
        TokenSheet.ExtractBlock(File.ReadAllText(TokenPaths.Dark), ":root[data-zen-theme=\"dark\"]"));

    /// <summary>
    /// The dark palette as a browser sees it: the light sheet with the dark block layered over it,
    /// so tokens the dark palette inherits unchanged (radii, indents) resolve normally.
    /// </summary>
    private static readonly TokenSheet Dark = TokenSheet.Parse(
        File.ReadAllText(TokenPaths.Base)
        + TokenSheet.ExtractBlock(File.ReadAllText(TokenPaths.Dark), ":root[data-zen-theme=\"dark\"]"));

    private static TokenSheet Palette(string name) => name == "dark" ? Dark : Light;

    // ---- Cascade correctness ---------------------------------------------------------------

    [Fact]
    public void DarkPalette_MediaQueryAndAttributeBlocks_AreIdentical()
    {
        // tokens/dark.css states the two blocks as byte-identical declaration lists. If they ever
        // drift, a viewer following a dark OS and a viewer who explicitly chose dark see different
        // colours - a bug that is close to invisible in review and obvious to users.
        var mediaBlock = TokenSheet.Parse(
            TokenSheet.ExtractBlock(File.ReadAllText(TokenPaths.Dark), ":root:not([data-zen-theme=\"light\"])"));

        var problems = new List<string>();

        foreach (var name in DarkOverrides.Names.Order(StringComparer.Ordinal))
        {
            if (!mediaBlock.Contains(name))
            {
                problems.Add($"--{name}: in the attribute block, missing from the media-query block.");
            }
            else if (mediaBlock[name] != DarkOverrides[name])
            {
                problems.Add($"--{name}: '{DarkOverrides[name]}' vs '{mediaBlock[name]}'.");
            }
        }

        foreach (var name in mediaBlock.Names.Order(StringComparer.Ordinal))
        {
            if (!DarkOverrides.Contains(name))
            {
                problems.Add($"--{name}: in the media-query block, missing from the attribute block.");
            }
        }

        problems.ShouldBeEmpty(Report("The two dark blocks in tokens/dark.css have drifted", problems));
    }

    [Fact]
    public void EveryDarkToken_IsAlsoDefinedInTheLightPalette()
    {
        // No token may exist only in dark. A viewer who explicitly chose light while their OS
        // prefers dark matches neither dark block, so anything not present on bare :root would
        // resolve to nothing at all.
        var problems = DarkOverrides.Names
            .Where(name => !Light.Contains(name))
            .Order(StringComparer.Ordinal)
            .Select(name => $"--{name} is overridden in dark but never defined on :root.")
            .ToList();

        problems.ShouldBeEmpty(Report("Dark-only tokens found", problems));
    }

    [Theory]
    [InlineData("zen-surface")]
    [InlineData("zen-content")]
    [InlineData("zen-border")]
    [InlineData("zen-primary")]
    [InlineData("zen-radius-md")]
    [InlineData("zen-shadow-md")]
    [InlineData("zen-indent")]
    public void LightPalette_DefinesCoreToken(string name) =>
        Light.Contains(name).ShouldBeTrue($"--{name} is missing from tokens/base.css.");

    [Fact]
    public void EveryIntent_DefinesItsFullQuartet()
    {
        var problems = new List<string>();

        foreach (var intent in Intents)
        {
            foreach (var suffix in new[] { "", "-content", "-soft", "-strong" })
            {
                var name = $"zen-{intent}{suffix}";

                if (!Light.Contains(name))
                {
                    problems.Add($"--{name} is missing from the light palette.");
                }

                if (!DarkOverrides.Contains(name))
                {
                    problems.Add($"--{name} is missing from the dark palette.");
                }
            }
        }

        problems.ShouldBeEmpty(Report("Incomplete intent quartets", problems));
    }

    // ---- Gamut -----------------------------------------------------------------------------

    [Theory]
    [InlineData("light")]
    [InlineData("dark")]
    public void EveryColourToken_IsInsideTheSrgbGamut(string paletteName)
    {
        var palette = Palette(paletteName);
        var problems = new List<string>();

        foreach (var name in palette.Names.Order(StringComparer.Ordinal))
        {
            var color = palette.ResolveColor(name);

            if (color is { } value && value.IsOutOfGamut())
            {
                problems.Add($"--{name} = {palette[name]} (max in-gamut chroma at this L/H is about {value.MaxInGamutChroma():F3})");
            }
        }

        problems.ShouldBeEmpty(Report(
            $"[{paletteName}] tokens outside the sRGB gamut. The browser clips these, so the colour " +
            "rendered is not the colour declared - and the contrast measured here is not the " +
            "contrast users get",
            problems));
    }

    // ---- Contrast ---------------------------------------------------------------------------

    [Theory]
    [InlineData("light")]
    [InlineData("dark")]
    public void PrimaryContent_MeetsEnhancedContrastOnEverySurface(string paletteName)
    {
        var palette = Palette(paletteName);
        var content = palette.ResolveColor("zen-content")!.Value;
        var problems = new List<string>();

        foreach (var surfaceName in Surfaces)
        {
            var ratio = OklchColor.ContrastRatio(content, palette.ResolveColor(surfaceName)!.Value);

            if (ratio < EnhancedBodyText)
            {
                problems.Add($"--zen-content on --{surfaceName}: {ratio:F2}:1 (need {EnhancedBodyText})");
            }
        }

        problems.ShouldBeEmpty(Report(
            $"[{paletteName}] primary content falls below the 7:1 promised in tokens/base.css",
            problems));
    }

    [Theory]
    [InlineData("light")]
    [InlineData("dark")]
    public void SecondaryContent_MeetsAaOnEverySurface(string paletteName)
    {
        var palette = Palette(paletteName);
        var problems = new List<string>();

        foreach (var contentName in new[] { "zen-content-muted", "zen-content-subtle" })
        {
            var content = palette.ResolveColor(contentName)!.Value;

            foreach (var surfaceName in Surfaces)
            {
                var ratio = OklchColor.ContrastRatio(content, palette.ResolveColor(surfaceName)!.Value);

                if (ratio < AaNormalText)
                {
                    problems.Add($"--{contentName} on --{surfaceName}: {ratio:F2}:1 (need {AaNormalText})");
                }
            }
        }

        problems.ShouldBeEmpty(Report(
            $"[{paletteName}] secondary content falls below AA for normal text",
            problems));
    }

    [Theory]
    [InlineData("light")]
    [InlineData("dark")]
    public void IntentContent_MeetsAaOnItsIntent(string paletteName)
    {
        var palette = Palette(paletteName);
        var problems = new List<string>();

        foreach (var intent in Intents)
        {
            var ratio = OklchColor.ContrastRatio(
                palette.ResolveColor($"zen-{intent}-content")!.Value,
                palette.ResolveColor($"zen-{intent}")!.Value);

            if (ratio < AaNormalText)
            {
                problems.Add($"--zen-{intent}-content on --zen-{intent}: {ratio:F2}:1 (need {AaNormalText})");
            }
        }

        problems.ShouldBeEmpty(Report(
            $"[{paletteName}] solid buttons and badges would have unreadable text",
            problems));
    }

    [Theory]
    [InlineData("light")]
    [InlineData("dark")]
    public void IntentStrong_MeetsAaOnItsSoftTint(string paletteName)
    {
        var palette = Palette(paletteName);
        var problems = new List<string>();

        foreach (var intent in Intents)
        {
            var ratio = OklchColor.ContrastRatio(
                palette.ResolveColor($"zen-{intent}-strong")!.Value,
                palette.ResolveColor($"zen-{intent}-soft")!.Value);

            if (ratio < AaNormalText)
            {
                problems.Add($"--zen-{intent}-strong on --zen-{intent}-soft: {ratio:F2}:1 (need {AaNormalText})");
            }
        }

        problems.ShouldBeEmpty(Report(
            $"[{paletteName}] soft chips would have unreadable text",
            problems));
    }

    [Theory]
    [InlineData("light")]
    [InlineData("dark")]
    public void IntentStrong_MeetsAaOnEverySurface(string paletteName)
    {
        // The whole reason -strong exists: coloured text and icons sitting directly on a page
        // surface. `text-warning` on white is about 2:1; `text-warning-strong` is the token that
        // has to carry that job, so it is the one held to the bar.
        var palette = Palette(paletteName);
        var problems = new List<string>();

        foreach (var intent in Intents)
        {
            var foreground = palette.ResolveColor($"zen-{intent}-strong")!.Value;

            foreach (var surfaceName in Surfaces)
            {
                var ratio = OklchColor.ContrastRatio(foreground, palette.ResolveColor(surfaceName)!.Value);

                if (ratio < AaNormalText)
                {
                    problems.Add($"--zen-{intent}-strong on --{surfaceName}: {ratio:F2}:1 (need {AaNormalText})");
                }
            }
        }

        problems.ShouldBeEmpty(Report(
            $"[{paletteName}] coloured text on a page surface would be unreadable",
            problems));
    }

    private static string Report(string headline, IReadOnlyCollection<string> problems) =>
        $"{headline}:{Environment.NewLine}  {string.Join($"{Environment.NewLine}  ", problems)}";
}
