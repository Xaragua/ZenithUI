namespace ZenithUI.Tests.Components;

/// <summary>
/// Checkbox, radio group, select, toggle, slider, progress and indicator. Most of these wrap a
/// native element on purpose, so the assertions concentrate on the semantics that are easy to lose
/// when restyling one.
/// </summary>
public class ZenChoiceTests : BunitContext
{
    public ZenChoiceTests() =>
        Renderer.SetRendererInfo(new RendererInfo("Server", isInteractive: true));

    // ---- Checkbox ---------------------------------------------------------------------------

    [Fact]
    public void Checkbox_KeepsTheNativeElement() =>
        // Keeping <input type="checkbox"> brings keyboard handling, form participation and correct
        // announcement for free. A div with role="checkbox" has to rebuild all three.
        Render<ZenCheckbox>(p => p.Add(x => x.Label, "Subscribe"))
            .Find("input").GetAttribute("type").ShouldBe("checkbox");

    [Fact]
    public void Checkbox_LabelIsAssociatedWithTheBox()
    {
        var cut = Render<ZenCheckbox>(p => p.Add(x => x.Label, "Subscribe"));

        cut.Find("label").GetAttribute("for").ShouldBe(cut.Find("input").GetAttribute("id"));
    }

    [Fact]
    public void Checkbox_Binds()
    {
        var value = false;

        var cut = Render<ZenCheckbox>(p => p
            .Add(x => x.Value, value)
            .Add(x => x.ValueChanged, v => value = v));

        cut.Find("input").Change(true);

        value.ShouldBeTrue();
    }

    [Fact]
    public void Checkbox_KeepsTheRingNotTheBorderTreatment()
    {
        // The one control where recolouring the border cannot work: a checked box is filled with
        // the accent colour, so a coloured border on it would be invisible.
        var classes = Render<ZenCheckbox>().Find("input").ClassList;

        classes.ShouldContain("zen-focus");
        classes.ShouldNotContain("zen-focus-border");
    }

    [Fact]
    public void Checkbox_IsDrawnByTheLibraryNotTheUserAgent()
    {
        // Regression: the box relied on the UA's own rendering plus accent-color, so in the dark
        // palette it stayed white. `color-scheme: dark` alone would only have made it the
        // operating system's grey - which is not this library's surface colour, and the two
        // sitting together in a form read as a mistake.
        var classes = Render<ZenCheckbox>().Find("input").ClassList;

        classes.ShouldContain("zen-check");
        classes.ShouldNotContain("accent-primary", "the mark is drawn from --zen-* tokens now.");
    }

    [Fact]
    public void Radio_IsDrawnByTheLibraryNotTheUserAgent()
    {
        var classes = RenderRadioGroup().FindAll("input")[0].ClassList;

        classes.ShouldContain("zen-radio");
        classes.ShouldNotContain("accent-primary");
    }

    // ---- Radio group ------------------------------------------------------------------------

    private IRenderedComponent<ZenRadioGroup<string>> RenderRadioGroup(
        string? selected = null,
        Action<string?>? onChange = null) =>
        Render<ZenRadioGroup<string>>(p => p
            .Add(x => x.Label, "Shipping")
            .Add(x => x.Value, selected)
            .Add(x => x.ValueChanged, v => onChange?.Invoke(v))
            .Add(x => x.ChildContent, (RenderFragment)(builder =>
            {
                var seq = 0;

                foreach (var option in new[] { "standard", "express" })
                {
                    builder.OpenComponent<ZenRadio<string>>(seq++);
                    builder.AddComponentParameter(seq++, nameof(ZenRadio<string>.Value), option);
                    builder.AddComponentParameter(seq++, nameof(ZenRadio<string>.Label), option);
                    builder.CloseComponent();
                }
            })));

    [Fact]
    public void RadioGroup_UsesFieldsetAndLegend()
    {
        // The only markup that associates a label with a *set* of controls. It is what makes a
        // screen reader announce "Shipping, group, standard, 1 of 2".
        var cut = RenderRadioGroup();

        cut.Find("fieldset").ShouldNotBeNull();
        cut.Find("legend").TextContent.ShouldContain("Shipping");
    }

