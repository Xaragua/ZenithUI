namespace ZenithUI.Tests.Components;

/// <summary>
/// ZenList: the semantics switch between a plain list and a listbox, and the keyboard contract
/// that comes with the latter.
/// </summary>
public class ZenListTests : BunitContext
{
    private static readonly string[] Fruit = ["Apple", "Banana", "Cherry"];

    public ZenListTests() =>
        Renderer.SetRendererInfo(new RendererInfo("Server", isInteractive: true));

    // ---- Semantics ----------------------------------------------------------------------------

    [Fact]
    public void APlainList_IsAPlainList()
    {
        // Giving a non-interactive list listbox semantics promises a widget the user cannot
        // operate - a screen reader announces "listbox, 3 items" and then arrow keys do nothing.
        var cut = Render<ZenList<string>>(p => p.Add(x => x.Items, Fruit));

        cut.Find("ul").HasAttribute("role").ShouldBeFalse();
        cut.FindAll("li").Count.ShouldBe(3);
    }

    [Fact]
    public void ASelectableList_IsAListbox()
    {
        var cut = Render<ZenList<string>>(p => p
            .Add(x => x.Items, Fruit)
            .Add(x => x.Selectable, true));

        cut.Find("ul").GetAttribute("role").ShouldBe("listbox");
        cut.FindAll("[role='option']").Count.ShouldBe(3);
    }

    [Fact]
    public void EveryOption_StatesWhetherItIsSelected()
    {
        // aria-selected goes on all of them, not just the selected one. Omitting it elsewhere
        // leaves a screen reader announcing "selected" for one row and nothing for the rest,
        // which reads as "unknown" rather than "not selected".
        var cut = Render<ZenList<string>>(p => p
            .Add(x => x.Items, Fruit)
            .Add(x => x.Selectable, true)
            .Add(x => x.Value, "Banana"));

        cut.FindAll("[role='option']")
            .Select(o => o.GetAttribute("aria-selected"))
            .ShouldBe(["false", "true", "false"]);
    }

    [Fact]
    public void TheListbox_NamesItsActiveOption()
    {
        // Focus stays on the listbox, so aria-activedescendant is the only thing telling a screen
        // reader which row the cursor is on. Without it the highlight moves silently.
        var cut = Render<ZenList<string>>(p => p
            .Add(x => x.Items, Fruit)
            .Add(x => x.Selectable, true));

        var list = cut.Find("ul");
        list.KeyDown(new KeyboardEventArgs { Key = "ArrowDown" });

        var active = cut.Find("ul").GetAttribute("aria-activedescendant");

        active.ShouldNotBeNullOrEmpty();
        cut.Find($"#{active}").TextContent.Trim().ShouldBe("Banana");
    }

    // ---- Selection ----------------------------------------------------------------------------

    [Fact]
    public void ClickingAnOption_Selects()
    {
        string? selected = null;

        var cut = Render<ZenList<string>>(p => p
            .Add(x => x.Items, Fruit)
            .Add(x => x.Selectable, true)
            .Add(x => x.ValueChanged, v => selected = v));

        cut.FindAll("[role='option']")[2].Click();

        selected.ShouldBe("Cherry");
    }

    [Fact]
    public void ArrowKeys_MoveTheCursorWithoutSelecting()
    {
        // The WAI-ARIA listbox pattern, and it matters beyond conformance: selecting on every
        // arrow press fires a change per keystroke, which in a bound form is a save per keystroke.
        var changes = 0;

        var cut = Render<ZenList<string>>(p => p
            .Add(x => x.Items, Fruit)
            .Add(x => x.Selectable, true)
            .Add(x => x.ValueChanged, _ => changes++));

        var list = cut.Find("ul");
        list.KeyDown(new KeyboardEventArgs { Key = "ArrowDown" });
        list.KeyDown(new KeyboardEventArgs { Key = "ArrowDown" });

        changes.ShouldBe(0);
    }

    [Fact]
    public void Enter_CommitsTheCursor()
    {
        string? selected = null;

        var cut = Render<ZenList<string>>(p => p
            .Add(x => x.Items, Fruit)
            .Add(x => x.Selectable, true)
            .Add(x => x.ValueChanged, v => selected = v));

        var list = cut.Find("ul");
        list.KeyDown(new KeyboardEventArgs { Key = "ArrowDown" });
        list.KeyDown(new KeyboardEventArgs { Key = "Enter" });

        selected.ShouldBe("Banana");
    }

    [Fact]
    public void ArrowKeys_SkipDisabledItems()
    {
        string? selected = null;

        var cut = Render<ZenList<string>>(p => p
            .Add(x => x.Items, Fruit)
            .Add(x => x.Selectable, true)
            .Add(x => x.IsItemDisabled, item => item == "Banana")
            .Add(x => x.ValueChanged, v => selected = v));

        var list = cut.Find("ul");
        list.KeyDown(new KeyboardEventArgs { Key = "ArrowDown" });
        list.KeyDown(new KeyboardEventArgs { Key = "Enter" });

        selected.ShouldBe("Cherry");
    }

