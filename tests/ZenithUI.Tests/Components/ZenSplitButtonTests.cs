using Bunit.JSInterop.InvocationHandlers;

namespace ZenithUI.Tests.Components;

/// <summary>
/// ZenSplitButton and ZenMenuItem: the menu button pattern's ARIA, where focus goes on each way
/// in and out of the menu, and that the two halves really are two separate actions.
/// </summary>
public class ZenSplitButtonTests : BunitContext
{
    private readonly BunitJSModuleInterop _dom;

    private readonly List<string> _chosen = [];
    private int _primary;

    public ZenSplitButtonTests()
    {
        Renderer.SetRendererInfo(new RendererInfo("Server", isInteractive: true));
        JSInterop.Mode = JSRuntimeMode.Loose;

        _dom = JSInterop.SetupModule("./_content/ZenithUI/js/zen-dom.js");
        _dom.SetupVoid("focusElement", _ => true).SetVoidResult();
    }

    private IRenderedComponent<ZenSplitButton> Split(
        Action<ComponentParameterCollectionBuilder<ZenSplitButton>>? extra = null) =>
        Render<ZenSplitButton>(p =>
        {
            p.Add(x => x.ChildContent, "Save")
                .Add(x => x.OnClick, () => _primary++)
                .Add(x => x.MenuContent, b =>
                {
                    Item(b, 0, "Save as draft");
                    Item(b, 10, "Save and close", disabled: true);
                    Item(b, 20, "Save a copy");
                });

            extra?.Invoke(p);
        });

    private void Item(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder b, int seq, string text, bool disabled = false)
    {
        b.OpenComponent<ZenMenuItem>(seq);
        b.AddComponentParameter(seq + 1, nameof(ZenMenuItem.ChildContent), (RenderFragment)(c => c.AddContent(0, text)));
        b.AddComponentParameter(seq + 2, nameof(ZenMenuItem.Disabled), disabled);
        b.AddComponentParameter(seq + 3, nameof(ZenMenuItem.OnClick),
            EventCallback.Factory.Create<MouseEventArgs>(this, () => _chosen.Add(text)));
        b.CloseComponent();
    }

    private static AngleSharp.Dom.IElement Toggle(IRenderedComponent<ZenSplitButton> cut) =>
        cut.Find("[aria-haspopup=menu]");

    private string? LastFocused() =>
        _dom.Invocations["focusElement"].LastOrDefault().Arguments?[0] as string;

    private static List<string> Items(IRenderedComponent<ZenSplitButton> cut) =>
        cut.FindAll("[role=menuitem]").Select(i => i.TextContent.Trim()).ToList();

    [Fact]
    public void Closed_TheToggleIsAMenuButton_ThatControlsNothingYet()
    {
        var toggle = Toggle(Split());

        toggle.GetAttribute("aria-expanded").ShouldBe("false");
        toggle.GetAttribute("aria-label").ShouldBe("More options");
        toggle.GetAttribute("type").ShouldBe("button");

        // aria-controls naming an element that does not exist is an IDREF error.
        toggle.HasAttribute("aria-controls").ShouldBeFalse();
    }

    [Fact]
    public void Opening_ShowsANamedMenu_AndFocusesItsFirstItem()
    {
        var cut = Split();

        Toggle(cut).Click();

        var toggle = Toggle(cut);
        var menu = cut.Find("[role=menu]");

        toggle.GetAttribute("aria-expanded").ShouldBe("true");
        toggle.GetAttribute("aria-controls").ShouldBe(menu.Id);
        menu.GetAttribute("aria-labelledby").ShouldBe(toggle.Id);
        Items(cut).ShouldBe(["Save as draft", "Save and close", "Save a copy"]);
        LastFocused().ShouldBe(cut.FindAll("[role=menuitem]")[0].Id);
    }

    [Fact]
    public void TheMenuIsShown_BeforeFocusMovesIntoIt()
    {
        // Found in the browser, invisible here without this test: the split button's after-render
        // runs before the popover's, so focus was sent to an item in a panel still display: none,
        // and focus() ignored it. The panel has to be opened first.
        var popover = JSInterop.SetupModule("./_content/ZenithUI/js/zen-popover.js");
        popover.SetupVoid("open", _ => true).SetVoidResult();

        var openedFirst = new List<bool>();
        var dom = JSInterop.SetupModule("./_content/ZenithUI/js/zen-dom.js");
        dom.SetupVoid("focusElement", _ =>
        {
            openedFirst.Add(popover.Invocations["open"].Count > 0);
            return true;
        }).SetVoidResult();

        var cut = Split();
        Toggle(cut).Click();

        openedFirst.ShouldBe([true]);
    }

    [Fact]
    public void Items_AreOutOfTheTabOrder()
    {
        var cut = Split();
        Toggle(cut).Click();

        cut.FindAll("[role=menuitem]").ShouldAllBe(i => i.GetAttribute("tabindex") == "-1");
    }

