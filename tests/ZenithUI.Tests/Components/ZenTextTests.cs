namespace ZenithUI.Tests.Components;

/// <summary>
/// ZenText. The decision worth guarding is that the element and the look are independent: a
/// regression that tied them back together would push callers into skipping heading levels.
/// </summary>
public class ZenTextTests : BunitContext
{
    public ZenTextTests() =>
        Renderer.SetRendererInfo(new RendererInfo("Server", isInteractive: true));

    [Fact]
    public void Default_IsABodyParagraph()
    {
        var p = Render<ZenText>(p => p.AddChildContent("Hello")).Find("p");

        p.TextContent.ShouldBe("Hello");
        p.ClassList.ShouldContain("text-base");
        p.ClassList.ShouldContain("text-content");
        p.ClassList.ShouldContain("text-pretty");
    }

    [Theory]
    [InlineData(ZenTextVariant.Display, "h1")]
    [InlineData(ZenTextVariant.H1, "h1")]
    [InlineData(ZenTextVariant.H2, "h2")]
    [InlineData(ZenTextVariant.H3, "h3")]
    [InlineData(ZenTextVariant.H4, "h4")]
    [InlineData(ZenTextVariant.H5, "h5")]
    [InlineData(ZenTextVariant.H6, "h6")]
    [InlineData(ZenTextVariant.Lead, "p")]
    [InlineData(ZenTextVariant.Small, "p")]
    [InlineData(ZenTextVariant.Caption, "span")]
    [InlineData(ZenTextVariant.Overline, "span")]
    public void Variant_ChoosesTheElement(ZenTextVariant variant, string tag) =>
        Render<ZenText>(p => p.Add(x => x.Variant, variant))
            .Find(tag).ClassList.ShouldNotBeEmpty();

    [Fact]
    public void As_ChangesTheElement_ButKeepsTheLook()
    {
        // An h2 that must look like a page title: the outline and the design disagree, and
        // neither should have to give way.
        var cut = Render<ZenText>(p => p
            .Add(x => x.Variant, ZenTextVariant.H1)
            .Add(x => x.As, ZenTextElement.H2));

        cut.FindAll("h1").ShouldBeEmpty();

        var h2 = cut.Find("h2");
        h2.ClassList.ShouldContain("text-3xl");
        h2.ClassList.ShouldContain("font-bold");
    }

    [Theory]
    [InlineData(ZenTextElement.Label, "label")]
    [InlineData(ZenTextElement.Blockquote, "blockquote")]
    [InlineData(ZenTextElement.Div, "div")]
    [InlineData(ZenTextElement.Strong, "strong")]
    public void As_RendersEveryElement(ZenTextElement element, string tag) =>
        Render<ZenText>(p => p.Add(x => x.As, element)).Find(tag).ShouldNotBeNull();

    [Fact]
    public void Align_IsInheritedWhenUnset()
    {
        var classes = Render<ZenText>().Find("p").ClassList;

        classes.ShouldNotContain("text-start");
        classes.ShouldNotContain("text-center");
        classes.ShouldNotContain("text-end");
    }

    [Theory]
    [InlineData(ZenAlign.Start, "text-start")]
    [InlineData(ZenAlign.Center, "text-center")]
    [InlineData(ZenAlign.End, "text-end")]
    public void Align_IsLogical(ZenAlign align, string expected) =>
        // text-start / text-end, never left / right: a right-to-left page must not need its
        // alignment re-specified.
        Render<ZenText>(p => p.Add(x => x.Align, align)).Find("p").ClassList.ShouldContain(expected);

    [Theory]
    [InlineData(ZenTextTone.Muted, "text-content-muted")]
    [InlineData(ZenTextTone.Subtle, "text-content-subtle")]
    [InlineData(ZenTextTone.Inverted, "text-content-inverted")]
    [InlineData(ZenTextTone.Danger, "text-danger-strong")]
    [InlineData(ZenTextTone.Primary, "text-primary-strong")]
    public void Tone_MapsToAToken(ZenTextTone tone, string expected)
    {
        var classes = Render<ZenText>(p => p.Add(x => x.Tone, tone)).Find("p").ClassList;

        classes.ShouldContain(expected);
        classes.Count(c => c.StartsWith("text-content", StringComparison.Ordinal) || c.EndsWith("-strong", StringComparison.Ordinal))
            .ShouldBe(1, "a tone override must replace the variant's colour, not add a second one.");
    }