    [Fact]
    public void ADisabledItem_IgnoresClicks()
    {
        var changes = 0;

        var cut = Render<ZenList<string>>(p => p
            .Add(x => x.Items, Fruit)
            .Add(x => x.Selectable, true)
            .Add(x => x.IsItemDisabled, item => item == "Banana")
            .Add(x => x.ValueChanged, _ => changes++));

        cut.FindAll("[role='option']")[1].Click();

        changes.ShouldBe(0);
    }

    [Fact]
    public void TheCursor_StopsAtTheEndsRatherThanWrapping()
    {
        // Wrapping is defensible, but it makes "hold ArrowUp to reach the top" never terminate,
        // and a user who cannot see the list has no way to tell they have been round twice.
        string? selected = null;

        var cut = Render<ZenList<string>>(p => p
            .Add(x => x.Items, Fruit)
            .Add(x => x.Selectable, true)
            .Add(x => x.ValueChanged, v => selected = v));

        var list = cut.Find("ul");

        for (var i = 0; i < 5; i++)
        {
            list.KeyDown(new KeyboardEventArgs { Key = "ArrowUp" });
        }

        list.KeyDown(new KeyboardEventArgs { Key = "Enter" });

        selected.ShouldBe("Apple");
    }

    [Fact]
    public void HomeAndEnd_JumpToTheEnds()
    {
        string? selected = null;

        var cut = Render<ZenList<string>>(p => p
            .Add(x => x.Items, Fruit)
            .Add(x => x.Selectable, true)
            .Add(x => x.ValueChanged, v => selected = v));

        var list = cut.Find("ul");
        list.KeyDown(new KeyboardEventArgs { Key = "End" });
        list.KeyDown(new KeyboardEventArgs { Key = "Enter" });

        selected.ShouldBe("Cherry");
    }

    [Fact]
    public void MultipleSelection_TogglesAndAccumulates()
    {
        IReadOnlyCollection<string> selected = [];

        var cut = Render<ZenList<string>>(p => p
            .Add(x => x.Items, Fruit)
            .Add(x => x.Selectable, true)
            .Add(x => x.Multiple, true)
            .Add(x => x.Values, selected)
            .Add(x => x.ValuesChanged, v => selected = v));

        cut.FindAll("[role='option']")[0].Click();
        cut.Render(p => p.Add(x => x.Values, selected));

        cut.FindAll("[role='option']")[2].Click();
        cut.Render(p => p.Add(x => x.Values, selected));

        selected.ShouldBe(["Apple", "Cherry"]);

        cut.FindAll("[role='option']")[0].Click();

        selected.ShouldBe(["Cherry"]);
    }

    [Fact]
    public void MultiSelectListboxes_SaySo() =>
        Render<ZenList<string>>(p => p
                .Add(x => x.Items, Fruit)
                .Add(x => x.Selectable, true)
                .Add(x => x.Multiple, true))
            .Find("ul").GetAttribute("aria-multiselectable").ShouldBe("true");

    // ---- Empty and loading --------------------------------------------------------------------

    [Fact]
    public void AnEmptyList_SaysSo()
    {
        // A list that renders nothing when it has nothing is the most common way a loading bug
        // looks like a data bug.
        var cut = Render<ZenList<string>>(p => p
            .Add(x => x.Items, Array.Empty<string>())
            .Add(x => x.EmptyText, "No fruit yet."));

        cut.Markup.ShouldContain("No fruit yet.");
    }

    [Fact]
    public void ANullItemsCollection_ShowsTheEmptyStateRatherThanThrowing() =>
        Render<ZenList<string>>(p => p.Add(x => x.Items, (IEnumerable<string>?)null))
            .Markup.ShouldContain("Nothing to show.");

    [Fact]
    public void Loading_ShowsPlaceholdersAndAnnouncesItself()
    {
        var cut = Render<ZenList<string>>(p => p
            .Add(x => x.Items, Fruit)
            .Add(x => x.Loading, true)
            .Add(x => x.LoadingRowCount, 4));

        var container = cut.Find("[aria-busy='true']");

        container.GetAttribute("aria-live").ShouldBe("polite");

        // Skeleton rows are decorative, so they are counted by the aria-hidden they carry rather
        // than by a class - ZenSkeleton is built from utilities and has no stable class to target.
        // Placeholders rather than a spinner so the list keeps its shape and the page does not
        // reflow when the data lands.
        cut.FindAll("[aria-hidden='true']").Count.ShouldBe(4);
    }
}
