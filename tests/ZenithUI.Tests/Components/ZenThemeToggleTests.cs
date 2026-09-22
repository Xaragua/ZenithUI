using Microsoft.Extensions.DependencyInjection;

namespace ZenithUI.Tests.Components;

public class ZenThemeToggleTests : BunitContext
{
    private readonly FakeThemeService _theme = new();

    public ZenThemeToggleTests()
    {
        Services.AddSingleton<IZenThemeService>(_theme);

        // ZenithUI components branch on RendererInfo.IsInteractive to decide whether JavaScript is
        // reachable, so every test has to say which renderer it is simulating. The default here is
        // an interactive Server circuit; StaticSsr_* tests override it.
        Renderer.SetRendererInfo(new RendererInfo("Server", isInteractive: true));
    }

    // ---- Toggle shape ----------------------------------------------------------------------

    [Fact]
    public void Toggle_RendersASingleButton()
    {
        var cut = Render<ZenThemeToggle>();

        var button = cut.Find("button");

        button.GetAttribute("type").ShouldBe("button", "a button inside a form must not submit it.");
    }

    [Fact]
    public void Toggle_CarriesAnAccessibleName()
    {
        // The default toggle shows only an icon, so aria-label is the only accessible name a
        // screen reader has to work with.
        var cut = Render<ZenThemeToggle>();

        cut.Find("button").GetAttribute("aria-label").ShouldBe("Switch to dark theme");
    }

    [Fact]
    public void Toggle_AccessibleName_DescribesTheDestinationNotTheCurrentState()
    {
        _theme.Apply(ZenThemeMode.Dark, ZenTheme.Dark);

        var cut = Render<ZenThemeToggle>();

        cut.Find("button").GetAttribute("aria-label").ShouldBe("Switch to light theme");
    }

    [Fact]
    public void Toggle_HidesItsIconFromAssistiveTechnology() =>
        Render<ZenThemeToggle>().Find("svg").GetAttribute("aria-hidden").ShouldBe("true");

    [Fact]
    public void Toggle_CallsToggleAsync_WhenClicked()
    {
        var cut = Render<ZenThemeToggle>();

        cut.Find("button").Click();

        _theme.ToggleCalls.ShouldBe(1);
    }

    [Fact]
    public void Toggle_HonoursDisabled()
    {
        var cut = Render<ZenThemeToggle>(p => p.Add(x => x.Disabled, true));

        cut.Find("button").HasAttribute("disabled").ShouldBeTrue();
    }

    [Fact]
    public void Toggle_ShowsALabel_OnlyWhenAsked()
    {
        Render<ZenThemeToggle>()
            .FindAll("span").ShouldBeEmpty();

        Render<ZenThemeToggle>(p => p.Add(x => x.ShowLabels, true))
            .Find("span").TextContent.Trim().ShouldBe("Light");
    }

    // ---- Segmented shape -------------------------------------------------------------------

    [Fact]
    public void Segmented_RendersARadioGroupOfThree()
    {
        var cut = RenderSegmented();

        var group = cut.Find("[role=radiogroup]");

        group.GetAttribute("aria-label").ShouldBe("Colour theme");
        cut.FindAll("[role=radio]").Count.ShouldBe(3);
    }

    [Fact]
    public void Segmented_MarksExactlyOneOptionChecked()
    {
        var cut = RenderSegmented();

        var checkedStates = cut.FindAll("[role=radio]")
            .Select(r => r.GetAttribute("aria-checked"))
            .ToList();

        // A radiogroup where nothing (or everything) is checked is announced as broken, and it is
        // an easy state to reach when selection is derived from a service rather than local state.
        checkedStates.Count(state => state == "true").ShouldBe(1);
        checkedStates[0].ShouldBe("true", "System is the default mode.");
    }

    [Fact]
    public void Segmented_MovesTheCheckedOption_WhenTheServiceChanges()
    {
        var cut = RenderSegmented();

        _theme.Apply(ZenThemeMode.Dark, ZenTheme.Dark);
        cut.Render();

        cut.FindAll("[role=radio]")[2].GetAttribute("aria-checked").ShouldBe("true");
    }

    [Theory]
    [InlineData(0, ZenThemeMode.System)]
    [InlineData(1, ZenThemeMode.Light)]
    [InlineData(2, ZenThemeMode.Dark)]
    public void Segmented_SetsTheMatchingMode_WhenAnOptionIsClicked(int index, ZenThemeMode expected)
    {
        var cut = RenderSegmented();

        cut.FindAll("[role=radio]")[index].Click();

        _theme.SetModeCalls.ShouldHaveSingleItem().ShouldBe(expected);
    }

