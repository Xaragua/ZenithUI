namespace ZenithUI.Tests.Components;

/// <summary>
/// ZenCalendar and ZenDatePicker: the date-grid pattern.
/// </summary>
/// <remarks>
/// <para>
/// These two shipped in M2 with no tests at all, which is how an invalid <c>aria-selected</c> on a
/// <c>role="button"</c> survived into M6 — every rendering test the suite had was for some other
/// component, and the browser draws an unsupported ARIA attribute exactly like a supported one.
/// </para>
/// <para>
/// What is asserted here is the part a restyle or a refactor can quietly break: which element
/// carries which state, the single tab stop, and the arithmetic the arrow keys do. The visual
/// calendar is not the contract; the grid is.
/// </para>
/// </remarks>
public class ZenCalendarTests : BunitContext
{
    private static readonly DateOnly Selected = new(2026, 9, 23);

    public ZenCalendarTests()
    {
        Renderer.SetRendererInfo(new RendererInfo("Server", isInteractive: true));

        // The calendar imports zen-dom.js to move focus to the cursor cell. Loose mode answers
        // that with a no-op: what these tests assert is the roving tabindex, which is what the
        // browser and a screen reader both act on.
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    private IRenderedComponent<ZenCalendar> Calendar(
        Action<ComponentParameterCollectionBuilder<ZenCalendar>>? extra = null) =>
        Render<ZenCalendar>(p =>
        {
            p.Add(x => x.Value, Selected).Add(x => x.Culture, CultureInfo.InvariantCulture);
            extra?.Invoke(p);
        });

    private static AngleSharp.Dom.IElement Cell(IRenderedComponent<ZenCalendar> cut, int day) =>
        cut.FindAll("td[role='gridcell']")
            .First(td => td.QuerySelector("button")?.TextContent.Trim() == day.ToString(CultureInfo.InvariantCulture)
                && !td.QuerySelector("button")!.ClassName!.Contains("text-content-subtle", StringComparison.Ordinal));

    // ---- Structure --------------------------------------------------------------------------

    [Fact]
    public void AMonth_IsAGridOfGridcells()
    {
        var cut = Calendar();

        cut.Find("table[role='grid']").GetAttribute("aria-label").ShouldBe("September 2026");

        // Six weeks of seven days is the maximum a month grid ever needs, and the component
        // always renders whole weeks - so the count is a multiple of seven either way.
        var cells = cut.FindAll("td[role='gridcell']");
        (cells.Count % 7).ShouldBe(0);
        cells.Count.ShouldBeGreaterThanOrEqualTo(28);
    }

    [Fact]
    public void Selection_IsStatedOnTheCell_NotOnTheButtonInsideIt()
    {
        var cut = Calendar();

        // aria-selected is defined for gridcell and not for button. On the button it was invalid
        // ARIA: a screen reader may ignore it entirely, and the selected day then reads exactly
        // like every other day.
        cut.FindAll("button[aria-selected]").ShouldBeEmpty();

        Cell(cut, 23).GetAttribute("aria-selected").ShouldBe("true");
        Cell(cut, 22).GetAttribute("aria-selected").ShouldBe("false");
    }

    [Fact]
    public void EveryCell_StatesSelection_NotOnlyTheSelectedOne()
    {
        // Omitting aria-selected on the unselected cells would leave a screen reader unable to
        // say a day is NOT selected - the absence of a state is not the same as a false one.
        var cut = Calendar();

        cut.FindAll("td[role='gridcell']").Count
            .ShouldBe(cut.FindAll("td[role='gridcell'][aria-selected]").Count);
    }

    [Fact]
    public void Today_IsMarkedWithAriaCurrent()
    {
        var today = DateOnly.FromDateTime(DateTime.Now);
        var cut = Render<ZenCalendar>(p => p
            .Add(x => x.Value, today)
            .Add(x => x.Culture, CultureInfo.InvariantCulture));

        var current = cut.FindAll("button[aria-current='date']");

        current.Count.ShouldBe(1);
        current[0].TextContent.Trim().ShouldBe(today.Day.ToString(CultureInfo.InvariantCulture));
    }

    [Fact]
    public void EveryDay_CarriesItsFullDateAsAnAccessibleName()
    {
        // "23" alone is meaningless read out of the grid, and a screen reader in browse mode
        // reads exactly that.
        var cut = Calendar();

        Cell(cut, 23).QuerySelector("button")!
            .GetAttribute("aria-label").ShouldBe("Wednesday, 23 September 2026");
    }

    // ---- Keyboard ---------------------------------------------------------------------------

    [Fact]
    public void TheGrid_HasExactlyOneTabStop()
    {
        var cut = Calendar();

        // The roving tabindex is what lets Tab leave the calendar rather than walking all 42
        // cells. Anything other than exactly one zero here is a keyboard trap or a dead grid.
        cut.FindAll("button[tabindex='0']").Count.ShouldBe(1);
        Cell(cut, 23).QuerySelector("button")!.GetAttribute("tabindex").ShouldBe("0");
    }

    [Theory]
    [InlineData("ArrowLeft", 22)]
    [InlineData("ArrowRight", 24)]
    [InlineData("ArrowUp", 16)]
    [InlineData("ArrowDown", 30)]
    [InlineData("Home", 20)]  // Sunday of that week, under the invariant culture.
    [InlineData("End", 26)]
    public void ArrowKeys_MoveTheCursorWithoutChangingTheSelection(string key, int expectedDay)
    {
        var cut = Calendar();

        cut.Find("table[role='grid']").KeyDown(new KeyboardEventArgs { Key = key });

        Cell(cut, expectedDay).QuerySelector("button")!.GetAttribute("tabindex").ShouldBe("0");

        // Moving the cursor is not choosing a date. A calendar that selected on arrow would fire
        // ValueChanged six times on the way across a week.
        Cell(cut, 23).GetAttribute("aria-selected").ShouldBe("true");
    }

    [Fact]
    public void PageKeys_ChangeTheMonth_AndTheHeadingSaysSo()
    {
        var cut = Calendar();

        cut.Find("table[role='grid']").KeyDown(new KeyboardEventArgs { Key = "PageDown" });

        cut.Find("table[role='grid']").GetAttribute("aria-label").ShouldBe("October 2026");

        // The month name lives in an aria-live region; without it a keyboard user hears the newly
        // focused day and never learns the month moved.
        cut.Find("[aria-live='polite']").TextContent.Trim().ShouldBe("October 2026");
    }

    [Fact]
    public void Enter_SelectsTheCursorDate()
    {
        DateOnly? chosen = null;
        var cut = Calendar(p => p.Add(x => x.ValueChanged, EventCallback.Factory.Create<DateOnly?>(this, v => chosen = v)));

        cut.Find("table[role='grid']").KeyDown(new KeyboardEventArgs { Key = "ArrowRight" });
        cut.Find("table[role='grid']").KeyDown(new KeyboardEventArgs { Key = "Enter" });

        chosen.ShouldBe(new DateOnly(2026, 9, 24));
    }

    // ---- Bounds -----------------------------------------------------------------------------

    [Fact]
    public void OutOfRangeDays_StayVisibleAndUnselectable()
    {
        var cut = Calendar(p => p
            .Add(x => x.Min, new DateOnly(2026, 9, 20))
            .Add(x => x.Max, new DateOnly(2026, 9, 26)));

        // Visible, because a month with a hole in it is harder to read than one with greyed days,
        // and because arrow-key travel across the grid must not be interrupted.
        Cell(cut, 19).QuerySelector("button")!.HasAttribute("disabled").ShouldBeTrue();
        Cell(cut, 23).QuerySelector("button")!.HasAttribute("disabled").ShouldBeFalse();
    }

    [Fact]
    public void ADisabledDatePredicate_AppliesPerDay()
    {
        var cut = Calendar(p => p.Add(x => x.IsDateDisabled, d => d.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday));

        Cell(cut, 26).QuerySelector("button")!.HasAttribute("disabled").ShouldBeTrue();  // Saturday
        Cell(cut, 25).QuerySelector("button")!.HasAttribute("disabled").ShouldBeFalse(); // Friday
    }

    // ---- ZenDatePicker ----------------------------------------------------------------------

    [Fact]
    public void ThePicker_PrerendersAsALabelledFieldWithACollapsedPopup()
    {
        // Static SSR renders this with no JavaScript attached. It has to read as a date field
        // rather than as a broken listbox.
        Renderer.SetRendererInfo(new RendererInfo("Static", isInteractive: false));

        var cut = Render<ZenDatePicker<DateOnly?>>(p => p
            .Add(x => x.Label, "Delivery date")
            .Add(x => x.Value, Selected)
            .Add(x => x.Culture, CultureInfo.InvariantCulture));

        var trigger = cut.Find("button[aria-haspopup]");

        trigger.GetAttribute("aria-expanded").ShouldBe("false");
        cut.FindAll("table[role='grid']").ShouldBeEmpty();
        cut.FindAll("[popover]").ShouldBeEmpty();

        // Named by its ZenField label rather than by an aria-label, which is what makes clicking
        // the text focus the field as well as naming it.
        cut.Find("label").GetAttribute("for").ShouldBe(cut.Find("input").GetAttribute("id"));
        cut.Find("label").TextContent.ShouldContain("Delivery date");
    }

    [Fact]
    public void ThePanel_IsInTheTopLayer_NotAbsolutelyPositioned()
    {
        // The regression this guards is not subtle once seen and invisible until then: an
        // absolutely positioned panel is clipped by any ancestor with overflow hidden, and
        // ZenCard - where most date pickers live - is exactly that. No z-index fixes it, because
        // clipping is not a stacking problem.
        var cut = Picker();

        cut.Find("button[aria-haspopup='dialog']").Click();

        var panel = cut.Find($"#{cut.Find("button[aria-haspopup='dialog']").GetAttribute("aria-controls")}");

        panel.GetAttribute("popover").ShouldNotBeNull(
            "the calendar panel must be a popover, which is what puts it in the top layer.");
        (panel.ClassName ?? string.Empty).ShouldNotContain("absolute");
        panel.GetAttribute("role").ShouldBe("dialog");
    }

    [Fact]
    public void Opening_MovesFocusIntoTheGrid()
    {
        // It did not, for four milestones: ToggleAsync reached for the calendar's @ref before the
        // render that creates it, so the call landed on null and the user was left on the trigger
        // with arrow keys that did nothing.
        var module = JSInterop.SetupModule("./_content/ZenithUI/js/zen-dom.js");
        module.SetupVoid("focusElement", _ => true).SetVoidResult();

        var cut = Picker();
        cut.Find("button[aria-haspopup='dialog']").Click();

        var cursor = cut.Find("td[role='gridcell'] button[tabindex='0']").GetAttribute("id");

        module.VerifyInvoke("focusElement").Arguments[0].ShouldBe(cursor);
    }

    private IRenderedComponent<ZenDatePicker<DateOnly?>> Picker() =>
        Render<ZenDatePicker<DateOnly?>>(p => p
            .Add(x => x.Label, "Delivery date")
            .Add(x => x.Value, Selected)
            .Add(x => x.Culture, CultureInfo.InvariantCulture));
}
