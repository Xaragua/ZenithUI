namespace ZenithUI.Tests.Components;

/// <summary>
/// ZenAppShell, ZenAppBar, ZenSideNav and ZenFooter: the landmarks, the skip link, and the
/// declarative wiring that lets the drawer work with no render mode at all.
/// </summary>
/// <remarks>
/// The renderer is deliberately left NON-interactive for most of these. A shell lives in a layout
/// and a layout is always static, so the render that matters is the one bUnit would only produce
/// if asked - and a drawer that quietly needed JavaScript would pass every interactive test.
/// </remarks>
public class ZenShellTests : BunitContext
{
    public ZenShellTests() =>
        Renderer.SetRendererInfo(new RendererInfo("Static", isInteractive: false));

    // ---- The shell ----------------------------------------------------------------------------

    [Fact]
    public void TheShell_GivesThePageAMainLandmark()
    {
        var cut = Render<ZenAppShell>(p => p.AddChildContent("<p>Body</p>"));

        var main = cut.Find("main");

        main.Id.ShouldNotBeNullOrEmpty();

        // Without tabindex the skip link scrolls to the landmark and leaves focus where it was, so
        // the next Tab returns to the second nav link - the exact thing it exists to avoid.
        main.GetAttribute("tabindex").ShouldBe("-1");
    }

    [Fact]
    public void TheSkipLink_PointsAtMain()
    {
        var cut = Render<ZenAppShell>(p => p.AddChildContent("<p>Body</p>"));

        var skip = cut.Find("a.zen-skip-link");

        skip.GetAttribute("href").ShouldBe($"#{cut.Find("main").Id}");

        // First in the DOM, because that is what "first in the tab order" means for a link with no
        // tabindex of its own.
        cut.Find("div > *").ClassList.ShouldContain("zen-skip-link");
    }

    [Fact]
    public void TheShell_TellsTheRailHowFarDownTheViewportToStart()
    {
        // The one measurement neither the bar nor the rail can work out alone: CSS gives an
        // element no way to measure a sticky sibling.
        var sticky = Render<ZenAppShell>(p => p.Add(x => x.StickyHeader, true));
        var loose = Render<ZenAppShell>(p => p.Add(x => x.StickyHeader, false));

        sticky.Find("div").GetAttribute("style")!
            .ShouldContain("--zen-sidenav-top: var(--zen-appbar-height)");

        loose.Find("div").GetAttribute("style")!.ShouldContain("--zen-sidenav-top: 0px");
    }

    [Fact]
    public void AnExplicitHeaderHeight_OverridesTheToken()
    {
        var cut = Render<ZenAppShell>(p => p
            .Add(x => x.StickyHeader, true)
            .Add(x => x.HeaderHeight, "5rem"));

        cut.Find("div").GetAttribute("style")!.ShouldContain("--zen-sidenav-top: 5rem");
    }

    // ---- The drawer wiring --------------------------------------------------------------------

    [Fact]
    public void TheMenuButton_TargetsTheSideNav_WithNoRenderModeInvolved()
    {
        // The milestone's load-bearing assertion. A layout can never be interactive, because Body
        // is a RenderFragment and a render fragment cannot cross a render-mode boundary - so if
        // the toggle needed an @onclick it would be undemonstrable in the only place a shell goes.
        var cut = RenderShellWithChrome();

        var button = cut.Find("header button");
        var nav = cut.Find("aside");

        button.GetAttribute("popovertarget").ShouldBe(nav.Id);
        button.GetAttribute("popovertargetaction").ShouldBe("toggle");
        button.HasAttribute("onclick").ShouldBeFalse();
    }

    [Fact]
    public void TheDrawer_IsAPopover()
    {
        // `auto` rather than `manual`: it is what brings Escape, light dismiss and focus restore,
        // none of which this component implements.
        RenderShellWithChrome().Find("aside").GetAttribute("popover").ShouldBe("auto");
    }

    [Fact]
    public void TheDrawersCloseButton_HidesItDeclarativelyToo()
    {
        var cut = RenderShellWithChrome();
        var nav = cut.Find("aside");

        var close = cut.Find("aside button[popovertargetaction='hide']");

        close.GetAttribute("popovertarget").ShouldBe(nav.Id);
    }