    [Fact]
    public void ThePrimaryAction_DoesNotOpenTheMenu()
    {
        var cut = Split();

        cut.Find("button").Click();

        _primary.ShouldBe(1);
        cut.FindAll("[role=menu]").ShouldBeEmpty();
    }

    [Fact]
    public void ChoosingAnItem_RunsIt_ClosesTheMenu_AndReturnsFocusToTheToggle()
    {
        var cut = Split();
        Toggle(cut).Click();

        cut.FindAll("[role=menuitem]")[2].Click();

        _chosen.ShouldBe(["Save a copy"]);
        _primary.ShouldBe(0);
        cut.FindAll("[role=menu]").ShouldBeEmpty();
        LastFocused().ShouldBe(Toggle(cut).Id);
    }

    [Fact]
    public void ArrowDown_SkipsDisabledItems_AndWraps()
    {
        var cut = Split();
        Toggle(cut).Click();
        var items = cut.FindAll("[role=menuitem]");

        items[0].KeyDown(new KeyboardEventArgs { Key = "ArrowDown" });
        LastFocused().ShouldBe(items[2].Id);

        cut.FindAll("[role=menuitem]")[2].KeyDown(new KeyboardEventArgs { Key = "ArrowDown" });
        LastFocused().ShouldBe(items[0].Id);
    }

    [Fact]
    public void HomeAndEnd_JumpToTheEnabledEnds()
    {
        var cut = Split();
        Toggle(cut).Click();
        var items = cut.FindAll("[role=menuitem]");

        items[0].KeyDown(new KeyboardEventArgs { Key = "End" });
        LastFocused().ShouldBe(items[2].Id);

        cut.FindAll("[role=menuitem]")[2].KeyDown(new KeyboardEventArgs { Key = "Home" });
        LastFocused().ShouldBe(items[0].Id);
    }

    [Fact]
    public void ArrowUpOnTheToggle_OpensOnTheLastItem()
    {
        var cut = Split();

        Toggle(cut).KeyDown(new KeyboardEventArgs { Key = "ArrowUp" });

        LastFocused().ShouldBe(cut.FindAll("[role=menuitem]")[2].Id);
    }

    [Fact]
    public void Escape_ClosesTheMenu_AndReturnsFocusToTheToggle()
    {
        var cut = Split();
        Toggle(cut).Click();

        cut.FindAll("[role=menuitem]")[0].KeyDown(new KeyboardEventArgs { Key = "Escape" });

        cut.FindAll("[role=menu]").ShouldBeEmpty();
        LastFocused().ShouldBe(Toggle(cut).Id);
    }

    [Fact]
    public void Tab_ClosesTheMenu_WithoutPullingFocusBack()
    {
        var cut = Split();
        Toggle(cut).Click();
        var calls = _dom.Invocations["focusElement"].Count;

        cut.FindAll("[role=menuitem]")[0].KeyDown(new KeyboardEventArgs { Key = "Tab" });

        cut.FindAll("[role=menu]").ShouldBeEmpty();
        _dom.Invocations["focusElement"].Count.ShouldBe(calls);
    }

    [Fact]
    public void ADisabledItem_IsReallyDisabled()
    {
        var cut = Split();
        Toggle(cut).Click();

        cut.FindAll("[role=menuitem]")[1].HasAttribute("disabled").ShouldBeTrue();
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void DisabledOrLoading_DisablesBothHalves(bool disabled, bool loading)
    {
        var buttons = Split(p => p.Add(x => x.Disabled, disabled).Add(x => x.Loading, loading)).FindAll("button");

        buttons.Count.ShouldBe(2);
        buttons.ShouldAllBe(b => b.HasAttribute("disabled"));
    }

    [Fact]
    public void UnderStaticRendering_TheActionWorks_ButTheMenuIsDisabled()
    {
        // The menu needs JavaScript to position; the primary action is a plain button.
        Renderer.SetRendererInfo(new RendererInfo("Static", isInteractive: false));

        var buttons = Split().FindAll("button");

        buttons[0].HasAttribute("disabled").ShouldBeFalse();
        buttons[1].HasAttribute("disabled").ShouldBeTrue();
    }

    [Fact]
    public void AnItemWithHref_IsALinkInTheMenu()
    {
        var cut = Split(p => p.Add(x => x.MenuContent, b =>
        {
            b.OpenComponent<ZenMenuItem>(0);
            b.AddComponentParameter(1, nameof(ZenMenuItem.Href), "/history");
            b.AddComponentParameter(2, nameof(ZenMenuItem.ChildContent), (RenderFragment)(c => c.AddContent(0, "History")));
            b.CloseComponent();
        }));

        Toggle(cut).Click();

        var link = cut.Find("a[role=menuitem]");
        link.GetAttribute("href").ShouldBe("/history");
        link.GetAttribute("tabindex").ShouldBe("-1");
    }
}
