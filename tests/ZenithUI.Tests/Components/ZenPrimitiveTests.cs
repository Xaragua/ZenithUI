namespace ZenithUI.Tests.Components;

/// <summary>
/// ZenIcon, ZenSpinner, ZenSkeleton and ZenBadge. Small components, but each carries an
/// accessibility decision that is invisible in a screenshot and wrong by default.
/// </summary>
public class ZenPrimitiveTests : BunitContext
{
    public ZenPrimitiveTests() =>
        Renderer.SetRendererInfo(new RendererInfo("Server", isInteractive: true));

    // ---- ZenIcon ----------------------------------------------------------------------------

    [Fact]
    public void Icon_IsDecorativeByDefault()
    {
        // An icon next to its own label is the common case, and announcing it makes a screen
        // reader say the label twice.
        var cut = Render<ZenIcon>(p => p.Add(x => x.Path, ZenIcons.Check));

        var svg = cut.Find("svg");

        svg.GetAttribute("aria-hidden").ShouldBe("true");
        svg.HasAttribute("role").ShouldBeFalse();
    }

    [Fact]
    public void Icon_BecomesAnImage_WhenGivenATitle()
    {
        var cut = Render<ZenIcon>(p => p
            .Add(x => x.Path, ZenIcons.Warning)
            .Add(x => x.Title, "Warning"));

        var svg = cut.Find("svg");

        svg.GetAttribute("role").ShouldBe("img");
        svg.GetAttribute("aria-label").ShouldBe("Warning");
        svg.HasAttribute("aria-hidden").ShouldBeFalse("a named icon must not also be hidden.");
    }

    [Fact]
    public void Icon_RendersPathMarkupUnescaped() =>
        Render<ZenIcon>(p => p.Add(x => x.Path, ZenIcons.Check))
            .Find("svg").InnerHtml.ShouldContain("path");

    [Theory]
    [InlineData(ZenSize.Small, "size-4")]
    [InlineData(ZenSize.Medium, "size-5")]
    [InlineData(ZenSize.Large, "size-6")]
    public void Icon_AppliesTheSizeClass(ZenSize size, string expected) =>
        Render<ZenIcon>(p => p.Add(x => x.Size, size))
            .Find("svg").ClassList.ShouldContain(expected);

    // ---- ZenSpinner -------------------------------------------------------------------------

    [Fact]
    public void Spinner_AnnouncesPolitely()
    {
        // Without a status role and a name, a spinner is silent - the page simply appears to stop.
        var cut = Render<ZenSpinner>();

        cut.Find("[role=status]").ShouldNotBeNull();
        cut.Markup.ShouldContain("Loading");
    }

    [Fact]
    public void Spinner_LabelIsScreenReaderOnly_ByDefault() =>
        Render<ZenSpinner>().Find("[role=status] span").ClassList.ShouldContain("zen-sr-only");

    [Fact]
    public void Spinner_ShowsItsLabelVisibly_WhenAsked() =>
        Render<ZenSpinner>(p => p.Add(x => x.ShowLabel, true))
            .Find("[role=status] span").ClassList.ShouldNotContain("zen-sr-only");

    [Fact]
    public void Spinner_WithAnEmptyLabel_IsSilent()
    {
        // For use inside a control that already announces its own busy state.
        var cut = Render<ZenSpinner>(p => p.Add(x => x.Label, string.Empty));

        cut.FindAll("[role=status]").ShouldBeEmpty();
        cut.Find("span").GetAttribute("aria-hidden").ShouldBe("true");
    }

    // ---- ZenSkeleton ------------------------------------------------------------------------

    [Fact]
    public void Skeleton_IsAlwaysHiddenFromAssistiveTechnology()
    {
        // A skeleton is a picture of absent content. Announcing it tells a screen reader user
        // nothing except that some shapes exist.
        var cut = Render<ZenSkeleton>();

        cut.Find("span").GetAttribute("aria-hidden").ShouldBe("true");
    }