    [Fact]
    public void RadioGroup_GivesEveryOptionTheSameName()
    {
        // The shared name is what makes the browser treat them as one exclusive set, including
        // arrow-key navigation between them.
        var names = RenderRadioGroup().FindAll("input").Select(i => i.GetAttribute("name")).Distinct().ToList();

        names.Count.ShouldBe(1);
        names[0].ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public void RadioGroup_MarksOnlyTheSelectedOption()
    {
        var cut = RenderRadioGroup("express");
        var inputs = cut.FindAll("input");

        inputs[0].HasAttribute("checked").ShouldBeFalse();
        inputs[1].HasAttribute("checked").ShouldBeTrue();
    }

    [Fact]
    public void RadioGroup_SelectingAnOptionUpdatesTheGroup()
    {
        string? selected = null;

        var cut = RenderRadioGroup(null, v => selected = v);

        cut.FindAll("input")[1].Change(true);

        selected.ShouldBe("express");
    }

    [Fact]
    public void Radio_OutsideAGroup_FailsLoudly()
    {
        // A radio with no group has no name, no binding and no validation - it is a programming
        // error, not a state to degrade from.
        var act = () => Render<ZenRadio<string>>(p => p.Add(x => x.Value, "orphan"));

        act.ShouldThrow<InvalidOperationException>()
            .Message.ShouldContain(nameof(ZenRadioGroup<string>));
    }

    // ---- Select -----------------------------------------------------------------------------

    [Fact]
    public void Select_PlaceholderCannotBeChosen()
    {
        // Disabled, not merely empty - otherwise "Choose one" is selectable and submittable.
        var cut = Render<ZenSelect<string>>(p => p
            .Add(x => x.Placeholder, "Choose one")
            .Add(x => x.ChildContent, (RenderFragment)(b =>
            {
                b.OpenElement(0, "option");
                b.AddAttribute(1, "value", "a");
                b.AddContent(2, "A");
                b.CloseElement();
            })));

        var placeholder = cut.FindAll("option")[0];

        placeholder.HasAttribute("disabled").ShouldBeTrue();
        placeholder.GetAttribute("value").ShouldBe(string.Empty);
    }

    [Fact]
    public void Select_Binds()
    {
        string? value = null;

        var cut = Render<ZenSelect<string>>(p => p
            .Add(x => x.Value, value)
            .Add(x => x.ValueChanged, v => value = v)
            .Add(x => x.ChildContent, (RenderFragment)(b =>
            {
                b.OpenElement(0, "option");
                b.AddAttribute(1, "value", "b");
                b.AddContent(2, "B");
                b.CloseElement();
            })));

        cut.Find("select").Change("b");

        value.ShouldBe("b");
    }

    // ---- Toggle -----------------------------------------------------------------------------

    [Fact]
    public void Toggle_IsASwitchNotACheckbox()
    {
        // A switch takes effect immediately; a checkbox is a value to be submitted later. Screen
        // readers announce them differently, and using the wrong one misstates what just happened.
        var cut = Render<ZenToggle>(p => p.Add(x => x.Label, "Wi-Fi"));

        var button = cut.Find("button");

        button.GetAttribute("role").ShouldBe("switch");
        button.GetAttribute("type").ShouldBe("button");
        cut.FindAll("input[type=checkbox]").ShouldBeEmpty();
    }

    [Fact]
    public void Toggle_ReportsItsState()
    {
        Render<ZenToggle>(p => p.Add(x => x.Label, "Wi-Fi"))
            .Find("button").GetAttribute("aria-checked").ShouldBe("false");

        Render<ZenToggle>(p => p.Add(x => x.Label, "Wi-Fi").Add(x => x.Value, true))
            .Find("button").GetAttribute("aria-checked").ShouldBe("true");
    }

    [Fact]
    public void Toggle_FlipsOnClick()
    {
        var value = false;

        var cut = Render<ZenToggle>(p => p
            .Add(x => x.Label, "Wi-Fi")
            .Add(x => x.Value, value)
            .Add(x => x.ValueChanged, v => value = v));

        cut.Find("button").Click();

        value.ShouldBeTrue();
    }

    [Fact]
    public void Toggle_IsLabelledByItsVisibleText()
    {
        // A <label for> pointing at a button does nothing, so the association has to be made with
        // aria-labelledby.
        var cut = Render<ZenToggle>(p => p.Add(x => x.Label, "Wi-Fi"));

        var labelledBy = cut.Find("button").GetAttribute("aria-labelledby");

        labelledBy.ShouldNotBeNullOrEmpty();
        cut.Find($"#{labelledBy}").TextContent.ShouldContain("Wi-Fi");
    }

    [Fact]
    public void Toggle_WithoutAnyName_FailsLoudly()
    {
        var act = () => Render<ZenToggle>();

        act.ShouldThrow<InvalidOperationException>();
    }

    // ---- Range slider -----------------------------------------------------------------------

    [Fact]
    public void Slider_KeepsTheNativeRange() =>
        // The platform already brings arrow keys, Home/End, a 24px touch target and drag behaviour
        // matching the OS. Hand-rolled sliders typically get the keyboard wrong.
        Render<ZenRangeSlider>(p => p.Add(x => x.Label, "Volume"))
            .Find("input").GetAttribute("type").ShouldBe("range");

    [Fact]
    public void Slider_AnnouncesAMeaningfulValue()
    {
        // "70 percent" is far more useful than "70", which is what aria-valuetext exists for.
        var cut = Render<ZenRangeSlider>(p => p
            .Add(x => x.Label, "Volume")
            .Add(x => x.Value, 70d)
            .Add(x => x.Format, v => $"{v:0} percent"));

        cut.Find("input").GetAttribute("aria-valuetext").ShouldBe("70 percent");
    }

    [Fact]
    public void Slider_ExposesTheFillPositionToCss()
    {
        var cut = Render<ZenRangeSlider>(p => p
            .Add(x => x.Label, "Volume")
            .Add(x => x.Min, 0d)
            .Add(x => x.Max, 200d)
            .Add(x => x.Value, 50d));

        (cut.Find("input").GetAttribute("style") ?? "").ShouldContain("--zen-slider-fill:25%");
    }

    [Fact]
    public void Slider_SurvivesADegenerateRange()
    {
        // Min == Max would divide by zero and emit an invalid gradient stop.
        var cut = Render<ZenRangeSlider>(p => p
            .Add(x => x.Label, "Fixed")
            .Add(x => x.Min, 5d)
            .Add(x => x.Max, 5d)
            .Add(x => x.Value, 5d));

        (cut.Find("input").GetAttribute("style") ?? "").ShouldContain("--zen-slider-fill:100%");
    }

    // ---- Progress ---------------------------------------------------------------------------

    [Fact]
    public void Progress_ReportsItsValue()
    {
        var bar = Render<ZenProgress>(p => p.Add(x => x.Value, 40d)).Find("[role=progressbar]");

        bar.GetAttribute("aria-valuenow").ShouldBe("40");
        bar.GetAttribute("aria-valuemax").ShouldBe("100");
    }

    [Fact]
    public void Progress_Indeterminate_OmitsValueNowEntirely()
    {
        // The single most important line in the component. Omitting aria-valuenow is exactly how
        // ARIA says "in progress, amount unknown"; setting 0 announces "0 percent", which claims a
        // fact that is not known and reads as a stalled operation.
        var bar = Render<ZenProgress>().Find("[role=progressbar]");

        bar.HasAttribute("aria-valuenow").ShouldBeFalse();
    }

    [Theory]
    [InlineData(ZenProgressShape.Linear)]
    [InlineData(ZenProgressShape.Circular)]
    public void Progress_BothShapesCarryTheRole(ZenProgressShape shape) =>
        Render<ZenProgress>(p => p
            .Add(x => x.Shape, shape)
            .Add(x => x.Value, 50d))
        .Find("[role=progressbar]").ShouldNotBeNull();

    [Fact]
    public void Progress_Circular_SpinsTheSvgNotTheArc()
    {
        // Regression: animate-spin sat on the <circle>. Two things went wrong there. A CSS
        // transform on the circle overrides its rotate(-90 18 18) presentation attribute, so the
        // arc jumps to the 3 o'clock start; and an SVG child rotates about the SVG origin (0,0)
        // rather than its own centre, so it orbited the top-left corner instead of turning.
        var cut = Render<ZenProgress>(p => p
            .Add(x => x.Shape, ZenProgressShape.Circular)
            .Add(x => x.Value, null));

        cut.Find("svg").ClassList.ShouldContain("motion-safe:animate-spin");

        foreach (var circle in cut.FindAll("circle"))
        {
            circle.ClassList.ShouldNotContain("animate-spin");
            circle.ClassList.ShouldNotContain("motion-safe:animate-spin");
        }
    }

    [Fact]
    public void Progress_Circular_DoesNotSpinWhenDeterminate() =>
        Render<ZenProgress>(p => p
            .Add(x => x.Shape, ZenProgressShape.Circular)
            .Add(x => x.Value, 40d))
        .Find("svg").ClassList.ShouldNotContain("motion-safe:animate-spin");

    [Fact]
    public void Progress_ClampsOutOfRangeValues()
    {
        Render<ZenProgress>(p => p.Add(x => x.Value, 150d))
            .Find("[role=progressbar]").GetAttribute("aria-valuenow").ShouldBe("100");

        Render<ZenProgress>(p => p.Add(x => x.Value, -20d))
            .Find("[role=progressbar]").GetAttribute("aria-valuenow").ShouldBe("0");
    }

    // ---- Form -------------------------------------------------------------------------------

    private sealed class FormModel
    {
        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Name is required.")]
        public string? Name { get; set; }
    }

    [Fact]
    public void Form_RendersAnEditForm()
    {
        // Regression: ZenForm passed both EditContext and Model to EditForm, which rejects the
        // combination outright - so every page using it returned a 500. The unit tests all passed,
        // because none of them had rendered a ZenForm.
        var act = () => Render<ZenForm>(p => p
            .Add(x => x.Model, new FormModel())
            .AddChildContent("<p>fields</p>"));

        act.ShouldNotThrow();
    }

    [Fact]
    public void Form_ShowsASummaryAfterAFailedSubmit()
    {
        var cut = Render<ZenForm>(p => p
            .Add(x => x.Model, new FormModel())
            .AddChildContent("<p>fields</p>"));

        cut.FindAll("[role=alert]").ShouldBeEmpty("nothing has been submitted yet.");

        cut.Find("form").Submit();

        var summary = cut.Find("[role=alert]");

        summary.TextContent.ShouldContain("Name is required.");
        // tabindex so a submit handler can move focus here. A failed submit that leaves focus on
        // the button, with the reason rendered above the fold, is how forms fail keyboard users.
        summary.GetAttribute("tabindex").ShouldBe("-1");
    }

    [Fact]
    public void Form_SummaryCanBeSuppressed()
    {
        var cut = Render<ZenForm>(p => p
            .Add(x => x.Model, new FormModel())
            .Add(x => x.ShowSummary, false)
            .AddChildContent("<p>fields</p>"));

        cut.Find("form").Submit();

        cut.FindAll("[role=alert]").ShouldBeEmpty();
    }

    // ---- Indicator --------------------------------------------------------------------------

    private static RenderFragment Child(string text) => b => b.AddContent(0, text);

    [Fact]
    public void Indicator_CountIsAnnounced()
    {
        // A count is real information, so it stays in the accessibility tree.
        var cut = Render<ZenIndicator>(p => p
            .Add(x => x.Count, 3)
            .Add(x => x.ChildContent, Child("Notifications")));

        cut.Markup.ShouldContain("3");
        cut.Find("span.absolute").HasAttribute("aria-hidden").ShouldBeFalse();
    }

    [Fact]
    public void Indicator_BareDotIsHiddenButExplained()
    {
        // "bullet" tells a screen reader user nothing, so the dot is hidden and the meaning is
        // carried by visually hidden text instead.
        var cut = Render<ZenIndicator>(p => p
            .Add(x => x.Label, "Online")
            .Add(x => x.ChildContent, Child("Avatar")));

        cut.Find("span.absolute").GetAttribute("aria-hidden").ShouldBe("true");
        cut.Find(".zen-sr-only").TextContent.ShouldBe("Online");
    }

    [Fact]
    public void Indicator_UnexplainedDot_FailsLoudly()
    {
        var act = () => Render<ZenIndicator>(p => p.Add(x => x.ChildContent, Child("Avatar")));

        act.ShouldThrow<InvalidOperationException>().Message.ShouldContain(nameof(ZenIndicator.Label));
    }

    [Fact]
    public void Indicator_HidesAZeroCountByDefault()
    {
        // An unread badge reading "0" is worse than no badge.
        Render<ZenIndicator>(p => p
            .Add(x => x.Count, 0)
            .Add(x => x.ChildContent, Child("Inbox")))
        .FindAll("span.absolute").ShouldBeEmpty();

        Render<ZenIndicator>(p => p
            .Add(x => x.Count, 0)
            .Add(x => x.ShowZero, true)
            .Add(x => x.ChildContent, Child("Inbox")))
        .FindAll("span.absolute").ShouldNotBeEmpty();
    }

    [Theory]
    [InlineData(ZenIntent.Neutral)]
    [InlineData(ZenIntent.Primary)]
    [InlineData(ZenIntent.Danger)]
    [InlineData(ZenIntent.Success)]
    public void Indicator_FillContrastsWithTheSurface(ZenIntent intent)
    {
        // Regression: the fill was sliced out of ZenStyles.Solid(), whose Neutral background is
        // surface-raised - so a neutral badge rendered white on a white page and was invisible.
        // A marker's whole job is to be noticed.
        var classes = Render<ZenIndicator>(p => p
            .Add(x => x.Count, 1)
            .Add(x => x.Intent, intent)
            .Add(x => x.ChildContent, Child("Inbox")))
            .Find("span.absolute").ClassList;

        classes.ShouldNotContain("bg-surface-raised");
        classes.ShouldNotContain("bg-surface");
        classes.ShouldContain(c => c.StartsWith("bg-", StringComparison.Ordinal));
    }

    [Fact]
    public void Indicator_CapsLargeCounts() =>
        // Keeps a four-digit badge from stretching across the element it marks.
        Render<ZenIndicator>(p => p
            .Add(x => x.Count, 1234)
            .Add(x => x.MaxCount, 99)
            .Add(x => x.ChildContent, Child("Inbox")))
        .Find("span.absolute").TextContent.Trim().ShouldBe("99+");
}
