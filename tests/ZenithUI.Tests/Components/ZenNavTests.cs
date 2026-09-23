using Bunit.TestDoubles;
using Microsoft.AspNetCore.Components.Routing;

namespace ZenithUI.Tests.Components;

/// <summary>
/// ZenNavMenu and ZenNavLink: the list semantics that let a screen reader skip a navigation, and
/// the matching rules that decide which item is announced as the current page.
/// </summary>
public class ZenNavTests : BunitContext
{
    private readonly BunitNavigationManager _nav;

    public ZenNavTests()
    {
        Renderer.SetRendererInfo(new RendererInfo("Server", isInteractive: true));
        _nav = Services.GetService<BunitNavigationManager>()!;
    }

    private IRenderedComponent<ZenNavLink> RenderLink(
        string href,
        Action<ComponentParameterCollectionBuilder<ZenNavLink>>? extra = null) =>
        Render<ZenNavLink>(p =>
        {
            p.Add(x => x.Href, href);
            p.Add(x => x.Text, "Orders");
            extra?.Invoke(p);
        });

    // ---- aria-current -------------------------------------------------------------------------

    [Fact]
    public void TheCurrentPage_SaysSoInAria()
    {
        // The whole reason this component exists rather than wrapping Blazor's NavLink. NavLink
        // works out the same answer and spends it on a CSS class alone, so the active item is
        // obvious to a sighted user and completely silent to a screen reader.
        _nav.NavigateTo("orders");

        RenderLink("orders").Find("a").GetAttribute("aria-current").ShouldBe("page");
    }

    [Fact]
    public void AnInactiveLink_OmitsAriaCurrentEntirely()
    {
        // Omitted rather than "false". Some screen readers announce an explicit false, which tells
        // the user about a state they never asked about - on every link in the menu.
        _nav.NavigateTo("catalog");

        RenderLink("orders").Find("a").HasAttribute("aria-current").ShouldBeFalse();
    }

    [Fact]
    public void ALinkBecomesCurrent_WhenTheLocationChangesUnderIt()
    {
        var cut = RenderLink("orders");
        cut.Find("a").HasAttribute("aria-current").ShouldBeFalse();

        _nav.NavigateTo("orders");

        cut.Find("a").GetAttribute("aria-current").ShouldBe("page");
    }

    // ---- Matching -----------------------------------------------------------------------------

    [Fact]
    public void APrefixMatch_RequiresAPathSeparator()
    {
        // A plain StartsWith would light up "Order" while the user is on /orders. Every nav whose
        // routes share a stem has this bug, and it always looks like a styling problem.
        _nav.NavigateTo("orders");

        RenderLink("order").Find("a").HasAttribute("aria-current").ShouldBeFalse();
    }

    [Fact]
    public void APrefixMatch_CoversChildRoutes()
    {
        _nav.NavigateTo("orders/1042");

        RenderLink("orders").Find("a").GetAttribute("aria-current").ShouldBe("page");
    }

    [Fact]
    public void TheRootLink_DefaultsToAnExactMatch()
    {
        // Prefix on the root would match every page in the application, leaving two items
        // highlighted on every screen. Nobody ever wants that, so it is not the default there.
        _nav.NavigateTo("orders");

        RenderLink("").Find("a").HasAttribute("aria-current").ShouldBeFalse();
    }

    [Fact]
    public void TheRootLink_StillMatchesTheRoot()
    {
        _nav.NavigateTo("");

        RenderLink("").Find("a").GetAttribute("aria-current").ShouldBe("page");
    }

    [Fact]
    public void AnExplicitMatch_OverridesThePerLinkDefault()
    {
        _nav.NavigateTo("orders/1042");

        var cut = RenderLink("orders", p => p.Add(x => x.Match, NavLinkMatch.All));

        cut.Find("a").HasAttribute("aria-current").ShouldBeFalse();
    }

    [Fact]
    public void AQueryString_DoesNotDeselectTheLink()
    {
        // A paged table appends ?page=2 and would otherwise switch its own nav item off - which
        // reads as a navigation bug and is a matching bug.
        _nav.NavigateTo("orders?page=2");

        RenderLink("orders").Find("a").GetAttribute("aria-current").ShouldBe("page");
    }