    [Fact]
    public void Skeleton_MultipleLines_RenderTheLastOneShort()
    {
        // Uniform-length lines read as a table rather than as prose.
        var cut = Render<ZenSkeleton>(p => p.Add(x => x.Lines, 3));

        var styles = cut.FindAll("span").Select(l => l.GetAttribute("style") ?? string.Empty).ToList();

        styles.Count.ShouldBe(3);
        styles[2].ShouldContain("60%");
        styles[0].ShouldNotContain("60%");
    }

    [Fact]
    public void Skeleton_RespectsReducedMotion() =>
        // motion-safe: the pulse is decoration, and a viewer who asked for reduced motion should
        // get a static block rather than a page that breathes at them.
        Render<ZenSkeleton>().Find("span").ClassList.ShouldContain("motion-safe:animate-pulse");

    [Fact]
    public void Skeleton_CanDisableItsAnimation() =>
        Render<ZenSkeleton>(p => p.Add(x => x.Animate, false))
            .Find("span").ClassList.ShouldNotContain("motion-safe:animate-pulse");

    // ---- ZenBadge ---------------------------------------------------------------------------

    [Fact]
    public void Badge_DefaultsToTheSoftVariant() =>
        // Soft is the right weight for a status chip - it should not shout louder than its row.
        Render<ZenBadge>(p => p
            .Add(x => x.Intent, ZenIntent.Success)
            .AddChildContent("Active"))
        .Find("span").ClassList.ShouldContain("bg-success-soft");

    [Fact]
    public void Badge_UsesStrongForItsText()
    {
        // The token roles are not interchangeable: -strong is the only one guaranteed readable on
        // a -soft background.
        var cut = Render<ZenBadge>(p => p
            .Add(x => x.Intent, ZenIntent.Warning)
            .AddChildContent("Pending"));

        var badge = cut.Find("span");

        badge.ClassList.ShouldContain("text-warning-strong");
        badge.ClassList.ShouldNotContain("text-warning", "plain -warning is a fill, not a foreground.");
    }

    [Theory]
    [InlineData(ZenVariant.Solid)]
    [InlineData(ZenVariant.Soft)]
    [InlineData(ZenVariant.Outline)]
    [InlineData(ZenVariant.Ghost)]
    public void Badge_HasNoHoverState_InAnyVariant(ZenVariant variant)
    {
        // A badge is a label. A hover state would tell the user it is clickable, which is a lie.
        // Regression: the non-interactive flag originally suppressed the hover on Soft only, so a
        // Solid or Outline badge still lit up under the pointer.
        var classes = Render<ZenBadge>(p => p
            .Add(x => x.Intent, ZenIntent.Primary)
            .Add(x => x.Variant, variant)
            .AddChildContent("Label"))
            .Find("span").ClassList;

        classes.ShouldNotContain(c => c.StartsWith("hover:", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(ZenVariant.Solid)]
    [InlineData(ZenVariant.Soft)]
    [InlineData(ZenVariant.Outline)]
    [InlineData(ZenVariant.Ghost)]
    public void Button_HasAHoverState_InEveryVariant(ZenVariant variant)
    {
        // The mirror of the badge case: a control that does act must show that it does.
        var classes = Render<ZenButton>(p => p
            .Add(x => x.Intent, ZenIntent.Primary)
            .Add(x => x.Variant, variant)
            .AddChildContent("Go"))
            .Find("button").ClassList;

        classes.ShouldContain(c => c.StartsWith("hover:", StringComparison.Ordinal));
    }

    [Fact]
    public void Badge_DotIsDecorativeOnly()
    {
        // Colour must never be the only carrier of meaning; the text still states the status.
        var cut = Render<ZenBadge>(p => p
            .Add(x => x.Dot, true)
            .Add(x => x.Intent, ZenIntent.Success)
            .AddChildContent("Active"));

        cut.Find("span > span").GetAttribute("aria-hidden").ShouldBe("true");
        cut.Markup.ShouldContain("Active");
    }

    [Fact]
    public void Badge_CanBeSquared() =>
        Render<ZenBadge>(p => p.Add(x => x.Pill, false))
            .Find("span").ClassList.ShouldContain("rounded-zen-sm");
}
