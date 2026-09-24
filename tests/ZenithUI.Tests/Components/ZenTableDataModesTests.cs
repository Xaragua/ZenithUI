namespace ZenithUI.Tests.Components;

/// <summary>
/// ZenTable's grouping, its <c>ItemsProvider</c> and virtualization - the three ways of feeding
/// it rows that are not "a list, all at once".
/// </summary>
public class ZenTableDataModesTests : BunitContext
{
    public ZenTableDataModesTests()
    {
        Renderer.SetRendererInfo(new RendererInfo("Server", isInteractive: true));
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    private sealed record Vendor(string Name, string Region, decimal Volume);

    private static readonly Vendor[] Vendors =
    [
        new("Cloudflare", "Global", 8200m),
        new("AWS", "US", 24500m),
        new("Datadog", "US", 4150m),
        new("Hetzner", "EU", 900m),
        new("OVH", "EU", 1200m),
    ];

    private static RenderFragment Columns(bool sortNames = false) => builder =>
    {
        builder.OpenComponent<ZenColumn<Vendor>>(0);
        builder.AddComponentParameter(1, nameof(ZenColumn<Vendor>.Title), "Vendor");
        builder.AddComponentParameter(2, nameof(ZenColumn<Vendor>.Field), (Func<Vendor, object?>)(v => v.Name));

        if (sortNames)
        {
            builder.AddComponentParameter(3, nameof(ZenColumn<Vendor>.SortName), "name");
        }

        builder.CloseComponent();

        builder.OpenComponent<ZenColumn<Vendor>>(4);
        builder.AddComponentParameter(5, nameof(ZenColumn<Vendor>.Title), "Volume");
        builder.AddComponentParameter(6, nameof(ZenColumn<Vendor>.Field), (Func<Vendor, object?>)(v => v.Volume));
        builder.CloseComponent();
    };

    private IRenderedComponent<ZenTable<Vendor>> Grouped(
        Action<ComponentParameterCollectionBuilder<ZenTable<Vendor>>>? extra = null) =>
        Render<ZenTable<Vendor>>(p =>
        {
            p.Add(x => x.Items, Vendors)
                .Add(x => x.Columns, Columns())
                .Add(x => x.GroupBy, v => v.Region)
                .Add(x => x.Label, "Vendors");

            extra?.Invoke(p);
        });

    private static List<string> GroupNames(IRenderedComponent<ZenTable<Vendor>> cut) =>
        cut.FindAll("tr.zen-table-group th")
            .Select(th => th.QuerySelector("span.font-medium")!.TextContent.Trim())
            .ToList();

    // ---- Grouping -----------------------------------------------------------------------------

    [Fact]
    public void GroupBy_OpensEachGroupWithAHeaderRow_InKeyOrder()
    {
        var cut = Grouped();

        GroupNames(cut).ShouldBe(["EU", "Global", "US"]);

        // Header, its rows, next header: the rows sit under the group they belong to.
        cut.FindAll("tbody tr")
            .Select(tr => tr.ClassList.Contains("zen-table-group") ? "#" : tr.QuerySelector("td")!.TextContent.Trim())
            .ShouldBe(["#", "Hetzner", "OVH", "#", "Cloudflare", "#", "AWS", "Datadog"]);
    }

    [Fact]
    public void GroupDescending_ReversesTheGroupOrder_ButNotTheRowsWithinThem() =>
        GroupNames(Grouped(p => p.Add(x => x.GroupDescending, true))).ShouldBe(["US", "Global", "EU"]);

    [Fact]
    public void GroupHeader_IsARowGroupHeaderSpanningTheTable()
    {
        // scope="rowgroup" is what lets a screen reader announce the group for a cell below it, the
        // way it announces the column header.
        var th = Grouped().Find("tr.zen-table-group th");

        th.GetAttribute("scope").ShouldBe("rowgroup");
        th.GetAttribute("colspan").ShouldBe("2");
    }

    [Fact]
    public void GroupHeader_StatesItsCount_InWordsForAScreenReader()
    {
        var header = Grouped().FindAll("tr.zen-table-group th")[1];

        header.TextContent.ShouldContain("1");
        header.QuerySelector(".zen-sr-only")!.TextContent.Trim().ShouldBe("row");
    }

    [Fact]
    public void CollapsingAGroup_RemovesItsRows_AndTheButtonSaysSo()
    {
        var cut = Grouped();

        cut.FindAll("tr.zen-table-group button")[0].Click();

        var toggle = cut.FindAll("tr.zen-table-group button")[0];
        toggle.GetAttribute("aria-expanded").ShouldBe("false");
        cut.Markup.ShouldNotContain("Hetzner");
        cut.Markup.ShouldContain("Cloudflare");
    }

    [Fact]
    public void IsGroupInitiallyCollapsed_StartsTheNamedGroupsClosed()
    {
        var cut = Grouped(p => p.Add(x => x.IsGroupInitiallyCollapsed, key => (string?)key == "US"));

        cut.Markup.ShouldNotContain("Datadog");
        cut.FindAll("tr.zen-table-group button")[2].GetAttribute("aria-expanded").ShouldBe("false");
    }

    [Fact]
    public void GroupsNotCollapsible_RenderNoToggle()
    {
        var cut = Grouped(p => p.Add(x => x.GroupsCollapsible, false));

        cut.FindAll("tr.zen-table-group button").ShouldBeEmpty();
    }

    [Fact]
    public void GroupCheckbox_SelectsEveryRowInTheGroup()
    {
        IReadOnlyCollection<Vendor>? selected = null;

        var cut = Grouped(p => p
            .Add(x => x.Selectable, true)
            .Add(x => x.SelectedItemsChanged, items => selected = items));

        cut.FindAll("tr.zen-table-group input[type=checkbox]")[2].Change(true);

        selected.ShouldNotBeNull();
        selected.Select(v => v.Name).ShouldBe(["AWS", "Datadog"], ignoreOrder: true);
    }

    [Fact]
    public void GroupCheckbox_IsNamedAfterItsGroup() =>
        Grouped(p => p.Add(x => x.Selectable, true))
            .FindAll("tr.zen-table-group input[type=checkbox]")[0]
            .GetAttribute("aria-label").ShouldBe("Select all rows in EU");

    [Fact]
    public void GroupRunningOverAPageBoundary_RepeatsItsHeaderOnTheNextPage()
    {
        // Page size 2: EU fills page one, Global and the first US row are page two, and page three
        // opens with the rest of US - which needs its header again, or the row is orphaned.
        var cut = Grouped(p => p.Add(x => x.PageSize, 2).Add(x => x.Page, 3));

        GroupNames(cut).ShouldBe(["US"]);
        cut.FindAll("tbody tr:not(.zen-table-group) td")[0].TextContent.Trim().ShouldBe("Datadog");
    }

    [Fact]
    public void Paging_CountsACollapsedGroupAsOneRow()
    {
        // EU collapsed: one entry for its header, then Cloudflare, AWS, Datadog - four in all.
        var cut = Grouped(p => p
            .Add(x => x.PageSize, 10)
            .Add(x => x.IsGroupInitiallyCollapsed, key => (string?)key == "EU"));

        cut.Find("nav[aria-label='Vendors pagination'] p").TextContent.ShouldBe("1–4 of 4");
    }

    [Fact]
    public void ThePager_IsNamedAfterItsTable()
    {
        // A <nav> is a landmark. Two paged tables on one page would otherwise be two landmarks
        // both called "Pagination" - the accessibility sweep caught exactly that on the demo.
        Grouped(p => p.Add(x => x.PageSize, 2)).Find("nav").GetAttribute("aria-label").ShouldBe("Vendors pagination");

        Render<ZenTable<Vendor>>(p => p.Add(x => x.Items, Vendors).Add(x => x.Columns, Columns()).Add(x => x.PageSize, 2))
            .Find("nav").GetAttribute("aria-label").ShouldBe("Pagination");
    }

    [Fact]
    public void NullKeys_FormTheirOwnGroup_NamedByUngroupedText()
    {
        var cut = Render<ZenTable<Vendor>>(p => p
            .Add(x => x.Items, [.. Vendors, new Vendor("Local", null!, 10m)])
            .Add(x => x.Columns, Columns())
            .Add(x => x.GroupBy, v => v.Region)
            .Add(x => x.UngroupedText, "No region"));

        // Nulls first, as in column sorting: the rows that need attention come first.
        GroupNames(cut)[0].ShouldBe("No region");
    }

    [Fact]
    public void GroupBy_WithChildrenProvider_Throws()
    {
        var act = () => Grouped(p => p.Add(x => x.ChildrenProvider, _ => null));

        act.ShouldThrow<InvalidOperationException>().Message.ShouldContain("GroupBy");
    }

    // ---- ItemsProvider ------------------------------------------------------------------------

    private sealed class FakeServer(IReadOnlyList<Vendor> all)
    {
        public List<ZenTableRequest> Requests { get; } = [];

        public ValueTask<ZenTableResult<Vendor>> Provide(ZenTableRequest request)
        {
            Requests.Add(request);

            IEnumerable<Vendor> rows = all;

            if (request.SortName == "name")
            {
                rows = request.SortDescending ? rows.OrderByDescending(v => v.Name) : rows.OrderBy(v => v.Name);
            }

            var window = rows.Skip(request.StartIndex);

            if (request.Count is { } count)
            {
                window = window.Take(count);
            }

            return ValueTask.FromResult(new ZenTableResult<Vendor>([.. window], all.Count));
        }
    }

    private IRenderedComponent<ZenTable<Vendor>> Provided(
        FakeServer server,
        Action<ComponentParameterCollectionBuilder<ZenTable<Vendor>>>? extra = null) =>
        Render<ZenTable<Vendor>>(p =>
        {
            p.Add(x => x.ItemsProvider, server.Provide)
                .Add(x => x.Columns, Columns(sortNames: true))
                .Add(x => x.Label, "Vendors");

            extra?.Invoke(p);
        });

    private static List<string> FirstColumn(IRenderedComponent<ZenTable<Vendor>> cut) =>
        cut.FindAll("tbody tr").Select(tr => tr.QuerySelector("td")!.TextContent.Trim()).ToList();

    [Fact]
    public void ItemsProvider_IsAskedForExactlyOnePage()
    {
        var server = new FakeServer(Vendors);

        var cut = Provided(server, p => p.Add(x => x.PageSize, 2));

        server.Requests.ShouldHaveSingleItem().ShouldSatisfyAllConditions(
            r => r.StartIndex.ShouldBe(0),
            r => r.Count.ShouldBe(2));

        FirstColumn(cut).ShouldBe(["Cloudflare", "AWS"]);
    }

    [Fact]
    public void ItemsProvider_TotalCount_DrivesThePager()
    {
        var cut = Provided(new FakeServer(Vendors), p => p.Add(x => x.PageSize, 2));

        cut.Find("nav[aria-label='Vendors pagination'] p").TextContent.ShouldBe("1–2 of 5");
        cut.FindAll("button[aria-label^='Page ']").Count.ShouldBe(3);
    }

    [Fact]
    public void ChangingPage_RequestsThatPage()
    {
        var server = new FakeServer(Vendors);
        var cut = Provided(server, p => p.Add(x => x.PageSize, 2));

        cut.Find("button[aria-label='Page 2']").Click();

        server.Requests[^1].StartIndex.ShouldBe(2);
        FirstColumn(cut).ShouldBe(["Datadog", "Hetzner"]);
    }

    [Fact]
    public void Sorting_SendsTheColumnsSortName_ToTheServer()
    {
        var server = new FakeServer(Vendors);
        var cut = Provided(server, p => p.Add(x => x.PageSize, 10));

        cut.Find("thead th button").Click();

        server.Requests[^1].SortName.ShouldBe("name");
        server.Requests[^1].SortDescending.ShouldBeFalse();
        FirstColumn(cut).ShouldBe(["AWS", "Cloudflare", "Datadog", "Hetzner", "OVH"]);
    }

    [Fact]
    public void WithAProvider_OnlyColumnsWithASortName_AreSortable()
    {
        // Volume has a Field, which would make it sortable over Items - but the server sorts here,
        // and it has no name for that column.
        var cut = Provided(new FakeServer(Vendors));

        cut.FindAll("thead th").Select(th => th.GetAttribute("aria-sort")).ShouldBe(["none", null]);
    }

    [Fact]
    public void AnUnchangedView_IsNotFetchedAgain_OnRerender()
    {
        var server = new FakeServer(Vendors);
        var cut = Provided(server, p => p.Add(x => x.PageSize, 2));

        cut.Render(p => p.Add(x => x.Caption, "Suppliers"));

        server.Requests.Count.ShouldBe(1);
    }

    [Fact]
    public async Task RefreshDataAsync_FetchesTheSameViewAgain()
    {
        var server = new FakeServer(Vendors);
        var cut = Provided(server, p => p.Add(x => x.PageSize, 2));

        await cut.InvokeAsync(() => cut.Instance.RefreshDataAsync());

        server.Requests.Count.ShouldBe(2);
        server.Requests[1].StartIndex.ShouldBe(0);
    }

    [Fact]
    public void AProviderStillLoading_ShowsSkeletonRows_AndIsBusy()
    {
        var pending = new TaskCompletionSource<ZenTableResult<Vendor>>();

        var cut = Render<ZenTable<Vendor>>(p => p
            .Add(x => x.ItemsProvider, _ => new ValueTask<ZenTableResult<Vendor>>(pending.Task))
            .Add(x => x.Columns, Columns())
            .Add(x => x.PageSize, 2));

        cut.Find("table").GetAttribute("aria-busy").ShouldBe("true");
        cut.FindAll("nav").ShouldBeEmpty();

        pending.SetResult(new ZenTableResult<Vendor>(Vendors[..2], 5));

        cut.WaitForAssertion(() => cut.Find("table").HasAttribute("aria-busy").ShouldBeFalse());
    }

    [Fact]
    public void ItemsAndItemsProvider_Together_Throw()
    {
        var act = () => Provided(new FakeServer(Vendors), p => p.Add(x => x.Items, Vendors));

        act.ShouldThrow<InvalidOperationException>().Message.ShouldContain("not both");
    }

    [Fact]
    public void ItemsProvider_WithGroupBy_Throws()
    {
        var act = () => Provided(new FakeServer(Vendors), p => p.Add(x => x.GroupBy, v => v.Region));

        act.ShouldThrow<InvalidOperationException>();
    }

    // ---- Virtualization -----------------------------------------------------------------------

    private static readonly Vendor[] Many =
        [.. Enumerable.Range(1, 500).Select(i => new Vendor($"V{i:000}", i % 2 == 0 ? "EU" : "US", i))];

    [Fact]
    public void Virtualize_WithPaging_Throws()
    {
        var act = () => Render<ZenTable<Vendor>>(p => p
            .Add(x => x.Items, Many)
            .Add(x => x.Columns, Columns())
            .Add(x => x.Virtualize, true)
            .Add(x => x.PageSize, 25));

        act.ShouldThrow<InvalidOperationException>().Message.ShouldContain(nameof(ZenTable<Vendor>.PageSize));
    }

    [Fact]
    public void Virtualized_StatesTheFullRowCount_AndEachRowsPosition()
    {
        // Only a window of rows exists in the DOM. Without these a screen reader counts the window
        // and announces "row 3 of 12" in a table of five hundred.
        var cut = Render<ZenTable<Vendor>>(p => p
            .Add(x => x.Items, Many)
            .Add(x => x.Columns, Columns())
            .Add(x => x.Virtualize, true)
            .Add(x => x.Height, "20rem"));

        cut.Find("table").GetAttribute("aria-rowcount").ShouldBe("501");
        cut.Find("thead tr").GetAttribute("aria-rowindex").ShouldBe("1");

        var first = cut.FindAll("tbody tr[aria-rowindex]")[0];
        first.GetAttribute("aria-rowindex").ShouldBe("2");
        first.QuerySelector("td")!.TextContent.Trim().ShouldBe("V001");
    }

    [Fact]
    public void Virtualized_DropsTheStackedLayout()
    {
        var cut = Render<ZenTable<Vendor>>(p => p
            .Add(x => x.Items, Many)
            .Add(x => x.Columns, Columns())
            .Add(x => x.Virtualize, true));

        cut.Find("table").ClassList.ShouldNotContain("zen-table-stack");
    }

    [Fact]
    public void Height_MakesTheWrapperTheScrollContainer_AndTheHeaderSticky()
    {
        var cut = Render<ZenTable<Vendor>>(p => p
            .Add(x => x.Items, Vendors)
            .Add(x => x.Columns, Columns())
            .Add(x => x.Height, "20rem"));

        var wrapper = cut.Find("table").ParentElement!;
        wrapper.GetAttribute("style")!.ShouldContain("max-height: 20rem");
        wrapper.ClassList.ShouldContain("overflow-y-auto");
        cut.Find("thead").ClassList.ShouldContain("sticky");
    }

    [Fact]
    public void Virtualized_UnderStaticRendering_RendersTheFirstRowsAsPlainMarkup()
    {
        // No JavaScript to measure a viewport with, so no <Virtualize>. The page still arrives with
        // data rather than an empty body.
        Renderer.SetRendererInfo(new RendererInfo("Static", isInteractive: false));

        var cut = Render<ZenTable<Vendor>>(p => p
            .Add(x => x.Items, Many)
            .Add(x => x.Columns, Columns())
            .Add(x => x.Virtualize, true));

        cut.FindAll("tbody tr").Count.ShouldBe(50);
        cut.Find("table").GetAttribute("aria-rowcount").ShouldBe("501");
    }

    [Fact]
    public void VirtualizedProvider_UnderStaticRendering_AsksForTheFirstWindowOnly()
    {
        Renderer.SetRendererInfo(new RendererInfo("Static", isInteractive: false));

        var server = new FakeServer(Many);

        var cut = Provided(server, p => p.Add(x => x.Virtualize, true));

        server.Requests.ShouldHaveSingleItem().Count.ShouldBe(50);
        cut.FindAll("tbody tr").Count.ShouldBe(50);
        cut.Find("table").GetAttribute("aria-rowcount").ShouldBe("501");
    }

    [Fact]
    public void VirtualizedAndGrouped_FlattenGroupHeadersIntoTheRowCount()
    {
        var cut = Render<ZenTable<Vendor>>(p => p
            .Add(x => x.Items, Many)
            .Add(x => x.Columns, Columns())
            .Add(x => x.GroupBy, v => v.Region)
            .Add(x => x.Virtualize, true));

        // 500 rows + 2 group headers + the column header row.
        cut.Find("table").GetAttribute("aria-rowcount").ShouldBe("503");
        cut.Find("tr.zen-table-group").GetAttribute("aria-rowindex").ShouldBe("2");
    }
}
