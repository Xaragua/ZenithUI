namespace ZenithUI.Tests.Components;

/// <summary>
/// ZenStack, ZenGrid, ZenGridItem, ZenContainer and ZenSpacer. What they have to get right is
/// narrow: the literal class for each parameter, nothing emitted for an unset one, and a loud
/// failure for a value that has no class - a silent no-op is the problem they exist to solve.
/// Whether each class actually exists in zenith.css is <c>StylesheetCoverageTests</c>' job.
/// </summary>
public class ZenLayoutTests : BunitContext
{
    public ZenLayoutTests() =>
        Renderer.SetRendererInfo(new RendererInfo("Server", isInteractive: true));

    // ---- ZenStack --------------------------------------------------------------------------

    [Fact]
    public void Stack_Default_IsAVerticalDivWithAMediumGap()
    {
        var div = Render<ZenStack>(p => p.AddChildContent("<span>a</span>")).Find("div");

        div.ClassList.ShouldBe(["flex", "flex-col", "gap-4"], ignoreOrder: true);
        div.InnerHtml.ShouldBe("<span>a</span>");
    }

    [Fact]
    public void Stack_Horizontal_IsARow() =>
        Render<ZenStack>(p => p.Add(x => x.Direction, ZenDirection.Horizontal))
            .Find("div").ClassList.ShouldContain("flex-row");

    [Theory]
    [InlineData(ZenBreakpoint.Sm, "sm:flex-row")]
    [InlineData(ZenBreakpoint.Md, "md:flex-row")]
    [InlineData(ZenBreakpoint.Lg, "lg:flex-row")]
    [InlineData(ZenBreakpoint.Xl, "xl:flex-row")]
    public void Stack_HorizontalFrom_StacksThenSwitchesToARow(ZenBreakpoint breakpoint, string expected)
    {
        var classes = Render<ZenStack>(p => p.Add(x => x.HorizontalFrom, breakpoint)).Find("div").ClassList;

        classes.ShouldContain("flex-col");
        classes.ShouldContain(expected);
    }

    [Fact]
    public void Stack_HorizontalFrom_OnAHorizontalStack_Throws()
    {
        var act = () => Render<ZenStack>(p => p
            .Add(x => x.Direction, ZenDirection.Horizontal)
            .Add(x => x.HorizontalFrom, ZenBreakpoint.Md));

        act.ShouldThrow<InvalidOperationException>().Message.ShouldContain(nameof(ZenStack.Direction));
    }

    [Theory]
    [InlineData(ZenSpace.None, "gap-0")]
    [InlineData(ZenSpace.Xs, "gap-1")]
    [InlineData(ZenSpace.Sm, "gap-2")]
    [InlineData(ZenSpace.Md, "gap-4")]
    [InlineData(ZenSpace.Lg, "gap-6")]
    [InlineData(ZenSpace.Xl, "gap-8")]
    [InlineData(ZenSpace.Xxl, "gap-12")]
    public void Gap_MapsToTheScale(ZenSpace space, string expected)
    {
        Render<ZenStack>(p => p.Add(x => x.Gap, space)).Find("div").ClassList.ShouldContain(expected);
        Render<ZenGrid>(p => p.Add(x => x.Gap, space)).Find("div").ClassList.ShouldContain(expected);
    }

    [Fact]
    public void Stack_AlignAndJustify_EmitNothingWhenUnset() =>
        Render<ZenStack>().Find("div").ClassList
            .ShouldNotContain(c => c.StartsWith("items-", StringComparison.Ordinal) || c.StartsWith("justify-", StringComparison.Ordinal));

    [Theory]
    [InlineData(ZenCrossAlign.Stretch, "items-stretch")]
    [InlineData(ZenCrossAlign.Start, "items-start")]
    [InlineData(ZenCrossAlign.Center, "items-center")]
    [InlineData(ZenCrossAlign.End, "items-end")]
    [InlineData(ZenCrossAlign.Baseline, "items-baseline")]
    public void Stack_Align(ZenCrossAlign align, string expected) =>
        Render<ZenStack>(p => p.Add(x => x.Align, align)).Find("div").ClassList.ShouldContain(expected);

    [Theory]
    [InlineData(ZenJustify.Start, "justify-start")]
    [InlineData(ZenJustify.Center, "justify-center")]
    [InlineData(ZenJustify.End, "justify-end")]
    [InlineData(ZenJustify.Between, "justify-between")]
    public void Stack_Justify(ZenJustify justify, string expected) =>
        Render<ZenStack>(p => p.Add(x => x.Justify, justify)).Find("div").ClassList.ShouldContain(expected);

