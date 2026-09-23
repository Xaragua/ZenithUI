namespace ZenithUI.Tests.Components;

/// <summary>
/// ZenTree: the WAI-ARIA tree pattern. The assertions concentrate on the structure that supplies
/// depth and position, and on the keyboard contract - the two things a restyle can quietly break.
/// </summary>
public class ZenTreeTests : BunitContext
{
    public ZenTreeTests()
    {
        Renderer.SetRendererInfo(new RendererInfo("Server", isInteractive: true));

        // The tree imports zen-dom.js for focusElement. Loose mode answers the import and every
        // call on it with a no-op, which is exactly right here: what is being asserted is the
        // tabindex, which is what a screen reader and the browser both act on. Whether focus()
        // was also called is a detail of the same decision.
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    private sealed record Folder(string Name, params Folder[] Children);

    private static readonly Folder[] Sample =
    [
        new("src",
            new Folder("Components",
                new Folder("Data"),
                new Folder("Forms")),
            new Folder("Core")),
        new("tests"),
        new("docs"),
    ];

    private IRenderedComponent<ZenTree<Folder>> Tree(
        Action<ComponentParameterCollectionBuilder<ZenTree<Folder>>>? extra = null) =>
        Render<ZenTree<Folder>>(p =>
        {
            p.Add(x => x.Items, Sample)
                .Add(x => x.ChildrenProvider, f => f.Children.Length == 0 ? null : f.Children)
                .Add(x => x.ItemText, f => f.Name)
                .Add(x => x.Label, "Files");

            extra?.Invoke(p);
        });

    private static AngleSharp.Dom.IElement Row(IRenderedComponent<ZenTree<Folder>> cut, string name) =>
        cut.FindAll("[role='treeitem']").First(li => li.TextContent.Trim().StartsWith(name, StringComparison.Ordinal));

    // ---- Structure --------------------------------------------------------------------------

    [Fact]
    public void ATree_IsATreeWithNamedRoot()
    {
        var cut = Tree();

        var tree = cut.Find("ul[role='tree']");

        tree.GetAttribute("aria-label").ShouldBe("Files");
        cut.FindAll("[role='treeitem']").Count.ShouldBe(3);
    }

    [Fact]
    public void Children_LiveInsideTheirParentAsAGroup()
    {
        // The nesting is what supplies level, set size and position in set. Rendering children as
        // siblings of their parent with an aria-level attribute instead would announce a flat
        // list to anything that trusts the DOM over the attributes.
        var cut = Tree(p => p.Add(x => x.IsInitiallyExpanded, f => f.Name == "src"));

        var src = Row(cut, "src");
        var group = src.QuerySelector("ul[role='group']");

        group.ShouldNotBeNull();
        group!.Children.Length.ShouldBe(2);
    }

    [Fact]
    public void TheTree_DoesNotRestateWhatTheStructureAlreadySays()
    {
        // aria-level, aria-setsize and aria-posinset are computed from the group nesting. Writing
        // them by hand is a second source of truth, and a level attribute that merely disagrees
        // with the DOM is invisible to every test that could have caught it.
        var cut = Tree(p => p.Add(x => x.IsInitiallyExpanded, f => f.Name == "src"));

        cut.Markup.ShouldNotContain("aria-level");
        cut.Markup.ShouldNotContain("aria-setsize");
        cut.Markup.ShouldNotContain("aria-posinset");
    }

    [Fact]
    public void OnlyExpandableNodes_ReportAnExpandedState()
    {
        // aria-expanded on a leaf tells a screen reader there is something to open. There is not.
        var cut = Tree();

        Row(cut, "src").HasAttribute("aria-expanded").ShouldBeTrue();
        Row(cut, "tests").HasAttribute("aria-expanded").ShouldBeFalse();
    }

    [Fact]
    public void Depth_IsPassedAsACustomPropertyRatherThanAClass()
    {
        // The class list stays static however deep the data nests; zen-indent multiplies
        // --zen-depth by the indent token. A class per level would need one utility per possible
        // depth, safelisted in advance.
        var cut = Tree(p => p.Add(x => x.IsInitiallyExpanded, f => f.Name == "src"));

        Row(cut, "Core").QuerySelector("div")!.GetAttribute("style")!.ShouldContain("--zen-depth: 1");
    }

    // ---- Tab stop ---------------------------------------------------------------------------

    [Fact]
    public void TheTree_IsExactlyOneTabStop()
    {
        var cut = Tree(p => p.Add(x => x.IsInitiallyExpanded, f => f.Name == "src"));

        cut.FindAll("[role='treeitem'][tabindex='0']").Count.ShouldBe(1);
        cut.FindAll("[role='treeitem']")[0].GetAttribute("tabindex").ShouldBe("0");
    }

    [Fact]
    public void TheToggle_IsNotATabStopOfItsOwn()
    {
        // A chevron per node would turn a hundred-node tree into a hundred tab stops. ArrowRight
        // is how a keyboard user expands.
        var cut = Tree();

        cut.FindAll("button").ShouldAllBe(b => b.GetAttribute("tabindex") == "-1");
    }

    // ---- Keyboard ---------------------------------------------------------------------------

    [Fact]
    public void ArrowDown_MovesTheTabStopToTheNextVisibleNode()
    {
        var cut = Tree();

        cut.Find("ul[role='tree']").KeyDown(new KeyboardEventArgs { Key = "ArrowDown" });

        Row(cut, "tests").GetAttribute("tabindex").ShouldBe("0");
    }

    [Fact]
    public void ArrowRight_OpensAClosedNode_ThenDescendsIntoIt()
    {
        // One key, two jobs. Without the second, a user who opened a node would have to reach for
        // ArrowDown to enter it, and the two keys would disagree about where they are.
        var cut = Tree();
        var tree = cut.Find("ul[role='tree']");

        tree.KeyDown(new KeyboardEventArgs { Key = "ArrowRight" });
        Row(cut, "src").GetAttribute("aria-expanded").ShouldBe("true");

        cut.Find("ul[role='tree']").KeyDown(new KeyboardEventArgs { Key = "ArrowRight" });
        Row(cut, "Components").GetAttribute("tabindex").ShouldBe("0");
    }

    [Fact]
    public void ArrowLeft_ClosesAnOpenNode_ThenClimbsToTheParent()
    {
        var cut = Tree(p => p.Add(x => x.IsInitiallyExpanded, f => f.Name == "src"));
        var tree = cut.Find("ul[role='tree']");

        // Move onto the first child, then back out of it.
        tree.KeyDown(new KeyboardEventArgs { Key = "ArrowDown" });
        cut.Find("ul[role='tree']").KeyDown(new KeyboardEventArgs { Key = "ArrowLeft" });

        Row(cut, "src").GetAttribute("tabindex").ShouldBe("0");
        Row(cut, "src").GetAttribute("aria-expanded").ShouldBe("true");
    }

    [Fact]
    public void EndAndHome_ReachTheEndsOfWhatIsVisible()
    {
        var cut = Tree();
        var tree = cut.Find("ul[role='tree']");

        tree.KeyDown(new KeyboardEventArgs { Key = "End" });
        Row(cut, "docs").GetAttribute("tabindex").ShouldBe("0");

        cut.Find("ul[role='tree']").KeyDown(new KeyboardEventArgs { Key = "Home" });
        Row(cut, "src").GetAttribute("tabindex").ShouldBe("0");
    }

    [Fact]
    public void TypeAhead_JumpsToTheNextNodeStartingWithWhatWasTyped()
    {
        var cut = Tree();

        cut.Find("ul[role='tree']").KeyDown(new KeyboardEventArgs { Key = "d" });

        Row(cut, "docs").GetAttribute("tabindex").ShouldBe("0");
    }

    [Fact]
    public void Asterisk_OpensEverySiblingAtTheCurrentLevel()
    {
        var cut = Tree();

        cut.Find("ul[role='tree']").KeyDown(new KeyboardEventArgs { Key = "*" });

        // Only "src" has children among the roots, so it is the only one that can open - but the
        // point is that the key reached every sibling, not just the focused one.
        Row(cut, "src").GetAttribute("aria-expanded").ShouldBe("true");
    }

    [Fact]
    public void Enter_SelectsTheFocusedNode()
    {
        Folder? selected = null;

        var cut = Tree(p => p.Add(x => x.ValueChanged, f => selected = f));

        cut.Find("ul[role='tree']").KeyDown(new KeyboardEventArgs { Key = "Enter" });

        selected?.Name.ShouldBe("src");
    }

    // ---- Selection --------------------------------------------------------------------------

    [Fact]
    public void OnlyTheSelectedNode_CarriesAriaSelected()
    {
        // Unlike a listbox, an unselected treeitem states nothing. Marking every node
        // "not selected" makes a screen reader say so on each of them while the user is doing
        // nothing but navigating, which is noise in a tree of any size.
        var cut = Tree(p => p.Add(x => x.Value, Sample[1]));

        cut.FindAll("[role='treeitem'][aria-selected]").Count.ShouldBe(1);
        Row(cut, "tests").GetAttribute("aria-selected").ShouldBe("true");
    }

    // ---- Lazy children ----------------------------------------------------------------------

    [Fact]
    public async Task ALazyNode_IsDrawnAsExpandableBeforeItsChildrenAreKnown()
    {
        // Drawing no chevron until the children arrive hides the subtree behind an interaction
        // nobody can discover. The guess corrects itself when the loader answers.
        var gate = new TaskCompletionSource<IEnumerable<Folder>>();

        var cut = Render<ZenTree<Folder>>(p => p
            .Add(x => x.Items, new[] { new Folder("remote") })
            .Add(x => x.ItemText, f => f.Name)
            .Add(x => x.ChildrenLoader, _ => gate.Task));

        Row(cut, "remote").GetAttribute("aria-expanded").ShouldBe("false");

        // Deliberately not awaited. The click handler awaits the loader, so awaiting the click
        // would wait for the very gate this test is about to open.
        var click = cut.Find("button").ClickAsync(new MouseEventArgs());

        cut.WaitForAssertion(() => Row(cut, "remote").GetAttribute("aria-busy").ShouldBe("true"));

        gate.SetResult([new Folder("one"), new Folder("two")]);
        await click;

        cut.WaitForAssertion(() => cut.FindAll("[role='treeitem']").Count.ShouldBe(3));
        Row(cut, "remote").HasAttribute("aria-busy").ShouldBeFalse();
    }

    [Fact]
    public async Task ALazyNodeThatTurnsOutToHaveNoChildren_BecomesALeaf()
    {
        var cut = Render<ZenTree<Folder>>(p => p
            .Add(x => x.Items, new[] { new Folder("empty") })
            .Add(x => x.ItemText, f => f.Name)
            .Add(x => x.ChildrenLoader, _ => Task.FromResult<IEnumerable<Folder>>([])));

        await cut.Find("button").ClickAsync(new MouseEventArgs());

        cut.WaitForAssertion(() => Row(cut, "empty").HasAttribute("aria-expanded").ShouldBeFalse());
    }
}
