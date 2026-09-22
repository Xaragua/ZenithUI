namespace ZenithUI.Tests.Components;

public class ZenButtonTests : BunitContext
{
    public ZenButtonTests() =>
        Renderer.SetRendererInfo(new RendererInfo("Server", isInteractive: true));

    // ---- Element choice ---------------------------------------------------------------------

    [Fact]
    public void RendersAButton_ByDefault() =>
        Render<ZenButton>().Find("button").ShouldNotBeNull();

    [Fact]
    public void TypeDefaultsToButton()
    {
        // An HTML <button> inside a <form> defaults to type="submit", so a "Cancel" button with no
        // explicit type submits the form. Opting into submission is the safer default.
        Render<ZenButton>().Find("button").GetAttribute("type").ShouldBe("button");
    }

    [Fact]
    public void TypeCanBeOverriddenForSubmitButtons() =>
        Render<ZenButton>(p => p.Add(x => x.ButtonType, "submit"))
            .Find("button").GetAttribute("type").ShouldBe("submit");

    [Fact]
    public void RendersAnAnchor_WhenHrefIsSet()
    {
        var cut = Render<ZenButton>(p => p.Add(x => x.Href, "/settings"));

        cut.Find("a").GetAttribute("href").ShouldBe("/settings");
        cut.FindAll("button").ShouldBeEmpty();
    }

    // ---- Disabled ---------------------------------------------------------------------------

    [Fact]
    public void DisabledButton_CarriesTheDisabledAttribute() =>
        Render<ZenButton>(p => p.Add(x => x.Disabled, true))
            .Find("button").HasAttribute("disabled").ShouldBeTrue();

    [Fact]
    public void DisabledLink_DropsItsHrefEntirely()
    {
        // A disabled link is not a thing in HTML - the browser follows an <a href> regardless of
        // any attribute claiming otherwise. Rendering a link that looks dead and still navigates
        // is worse than rendering no link at all.
        var cut = Render<ZenButton>(p => p
            .Add(x => x.Href, "/settings")
            .Add(x => x.Disabled, true));

        var anchor = cut.Find("a");

        anchor.HasAttribute("href").ShouldBeFalse();
        anchor.GetAttribute("aria-disabled").ShouldBe("true");
        anchor.GetAttribute("tabindex").ShouldBe("-1", "it must also leave the tab order.");
    }

    [Fact]
    public async Task DisabledButton_DoesNotInvokeOnClick()
    {
        var clicks = 0;

        var cut = Render<ZenButton>(p => p
            .Add(x => x.Disabled, true)
            .Add(x => x.OnClick, () => clicks++));

        await cut.Find("button").ClickAsync(new());

        clicks.ShouldBe(0);
    }

    // ---- Loading ----------------------------------------------------------------------------

    [Fact]
    public void Loading_KeepsTheLabelInTheDom()
    {
        // Replacing the label with a spinner would resize the button mid-click, moving the target
        // out from under the pointer. The label stays and is faded instead.
        var cut = Render<ZenButton>(p => p
            .Add(x => x.Loading, true)
            .AddChildContent("Save"));

        cut.Markup.ShouldContain("Save");
        cut.Find("svg.animate-spin").ShouldNotBeNull();
    }

    [Fact]
    public void Loading_MarksTheControlBusyAndInert()
    {
        var cut = Render<ZenButton>(p => p.Add(x => x.Loading, true));

        var button = cut.Find("button");

        button.GetAttribute("aria-busy").ShouldBe("true");
        button.HasAttribute("disabled").ShouldBeTrue();
    }

    [Fact]
    public void Loading_SpinnerDoesNotAnnounceSeparately()
    {
        // The button already carries aria-busy. A spinner with its own role="status" would make a
        // screen reader announce the same state twice.
        var cut = Render<ZenButton>(p => p.Add(x => x.Loading, true));

        cut.FindAll("[role=status]").ShouldBeEmpty();
    }

    [Fact]
    public async Task Loading_DoesNotInvokeOnClick()
    {
        var clicks = 0;

        var cut = Render<ZenButton>(p => p
            .Add(x => x.Loading, true)
            .Add(x => x.OnClick, () => clicks++));

        await cut.Find("button").ClickAsync(new());

        clicks.ShouldBe(0);
    }

    // ---- Interaction ------------------------------------------------------------------------

    [Fact]
    public async Task InvokesOnClick()
    {
        var clicks = 0;
        var cut = Render<ZenButton>(p => p.Add(x => x.OnClick, () => clicks++));

        await cut.Find("button").ClickAsync(new());

        clicks.ShouldBe(1);
    }