    [Fact]
    public void Stack_Wrap() =>
        Render<ZenStack>(p => p.Add(x => x.Wrap, true)).Find("div").ClassList.ShouldContain("flex-wrap");

    [Theory]
    [InlineData(ZenLayoutElement.Section, "section")]
    [InlineData(ZenLayoutElement.Article, "article")]
    [InlineData(ZenLayoutElement.Header, "header")]
    [InlineData(ZenLayoutElement.Footer, "footer")]
    [InlineData(ZenLayoutElement.Nav, "nav")]
    [InlineData(ZenLayoutElement.Aside, "aside")]
    [InlineData(ZenLayoutElement.Ul, "ul")]
    [InlineData(ZenLayoutElement.Ol, "ol")]
    [InlineData(ZenLayoutElement.Li, "li")]
    public void As_RendersEveryElement(ZenLayoutElement element, string tag)
    {
        Render<ZenStack>(p => p.Add(x => x.As, element)).Find(tag).ClassList.ShouldContain("flex");
        Render<ZenGrid>(p => p.Add(x => x.As, element)).Find(tag).ClassList.ShouldContain("grid");
        Render<ZenGridItem>(p => p.Add(x => x.As, element)).Find(tag).ClassList.ShouldContain("min-w-0");
        Render<ZenContainer>(p => p.Add(x => x.As, element)).Find(tag).ClassList.ShouldContain("mx-auto");
    }

    [Fact]
    public void Attributes_PassThrough_AndClassMerges()
    {
        var nav = Render<ZenStack>(p => p
                .Add(x => x.As, ZenLayoutElement.Nav)
                .Add(x => x.Id, "crumbs")
                .Add(x => x.Style, "color: red")
                .AddUnmatched("class", "mt-4")
                .AddUnmatched("aria-label", "Breadcrumb"))
            .Find("nav");

        nav.Id.ShouldBe("crumbs");
        nav.GetAttribute("style").ShouldBe("color: red");
        nav.GetAttribute("aria-label").ShouldBe("Breadcrumb");
        nav.ClassList.ShouldContain("mt-4");
        nav.ClassList.ShouldContain("flex", "a caller's class must merge with the component's, not replace them.");
    }

    [Fact]
    public void GeneratedId_IsNotEmitted()
    {
        Render<ZenStack>().Find("div").HasAttribute("id").ShouldBeFalse();
        Render<ZenGrid>().Find("div").HasAttribute("id").ShouldBeFalse();
        Render<ZenGridItem>().Find("div").HasAttribute("id").ShouldBeFalse();
        Render<ZenContainer>().Find("div").HasAttribute("id").ShouldBeFalse();
    }

    // ---- ZenGrid ---------------------------------------------------------------------------

    [Fact]
    public void Grid_Default_IsASingleColumnWithNoCountClass()
    {
        var classes = Render<ZenGrid>().Find("div").ClassList;

        classes.ShouldBe(["grid", "gap-4"], ignoreOrder: true);
    }

    [Fact]
    public void Grid_ColumnsPerBreakpoint()
    {
        var classes = Render<ZenGrid>(p => p
                .Add(x => x.Columns, 1)
                .Add(x => x.ColumnsSm, 2)
                .Add(x => x.ColumnsMd, 3)
                .Add(x => x.ColumnsLg, 4)
                .Add(x => x.ColumnsXl, 12))
            .Find("div").ClassList;

        classes.ShouldContain("grid-cols-1");
        classes.ShouldContain("sm:grid-cols-2");
        classes.ShouldContain("md:grid-cols-3");
        classes.ShouldContain("lg:grid-cols-4");
        classes.ShouldContain("xl:grid-cols-12");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(7)]
    [InlineData(11)]
    [InlineData(13)]
    public void Grid_UnsupportedCount_Throws(int count)
    {
        var act = () => Render<ZenGrid>(p => p.Add(x => x.ColumnsMd, count));

        act.ShouldThrow<ArgumentOutOfRangeException>().Message.ShouldContain(nameof(ZenGrid.ColumnsMd));
    }

    [Fact]
    public void Grid_MinItemWidth_IsOneClassAndACustomProperty()
    {
        var div = Render<ZenGrid>(p => p
                .Add(x => x.MinItemWidth, "16rem")
                .Add(x => x.Style, "margin: 0"))
            .Find("div");

        div.ClassList.ShouldContain("grid-cols-[repeat(auto-fill,minmax(min(var(--zen-grid-min),100%),1fr))]");
        div.GetAttribute("style").ShouldBe("--zen-grid-min: 16rem;margin: 0",
            "the caller's style follows the width, so it can still override it.");
    }

