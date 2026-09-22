namespace ZenithUI.Tests.Components;

/// <summary>
/// ZenField's whole reason to exist is the ARIA wiring between a label, a control, and whichever
/// message is showing. That wiring is invisible without a screen reader, so it is asserted here in
/// detail.
/// </summary>
public class ZenFieldTests : BunitContext
{
    public ZenFieldTests() =>
        Renderer.SetRendererInfo(new RendererInfo("Server", isInteractive: true));

    private IRenderedComponent<ZenField> RenderField(
        Action<ComponentParameterCollectionBuilder<ZenField>>? configure = null) =>
        Render<ZenField>(p =>
        {
            // A plain input standing in for the real controls that arrive in M2. It applies every
            // attribute the context offers, which is exactly what the assertions below inspect.
            p.Add(x => x.ChildContent, (RenderFragment<ZenFieldContext>)(ctx => builder =>
            {
                builder.OpenElement(0, "input");
                builder.AddAttribute(1, "id", ctx.InputId);
                builder.AddAttribute(2, "aria-describedby", ctx.DescribedBy);
                builder.AddAttribute(3, "aria-invalid", ctx.AriaInvalid);
                builder.AddAttribute(4, "aria-required", ctx.AriaRequired);
                builder.CloseElement();
            }));

            configure?.Invoke(p);
        });

    // ---- Label association ------------------------------------------------------------------

    [Fact]
    public void LabelPointsAtTheControl()
    {
        // This is what makes clicking a label focus its control - and what lets a screen reader
        // announce the field's name at all.
        var cut = RenderField(p => p.Add(x => x.Label, "Email"));

        var forAttribute = cut.Find("label").GetAttribute("for");

        forAttribute.ShouldNotBeNullOrEmpty();
        cut.Find("input").GetAttribute("id").ShouldBe(forAttribute);
    }

    [Fact]
    public void NoLabelElement_WhenNoLabelIsGiven() =>
        RenderField().FindAll("label").ShouldBeEmpty();

    [Fact]
    public void ExplicitFor_OverridesTheGeneratedId()
    {
        var cut = RenderField(p => p
            .Add(x => x.Label, "Email")
            .Add(x => x.For, "custom-id"));

        cut.Find("label").GetAttribute("for").ShouldBe("custom-id");
        cut.Find("input").GetAttribute("id").ShouldBe("custom-id");
    }

    // ---- Describedby ------------------------------------------------------------------------

    [Fact]
    public void HelpText_IsAssociatedWithTheControl()
    {
        var cut = RenderField(p => p
            .Add(x => x.Label, "Email")
            .Add(x => x.HelpText, "We never share it."));

        var describedBy = cut.Find("input").GetAttribute("aria-describedby");

        describedBy.ShouldNotBeNullOrEmpty();
        cut.Find($"#{describedBy}").TextContent.ShouldContain("We never share it.");
    }

    [Fact]
    public void NoDescribedBy_WhenThereIsNothingToDescribe() =>
        RenderField(p => p.Add(x => x.Label, "Email"))
            .Find("input").HasAttribute("aria-describedby").ShouldBeFalse();

    [Fact]
    public void Error_ReplacesHelpText_AndOwnsTheDescription()
    {
        // Help text and an error are not stacked: showing both leaves the user reading advice they
        // have already failed to follow, and aria-describedby would point at two nodes.
        var cut = RenderField(p => p
            .Add(x => x.Label, "Email")
            .Add(x => x.HelpText, "We never share it.")
            .Add(x => x.Error, "Enter a valid email address."));

        cut.Markup.ShouldNotContain("We never share it.");

        var describedBy = cut.Find("input").GetAttribute("aria-describedby");
        cut.Find($"#{describedBy}").TextContent.ShouldContain("Enter a valid email address.");
    }

    // ---- Error presentation -----------------------------------------------------------------

    [Fact]
    public void Error_IsAnnouncedWhenItAppears() =>
        // role="alert" so a message that appears after a failed submit is spoken, rather than
        // sitting silently below a control the user has already left.
        RenderField(p => p.Add(x => x.Error, "Required."))
            .Find("[role=alert]").TextContent.ShouldContain("Required.");

    [Fact]
    public void Error_SetsAriaInvalid() =>
        RenderField(p => p.Add(x => x.Error, "Required."))
            .Find("input").GetAttribute("aria-invalid").ShouldBe("true");

    [Fact]
    public void ValidField_OmitsAriaInvalidEntirely() =>
        // Omitting beats aria-invalid="false": some screen readers announce the explicit false,
        // telling the user about a validity state they never asked about.
        RenderField().Find("input").HasAttribute("aria-invalid").ShouldBeFalse();

    [Fact]
    public void OnlyTheFirstError_IsShown()
    {
        // A stack of messages for one field is more noise than help, and the rest are usually
        // consequences of the first.
        var cut = RenderField(p => p.Add(
            x => x.Errors,
            new[] { "Enter an email address.", "Must contain an @ sign." }));

        cut.Markup.ShouldContain("Enter an email address.");
        cut.Markup.ShouldNotContain("Must contain an @ sign.");
    }

    [Fact]
    public void BlankErrorsAreIgnored() =>
        RenderField(p => p.Add(x => x.Errors, new[] { "", "  ", "Real problem." }))
            .Find("[role=alert]").TextContent.ShouldContain("Real problem.");

    // ---- Required ---------------------------------------------------------------------------

    [Fact]
    public void Required_SetsAriaRequiredOnTheControl() =>
        RenderField(p => p.Add(x => x.Required, true))
            .Find("input").GetAttribute("aria-required").ShouldBe("true");

    [Fact]
    public void RequiredAsterisk_IsNotAnnounced()
    {
        // aria-required on the control already states this; announcing "star" helps nobody.
        var cut = RenderField(p => p
            .Add(x => x.Label, "Email")
            .Add(x => x.Required, true));

        cut.Find("label span").GetAttribute("aria-hidden").ShouldBe("true");
    }

    [Fact]
    public void OptionalHint_IsShownOnlyWhenNotRequired()
    {
        RenderField(p => p
            .Add(x => x.Label, "Nickname")
            .Add(x => x.OptionalHint, "(optional)"))
        .Markup.ShouldContain("(optional)");

        RenderField(p => p
            .Add(x => x.Label, "Nickname")
            .Add(x => x.OptionalHint, "(optional)")
            .Add(x => x.Required, true))
        .Markup.ShouldNotContain("(optional)");
    }

    // ---- Static SSR -------------------------------------------------------------------------

    [Fact]
    public void StaticSsr_WiringIsIntact()
    {
        // The association has to be correct in the very first HTML: a form that is only accessible
        // once a circuit connects is not accessible.
        Renderer.SetRendererInfo(new RendererInfo("Static", isInteractive: false));

        var cut = RenderField(p => p
            .Add(x => x.Label, "Email")
            .Add(x => x.HelpText, "We never share it."));

        cut.Find("label").GetAttribute("for").ShouldBe(cut.Find("input").GetAttribute("id"));
        cut.Find("input").GetAttribute("aria-describedby").ShouldNotBeNullOrEmpty();
    }
}