    // ---- Accessibility ----------------------------------------------------------------------

    [Fact]
    public void IconOnly_WithoutAnAccessibleName_Throws()
    {
        // Announced as just "button" otherwise. This is a development-time mistake with no runtime
        // recovery, so it fails loudly rather than shipping an unidentifiable control.
        var act = () => Render<ZenButton>(p => p
            .Add(x => x.IconOnly, true)
            .Add(x => x.Icon, ZenIcons.Search));

        act.ShouldThrow<InvalidOperationException>()
            .Message.ShouldContain(nameof(ZenButton.AriaLabel));
    }

    [Fact]
    public void IconOnly_WithAriaLabel_Renders() =>
        Render<ZenButton>(p => p
            .Add(x => x.IconOnly, true)
            .Add(x => x.Icon, ZenIcons.Search)
            .Add(x => x.AriaLabel, "Search"))
        .Find("button").GetAttribute("aria-label").ShouldBe("Search");

    [Fact]
    public void IconOnly_AcceptsASplattedAriaLabel()
    {
        // A caller writing aria-label="..." directly has satisfied the requirement just as well as
        // one using the parameter; the guard must not reject them.
        var act = () => Render<ZenButton>(p => p
            .Add(x => x.IconOnly, true)
            .Add(x => x.Icon, ZenIcons.Search)
            .AddUnmatched("aria-label", "Search"));

        act.ShouldNotThrow();
    }

    [Fact]
    public void ButtonIcons_AreHiddenFromAssistiveTechnology()
    {
        // The label already says what the button does; announcing the icon repeats it.
        var cut = Render<ZenButton>(p => p
            .Add(x => x.Icon, ZenIcons.Check)
            .AddChildContent("Confirm"));

        cut.Find("svg").GetAttribute("aria-hidden").ShouldBe("true");
    }

    [Fact]
    public void ExternalTarget_GetsNoopenerRel()
    {
        // Without rel="noopener", the opened page can reach back through window.opener.
        var cut = Render<ZenButton>(p => p
            .Add(x => x.Href, "https://example.com")
            .Add(x => x.Target, "_blank"));

        cut.Find("a").GetAttribute("rel").ShouldBe("noopener noreferrer");
    }

    [Fact]
    public void ExplicitRel_IsNotOverridden() =>
        Render<ZenButton>(p => p
            .Add(x => x.Href, "https://example.com")
            .Add(x => x.Target, "_blank")
            .Add(x => x.Rel, "external"))
        .Find("a").GetAttribute("rel").ShouldBe("external");

    // ---- Pass-through -----------------------------------------------------------------------

    [Fact]
    public void MergesCallerClassesWithItsOwn()
    {
        var cut = Render<ZenButton>(p => p
            .Add(x => x.Class, "mt-4")
            .AddUnmatched("data-testid", "save"));

        var button = cut.Find("button");

        button.ClassList.ShouldContain("mt-4");
        button.ClassList.ShouldContain("zen-focus");
        button.GetAttribute("data-testid").ShouldBe("save");
    }

    [Theory]
    [InlineData(ZenIntent.Primary, ZenVariant.Solid, "bg-primary")]
    [InlineData(ZenIntent.Primary, ZenVariant.Soft, "bg-primary-soft")]
    [InlineData(ZenIntent.Danger, ZenVariant.Outline, "border-danger")]
    [InlineData(ZenIntent.Danger, ZenVariant.Ghost, "text-danger-strong")]
    public void AppliesTheIntentAndVariantClasses(ZenIntent intent, ZenVariant variant, string expected) =>
        Render<ZenButton>(p => p
            .Add(x => x.Intent, intent)
            .Add(x => x.Variant, variant))
        .Find("button").ClassList.ShouldContain(expected);

    // ---- Static SSR -------------------------------------------------------------------------

    [Fact]
    public void StaticSsr_RendersCompleteMarkup()
    {
        Renderer.SetRendererInfo(new RendererInfo("Static", isInteractive: false));

        var cut = Render<ZenButton>(p => p
            .Add(x => x.Intent, ZenIntent.Primary)
            .Add(x => x.Icon, ZenIcons.Check)
            .AddChildContent("Confirm"));

        cut.Find("button").ClassList.ShouldContain("bg-primary");
        cut.Markup.ShouldContain("Confirm");
        cut.Find("svg").ShouldNotBeNull();
    }
}
