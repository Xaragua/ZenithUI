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
    public void OnOnePage_TheSizeSelectorStays_ButThePageButtonsGo()
    {
        // The footer is not hidden just because there is one page: the page-size control is the
        // only way to discover that 10 rows fit where 50 did not, and gating it on "more than one
        // page already" is a chicken-and-egg. Buttons to nowhere are a different matter.
        var cut = Table(p => p.Add(x => x.PageSize, 10));

        cut.FindAll("nav").Count.ShouldBe(1);
        cut.FindAll("nav select").Count.ShouldBe(1);
        cut.FindAll("nav [aria-label^='Page ']").ShouldBeEmpty();
    }

    [Fact]
    public void NoPagerAtAll_WhenPagingIsOff()
    {
        Table(p => p.Add(x => x.PageSize, 0)).FindAll("nav").ShouldBeEmpty();
    }

    // ---- Numbered pages -----------------------------------------------------------------------

    [Fact]
    public void ThePages_AreNumbered_AndTheCurrentOneSaysSo()
    {
        // aria-current is what tells a screen reader which of eight identical-looking buttons is
        // the page they are on. Colour says it to everyone else.
        var cut = Table(p => p.Add(x => x.PageSize, 1));

        cut.FindAll("nav [aria-label^='Page ']").Count.ShouldBe(3);

        cut.FindAll("nav [aria-current='page']").Count.ShouldBe(1);
        cut.Find("nav [aria-current='page']").TextContent.Trim().ShouldBe("1");

        cut.Find("nav [aria-label='Page 3']").Click();

        cut.Find("nav [aria-current='page']").TextContent.Trim().ShouldBe("3");
        cut.Find("tbody tr td:first-child").TextContent.Trim().ShouldBe("A-2");
    }

    [Fact]
    public void ALongPageRange_IsElided_ButNeverToHideASinglePage()
    {
        // An ellipsis standing in for one page is strictly worse than the page: same width, and it
        // turns a one-click jump into a guess.
        var many = Enumerable.Range(1, 40).Select(i => new Order($"A-{i}", "Customer", i)).ToArray();

        var cut = Render<ZenTable<Order>>(p => p
            .Add(x => x.Items, many)
            .Add(x => x.Columns, TwoColumns)
            .Add(x => x.PageSize, 1));

        static string[] Links(IRenderedComponent<ZenTable<Order>> c) =>
            c.Find("nav div:last-child").Children
                .Where(e => e.TextContent.Trim().Length > 0 && !e.HasAttribute("aria-label")
                    || e.GetAttribute("aria-label")?.StartsWith("Page ", StringComparison.Ordinal) == true)
                .Select(e => e.TextContent.Trim())
                .ToArray();

        // Page 1 of 40: 1, 2, gap, 40.
        Links(cut).ShouldBe(["1", "2", "…", "40"]);

        // Page 3 of 40: the gap between 1 and 2 would be a single page, so 2 is drawn instead.
        cut.Find("nav [aria-label='Page 2']").Click();
        cut.Find("nav [aria-label='Page 3']").Click();
        Links(cut).ShouldBe(["1", "2", "3", "4", "…", "40"]);
    }

    // ---- Page size ----------------------------------------------------------------------------

    [Fact]
    public void ChangingThePageSize_KeepsTheFirstVisibleRowInView()
    {
        // Jumping back to page 1 is the usual implementation and it is quietly hostile: someone at
        // row 5 who asks for more rows per page wants to see more of where they are, not to be
        // sent back to the start.
        var many = Enumerable.Range(1, 40).Select(i => new Order($"A-{i}", "Customer", i)).ToArray();

        var cut = Render<ZenTable<Order>>(p => p
            .Add(x => x.Items, many)
            .Add(x => x.Columns, TwoColumns)
            .Add(x => x.PageSize, 10));

        // Page 3 at 10 per page starts at row 21.
        cut.Find("nav [aria-label='Page 3']").Click();
        cut.Find("tbody tr td:first-child").TextContent.Trim().ShouldBe("A-21");

        cut.Find("nav select").Change("25");

        // Row 21 lives on page 1 at 25 per page, and that page is where we land.
        cut.Find("nav [aria-current='page']").TextContent.Trim().ShouldBe("1");
        cut.FindAll("tbody tr").Count.ShouldBe(25);
    }

    [Fact]
    public void ThePageSizeInForce_IsAlwaysOneOfTheOptions()
    {
        // A select whose value matches no option shows the first one instead, so the control would
        // claim a page size the table is not using.
        var cut = Table(p => p.Add(x => x.PageSize, 15));

        cut.FindAll("nav option").Select(o => o.TextContent.Trim())
            .ShouldBe(["10", "15", "25", "50", "100"]);

        cut.Find("nav option[selected]").TextContent.Trim().ShouldBe("15");
    }

    [Fact]
    public void TheSizeSelector_IsNotAFormControl()
    {
        // Deliberately a bare <select> rather than ZenSelect. ZenSelect derives from ZenInputBase
        // and registers a field in any cascading EditContext, so a table inside an EditForm would
        // have its page size join that form's validation and dirty tracking.
        var cut = Table(p => p.Add(x => x.PageSize, 2));

        cut.Find("nav select").HasAttribute("aria-invalid").ShouldBeFalse();
        cut.FindAll("nav [role='alert']").ShouldBeEmpty();
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
        cut.Find("tbody td").TextContent.ShouldContain("No data");
    }

    [Fact]
    public void TheEmptyState_CanExplainItselfAndOfferAWayOut()
    {
        // "No data" alone leaves someone who has just typed a filter unable to tell "nothing
        // matched" from "nothing exists", and the two call for opposite actions.
        var cut = Render<ZenTable<Order>>(p => p
            .Add(x => x.Items, Array.Empty<Order>())
            .Add(x => x.Columns, TwoColumns)
            .Add(x => x.EmptyText, "No orders match")
            .Add(x => x.EmptyDescription, "Try widening the date range.")
            .Add(x => x.EmptyActions, (RenderFragment)(b =>
            {
                b.OpenComponent<ZenButton>(0);
                b.AddComponentParameter(1, nameof(ZenButton.ChildContent),
                    (RenderFragment)(c => c.AddContent(0, "Clear filters")));
                b.CloseComponent();
            })));

        cut.Markup.ShouldContain("No orders match");
        cut.Markup.ShouldContain("Try widening the date range.");
        cut.Find("tbody button").TextContent.Trim().ShouldBe("Clear filters");
    }

    [Fact]
    public void TheEmptyStatesIllustration_IsDecorative()
    {
        // It carries nothing the text does not. A screen reader announcing "image, inbox" before
        // the sentence that actually explains the state is noise.
        var cut = Render<ZenTable<Order>>(p => p
            .Add(x => x.Items, Array.Empty<Order>())
            .Add(x => x.Columns, TwoColumns));

        cut.Find("tbody td span[aria-hidden='true']").ShouldNotBeNull();
    }

    // ---- Detail rows --------------------------------------------------------------------------

    [Fact]
    public void WithoutADetailTemplate_ThereIsNoDetailColumn()
    {
        Table().FindAll("thead th").Count.ShouldBe(2);
    }

    [Fact]
    public void ADetailRow_OpensBeneathItsRowAndSpansEveryColumn()
    {
        // A sibling <tr>, not content nested in a <td>: a detail panel constrained to one column's
        // width is the one thing a panel meant to hold a whole table must not be.
        var cut = Table(p => p.Add(x => x.RowDetailTemplate,
            (RenderFragment<Order>)(o => b => b.AddMarkupContent(0, $"<p>Detail for {o.Id}</p>"))));

        cut.FindAll("thead th").Count.ShouldBe(3);
        cut.Markup.ShouldNotContain("Detail for");

        var toggle = cut.FindAll("tbody button")[0];
        toggle.GetAttribute("aria-expanded").ShouldBe("false");
        toggle.Click();

        cut.Markup.ShouldContain("Detail for A-3");
        cut.Find("td.zen-table-detail").GetAttribute("colspan").ShouldBe("3");
        cut.FindAll("tbody button")[0].GetAttribute("aria-expanded").ShouldBe("true");
    }

    [Fact]
    public void ADetailToggle_PointsAtWhatItOpens_OnlyWhileItExists()
    {
        // aria-controls naming an element that is not in the document is worse than saying
        // nothing: a screen reader asked to follow it finds nothing at all.
        var cut = Table(p => p.Add(x => x.RowDetailTemplate,
            (RenderFragment<Order>)(_ => b => b.AddMarkupContent(0, "<p>Detail</p>"))));

        cut.FindAll("tbody button")[0].HasAttribute("aria-controls").ShouldBeFalse();

        cut.FindAll("tbody button")[0].Click();

        var controls = cut.FindAll("tbody button")[0].GetAttribute("aria-controls");
        controls.ShouldNotBeNullOrEmpty();
        cut.Find($"#{controls}").ShouldNotBeNull();
    }

    [Fact]
    public void ARowWithNoDetail_GetsNoToggle()
    {
        // A button that opens an empty panel is more annoying than no button, because it costs a
        // click to find out.
        var cut = Table(p => p
            .Add(x => x.RowDetailTemplate, (RenderFragment<Order>)(_ => b => b.AddMarkupContent(0, "<p>Detail</p>")))
            .Add(x => x.HasRowDetail, o => o.Id == "A-1"));

        cut.FindAll("tbody button").Count.ShouldBe(1);
    }

    [Fact]
    public void ADetailRow_CanHostAnotherTable()
    {
        // The case the feature exists for. The inner table collects its own columns through its
        // own cascade; the two must not see each other's.
        var cut = Table(p => p.Add(x => x.RowDetailTemplate, (RenderFragment<Order>)(o => b =>
        {
            b.OpenComponent<ZenTable<Order>>(0);
            b.AddComponentParameter(1, nameof(ZenTable<Order>.Items), (IEnumerable<Order>)Orders);
            b.AddComponentParameter(2, nameof(ZenTable<Order>.Columns), TwoColumns);
            b.AddComponentParameter(3, nameof(ZenTable<Order>.Label), $"Lines for {o.Id}");
            b.CloseComponent();
        })));

        cut.FindAll("tbody button")[0].Click();

        var inner = cut.Find("td.zen-table-detail table");

        inner.GetAttribute("aria-label").ShouldBe("Lines for A-3");
        inner.QuerySelectorAll("thead th").Length.ShouldBe(2);

        // Counted through the inner tbody's own children rather than with a "tbody tr" selector.
        // A descendant combinator is not scoped to the element it is queried from, so inside a
        // nested table "tbody tr" also matches the inner table's HEADER row - which is a
        // descendant of the OUTER tbody. The count comes out one too high and the table looks
        // like it grew a row.
        inner.QuerySelector("tbody")!.Children.Length.ShouldBe(3);
    }

    [Fact]
    public void DetailsAndHierarchy_CoexistWithoutFightingOverTheChevron()
    {
        // The hierarchy chevron lives inline in the first data cell; the detail disclosure has a
        // column of its own. One expander doing both jobs could not express "expanded children,
        // closed detail".
        var cut = HierarchyTable(p => p.Add(x => x.RowDetailTemplate,
            (RenderFragment<Order>)(_ => b => b.AddMarkupContent(0, "<p>Detail</p>"))));

        cut.Find("table").GetAttribute("role").ShouldBe("treegrid");
        cut.FindAll("thead th").Count.ShouldBe(3);

        var firstRow = cut.FindAll("tbody tr")[0];

        firstRow.GetAttribute("aria-expanded").ShouldBe("true");
        firstRow.QuerySelector("td button")!.GetAttribute("aria-expanded").ShouldBe("false");
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

    // ---- Interactive cells --------------------------------------------------------------------

    private static RenderFragment ActionColumn(bool interactive, Action<Order> onDelete) => builder =>
    {
        builder.OpenComponent<ZenColumn<Order>>(0);
        builder.AddComponentParameter(1, nameof(ZenColumn<Order>.Title), "Actions");
        builder.AddComponentParameter(2, nameof(ZenColumn<Order>.Interactive), interactive);
        builder.AddComponentParameter(3, nameof(ZenColumn<Order>.CellTemplate), (RenderFragment<Order>)(order => b =>
        {
            b.OpenElement(0, "button");
            b.AddAttribute(1, "type", "button");
            b.AddAttribute(2, "class", "delete");
            b.AddAttribute(3, "onclick", EventCallback.Factory.Create<MouseEventArgs>(new object(), () => onDelete(order)));
            b.AddContent(4, "Delete");
            b.CloseElement();
        }));
        builder.CloseComponent();
    };

    [Fact]
    public void AnInteractiveCell_KeepsItsClicksFromTheRow()
    {
        // A Delete button that also opens the row it deletes. The button's handler runs, and the
        // click must stop at the cell rather than bubbling on to OnRowClick.
        var opened = new List<string>();
        var deleted = new List<string>();

        var cut = Table(p => p
            .Add(x => x.Columns, ActionColumn(interactive: true, o => deleted.Add(o.Id)))
            .Add(x => x.OnRowClick, (Order o) => opened.Add(o.Id)));

        cut.FindAll("button.delete")[0].Click();

        deleted.ShouldBe(["A-3"]);
        opened.ShouldBeEmpty();
    }

    [Fact]
    public void AnOrdinaryCell_StillOpensItsRow()
    {
        // The guard is opt-in: a plain column's click is exactly what OnRowClick is listening for.
        var opened = new List<string>();

        var cut = Table(p => p
            .Add(x => x.Columns, ActionColumn(interactive: false, _ => { }))
            .Add(x => x.OnRowClick, (Order o) => opened.Add(o.Id)));

        cut.FindAll("button.delete")[0].Click();

        opened.ShouldBe(["A-3"]);
    }
}