    [Fact]
    public void Tone_IntentIsTheStrongRole_NeverTheFill() =>
        // The bare intent is a fill colour and fails contrast as text on the warning and success
        // palettes; -strong is the role the audit checks as a foreground.
        Render<ZenText>(p => p.Add(x => x.Tone, ZenTextTone.Warning))
            .Find("p").ClassList.ShouldNotContain("text-warning");

    [Fact]
    public void Tone_Inherit_EmitsNoColour() =>
        Render<ZenText>(p => p
                .Add(x => x.Variant, ZenTextVariant.Lead)
                .Add(x => x.Tone, ZenTextTone.Inherit))
            .Find("p").ClassList.ShouldNotContain(c => c.StartsWith("text-content", StringComparison.Ordinal));

    [Fact]
    public void Lead_IsMutedByDefault() =>
        Render<ZenText>(p => p.Add(x => x.Variant, ZenTextVariant.Lead))
            .Find("p").ClassList.ShouldContain("text-content-muted");

    [Fact]
    public void Weight_ReplacesTheVariantWeight()
    {
        var classes = Render<ZenText>(p => p
                .Add(x => x.Variant, ZenTextVariant.H1)
                .Add(x => x.Weight, ZenTextWeight.Medium))
            .Find("h1").ClassList;

        classes.ShouldContain("font-medium");
        classes.ShouldNotContain("font-bold");
    }

    [Fact]
    public void Body_EmitsNoWeight_SoItInheritsOne() =>
        Render<ZenText>().Find("p").ClassList.ShouldNotContain(c => c.StartsWith("font-", StringComparison.Ordinal));

    [Theory]
    [InlineData(ZenTextVariant.H1, "h1")]
    [InlineData(ZenTextVariant.Body, "p")]
    public void Truncate_DropsTheWrapBalancing(ZenTextVariant variant, string tag)
    {
        // text-balance and text-pretty set the text-wrap shorthand, which resets
        // text-wrap-mode to wrap and silently cancels truncate's nowrap. Found in the browser: a
        // truncated heading wrapped anyway and had its descenders clipped.
        var classes = Render<ZenText>(p => p
                .Add(x => x.Variant, variant)
                .Add(x => x.Truncate, true))
            .Find(tag).ClassList;

        classes.ShouldContain("truncate");
        classes.ShouldNotContain("text-balance");
        classes.ShouldNotContain("text-pretty");
    }

    [Fact]
    public void Attributes_PassThrough_AndClassMerges()
    {
        var p = Render<ZenText>(p => p
                .Add(x => x.Variant, ZenTextVariant.H2)
                .Add(x => x.Id, "title")
                .Add(x => x.Style, "margin: 0")
                .AddUnmatched("class", "mt-4")
                .AddUnmatched("aria-describedby", "sub"))
            .Find("h2");

        p.Id.ShouldBe("title");
        p.GetAttribute("style").ShouldBe("margin: 0");
        p.GetAttribute("aria-describedby").ShouldBe("sub");
        p.ClassList.ShouldContain("mt-4");
        p.ClassList.ShouldContain("text-2xl", "a caller's class must merge with the component's, not replace them.");
    }

    [Fact]
    public void GeneratedId_IsNotEmitted() =>
        // Only an id a caller chose is useful: nothing can point at one it never saw.
        Render<ZenText>().Find("p").HasAttribute("id").ShouldBeFalse();

    [Fact]
    public void RendersTheSame_UnderStaticRendering()
    {
        var interactive = Render<ZenText>(p => p.Add(x => x.Variant, ZenTextVariant.H3).AddChildContent("x")).Markup;

        Renderer.SetRendererInfo(new RendererInfo("Static", isInteractive: false));
        var staticMarkup = Render<ZenText>(p => p.Add(x => x.Variant, ZenTextVariant.H3).AddChildContent("x")).Markup;

        staticMarkup.ShouldBe(interactive);
    }
}
