using System.Text;
using System.Text.RegularExpressions;

namespace ZenithUI.Tests.Theming;

/// <summary>
/// Every class the layout and typography components can emit has a rule in the shipped
/// <c>zenith.css</c>.
/// </summary>
/// <remarks>
/// <para>
/// That is the whole promise of <c>ZenStack</c>, <c>ZenGrid</c> and <c>ZenText</c>: a consumer who
/// does not run Tailwind gets a working layout and type scale anyway. It rests on Tailwind's
/// scanner finding each class as literal text in the library's sources, and nothing else checks
/// that. A class assembled at runtime, or a source glob that stops matching a folder, compiles
/// fine, passes every markup test, and renders as nothing in the consumer's browser.
/// </para>
/// <para>
/// The test renders every value of every parameter, collects the classes that come out, and looks
/// each one up as a selector in the built stylesheet - so it follows the components as they change
/// rather than a list someone has to remember to update.
/// </para>
/// </remarks>
public partial class StylesheetCoverageTests : BunitContext
{
    private static readonly string Stylesheet = LoadStylesheet();

    public StylesheetCoverageTests() =>
        Renderer.SetRendererInfo(new RendererInfo("Server", isInteractive: true));

    [Fact]
    public void LayoutClasses_AreAllInTheStylesheet()
    {
        var classes = new SortedSet<string>(StringComparer.Ordinal);

        foreach (var direction in Enum.GetValues<ZenDirection>())
        {
            Collect(classes, Render<ZenStack>(p => p.Add(x => x.Direction, direction)).Markup);
        }

        foreach (var breakpoint in Enum.GetValues<ZenBreakpoint>())
        {
            Collect(classes, Render<ZenStack>(p => p.Add(x => x.HorizontalFrom, breakpoint)).Markup);
        }

        foreach (var space in Enum.GetValues<ZenSpace>())
        {
            Collect(classes, Render<ZenStack>(p => p.Add(x => x.Gap, space)).Markup);
        }

        foreach (var align in Enum.GetValues<ZenCrossAlign>())
        {
            Collect(classes, Render<ZenStack>(p => p.Add(x => x.Align, align)).Markup);
        }

        foreach (var justify in Enum.GetValues<ZenJustify>())
        {
            Collect(classes, Render<ZenStack>(p => p.Add(x => x.Justify, justify)).Markup);
        }

        Collect(classes, Render<ZenStack>(p => p.Add(x => x.Wrap, true)).Markup);

        foreach (var count in ZenGrid.SupportedColumns)
        {
            Collect(classes, Render<ZenGrid>(p => p
                .Add(x => x.Columns, count)
                .Add(x => x.ColumnsSm, count)
                .Add(x => x.ColumnsMd, count)
                .Add(x => x.ColumnsLg, count)
                .Add(x => x.ColumnsXl, count)).Markup);

            Collect(classes, Render<ZenGridItem>(p => p
                .Add(x => x.Span, count)
                .Add(x => x.SpanSm, count)
                .Add(x => x.SpanMd, count)
                .Add(x => x.SpanLg, count)
                .Add(x => x.SpanXl, count)).Markup);
        }

        Collect(classes, Render<ZenGrid>(p => p.Add(x => x.MinItemWidth, "16rem")).Markup);
        Collect(classes, Render<ZenGridItem>(p => p.Add(x => x.FullWidth, true)).Markup);

        foreach (var width in Enum.GetValues<ZenContentWidth>())
        {
            Collect(classes, Render<ZenContainer>(p => p.Add(x => x.MaxWidth, width)).Markup);
        }

        Collect(classes, Render<ZenSpacer>().Markup);

        AssertAllPresent(classes);
    }

    [Fact]
    public void TypographyClasses_AreAllInTheStylesheet()
    {
        var classes = new SortedSet<string>(StringComparer.Ordinal);

        foreach (var variant in Enum.GetValues<ZenTextVariant>())
        {
            Collect(classes, Render<ZenText>(p => p.Add(x => x.Variant, variant)).Markup);
            Collect(classes, Render<ZenText>(p => p.Add(x => x.Variant, variant).Add(x => x.Truncate, true)).Markup);
        }

        foreach (var tone in Enum.GetValues<ZenTextTone>())
        {
            Collect(classes, Render<ZenText>(p => p.Add(x => x.Tone, tone)).Markup);
        }

        foreach (var weight in Enum.GetValues<ZenTextWeight>())
        {
            Collect(classes, Render<ZenText>(p => p.Add(x => x.Weight, weight)).Markup);
        }

        foreach (var align in Enum.GetValues<ZenAlign>())
        {
            Collect(classes, Render<ZenText>(p => p.Add(x => x.Align, align)).Markup);
        }

        AssertAllPresent(classes);
    }

    [Theory]
    [InlineData("gap-1", ".gap-1{")]
    [InlineData("md:grid-cols-3", ".md\\:grid-cols-3{")]
    [InlineData("grid-cols-[repeat(auto-fill,1fr)]", ".grid-cols-\\[repeat\\(auto-fill\\,1fr\\)\\]{")]
    public void Selector_EscapesLikeTailwind(string className, string css) =>
        // The lookup is only as good as its escaping. These are the three shapes the components
        // emit: a plain utility, a variant, and an arbitrary value.
        HasRule(css, className).ShouldBeTrue();

    [Fact]
    public void Selector_DoesNotMatchAPrefix() =>
        // gap-1 must not be satisfied by .gap-12 - the scale has both.
        HasRule(".gap-12{gap:3rem}", "gap-1").ShouldBeFalse();

    private static void Collect(SortedSet<string> classes, string markup)
    {
        foreach (Match match in ClassAttribute().Matches(markup))
        {
            foreach (var name in match.Groups[1].Value.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                classes.Add(name);
            }
        }
    }

    private static void AssertAllPresent(IEnumerable<string> classes)
    {
        var missing = classes.Where(c => !HasRule(Stylesheet, c)).ToList();

        missing.ShouldBeEmpty(
            "these classes are emitted by a component but have no rule in zenith.css, so they do " +
            "nothing in an app that does not run its own Tailwind. Check they are written as " +
            "literals, in a file the Styles/zenith.css @source globs cover.");
    }

    private static bool HasRule(string css, string className)
    {
        // A selector ends where the class name does: at a brace, a comma, a pseudo-class, a
        // combinator or another class. Anything that could continue the name means it is a
        // different, longer class.
        var selector = "." + Escape(className);
        var pattern = Regex.Escape(selector) + @"(?![A-Za-z0-9_\-\\])";

        return Regex.IsMatch(css, pattern);
    }

    /// <summary>CSS identifier escaping, as Tailwind writes it: a backslash before each special character.</summary>
    private static string Escape(string className)
    {
        var escaped = new StringBuilder(className.Length * 2);

        foreach (var c in className)
        {
            if (!char.IsAsciiLetterOrDigit(c) && c is not '-' and not '_')
            {
                escaped.Append('\\');
            }

            escaped.Append(c);
        }

        return escaped.ToString();
    }

    private static string LoadStylesheet()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Stylesheet", "zenith.css");

        if (!File.Exists(path))
        {
            throw new FileNotFoundException(
                "The built zenith.css was not copied to the test output. It is generated by the " +
                "library's BuildTailwind target; check the <Content> item for it in ZenithUI.Tests.csproj.",
                path);
        }

        return File.ReadAllText(path);
    }

    [GeneratedRegex("class=\"([^\"]*)\"")]
    private static partial Regex ClassAttribute();
}