    [Fact]
    public void AnAppBarWithNothingToOpen_DrawsNoMenuButton()
    {
        // A button whose popovertarget names no element is inert and perfectly focusable: the user
        // lands on a control that does nothing, with no clue why.
        var cut = Render<ZenAppBar>(p => p.Add(x => x.Brand, (RenderFragment)(b => b.AddContent(0, "Z"))));

        cut.FindAll("button").Count.ShouldBe(0);
    }

    [Fact]
    public void TheMenuToggle_CanBeSuppressedForAShellWithNoSideNav()
    {
        var cut = RenderShellWithChrome(showMenuToggle: false);

        cut.FindAll("header button").Count.ShouldBe(0);
    }

    [Fact]
    public void AnAppBarOutsideAShell_TakesAnExplicitTarget()
    {
        var cut = Render<ZenAppBar>(p => p.Add(x => x.MenuTarget, "app-nav"));

        cut.Find("button").GetAttribute("popovertarget").ShouldBe("app-nav");
    }

    // ---- Landmarks ----------------------------------------------------------------------------

    [Fact]
    public void TheChrome_RendersTheLandmarksItClaims()
    {
        var cut = RenderShellWithChrome();

        cut.FindAll("header").Count.ShouldBe(1);
        cut.FindAll("main").Count.ShouldBe(1);
        cut.FindAll("footer").Count.ShouldBe(1);
        cut.Find("aside").GetAttribute("aria-label").ShouldBe("Main navigation");
    }

    [Fact]
    public void TheSideNav_TakesItsIdFromTheShell()
    {
        // Both halves read the same cascade, so a caller never wires an id by hand - and the two
        // cannot drift apart into a button that opens nothing.
        var cut = RenderShellWithChrome();

        cut.Find("aside").Id.ShouldBe(cut.Find("header button").GetAttribute("popovertarget"));
    }

    // ---- Content width ------------------------------------------------------------------------

    [Fact]
    public void ABoundedContentColumn_IsAlsoCentred()
    {
        // A max-width with no auto margin pins the column to the start edge, which on a wide
        // display reads as a layout that forgot to finish.
        var cut = Render<ZenAppShell>(p => p.Add(x => x.MaxWidth, ZenContentWidth.Medium));

        var main = cut.Find("main");

        main.ClassList.ShouldContain("max-w-5xl");
        main.ClassList.ShouldContain("mx-auto");
    }

    [Fact]
    public void AFullWidthShell_DoesNotCentreAnything()
    {
        var cut = Render<ZenAppShell>(p => p.Add(x => x.MaxWidth, ZenContentWidth.Full));

        cut.Find("main").ClassList.ShouldNotContain("mx-auto");
    }

    // ---- Progressive enhancement ---------------------------------------------------------------

    [Fact]
    public void TheSideNav_AsksForNoJavaScriptUnderStaticSsr()
    {
        // The guarantee behind the whole design: rendered without an interactive renderer, the nav
        // is complete markup and makes no interop call that would throw for want of a runtime.
        var cut = Render<ZenSideNav>(p => p.AddChildContent("<a href=\"/\">Home</a>"));

        cut.Find("aside").GetAttribute("popover").ShouldBe("auto");
        cut.Find("a").GetAttribute("href").ShouldBe("/");
    }

    [Fact]
    public void TheFooter_IsDockedOutsideTheScrollingLinks()
    {
        // Settings and Help at the bottom of the rail: they must not scroll away with a long list
        // of links above them. That holds only while the footer is a sibling of the scrolling
        // region rather than inside it, and does not shrink when the links overflow.
        var cut = Render<ZenSideNav>(p => p
            .AddChildContent("<a href=\"/orders\">Orders</a>")
            .Add(x => x.Footer, "<a href=\"/settings\">Settings</a>"));

        var regions = cut.Find("aside").Children.Where(e => e.TagName == "DIV").ToList();
        var scrolling = regions.Single(e => e.ClassList.Contains("overflow-y-auto"));
        var footer = regions[^1];

        footer.ShouldNotBe(scrolling);
        footer.ClassList.ShouldContain("shrink-0");
        footer.QuerySelector("a")!.GetAttribute("href").ShouldBe("/settings");
        scrolling.QuerySelector("a[href='/settings']").ShouldBeNull();
        scrolling.ClassList.ShouldContain("flex-1", "the links must take the spare height, or the footer floats up under them.");
    }