    [Fact]
    public void ALinkCarryingAQueryString_ComparesAgainstIt()
    {
        _nav.NavigateTo("orders?status=open");

        RenderLink("orders?status=closed").Find("a").HasAttribute("aria-current").ShouldBeFalse();
        RenderLink("orders?status=open").Find("a").GetAttribute("aria-current").ShouldBe("page");
    }

    [Fact]
    public void Active_ForcesTheCurrentState()
    {
        _nav.NavigateTo("catalog");

        var cut = RenderLink("orders", p => p.Add(x => x.Active, true));

        cut.Find("a").GetAttribute("aria-current").ShouldBe("page");
    }

    // ---- Disabled -----------------------------------------------------------------------------

    [Fact]
    public void ADisabledLink_IsNotALinkAtAll()
    {
        // There is no `disabled` for <a>, and `pointer-events: none` stops the mouse while leaving
        // the link in the tab order and followable with Enter. A span with the role restored is
        // the only version that is actually unavailable.
        var cut = RenderLink("orders", p => p.Add(x => x.Disabled, true));

        var element = cut.Find("[role='link']");

        element.TagName.ShouldBe("SPAN");
        element.HasAttribute("href").ShouldBeFalse();
        element.GetAttribute("aria-disabled").ShouldBe("true");
    }

    // ---- Menu structure -----------------------------------------------------------------------

    [Fact]
    public void AMenu_IsAListInsideALandmark()
    {
        // The list is what makes a screen reader announce "list, 2 items" and offer to skip the
        // navigation. A row of bare anchors in a <nav> gives that up for nothing.
        var cut = RenderMenu(label: "Sections");

        cut.Find("nav").GetAttribute("aria-label").ShouldBe("Sections");
        cut.FindAll("nav > ul > li").Count.ShouldBe(2);
    }

    [Fact]
    public void AMenuWithoutALandmark_IsStillAList()
    {
        // For a group nested inside an existing nav: the inner landmark would be announced as a
        // separate navigation region the user then has to explore to find is part of the outer.
        var cut = RenderMenu(landmark: false);

        cut.FindAll("nav").Count.ShouldBe(0);
        cut.FindAll("ul > li").Count.ShouldBe(2);
    }

    [Fact]
    public void ATitle_NamesTheLandmarkItHeads()
    {
        var cut = RenderMenu(title: "Workspace");

        var nav = cut.Find("nav");
        var headingId = cut.Find("h2").Id;

        nav.GetAttribute("aria-labelledby").ShouldBe(headingId);
        nav.HasAttribute("aria-label").ShouldBeFalse();
    }

    [Fact]
    public void AnExplicitLabel_WinsOverTheTitle()
    {
        var cut = RenderMenu(title: "Workspace", label: "Workspace navigation");

        cut.Find("nav").GetAttribute("aria-label").ShouldBe("Workspace navigation");
        cut.Find("nav").HasAttribute("aria-labelledby").ShouldBeFalse();
    }

    [Fact]
    public void ALinkOutsideAMenu_RendersNoListItem()
    {
        // An <li> with no list around it is invalid HTML, and browsers recover from it
        // inconsistently - so the wrapper is the menu's contribution, not the link's.
        RenderLink("orders").FindAll("li").Count.ShouldBe(0);
    }

    private IRenderedComponent<ZenNavMenu> RenderMenu(
        string? label = null,
        string? title = null,
        bool landmark = true) =>
        Render<ZenNavMenu>(p => p
            .Add(x => x.Label, label)
            .Add(x => x.Title, title)
            .Add(x => x.Landmark, landmark)
            .Add(x => x.ChildContent, (RenderFragment)(builder =>
            {
                var seq = 0;

                foreach (var (href, text) in new[] { ("orders", "Orders"), ("catalog", "Catalog") })
                {
                    builder.OpenComponent<ZenNavLink>(seq++);
                    builder.AddComponentParameter(seq++, nameof(ZenNavLink.Href), href);
                    builder.AddComponentParameter(seq++, nameof(ZenNavLink.Text), text);
                    builder.CloseComponent();
                }
            })));
}
