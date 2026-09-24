namespace ZenithUI.Tests.Components;

/// <summary>
/// ZenLookup: a search field whose popup is a table. Most of these pin the combobox-with-grid
/// contract - focus stays in the field, the grid is named and pointed at, and closing without a
/// pick never changes the value.
/// </summary>
public class ZenLookupTests : BunitContext
{
    public ZenLookupTests()
    {
        Renderer.SetRendererInfo(new RendererInfo("Server", isInteractive: true));
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    private sealed record Vendor(string Name, string Account, string Region);

    private static readonly Vendor[] Vendors =
    [
        new("AWS", "ACT-9012", "US-East"),
        new("Cloudflare", "ACT-7104", "Global"),
        new("Google Cloud", "ACT-3419", "US-Central"),
        new("Datadog", "ACT-5502", "US-West"),
    ];

    private static RenderFragment Columns => builder =>
    {
        builder.OpenComponent<ZenColumn<Vendor>>(0);
        builder.AddComponentParameter(1, nameof(ZenColumn<Vendor>.Title), "Vendor");
        builder.AddComponentParameter(2, nameof(ZenColumn<Vendor>.Field), (Func<Vendor, object?>)(v => v.Name));
        builder.CloseComponent();

        builder.OpenComponent<ZenColumn<Vendor>>(3);
        builder.AddComponentParameter(4, nameof(ZenColumn<Vendor>.Title), "Account");
        builder.AddComponentParameter(5, nameof(ZenColumn<Vendor>.Field), (Func<Vendor, object?>)(v => v.Account));
        builder.CloseComponent();
    };

    private Vendor? _value;

    private IRenderedComponent<ZenLookup<Vendor>> Lookup(
        Action<ComponentParameterCollectionBuilder<ZenLookup<Vendor>>>? extra = null) =>
        Render<ZenLookup<Vendor>>(p =>
        {
            p.Add(x => x.Items, Vendors)
                .Add(x => x.Columns, Columns)
                .Add(x => x.ItemText, v => v.Name)
                .Add(x => x.Label, "Vendor")
                .Add(x => x.Value, _value)
                .Add(x => x.ValueChanged, v => _value = v);

            extra?.Invoke(p);
        });

    private static void Open(IRenderedComponent<ZenLookup<Vendor>> cut) =>
        cut.Find("input[role=combobox]").KeyDown(new KeyboardEventArgs { Key = "ArrowDown" });

    private static List<string> RowNames(IRenderedComponent<ZenLookup<Vendor>> cut) =>
        cut.FindAll("[role=grid] tbody tr").Select(tr => tr.QuerySelector("td")!.TextContent.Trim()).ToList();

    // ---- The field ----------------------------------------------------------------------------

    [Fact]
    public void Field_IsAComboboxThatOpensAGrid()
    {
        var input = Lookup().Find("input");

        input.GetAttribute("role").ShouldBe("combobox");
        input.GetAttribute("aria-haspopup").ShouldBe("grid");
        input.GetAttribute("aria-expanded").ShouldBe("false");
    }

    [Fact]
    public void AriaControls_NamesTheGrid_NotThePanelAroundIt()
    {
        // The panel also holds a pager and a confirm bar. The pattern's popup is the grid, so that
        // is what aria-controls must reach.
        var cut = Lookup();
        Open(cut);

        var grid = cut.Find("table");
        grid.GetAttribute("role").ShouldBe("grid");
        cut.Find("input").GetAttribute("aria-controls").ShouldBe(grid.Id);
    }

    [Fact]
    public void Field_IsDescribedByItsHelpText()
    {
        // The gap ZenCombobox has: without aria-describedby a screen reader never hears the hint.
        var cut = Lookup(p => p.Add(x => x.HelpText, "Search by name or account"));

        var describedBy = cut.Find("input").GetAttribute("aria-describedby");
        describedBy.ShouldNotBeNull();
        cut.Find($"#{describedBy}").TextContent.ShouldContain("Search by name or account");
    }

    [Fact]
    public void Closed_TheGridIsNotRendered() =>
        Lookup().FindAll("table").ShouldBeEmpty();

    [Fact]
    public void Panel_TakesItsOwnWidth_RatherThanTheFields()
    {
        var cut = Lookup(p => p.Add(x => x.PanelWidth, "40rem"));
        Open(cut);

        cut.Find($"#{cut.Instance.Id}-panel").GetAttribute("style")!.ShouldContain("width: 40rem");
    }

    [Fact]
    public void UnderStaticRendering_TheFieldShowsTheValue()
    {
        Renderer.SetRendererInfo(new RendererInfo("Static", isInteractive: false));
        _value = Vendors[1];

        Lookup().Find("input").GetAttribute("value").ShouldBe("Cloudflare");
    }

    // ---- Searching ----------------------------------------------------------------------------

    [Fact]
    public void Opening_ShowsEveryRow_EvenWithAValueInTheField()
    {
        // The field shows the chosen row's name; filtering by it on open would hide every other
        // row until the user cleared the field.
        _value = Vendors[1];
        var cut = Lookup();
        Open(cut);

        RowNames(cut).Count.ShouldBe(4);
    }

    [Fact]
    public void Typing_FiltersByAnyVisibleColumn()
    {
        var cut = Lookup();
        Open(cut);

        cut.Find("input").Input("7104");

        RowNames(cut).ShouldBe(["Cloudflare"]);
    }

    [Fact]
    public void Typing_WithNoMatch_ShowsTheEmptyState()
    {
        var cut = Lookup();

        cut.Find("input").Input("zzz");

        cut.Find(".zen-table-empty").TextContent.ShouldContain("No matches");
    }

    [Fact]
    public void ItemsProvider_ReceivesTheQuery()
    {
        var queries = new List<string>();

        var cut = Render<ZenLookup<Vendor>>(p => p
            .Add(x => x.Columns, Columns)
            .Add(x => x.SearchDebounceMilliseconds, 0)
            .Add(x => x.ItemsProvider, request =>
            {
                queries.Add(request.Query);
                Vendor[] rows = [.. Vendors.Where(v => v.Name.Contains(request.Query, StringComparison.OrdinalIgnoreCase))];
                return ValueTask.FromResult(new ZenTableResult<Vendor>(rows, rows.Length));
            }));

        cut.Find("input").Input("cloud");

        cut.WaitForAssertion(() => RowNames(cut).ShouldBe(["Cloudflare", "Google Cloud"]));
        queries[^1].ShouldBe("cloud");
    }

    // ---- Keyboard -----------------------------------------------------------------------------

    [Fact]
    public void Opening_PutsTheCursorOnTheFirstRow_ByItsFirstCell()
    {
        // The grid-popup pattern points aria-activedescendant at a cell, not a row.
        var cut = Lookup();
        Open(cut);

        var active = cut.Find("input").GetAttribute("aria-activedescendant");
        active.ShouldNotBeNull();
        cut.Find($"#{active}").TagName.ShouldBe("TD");
        cut.Find($"#{active}").TextContent.Trim().ShouldBe("AWS");
    }

    [Fact]
    public void ArrowDown_MovesTheCursor()
    {
        var cut = Lookup();
        Open(cut);

        cut.Find("input").KeyDown(new KeyboardEventArgs { Key = "ArrowDown" });

        var active = cut.Find("input").GetAttribute("aria-activedescendant");
        cut.Find($"#{active}").TextContent.Trim().ShouldBe("Cloudflare");
    }

    [Fact]
    public void Opening_PutsTheCursorOnTheChosenRow()
    {
        _value = Vendors[2];
        var cut = Lookup();
        Open(cut);

        var active = cut.Find("input").GetAttribute("aria-activedescendant");
        cut.Find($"#{active}").TextContent.Trim().ShouldBe("Google Cloud");
    }

    [Fact]
    public void TheChosenRow_IsTheOnlyOneSelected()
    {
        _value = Vendors[1];
        var cut = Lookup();
        Open(cut);

        cut.FindAll("[role=grid] tbody tr").Select(tr => tr.GetAttribute("aria-selected"))
            .ShouldBe(["false", "true", "false", "false"]);
    }

    // ---- Immediate commit ---------------------------------------------------------------------

    [Fact]
    public void Enter_PicksTheActiveRow_AndCloses()
    {
        var cut = Lookup();
        Open(cut);

        cut.Find("input").KeyDown(new KeyboardEventArgs { Key = "ArrowDown" });
        cut.Find("input").KeyDown(new KeyboardEventArgs { Key = "Enter" });

        _value.ShouldBe(Vendors[1]);
        cut.FindAll("table").ShouldBeEmpty();
        cut.Find("input").GetAttribute("value").ShouldBe("Cloudflare");
    }

    [Fact]
    public void ClickingARow_PicksIt()
    {
        var cut = Lookup();
        Open(cut);

        cut.FindAll("[role=grid] tbody tr")[3].Click();

        _value.ShouldBe(Vendors[3]);
    }

    [Fact]
    public void Escape_ClosesWithoutChanging_AndRestoresTheText()
    {
        _value = Vendors[0];
        var cut = Lookup();

        cut.Find("input").Input("clo");
        cut.Find("input").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        _value.ShouldBe(Vendors[0]);
        cut.FindAll("table").ShouldBeEmpty();
        cut.Find("input").GetAttribute("value").ShouldBe("AWS");
    }

    [Fact]
    public async Task ClickingOutside_ClosesWithoutChanging()
    {
        _value = Vendors[0];
        var cut = Lookup();
        Open(cut);

        var popover = cut.FindComponent<ZenPopover>();
        await cut.InvokeAsync(() => popover.Instance.OnOutsideClick());

        _value.ShouldBe(Vendors[0]);
        cut.FindAll("table").ShouldBeEmpty();
    }

    // ---- Confirm commit -----------------------------------------------------------------------

    private IRenderedComponent<ZenLookup<Vendor>> Confirming() =>
        Lookup(p => p.Add(x => x.Commit, ZenLookupCommit.Confirm));

    [Fact]
    public void ConfirmMode_EnterOnlyMarksTheRow()
    {
        var cut = Confirming();
        Open(cut);

        cut.Find("input").KeyDown(new KeyboardEventArgs { Key = "ArrowDown" });
        cut.Find("input").KeyDown(new KeyboardEventArgs { Key = "Enter" });

        _value.ShouldBeNull();
        cut.FindAll("[role=grid] tbody tr")[1].GetAttribute("aria-selected").ShouldBe("true");
        cut.Find("[aria-live=polite]").TextContent.ShouldContain("Cloudflare");
    }

    [Fact]
    public void ConfirmMode_TheConfirmButtonCommits()
    {
        var cut = Confirming();
        Open(cut);

        cut.FindAll("[role=grid] tbody tr")[2].Click();
        cut.FindAll("button").Single(b => b.TextContent.Trim() == "Confirm").Click();

        _value.ShouldBe(Vendors[2]);
        cut.FindAll("table").ShouldBeEmpty();
    }

    [Fact]
    public void ConfirmMode_ConfirmIsDisabledUntilARowIsMarked()
    {
        var cut = Confirming();
        Open(cut);

        cut.FindAll("button").Single(b => b.TextContent.Trim() == "Confirm").HasAttribute("disabled").ShouldBeTrue();
    }

    [Fact]
    public void ConfirmMode_EscapeDiscardsTheMarkedRow()
    {
        _value = Vendors[0];
        var cut = Confirming();
        Open(cut);

        cut.FindAll("[role=grid] tbody tr")[2].Click();
        cut.Find("input").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        _value.ShouldBe(Vendors[0]);

        // Reopened, the committed row is the chosen one again - the mark did not survive.
        Open(cut);
        cut.FindAll("[role=grid] tbody tr").Select(tr => tr.GetAttribute("aria-selected"))
            .ShouldBe(["true", "false", "false", "false"]);
    }

    [Fact]
    public async Task ConfirmMode_ClickingOutsideDiscardsTheMarkedRow()
    {
        var cut = Confirming();
        Open(cut);

        cut.FindAll("[role=grid] tbody tr")[2].Click();
        await cut.InvokeAsync(() => cut.FindComponent<ZenPopover>().Instance.OnOutsideClick());

        _value.ShouldBeNull();
    }

    [Fact]
    public void ConfirmMode_CtrlEnterConfirms()
    {
        var cut = Confirming();
        Open(cut);

        cut.Find("input").KeyDown(new KeyboardEventArgs { Key = "Enter" });
        cut.Find("input").KeyDown(new KeyboardEventArgs { Key = "Enter", CtrlKey = true });

        _value.ShouldBe(Vendors[0]);
    }

    [Fact]
    public void Clear_RemovesTheValue()
    {
        _value = Vendors[0];
        var cut = Lookup();

        cut.Find("button[aria-label='Clear selection']").Click();

        _value.ShouldBeNull();
        cut.Find("input").GetAttribute("value").ShouldBe(string.Empty);
    }
}
