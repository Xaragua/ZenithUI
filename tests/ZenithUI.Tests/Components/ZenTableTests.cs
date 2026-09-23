namespace ZenithUI.Tests.Components;

/// <summary>
/// ZenTable. The first group is about column collection, which is the mechanism the whole
/// component rests on: if columns are not registered before the body renders, a table ships with
/// an empty <c>&lt;tbody&gt;</c> and every other assertion here is meaningless.
/// </summary>
public class ZenTableTests : BunitContext
{
    public ZenTableTests()
    {
        Renderer.SetRendererInfo(new RendererInfo("Server", isInteractive: true));

        // Only the hierarchical table loads a module, and only to move focus.
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    private sealed record Order(string Id, string Customer, decimal Amount, Order[] Lines)
    {
        public Order(string id, string customer, decimal amount) : this(id, customer, amount, []) { }
    }

    private static readonly Order[] Orders =
    [
        new("A-3", "Wren", 120.50m),
        new("A-1", "Alder", 40m),
        new("A-2", "Birch", 980.00m),
    ];

    private static RenderFragment TwoColumns => builder =>
    {
        builder.OpenComponent<ZenColumn<Order>>(0);
        builder.AddComponentParameter(1, nameof(ZenColumn<Order>.Title), "Order");
        builder.AddComponentParameter(2, nameof(ZenColumn<Order>.Field), (Func<Order, object?>)(o => o.Id));
        builder.CloseComponent();

        builder.OpenComponent<ZenColumn<Order>>(3);
        builder.AddComponentParameter(4, nameof(ZenColumn<Order>.Title), "Customer");
        builder.AddComponentParameter(5, nameof(ZenColumn<Order>.Field), (Func<Order, object?>)(o => o.Customer));
        builder.CloseComponent();
    };

    private IRenderedComponent<ZenTable<Order>> Table(
        Action<ComponentParameterCollectionBuilder<ZenTable<Order>>>? extra = null) =>
        Render<ZenTable<Order>>(p =>
        {
            p.Add(x => x.Items, Orders)
                .Add(x => x.Columns, TwoColumns)
                .Add(x => x.Label, "Orders");

            extra?.Invoke(p);
        });

    // ---- Column collection --------------------------------------------------------------------

    [Fact]
    public void Columns_AreRegisteredBeforeTheBodyRenders()
    {
        // The whole ZenDefer mechanism in one assertion. A parent builds its entire render tree
        // before any child component exists, so without the deferral the first pass would emit a
        // header and body with no columns in them.
        var cut = Table();

        cut.FindAll("thead th").Select(th => th.TextContent.Trim()).ShouldBe(["Order", "Customer"]);
        cut.FindAll("tbody tr").Count.ShouldBe(3);
        cut.FindAll("tbody tr")[0].QuerySelectorAll("td").Length.ShouldBe(2);
    }

    [Fact]
    public void Columns_KeepDocumentOrderWhenOneIsAddedLater()
    {
        // The registration list is rebuilt per render pass rather than appended to for the life of
        // the component, so a column revealed by an @if lands where it was written rather than at
        // the end.
        var showMiddle = false;

        RenderFragment columns = builder =>
        {
            builder.OpenComponent<ZenColumn<Order>>(0);
            builder.AddComponentParameter(1, nameof(ZenColumn<Order>.Title), "Order");
            builder.AddComponentParameter(2, nameof(ZenColumn<Order>.Field), (Func<Order, object?>)(o => o.Id));
            builder.CloseComponent();

            if (showMiddle)
            {
                builder.OpenComponent<ZenColumn<Order>>(3);
                builder.AddComponentParameter(4, nameof(ZenColumn<Order>.Title), "Customer");
                builder.AddComponentParameter(5, nameof(ZenColumn<Order>.Field), (Func<Order, object?>)(o => o.Customer));
                builder.CloseComponent();
            }

            builder.OpenComponent<ZenColumn<Order>>(6);
            builder.AddComponentParameter(7, nameof(ZenColumn<Order>.Title), "Amount");
            builder.AddComponentParameter(8, nameof(ZenColumn<Order>.Field), (Func<Order, object?>)(o => o.Amount));
            builder.CloseComponent();
        };

        var cut = Render<ZenTable<Order>>(p => p
            .Add(x => x.Items, Orders)
            .Add(x => x.Columns, columns));

        cut.FindAll("thead th").Select(th => th.TextContent.Trim()).ShouldBe(["Order", "Amount"]);

        showMiddle = true;
        cut.Render();

        cut.FindAll("thead th").Select(th => th.TextContent.Trim()).ShouldBe(["Order", "Customer", "Amount"]);
    }

    [Fact]
    public void Columns_AreCollectedUnderStaticSsrToo()
    {
        // The reason the deferral exists rather than a StateHasChanged from OnAfterRender: static
        // SSR never renders a second time, so a two-pass table would ship an empty body.
        Renderer.SetRendererInfo(new RendererInfo("Static", isInteractive: false));

        Table().FindAll("tbody tr").Count.ShouldBe(3);
    }

    // ---- Semantics --------------------------------------------------------------------------

    [Fact]
    public void AFlatTable_StaysAPlainTable()
    {
        // No role="grid", even with selection. A grid owes the user arrow-key cell navigation and
        // this component does not provide it; the checkboxes make selection accessible without
        // claiming anything.
        var cut = Table(p => p.Add(x => x.Selectable, true));

        cut.Find("table").HasAttribute("role").ShouldBeFalse();
        cut.FindAll("tbody input[type='checkbox']").Count.ShouldBe(3);
    }

    [Fact]
    public void AHierarchicalTable_IsATreegridAndStatesItsLevels()
    {
        // HTML cannot say "this row is a child of that one", so unlike ZenTree - which gets level
        // and position from its nested lists - a table has to state them.
        var cut = HierarchyTable();

        cut.Find("table").GetAttribute("role").ShouldBe("treegrid");

        cut.FindAll("tbody tr").Select(tr => tr.GetAttribute("aria-level"))
            .ShouldBe(["1", "2", "2", "1"]);
    }

    [Fact]
    public void OnlyRowsWithChildren_ReportAnExpandedState()
    {
        var rows = HierarchyTable().FindAll("tbody tr");

        rows[0].GetAttribute("aria-expanded").ShouldBe("true");
        rows[1].HasAttribute("aria-expanded").ShouldBeFalse();
    }

    // ---- Sorting ------------------------------------------------------------------------------

    [Fact]
    public void EverySortableColumn_SaysSo_AndOnlyOneSaysHow()
    {
        // A sortable column that states nothing gives no hint it can be sorted; the user has to
        // discover it by clicking. "none" is the hint.
        var cut = Table();

        cut.FindAll("th").Select(th => th.GetAttribute("aria-sort")).ShouldBe(["none", "none"]);

        cut.FindAll("th button")[0].Click();

        cut.FindAll("th").Select(th => th.GetAttribute("aria-sort")).ShouldBe(["ascending", "none"]);
    }

    [Fact]
    public void Sorting_CyclesThroughAscendingDescendingAndBackToTheOriginalOrder()
    {
        // The third state is the point. The order the data arrived in carries information - very
        // often "newest first" from the server - and a two-state toggle gives no way back to it.
        var cut = Table();

        static string[] Ids(IRenderedComponent<ZenTable<Order>> c) =>
            c.FindAll("tbody tr td:first-child").Select(td => td.TextContent.Trim()).ToArray();

        Ids(cut).ShouldBe(["A-3", "A-1", "A-2"]);

        cut.FindAll("th button")[0].Click();
        Ids(cut).ShouldBe(["A-1", "A-2", "A-3"]);

        cut.FindAll("th button")[0].Click();
        Ids(cut).ShouldBe(["A-3", "A-2", "A-1"]);

        cut.FindAll("th button")[0].Click();
        Ids(cut).ShouldBe(["A-3", "A-1", "A-2"]);
    }

    [Fact]
    public void ANonSortableColumn_MakesNoClaimAtAll()
    {
        RenderFragment columns = builder =>
        {
            builder.OpenComponent<ZenColumn<Order>>(0);
            builder.AddComponentParameter(1, nameof(ZenColumn<Order>.Title), "Actions");
            builder.CloseComponent();
        };

        var cut = Render<ZenTable<Order>>(p => p
            .Add(x => x.Items, Orders)
            .Add(x => x.Columns, columns));

        cut.Find("th").HasAttribute("aria-sort").ShouldBeFalse();
        cut.FindAll("th button").ShouldBeEmpty();
    }

    // ---- Paging -------------------------------------------------------------------------------

    [Fact]
    public void Paging_CountsTopLevelRowsAndCarriesSubtreesWithTheirParent()
    {
        // Paging after flattening would let one expanded row push its siblings onto the next page,
        // so opening a row would look like it had deleted the ones below it.
        var cut = HierarchyTable(p => p.Add(x => x.PageSize, 1));

        // One root row, plus the two children it has open.
        cut.FindAll("tbody tr").Count.ShouldBe(3);
        cut.Find("nav[aria-label='Pagination']").ShouldNotBeNull();
    }

    [Fact]
    public void ThePager_IsAnnouncedWhenItMoves()
    {
        // Nothing takes focus when the page changes, so without a live region a screen reader user
        // gets no confirmation that Next did anything.
        var cut = Table(p => p.Add(x => x.PageSize, 2));

        cut.Find("nav p").GetAttribute("aria-live").ShouldBe("polite");
        cut.Find("nav p").TextContent.ShouldBe("1–2 of 3");
    }

    [Fact]
    public void NoPager_WhenEverythingFitsOnOnePage()
    {
        Table(p => p.Add(x => x.PageSize, 10)).FindAll("nav").ShouldBeEmpty();
    }

    // ---- Selection ----------------------------------------------------------------------------

    [Fact]
    public void SelectAll_GovernsThePage_NotTheDataset()
    {
        // A box that silently selects rows the user has never seen is how a bulk action takes out
        // more than it was meant to.
        IReadOnlyCollection<Order> selected = [];

        var cut = Table(p => p
            .Add(x => x.Selectable, true)
            .Add(x => x.PageSize, 2)
            .Add(x => x.SelectedItems, selected)
            .Add(x => x.SelectedItemsChanged, s => selected = s));

        cut.Find("thead input[type='checkbox']").Change(true);

        selected.Count.ShouldBe(2);
    }

    [Fact]
    public void ASelectionCheckbox_IsNamedAfterItsRow()
    {
        // Without a name every checkbox announces identically, and a screen reader user has
        // nothing to tell three "Select row" controls apart.
        var cut = Table(p => p
            .Add(x => x.Selectable, true)
            .Add(x => x.RowText, o => o.Customer));

        cut.FindAll("tbody input[type='checkbox']")[0]
            .GetAttribute("aria-label").ShouldBe("Select Wren");
    }

    // ---- States -------------------------------------------------------------------------------

    [Fact]
    public void Loading_KeepsTheTablesShape()
    {
        // Skeleton rows rather than a spinner: the table does not collapse to nothing and then
        // push the rest of the page back down when the data lands.
        var cut = Table(p => p
            .Add(x => x.Loading, true)
            .Add(x => x.LoadingRowCount, 4));

        cut.Find("table").GetAttribute("aria-busy").ShouldBe("true");
        cut.FindAll("tbody tr").Count.ShouldBe(4);
        cut.FindAll("tbody tr")[0].QuerySelectorAll("td").Length.ShouldBe(2);
    }

    [Fact]
    public void Empty_SaysSoAcrossTheWholeTable()
    {
        var cut = Render<ZenTable<Order>>(p => p
            .Add(x => x.Items, Array.Empty<Order>())
            .Add(x => x.Columns, TwoColumns));

        cut.Find("tbody td").GetAttribute("colspan").ShouldBe("2");
        cut.Find("tbody td").TextContent.Trim().ShouldBe("No rows to show.");
    }

    // ---- Responsive ---------------------------------------------------------------------------

    [Fact]
    public void EveryCell_CarriesItsColumnTitle_ForTheStackedLayout()
    {
        // The stacked layout below `md` prints data-label in front of each value, because the
        // header row is not on screen there. An attribute rather than a duplicated element, so a
        // screen reader - which still has the real header association - does not hear it twice.
        var cut = Table();

        cut.Find("table").ClassList.ShouldContain("zen-table-stack");

        cut.FindAll("tbody tr")[0].QuerySelectorAll("td")
            .Select(td => td.GetAttribute("data-label"))
            .ShouldBe(["Order", "Customer"]);
    }

    // ---- Helpers ------------------------------------------------------------------------------

    private static readonly Order[] Nested =
    [
        new("A-1", "Alder", 60m, [new Order("A-1a", "Alder", 20m), new Order("A-1b", "Alder", 40m)]),
        new("A-2", "Birch", 10m),
    ];

    private IRenderedComponent<ZenTable<Order>> HierarchyTable(
        Action<ComponentParameterCollectionBuilder<ZenTable<Order>>>? extra = null) =>
        Render<ZenTable<Order>>(p =>
        {
            p.Add(x => x.Items, Nested)
                .Add(x => x.Columns, TwoColumns)
                .Add(x => x.ChildrenProvider, o => o.Lines.Length == 0 ? null : o.Lines)
                .Add(x => x.IsInitiallyExpanded, o => o.Id == "A-1");

            extra?.Invoke(p);
        });
}