    [Fact]
    public void Grid_MinItemWidth_WithColumns_Throws()
    {
        var act = () => Render<ZenGrid>(p => p
            .Add(x => x.MinItemWidth, "16rem")
            .Add(x => x.ColumnsLg, 3));

        act.ShouldThrow<InvalidOperationException>().Message.ShouldContain(nameof(ZenGrid.MinItemWidth));
    }

    // ---- ZenGridItem -----------------------------------------------------------------------

    [Fact]
    public void GridItem_SpansPerBreakpoint()
    {
        var classes = Render<ZenGridItem>(p => p
                .Add(x => x.Span, 1)
                .Add(x => x.SpanSm, 2)
                .Add(x => x.SpanMd, 3)
                .Add(x => x.SpanLg, 6)
                .Add(x => x.SpanXl, 12))
            .Find("div").ClassList;

        classes.ShouldContain("col-span-1");
        classes.ShouldContain("sm:col-span-2");
        classes.ShouldContain("md:col-span-3");
        classes.ShouldContain("lg:col-span-6");
        classes.ShouldContain("xl:col-span-12");
    }

    [Fact]
    public void GridItem_AlwaysShrinks() =>
        // A grid item will not shrink below its content by default: one long unbroken line would
        // widen its column past the screen.
        Render<ZenGridItem>().Find("div").ClassList.ShouldBe(["min-w-0"]);

    [Fact]
    public void GridItem_FullWidth() =>
        Render<ZenGridItem>(p => p.Add(x => x.FullWidth, true)).Find("div").ClassList.ShouldContain("col-span-full");

    [Fact]
    public void GridItem_FullWidth_WithSpan_Throws()
    {
        var act = () => Render<ZenGridItem>(p => p
            .Add(x => x.FullWidth, true)
            .Add(x => x.SpanMd, 2));

        act.ShouldThrow<InvalidOperationException>().Message.ShouldContain(nameof(ZenGridItem.FullWidth));
    }

    [Fact]
    public void GridItem_UnsupportedSpan_Throws()
    {
        var act = () => Render<ZenGridItem>(p => p.Add(x => x.Span, 8));

        act.ShouldThrow<ArgumentOutOfRangeException>().Message.ShouldContain(nameof(ZenGridItem.Span));
    }

    // ---- ZenContainer ----------------------------------------------------------------------

    [Fact]
    public void Container_Default_IsACentredMediumColumnWithGutters() =>
        Render<ZenContainer>().Find("div").ClassList
            .ShouldBe(["mx-auto", "w-full", "max-w-5xl", "px-4", "sm:px-6"], ignoreOrder: true);

    [Theory]
    [InlineData(ZenContentWidth.Narrow, "max-w-3xl")]
    [InlineData(ZenContentWidth.Wide, "max-w-7xl")]
    [InlineData(ZenContentWidth.Full, "max-w-none")]
    public void Container_MaxWidth_MatchesTheShell(ZenContentWidth width, string expected) =>
        Render<ZenContainer>(p => p.Add(x => x.MaxWidth, width)).Find("div").ClassList.ShouldContain(expected);

    [Fact]
    public void Container_WithoutGutters() =>
        Render<ZenContainer>(p => p.Add(x => x.Gutters, false)).Find("div").ClassList
            .ShouldNotContain(c => c.Contains("px-", StringComparison.Ordinal));

    // ---- ZenSpacer -------------------------------------------------------------------------

    [Fact]
    public void Spacer_FillsTheRoom_AndIsHidden()
    {
        var div = Render<ZenSpacer>(p => p.AddUnmatched("class", "min-w-4")).Find("div");

        div.ClassList.ShouldBe(["flex-1", "min-w-4"]);
        div.GetAttribute("aria-hidden").ShouldBe("true");
    }

    // ---- Render modes ----------------------------------------------------------------------

    [Fact]
    public void RendersTheSame_UnderStaticRendering()
    {
        string Markup() => Render<ZenStack>(p => p
            .Add(x => x.HorizontalFrom, ZenBreakpoint.Md)
            .AddChildContent("x")).Markup;

        var interactive = Markup();
        Renderer.SetRendererInfo(new RendererInfo("Static", isInteractive: false));

        Markup().ShouldBe(interactive);
    }
}
