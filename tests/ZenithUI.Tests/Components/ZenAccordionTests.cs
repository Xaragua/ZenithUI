namespace ZenithUI.Tests.Components;

/// <summary>
/// ZenAccordion: native details and summary, so most of the behaviour is the browser's. These pin
/// the markup that hands it over, and the rule that the component writes the open state only
/// when its parameter changes.
/// </summary>
public class ZenAccordionTests : BunitContext
{
    public ZenAccordionTests()
    {
        Renderer.SetRendererInfo(new RendererInfo("Server", isInteractive: true));
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    private IRenderedComponent<ZenAccordion> Accordion(
        Action<ComponentParameterCollectionBuilder<ZenAccordion>>? extra = null,
        bool firstOpen = false,
        bool secondDisabled = false) =>
        Render<ZenAccordion>(p =>
        {
            p.Add(x => x.ChildContent, (RenderFragment)(builder =>
            {
                builder.OpenComponent<ZenAccordionItem>(0);
                builder.AddComponentParameter(1, nameof(ZenAccordionItem.Title), "Shipping");
                builder.AddComponentParameter(2, nameof(ZenAccordionItem.Open), firstOpen);
                builder.AddComponentParameter(3, nameof(ZenAccordionItem.ChildContent), (RenderFragment)(b => b.AddContent(0, "Ships in 2 days.")));
                builder.CloseComponent();

                builder.OpenComponent<ZenAccordionItem>(10);
                builder.AddComponentParameter(11, nameof(ZenAccordionItem.Title), "Returns");
                builder.AddComponentParameter(12, nameof(ZenAccordionItem.Disabled), secondDisabled);
                builder.AddComponentParameter(13, nameof(ZenAccordionItem.ChildContent), (RenderFragment)(b => b.AddContent(0, "30 days.")));
                builder.CloseComponent();
            }));

            extra?.Invoke(p);
        });

    [Fact]
    public void EachSection_IsADetailsWithASummary()
    {
        var cut = Accordion();

        cut.FindAll("details").Count.ShouldBe(2);
        cut.FindAll("details > summary").Select(s => s.TextContent.Trim()).ShouldBe(["Shipping", "Returns"]);
    }

    [Fact]
    public void OneAtATime_IsTheNativeExclusiveAccordion()
    {
        var names = Accordion().FindAll("details").Select(d => d.GetAttribute("name")).ToList();

        names.ShouldAllBe(n => !string.IsNullOrEmpty(n));
        names.Distinct().Count().ShouldBe(1);
    }

    [Fact]
    public void Multiple_DropsTheName() =>
        Accordion(p => p.Add(x => x.Multiple, true)).FindAll("details").ShouldAllBe(d => !d.HasAttribute("name"));

    [Fact]
    public void Open_RendersTheOpenAttribute()
    {
        var details = Accordion(firstOpen: true).FindAll("details");

        details[0].HasAttribute("open").ShouldBeTrue();
        details[1].HasAttribute("open").ShouldBeFalse();
    }

    [Fact]
    public void Disabled_LeavesTheTabOrder()
    {
        var summary = Accordion(secondDisabled: true).FindAll("summary")[1];

        summary.GetAttribute("tabindex").ShouldBe("-1");
        summary.GetAttribute("aria-disabled").ShouldBe("true");
    }

    [Fact]
    public void Flush_HasNoOuterBox()
    {
        var root = Accordion(p => p.Add(x => x.Flush, true)).Find("div");

        root.ClassList.ShouldNotContain("rounded-zen-lg");
        root.ClassList.ShouldContain("divide-y");
    }

    [Fact]
    public void AToggle_ReportsTheStateReadFromTheElement()
    {
        JSInterop.SetupModule("./_content/ZenithUI/js/zen-dom.js").Setup<bool>("isOpen", _ => true).SetResult(true);
        var reported = new List<bool>();

        var cut = Render<ZenAccordionItem>(p => p
            .Add(x => x.Title, "Shipping")
            .Add(x => x.OpenChanged, open => reported.Add(open)));

        cut.Find("details").TriggerEvent("ontoggle", EventArgs.Empty);

        reported.ShouldBe([true]);
    }

    [Fact]
    public void TheToggleFromRenderingOpen_IsNotReportedAsAChange()
    {
        // The browser fires toggle when a section is first rendered open. Inferring the state by
        // flipping a flag would report "closed" and shut it.
        JSInterop.SetupModule("./_content/ZenithUI/js/zen-dom.js").Setup<bool>("isOpen", _ => true).SetResult(true);
        var reported = new List<bool>();

        var cut = Render<ZenAccordionItem>(p => p
            .Add(x => x.Title, "Shipping")
            .Add(x => x.Open, true)
            .Add(x => x.OpenChanged, open => reported.Add(open)));

        cut.Find("details").TriggerEvent("ontoggle", EventArgs.Empty);

        reported.ShouldBeEmpty();
        cut.Find("details").HasAttribute("open").ShouldBeTrue();
    }
}