    [Fact]
    public void AnExplicitId_OutranksTheShellCascade()
    {
        // The escape hatch for a nav the cascade cannot reach: one the consumer made an
        // interactive island under a static shell. Letting the cascade win would discard the id
        // and leave the app bar pointed at an element that does not exist.
        var cut = Render<ZenAppShell>(p => p.Add(x => x.SideNav, (RenderFragment)(builder =>
        {
            builder.OpenComponent<ZenSideNav>(0);
            builder.AddComponentParameter(1, nameof(ZenSideNav.Id), "app-nav");
            builder.CloseComponent();
        })));

        cut.Find("aside").Id.ShouldBe("app-nav");
    }

    private IRenderedComponent<ZenAppShell> RenderShellWithChrome(bool showMenuToggle = true) =>
        Render<ZenAppShell>(p => p
            .Add(x => x.Header, (RenderFragment)(builder =>
            {
                builder.OpenComponent<ZenAppBar>(0);
                builder.AddComponentParameter(1, nameof(ZenAppBar.ShowMenuToggle), showMenuToggle);
                builder.CloseComponent();
            }))
            .Add(x => x.SideNav, (RenderFragment)(builder =>
            {
                builder.OpenComponent<ZenSideNav>(0);
                builder.AddComponentParameter(
                    1,
                    nameof(ZenSideNav.ChildContent),
                    (RenderFragment)(b => b.AddMarkupContent(0, "<a href=\"/\">Home</a>")));
                builder.CloseComponent();
            }))
            .Add(x => x.Footer, (RenderFragment)(builder =>
            {
                builder.OpenComponent<ZenFooter>(0);
                builder.CloseComponent();
            }))
            .AddChildContent("<p>Body</p>"));

    // ---- Collapsible rail ---------------------------------------------------------------------

    [Fact]
    public void ACollapsibleNav_RestoresItsStateBeforeTheRailPaints()
    {
        // The inline script has to come before the <aside>: run after it, a user who collapsed
        // the rail could watch it paint open and snap shut on every full page load.
        var cut = Render<ZenSideNav>(p => p.Add(x => x.Collapsible, true));

        var html = cut.Markup;
        var script = html.IndexOf("<script>", StringComparison.Ordinal);

        script.ShouldBeGreaterThanOrEqualTo(0);
        script.ShouldBeLessThan(html.IndexOf("<aside", StringComparison.Ordinal));
        cut.Find("aside").ClassList.ShouldContain("zen-sidenav-collapsible");
    }

    [Fact]
    public void ACollapsibleNav_HasAToggleThatNeedsNoRenderMode()
    {
        // Rendered statically here: the toggle is a plain button that zen-sidenav.js finds by
        // attribute, not an @onclick that would be dead in a layout.
        var cut = Render<ZenSideNav>(p => p.Add(x => x.Collapsible, true));

        var toggle = cut.Find("button[data-zen-sidenav-toggle]");

        toggle.GetAttribute("type").ShouldBe("button");
        toggle.GetAttribute("aria-controls").ShouldBe(cut.Find("aside").Id);

        // Both labels, so CSS can pick the right one - and with it the accessible name - before
        // any script has run.
        toggle.QuerySelector(".zen-sidenav-collapse-text")!.TextContent.ShouldBe("Collapse navigation");
        toggle.QuerySelector(".zen-sidenav-expand-text")!.TextContent.ShouldBe("Expand navigation");
    }

    [Fact]
    public void ANavThatIsNotCollapsible_RendersNoneOfIt()
    {
        var cut = Render<ZenSideNav>();

        cut.FindAll("script").ShouldBeEmpty();
        cut.FindAll("[data-zen-sidenav-toggle]").ShouldBeEmpty();
        cut.Find("aside").ClassList.ShouldNotContain("zen-sidenav-collapsible");
    }
}
