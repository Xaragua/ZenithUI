namespace ZenithUI.Tests.Components;

/// <summary>
/// ZenPopover and the placement arithmetic behind it.
/// </summary>
/// <remarks>
/// bUnit has no layout engine, so nothing here can assert where a panel lands - that is
/// zen-popover.js reading real rectangles, and it belongs in the demo. What these tests do cover is
/// the part that is pure logic and easy to get subtly wrong: the placement decomposition, the ARIA
/// wiring, and the rule that the panel's markup exists only while it is open.
/// </remarks>
public class ZenPopoverTests : BunitContext
{
    public ZenPopoverTests()
    {
        Renderer.SetRendererInfo(new RendererInfo("Server", isInteractive: true));

        // The component imports its module on first interactive render. Without this the import
        // throws and the render fails before any assertion runs.
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    // ---- Placement decomposition --------------------------------------------------------------

    [Theory]
    [InlineData(ZenPlacement.BottomStart, "bottom", "start")]
    [InlineData(ZenPlacement.Bottom, "bottom", "center")]
    [InlineData(ZenPlacement.BottomEnd, "bottom", "end")]
    [InlineData(ZenPlacement.TopStart, "top", "start")]
    [InlineData(ZenPlacement.Top, "top", "center")]
    [InlineData(ZenPlacement.TopEnd, "top", "end")]
    [InlineData(ZenPlacement.StartTop, "start", "start")]
    [InlineData(ZenPlacement.Start, "start", "center")]
    [InlineData(ZenPlacement.StartBottom, "start", "end")]
    [InlineData(ZenPlacement.EndTop, "end", "start")]
    [InlineData(ZenPlacement.End, "end", "center")]
    [InlineData(ZenPlacement.EndBottom, "end", "end")]
    public void Placement_DecomposesIntoSideAndAlignment(ZenPlacement placement, string side, string align)
    {
        // The two halves are sent to JavaScript as separate strings. A wrong mapping here places
        // panels on the wrong side of their anchor with nothing in the C# to point at.
        placement.Side().ShouldBe(side);
        placement.Align().ShouldBe(align);
    }

    [Fact]
    public void Placement_SidesStayLogical()
    {
        // Start and End must reach JavaScript unresolved. Resolving them in C# would need the
        // writing direction, which .NET cannot see - it can come from a dir attribute anywhere up
        // the tree or from a stylesheet, and only the browser knows which applies.
        ZenPlacement.Start.Side().ShouldBe("start");
        ZenPlacement.End.Side().ShouldBe("end");

        ZenPlacement.Start.Side().ShouldNotBe("left");
        ZenPlacement.End.Side().ShouldNotBe("right");
    }

    // ---- Rendering ----------------------------------------------------------------------------

    [Fact]
    public void ClosedPopover_RendersTheAnchorAndNoPanel()
    {
        var cut = Render<ZenPopover>(p => p
            .Add(x => x.Open, false)
            .Add(x => x.Anchor, (RenderFragment)(b => b.AddMarkupContent(0, "<button>Open</button>")))
            .Add(x => x.ChildContent, (RenderFragment)(b => b.AddMarkupContent(0, "<p>Panel body</p>"))));

        cut.Find("button").TextContent.ShouldBe("Open");
        cut.FindAll("[role='dialog']").ShouldBeEmpty();
        cut.Markup.ShouldNotContain("Panel body");
    }

    [Fact]
    public void OpenPopover_RendersThePanel()
    {
        var cut = Render<ZenPopover>(p => p
            .Add(x => x.Open, true)
            .Add(x => x.Anchor, (RenderFragment)(b => b.AddMarkupContent(0, "<button>Open</button>")))
            .Add(x => x.ChildContent, (RenderFragment)(b => b.AddMarkupContent(0, "<p>Panel body</p>"))));

        cut.Markup.ShouldContain("Panel body");
    }

    [Fact]
    public void Panel_IsAManualPopoverWhenInteractive()
    {
        // "manual" rather than "auto": auto brings its own light-dismiss and Escape handling, which
        // takes both decisions away from the owning component. A combobox needs Escape to clear its
        // active option before it closes, and light-dismiss fires after the click has already
        // landed on whatever was underneath.
        var cut = Render<ZenPopover>(p => p
            .Add(x => x.Open, true)
            .Add(x => x.ChildContent, (RenderFragment)(b => b.AddMarkupContent(0, "<p>Body</p>"))));

        cut.Find(".zen-popover").GetAttribute("popover").ShouldBe("manual");
    }

    [Fact]
    public void Panel_OmitsThePopoverAttributeWhenNotInteractive()
    {
        // A [popover] element is display:none until showPopover() runs. Emitting the attribute
        // during prerender or static SSR would render an open panel that nobody can ever see;
        // without it the panel is ordinary flow content - unpositioned, but readable and operable.
        Renderer.SetRendererInfo(new RendererInfo("Static", isInteractive: false));

        var cut = Render<ZenPopover>(p => p
            .Add(x => x.Open, true)
            .Add(x => x.ChildContent, (RenderFragment)(b => b.AddMarkupContent(0, "<p>Body</p>"))));

        cut.Find(".zen-popover").HasAttribute("popover").ShouldBeFalse();
        cut.Markup.ShouldContain("Body");
    }

    [Fact]
    public void Panel_CarriesItsRoleAndAccessibleName()
    {
        var cut = Render<ZenPopover>(p => p
            .Add(x => x.Open, true)
            .Add(x => x.Role, "listbox")
            .Add(x => x.Label, "Countries")
            .Add(x => x.ChildContent, (RenderFragment)(b => b.AddMarkupContent(0, "<p>Body</p>"))));

        var panel = cut.Find(".zen-popover");

        panel.GetAttribute("role").ShouldBe("listbox");
        panel.GetAttribute("aria-label").ShouldBe("Countries");
    }

    [Fact]
    public void AnchorWrapper_IsDisplayContents()
    {
        // The wrapper exists only to give the positioner an id to measure. `contents` keeps it out
        // of layout entirely, so an anchor inside a flex or grid parent is not re-parented into a
        // stray box that changes how it sizes.
        var cut = Render<ZenPopover>(p => p
            .Add(x => x.Anchor, (RenderFragment)(b => b.AddMarkupContent(0, "<button>Open</button>"))));

        cut.Find("div").ClassList.ShouldContain("contents");
    }

    [Fact]
    public void Class_LandsOnThePanelNotTheAnchor()
    {
        // A caller styling a ZenPopover means the floating surface. Applying it to a
        // `display: contents` wrapper would silently do nothing.
        var cut = Render<ZenPopover>(p => p
            .Add(x => x.Open, true)
            .Add(x => x.Class, "w-64")
            .Add(x => x.Anchor, (RenderFragment)(b => b.AddMarkupContent(0, "<button>Open</button>")))
            .Add(x => x.ChildContent, (RenderFragment)(b => b.AddMarkupContent(0, "<p>Body</p>"))));

        cut.Find(".zen-popover").ClassList.ShouldContain("w-64");
        cut.Find("div.contents").ClassList.ShouldNotContain("w-64");
    }

    // ---- Dismissal ----------------------------------------------------------------------------

    [Fact]
    public async Task OutsideClick_AsksTheOwnerToClose()
    {
        // The popover reports; it does not close itself. The owner holds the state, which is what
        // lets a combobox decide that the first Escape clears its filter and only the second
        // closes the panel.
        var closed = false;

        var cut = Render<ZenPopover>(p => p
            .Add(x => x.Open, true)
            .Add(x => x.OpenChanged, open => closed = !open)
            .Add(x => x.ChildContent, (RenderFragment)(b => b.AddMarkupContent(0, "<p>Body</p>"))));

        await cut.Instance.OnOutsideClick();

        closed.ShouldBeTrue();
    }

    [Fact]
    public void Escape_AsksTheOwnerToClose()
    {
        var closed = false;

        var cut = Render<ZenPopover>(p => p
            .Add(x => x.Open, true)
            .Add(x => x.OpenChanged, open => closed = !open)
            .Add(x => x.ChildContent, (RenderFragment)(b => b.AddMarkupContent(0, "<p>Body</p>"))));

        cut.Find(".zen-popover").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        closed.ShouldBeTrue();
    }

    [Fact]
    public void Escape_IsIgnoredWhenTheCallerOptedOut()
    {
        var closed = false;

        var cut = Render<ZenPopover>(p => p
            .Add(x => x.Open, true)
            .Add(x => x.CloseOnEscape, false)
            .Add(x => x.OpenChanged, open => closed = !open)
            .Add(x => x.ChildContent, (RenderFragment)(b => b.AddMarkupContent(0, "<p>Body</p>"))));

        cut.Find(".zen-popover").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        closed.ShouldBeFalse();
    }
}