    [Fact]
    public void Segmented_KeepsOptionLabelsAvailableToScreenReaders_WhenLabelsAreHidden()
    {
        // Icon-only segments still need a name each. The label text stays in the DOM under
        // zen-sr-only rather than being dropped.
        var cut = RenderSegmented();

        var labels = cut.FindAll("[role=radio] span").Select(s => s.TextContent.Trim()).ToList();

        labels.ShouldBe(["System", "Light", "Dark"]);
        cut.FindAll("[role=radio] span").ShouldAllBe(s => s.ClassList.Contains("zen-sr-only"));
    }

    [Fact]
    public void Segmented_UsesCustomLabels()
    {
        var cut = Render<ZenThemeToggle>(p => p
            .Add(x => x.Mode, ZenThemeToggleMode.Segmented)
            .Add(x => x.SystemLabel, "Auto")
            .Add(x => x.LightLabel, "Day")
            .Add(x => x.DarkLabel, "Night"));

        cut.FindAll("[role=radio] span").Select(s => s.TextContent.Trim())
            .ShouldBe(["Auto", "Day", "Night"]);
    }

    // ---- Pass-through and lifecycle --------------------------------------------------------

    [Fact]
    public void SplattedAttributesAndClass_AreBothApplied()
    {
        var cut = Render<ZenThemeToggle>(p => p
            .Add(x => x.Class, "mt-4")
            .AddUnmatched("data-testid", "theme"));

        var button = cut.Find("button");

        button.GetAttribute("data-testid").ShouldBe("theme");
        button.ClassList.ShouldContain("mt-4");
        button.ClassList.ShouldContain("zen-focus", "the component's own classes must survive.");
    }

    [Fact]
    public void Toggle_InitializesTheService_WithoutNeedingAProvider()
    {
        // Regression: the toggle used to rely on a ZenThemeProvider higher in the tree to call
        // InitializeAsync. An app bar is the toggle's most common home, and an app bar usually
        // lives in a static layout where a provider cannot be placed at all - a RenderFragment
        // cannot cross a static-to-interactive boundary. The symptom was nasty precisely because
        // it was half-working: clicking moved the control's own highlight, so it looked alive,
        // while the DOM attribute was never written and the page never repainted.
        Render<ZenThemeToggle>();

        _theme.InitializeCalls.ShouldBe(1);
    }

    [Fact]
    public void Segmented_InitializesTheService_WithoutNeedingAProvider()
    {
        RenderSegmented();

        _theme.InitializeCalls.ShouldBe(1);
    }

    [Fact]
    public void MultipleToggles_ShareASingleInitialization()
    {
        // InitializeAsync is idempotent, but each toggle still calls it; the service is what
        // collapses them into one module import. Asserting the call count here would lock in the
        // wrong contract, so this only pins down that every instance ends up initialized.
        Render<ZenThemeToggle>();
        RenderSegmented();

        _theme.IsInitialized.ShouldBeTrue();
    }

    // ---- Static SSR ------------------------------------------------------------------------

    [Fact]
    public void StaticSsr_StillRendersAUsableControl()
    {
        // Under static SSR the toggle cannot work - its entire job is a client-side preference.
        // What it must not do is throw, or render an empty or unlabelled element: this markup is
        // what a prerender pass, a no-JavaScript client and a crawler all see.
        Renderer.SetRendererInfo(new RendererInfo("Static", isInteractive: false));

        var cut = Render<ZenThemeToggle>(p => p.Add(x => x.Mode, ZenThemeToggleMode.Segmented));

        cut.FindAll("[role=radio]").Count.ShouldBe(3);
        cut.Find("[role=radiogroup]").GetAttribute("aria-label").ShouldBe("Colour theme");
    }

    [Fact]
    public void StaticSsr_DoesNotReachForJavaScript()
    {
        // Initialization means a JS module import, which throws on a renderer with no JavaScript
        // runtime attached. The guard is RendererInfo.IsInteractive, and this is what pins it.
        Renderer.SetRendererInfo(new RendererInfo("Static", isInteractive: false));

        Render<ZenThemeToggle>();

        _theme.InitializeCalls.ShouldBe(0);
    }

    [Fact]
    public void Dispose_UnsubscribesFromTheService()
    {
        // The service outlives any single component, so a toggle that fails to detach keeps itself
        // - and its whole render tree - alive for the lifetime of the circuit.
        var cut = Render<ZenThemeToggle>();

        cut.Instance.Dispose();

        _theme.HasNoSubscribers.ShouldBeTrue();
    }

    private IRenderedComponent<ZenThemeToggle> RenderSegmented() =>
        Render<ZenThemeToggle>(p => p.Add(x => x.Mode, ZenThemeToggleMode.Segmented));
}
