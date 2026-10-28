namespace ZenithUI.Tests.Components;

/// <summary>
/// ZenBreadcrumb: the WAI-ARIA breadcrumb pattern, the current page decided by position, and a
/// collapse that only happens where something can expand it again.
/// </summary>
public class ZenBreadcrumbTests : BunitContext
{
    public ZenBreadcrumbTests()
    {
        Renderer.SetRendererInfo(new RendererInfo("Server", isInteractive: true));
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    private sealed record Crumb(string Text, string? Href = null, bool? Current = null);

    private static readonly Crumb[] Trail =
    [
        new("Home", "/"),
        new("Projects", "/projects"),
        new("Zenith", "/projects/zenith"),
        new("Settings", "/projects/zenith/settings"),
        new("Members", "/projects/zenith/settings/members"),
    ];

    private IRenderedComponent<ZenBreadcrumb> Breadcrumb(
        Crumb[] crumbs,
        Action<ComponentParameterCollectionBuilder<ZenBreadcrumb>>? extra = null) =>
        Render<ZenBreadcrumb>(p =>
        {
            p.Add(x => x.ChildContent, (RenderFragment)(builder =>
            {
                var seq = 0;

                foreach (var crumb in crumbs)
                {
                    builder.OpenComponent<ZenBreadcrumbItem>(seq++);
                    builder.AddComponentParameter(seq++, nameof(ZenBreadcrumbItem.Text), crumb.Text);
                    builder.AddComponentParameter(seq++, nameof(ZenBreadcrumbItem.Href), crumb.Href);
                    builder.AddComponentParameter(seq++, nameof(ZenBreadcrumbItem.Current), crumb.Current);
                    builder.CloseComponent();
                }
            }));

            extra?.Invoke(p);
        });

    private static List<string> Levels(IRenderedComponent<ZenBreadcrumb> cut) =>
        cut.FindAll("li").Select(li => li.TextContent.Trim()).ToList();

    [Fact]
    public void IsANamedNavLandmark_WithAnOrderedList()
    {
        var cut = Breadcrumb(Trail);

        cut.Find("nav").GetAttribute("aria-label").ShouldBe("Breadcrumb");
        cut.Find("nav > ol").Children.Length.ShouldBe(5);
    }

    [Fact]
    public void TheLastItem_IsTheCurrentPage()
    {
        var cut = Breadcrumb(Trail);

        var current = cut.FindAll("[aria-current]");
        current.Count.ShouldBe(1);
        current[0].GetAttribute("aria-current").ShouldBe("page");
        current[0].TextContent.Trim().ShouldBe("Members");
    }

    [Fact]
    public void AnItemMarkedCurrent_WinsOverPosition()
    {
        var cut = Breadcrumb([new("Home", "/"), new("Reports", "/reports", Current: true), new("Q3", "/reports/q3")]);

        cut.Find("[aria-current=page]").TextContent.Trim().ShouldBe("Reports");
    }

    [Fact]
    public void Separators_AreHidden()
    {
        var cut = Breadcrumb(Trail);

        // One between each pair of levels, never before the first.
        var separators = cut.FindAll("li > span[aria-hidden=true]");
        separators.Count.ShouldBe(4);
        cut.FindAll("li")[0].QuerySelector("span[aria-hidden=true]").ShouldBeNull();
    }

    [Fact]
    public void AnItemWithoutHref_IsText()
    {
        var cut = Breadcrumb([new("Home", "/"), new("Archive")]);

        cut.FindAll("a").Count.ShouldBe(1);
        cut.Find("li:last-child > span[aria-current=page]").TextContent.Trim().ShouldBe("Archive");
    }

    [Fact]
    public void MaxItems_CollapsesTheMiddle()
    {
        var cut = Breadcrumb(Trail, p => p.Add(x => x.MaxItems, 3));

        Levels(cut).ShouldBe(["Home", "…", "Settings", "Members"]);
        cut.Find("button").GetAttribute("aria-label").ShouldBe("Show all levels");
    }

    [Fact]
    public void TheEllipsis_ExpandsTheTrail()
    {
        var cut = Breadcrumb(Trail, p => p.Add(x => x.MaxItems, 3));

        cut.Find("button").Click();

        Levels(cut).ShouldBe(["Home", "Projects", "Zenith", "Settings", "Members"]);
        cut.FindAll("button").ShouldBeEmpty();
    }

    [Fact]
    public void UnderStaticRendering_ItNeverCollapses()
    {
        // Nothing would answer the ellipsis button.
        Renderer.SetRendererInfo(new RendererInfo("Static", isInteractive: false));

        var cut = Breadcrumb(Trail, p => p.Add(x => x.MaxItems, 3));

        Levels(cut).Count.ShouldBe(5);
        cut.FindAll("button").ShouldBeEmpty();
    }

    [Fact]
    public void AShortTrail_IsNotCollapsed() =>
        Levels(Breadcrumb(Trail[..3], p => p.Add(x => x.MaxItems, 3))).Count.ShouldBe(3);

    [Fact]
    public void LabelOverride() =>
        Breadcrumb(Trail, p => p.Add(x => x.Label, "You are here")).Find("nav").GetAttribute("aria-label").ShouldBe("You are here");
}
