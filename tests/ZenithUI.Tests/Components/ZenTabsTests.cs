namespace ZenithUI.Tests.Components;

/// <summary>
/// ZenTabs: the WAI-ARIA tabs pattern - roles and id wiring, roving tabindex, and the arrow keys.
/// </summary>
public class ZenTabsTests : BunitContext
{
    public ZenTabsTests()
    {
        Renderer.SetRendererInfo(new RendererInfo("Server", isInteractive: true));
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    private sealed record TabSpec(string Title, string? Key = null, bool Disabled = false);

    private static readonly TabSpec[] Specs =
    [
        new("Overview", "overview"),
        new("Activity", "activity"),
        new("Billing", "billing", Disabled: true),
        new("Settings", "settings"),
    ];

    private int _active;

    private IRenderedComponent<ZenTabs> Tabs(
        Action<ComponentParameterCollectionBuilder<ZenTabs>>? extra = null,
        TabSpec[]? specs = null) =>
        Render<ZenTabs>(p =>
        {
            p.Add(x => x.ActiveIndex, _active)
                .Add(x => x.ActiveIndexChanged, i => _active = i)
                .Add(x => x.Label, "Project")
                .Add(x => x.ChildContent, (RenderFragment)(builder =>
                {
                    var seq = 0;

                    foreach (var spec in specs ?? Specs)
                    {
                        builder.OpenComponent<ZenTab>(seq++);
                        builder.AddComponentParameter(seq++, nameof(ZenTab.Title), spec.Title);
                        builder.AddComponentParameter(seq++, nameof(ZenTab.Key), spec.Key);
                        builder.AddComponentParameter(seq++, nameof(ZenTab.Disabled), spec.Disabled);
                        builder.AddComponentParameter(seq++, nameof(ZenTab.ChildContent),
                            (RenderFragment)(b => b.AddContent(0, $"{spec.Title} panel")));
                        builder.CloseComponent();
                    }
                }));

            extra?.Invoke(p);
        });

    private static void Key(IRenderedComponent<ZenTabs> cut, string key) =>
        cut.Find("[role=tablist]").KeyDown(new KeyboardEventArgs { Key = key });

    private static string Panel(IRenderedComponent<ZenTabs> cut) =>
        cut.FindAll("[role=tabpanel]").Single(p => !p.HasAttribute("hidden")).TextContent.Trim();

    [Fact]
    public void TabsAndPanel_AreWiredByIds()
    {
        var cut = Tabs();

        var list = cut.Find("[role=tablist]");
        list.GetAttribute("aria-label").ShouldBe("Project");

        var tabs = cut.FindAll("[role=tab]");
        tabs.Count.ShouldBe(4);

        var panel = cut.Find("[role=tabpanel]");
        tabs[0].GetAttribute("aria-controls").ShouldBe(panel.Id);
        panel.GetAttribute("aria-labelledby").ShouldBe(tabs[0].Id);
        panel.GetAttribute("tabindex").ShouldBe("0");
    }

    [Fact]
    public void OnlyTheSelectedTab_IsInTheTabOrder()
    {
        var tabs = Tabs().FindAll("[role=tab]");

        tabs.Select(t => t.GetAttribute("tabindex")).ShouldBe(["0", "-1", "-1", "-1"]);
        tabs.Select(t => t.GetAttribute("aria-selected")).ShouldBe(["true", "false", "false", "false"]);
    }

    [Fact]
    public void OnlyTheSelectedPanel_IsRendered()
    {
        var cut = Tabs();

        cut.FindAll("[role=tabpanel]").Count.ShouldBe(1);
        Panel(cut).ShouldBe("Overview panel");
    }

    [Fact]
    public void Clicking_SelectsAndBinds()
    {
        var cut = Tabs();

        cut.FindAll("[role=tab]")[1].Click();

        _active.ShouldBe(1);
        Panel(cut).ShouldBe("Activity panel");
    }

    [Fact]
    public void ArrowRight_SelectsTheNext_SkippingDisabled()
    {
        _active = 1;
        var cut = Tabs();

        Key(cut, "ArrowRight");

        _active.ShouldBe(3);
        Panel(cut).ShouldBe("Settings panel");
    }

    [Fact]
    public void TheArrows_Wrap()
    {
        var cut = Tabs();

        Key(cut, "ArrowLeft");

        _active.ShouldBe(3);
    }

    [Fact]
    public void HomeAndEnd()
    {
        _active = 1;
        var cut = Tabs();

        Key(cut, "End");
        _active.ShouldBe(3);

        Key(cut, "Home");
        _active.ShouldBe(0);
    }

    [Fact]
    public void Vertical_UsesUpAndDown()
    {
        var cut = Tabs(p => p.Add(x => x.Vertical, true));

        cut.Find("[role=tablist]").GetAttribute("aria-orientation").ShouldBe("vertical");

        Key(cut, "ArrowRight");
        _active.ShouldBe(0);

        Key(cut, "ArrowDown");
        _active.ShouldBe(1);
    }

    [Fact]
    public void ManualActivation_ArrowsMoveFocusOnly()
    {
        var cut = Tabs(p => p.Add(x => x.ManualActivation, true));

        Key(cut, "ArrowRight");

        _active.ShouldBe(0);
        Panel(cut).ShouldBe("Overview panel");
        JSInterop.VerifyInvoke("focusElement").Arguments[0].ShouldBe(cut.FindAll("[role=tab]")[1].Id);
    }

    [Fact]
    public void ADisabledTab_CannotBeChosen()
    {
        var cut = Tabs();
        var billing = cut.FindAll("[role=tab]")[2];

        billing.HasAttribute("disabled").ShouldBeTrue();
    }

    [Fact]
    public void ActiveKey_SelectsByKey()
    {
        var cut = Tabs(p => p.Add(x => x.ActiveKey, "settings"));

        Panel(cut).ShouldBe("Settings panel");
    }

    [Fact]
    public void KeepAlive_RendersEveryPanel_HidingTheRest()
    {
        var cut = Tabs(p => p.Add(x => x.KeepAlive, true));

        var panels = cut.FindAll("[role=tabpanel]");
        panels.Count.ShouldBe(4);
        panels.Count(p => p.HasAttribute("hidden")).ShouldBe(3);
    }

    [Fact]
    public void UnderStaticRendering_TheSelectedTabIsRight_AndTheRestAreDisabled()
    {
        Renderer.SetRendererInfo(new RendererInfo("Static", isInteractive: false));
        _active = 1;

        var cut = Tabs();

        var tabs = cut.FindAll("[role=tab]");
        tabs[1].HasAttribute("disabled").ShouldBeFalse();
        tabs[0].HasAttribute("disabled").ShouldBeTrue();
        Panel(cut).ShouldBe("Activity panel");
    }

    [Fact]
    public void AnInitiallyDisabledActiveTab_MovesToTheNextEnabled()
    {
        _active = 2;

        Panel(Tabs()).ShouldBe("Settings panel");
    }
}
