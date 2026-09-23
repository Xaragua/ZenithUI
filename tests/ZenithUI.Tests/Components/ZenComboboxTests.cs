namespace ZenithUI.Tests.Components;

/// <summary>
/// ZenCombobox: the WAI-ARIA combobox contract, the keyboard, and the two behaviours every
/// autocomplete gets complained about for.
/// </summary>
public class ZenComboboxTests : BunitContext
{
    private static readonly string[] Fruit = ["Apple", "Apricot", "Banana", "Cherry"];

    public ZenComboboxTests()
    {
        Renderer.SetRendererInfo(new RendererInfo("Server", isInteractive: true));
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    private IRenderedComponent<ZenCombobox<string>> RenderBox(
        Action<ComponentParameterCollectionBuilder<ZenCombobox<string>>>? extra = null) =>
        Render<ZenCombobox<string>>(p =>
        {
            p.Add(x => x.Items, Fruit).Add(x => x.Label, "Fruit");
            extra?.Invoke(p);
        });

    // ---- ARIA ---------------------------------------------------------------------------------

    [Fact]
    public void TheField_IsACombobox()
    {
        var input = RenderBox().Find("input");

        input.GetAttribute("role").ShouldBe("combobox");
        input.GetAttribute("aria-autocomplete").ShouldBe("list");
        input.GetAttribute("aria-expanded").ShouldBe("false");
        input.GetAttribute("aria-controls").ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public void TheField_ReportsWhenTheListOpens()
    {
        var cut = RenderBox();

        cut.Find("input").Focus();

        cut.Find("input").GetAttribute("aria-expanded").ShouldBe("true");
    }

    [Fact]
    public void TheActiveOption_IsNamedRatherThanFocused()
    {
        // DOM focus never leaves the input - that is what lets the user keep typing while arrowing
        // through results. aria-activedescendant is the only thing telling a screen reader which
        // option the cursor is on.
        var cut = RenderBox();

        cut.Find("input").Focus();

        var active = cut.Find("input").GetAttribute("aria-activedescendant");

        active.ShouldNotBeNullOrEmpty();
        cut.Find($"#{active}").GetAttribute("role").ShouldBe("option");
    }

    [Fact]
    public void ClosedCombobox_NamesNoActiveOption() =>
        // A stale aria-activedescendant on a closed combobox points at an element that is no
        // longer rendered, and a screen reader then announces nothing at all.
        RenderBox().Find("input").HasAttribute("aria-activedescendant").ShouldBeFalse();

    [Fact]
    public void EveryOption_StatesItsSelectedness()
    {
        var cut = RenderBox(p => p.Add(x => x.Value, "Banana"));

        cut.Find("input").Focus();

        cut.FindAll("[role='option']")
            .Select(o => o.GetAttribute("aria-selected"))
            .ShouldBe(["false", "false", "true", "false"]);
    }

    [Fact]
    public void ItPrerendersAsAClosedLabelledField()
    {
        // The M3 exit criterion. A combobox that prerenders as an empty div breaks the page on a
        // slow connection; this one is a real labelled input carrying its value, and it submits.
        Renderer.SetRendererInfo(new RendererInfo("Static", isInteractive: false));

        var cut = Render<ZenCombobox<string>>(p => p
            .Add(x => x.Items, Fruit)
            .Add(x => x.Label, "Fruit")
            .Add(x => x.Value, "Cherry"));

        var input = cut.Find("input");

        input.GetAttribute("value").ShouldBe("Cherry");
        input.GetAttribute("aria-expanded").ShouldBe("false");
        cut.FindAll("[role='option']").ShouldBeEmpty();
        cut.Find("label").TextContent.ShouldContain("Fruit");
    }

    // ---- Filtering ----------------------------------------------------------------------------

    [Fact]
    public void TypingFilters_CaseInsensitively()
    {
        var cut = RenderBox();

        cut.Find("input").Input("ap");

        cut.FindAll("[role='option']").Select(o => o.TextContent.Trim())
            .ShouldBe(["Apple", "Apricot"]);
    }

    [Fact]
    public void NoMatches_SaysSoInALiveRegion()
    {
        // Silence is indistinguishable from "still loading" to someone who cannot see the list.
        var cut = RenderBox();

        cut.Find("input").Input("zzz");

        cut.FindAll("[role='option']").ShouldBeEmpty();
        cut.Find("[role='status']").TextContent.ShouldContain("No matches");
    }

    // ---- Keyboard -----------------------------------------------------------------------------

    [Fact]
    public void ArrowDown_OpensAClosedList()
    {
        var cut = RenderBox();

        cut.Find("input").KeyDown(new KeyboardEventArgs { Key = "ArrowDown" });

        cut.Find("input").GetAttribute("aria-expanded").ShouldBe("true");
    }

    [Fact]
    public void Enter_CommitsTheActiveOption()
    {
        string? selected = null;

        var cut = RenderBox(p => p.Add(x => x.ValueChanged, v => selected = v));

        var input = cut.Find("input");
        input.Focus();
        input.KeyDown(new KeyboardEventArgs { Key = "ArrowDown" });
        input.KeyDown(new KeyboardEventArgs { Key = "Enter" });

        selected.ShouldBe("Apricot");
    }

    [Fact]
    public void TheFirstOptionIsHighlighted_ButNotSelected()
    {
        // Auto-selecting the highlight means a user who types and tabs away commits a value they
        // never chose. The classic autocomplete complaint.
        var changes = 0;

        var cut = RenderBox(p => p.Add(x => x.ValueChanged, _ => changes++));

        cut.Find("input").Focus();

        changes.ShouldBe(0);
        cut.Find("input").GetAttribute("aria-activedescendant").ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public void Tab_ClosesWithoutCommitting()
    {
        // The other classic complaint: tabbing out of a field should not change its value.
        var changes = 0;

        var cut = RenderBox(p => p.Add(x => x.ValueChanged, _ => changes++));

        var input = cut.Find("input");
        input.Focus();
        input.KeyDown(new KeyboardEventArgs { Key = "Tab" });

        changes.ShouldBe(0);
        cut.Find("input").GetAttribute("aria-expanded").ShouldBe("false");
    }

    [Fact]
    public void Escape_ClosesWithoutClearingTheSelection()
    {
        // A user who opened the list by accident should not lose their value getting rid of it.
        var cut = RenderBox(p => p.Add(x => x.Value, "Cherry"));

        var input = cut.Find("input");
        input.Focus();
        input.KeyDown(new KeyboardEventArgs { Key = "Escape" });

        cut.Find("input").GetAttribute("aria-expanded").ShouldBe("false");
        cut.Find("input").GetAttribute("value").ShouldBe("Cherry");
    }

    [Fact]
    public void TheCursor_StopsAtTheEnds()
    {
        string? selected = null;

        var cut = RenderBox(p => p.Add(x => x.ValueChanged, v => selected = v));

        var input = cut.Find("input");
        input.Focus();

        for (var i = 0; i < 10; i++)
        {
            input.KeyDown(new KeyboardEventArgs { Key = "ArrowDown" });
        }

        input.KeyDown(new KeyboardEventArgs { Key = "Enter" });

        selected.ShouldBe("Cherry");
    }

    // ---- Selection ----------------------------------------------------------------------------

    [Fact]
    public void ClickingAnOption_Selects()
    {
        string? selected = null;

        var cut = RenderBox(p => p.Add(x => x.ValueChanged, v => selected = v));

        cut.Find("input").Focus();
        cut.FindAll("[role='option']")[2].Click();

        selected.ShouldBe("Banana");
    }

    [Fact]
    public void TypingPastASelection_ClearsIt()
    {
        // Leaving the old value bound while the field shows different text is the state where a
        // form saves something the user cannot see.
        var cleared = false;

        var cut = RenderBox(p => p
            .Add(x => x.Value, "Banana")
            .Add(x => x.ValueChanged, v => cleared = v is null));

        cut.Find("input").Input("Ch");

        cleared.ShouldBeTrue();
    }

    [Fact]
    public void Multiple_AccumulatesChips()
    {
        IReadOnlyCollection<string> selected = [];

        var cut = Render<ZenCombobox<string>>(p => p
            .Add(x => x.Items, Fruit)
            .Add(x => x.Multiple, true)
            .Add(x => x.Values, selected)
            .Add(x => x.ValuesChanged, v => selected = v));

        cut.Find("input").Focus();
        cut.FindAll("[role='option']")[0].Click();
        cut.Render(p => p.Add(x => x.Values, selected));

        cut.Find("input").Focus();
        cut.FindAll("[role='option']")[2].Click();

        selected.ShouldBe(["Apple", "Banana"]);
    }

    [Fact]
    public void Multiple_BackspaceOnAnEmptyQueryRemovesTheLastChip()
    {
        // What every tag input has trained users to expect.
        IReadOnlyCollection<string> selected = ["Apple", "Banana"];

        var cut = Render<ZenCombobox<string>>(p => p
            .Add(x => x.Items, Fruit)
            .Add(x => x.Multiple, true)
            .Add(x => x.Values, selected)
            .Add(x => x.ValuesChanged, v => selected = v));

        cut.Find("input").KeyDown(new KeyboardEventArgs { Key = "Backspace" });

        selected.ShouldBe(["Apple"]);
    }

    [Fact]
    public void Clear_EmptiesTheSelection()
    {
        var cleared = false;

        var cut = RenderBox(p => p
            .Add(x => x.Value, "Banana")
            .Add(x => x.ValueChanged, v => cleared = v is null));

        cut.Find("button[aria-label='Clear selection']").Click();

        cleared.ShouldBeTrue();
    }

    // ---- Async options ------------------------------------------------------------------------

    [Fact]
    public async Task AnItemsProvider_PopulatesTheList()
    {
        var cut = Render<ZenCombobox<string>>(p => p
            .Add(x => x.SearchDebounceMilliseconds, 0)
            .Add(x => x.ItemsProvider, (query, _) =>
                Task.FromResult<IEnumerable<string>>(Fruit.Where(f => f.StartsWith(query, StringComparison.OrdinalIgnoreCase)))));

        cut.Find("input").Input("ch");

        await Task.Delay(100);
        cut.Render();

        cut.FindAll("[role='option']").Select(o => o.TextContent.Trim()).ShouldBe(["Cherry"]);
    }

    [Fact]
    public async Task ASupersededRequest_DoesNotRepopulateTheList()
    {
        // A slow response for "a" landing after a fast one for "ch" would otherwise show results
        // for a query the user has already typed past. The provider is handed a token precisely
        // so this can be cancelled.
        var cut = Render<ZenCombobox<string>>(p => p
            .Add(x => x.SearchDebounceMilliseconds, 0)
            .Add(x => x.ItemsProvider, async (query, token) =>
            {
                // The first, broader query is deliberately the slow one.
                await Task.Delay(query.Length == 1 ? 250 : 10, token);
                return Fruit.Where(f => f.StartsWith(query, StringComparison.OrdinalIgnoreCase));
            }));

        var input = cut.Find("input");
        input.Input("a");
        input.Input("ch");

        await Task.Delay(500);
        cut.Render();

        cut.FindAll("[role='option']").Select(o => o.TextContent.Trim()).ShouldBe(["Cherry"]);
    }
}
