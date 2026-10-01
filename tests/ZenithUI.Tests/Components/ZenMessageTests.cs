namespace ZenithUI.Tests.Components;

/// <summary>
/// ZenMessage. Most of what matters is what it does NOT do by default - claim a live region - and
/// that dismissal works both when the message owns its visibility and when the page does.
/// </summary>
public class ZenMessageTests : BunitContext
{
    public ZenMessageTests() =>
        Renderer.SetRendererInfo(new RendererInfo("Server", isInteractive: true));

    private IRenderedComponent<ZenMessage> Message(
        Action<ComponentParameterCollectionBuilder<ZenMessage>>? extra = null) =>
        Render<ZenMessage>(p =>
        {
            p.Add(x => x.ChildContent, "Your trial ends in three days.");
            extra?.Invoke(p);
        });

    /// <summary>A path as the DOM serialises it, which is not how the constant spells it.</summary>
    private string Glyph(string path) => Render<ZenIcon>(p => p.Add(x => x.Path, path)).Find("svg").InnerHtml;

    [Fact]
    public void ByDefault_ItIsContent_NotALiveRegion()
    {
        // A message present when the page loads is read when the user reaches it, not announced
        // over whatever they were listening to.
        Message(p => p.Add(x => x.Intent, ZenIntent.Danger)).Find("div").HasAttribute("role").ShouldBeFalse();
    }

    [Theory]
    [InlineData(ZenIntent.Danger, "alert")]
    [InlineData(ZenIntent.Warning, "alert")]
    [InlineData(ZenIntent.Success, "status")]
    [InlineData(ZenIntent.Info, "status")]
    [InlineData(ZenIntent.Neutral, "status")]
    public void Announce_InterruptsOnlyForDangerAndWarning(ZenIntent intent, string role) =>
        Message(p => p.Add(x => x.Intent, intent).Add(x => x.Announce, true))
            .Find("div").GetAttribute("role").ShouldBe(role);

    [Theory]
    [InlineData(ZenIntent.Info, ZenIcons.Info)]
    [InlineData(ZenIntent.Success, ZenIcons.Success)]
    [InlineData(ZenIntent.Warning, ZenIcons.Warning)]
    [InlineData(ZenIntent.Danger, ZenIcons.Danger)]
    [InlineData(ZenIntent.Primary, ZenIcons.Info)]
    public void TheIcon_FollowsTheIntent_AndIsHiddenFromAssistiveTechnology(ZenIntent intent, string path)
    {
        var svg = Message(p => p.Add(x => x.Intent, intent)).Find("svg");

        svg.InnerHtml.ShouldBe(Glyph(path));
        svg.GetAttribute("aria-hidden").ShouldBe("true");
    }

    [Fact]
    public void Icon_ReplacesTheIntentsGlyph_AndShowIconFalseRemovesIt()
    {
        Message(p => p.Add(x => x.Icon, ZenIcons.Search)).Find("svg").InnerHtml.ShouldBe(Glyph(ZenIcons.Search));
        Message(p => p.Add(x => x.ShowIcon, false)).FindAll("svg").ShouldBeEmpty();
    }

    [Fact]
    public void TitleAndActions_RenderAroundTheText()
    {
        var cut = Message(p => p
            .Add(x => x.Title, "Trial ending")
            .Add(x => x.Actions, b =>
            {
                b.OpenElement(0, "a");
                b.AddAttribute(1, "href", "/billing");
                b.AddContent(2, "Upgrade");
                b.CloseElement();
            }));

        cut.Find("p.font-semibold").TextContent.ShouldBe("Trial ending");
        cut.Find("a[href='/billing']").TextContent.ShouldBe("Upgrade");
    }

    [Fact]
    public void ANonDismissibleMessage_HasNoCloseButton() =>
        Message().FindAll("button").ShouldBeEmpty();

    [Fact]
    public void Dismissing_HidesTheMessage_WhenThePageDoesNotOwnVisibility()
    {
        var dismissed = 0;
        var cut = Message(p => p.Add(x => x.Dismissible, true).Add(x => x.OnDismiss, () => dismissed++));

        var close = cut.Find("button");
        close.GetAttribute("aria-label").ShouldBe("Dismiss");
        close.GetAttribute("type").ShouldBe("button");

        close.Click();

        cut.Markup.Trim().ShouldBeEmpty();
        dismissed.ShouldBe(1);
    }

    [Fact]
    public void Dismissing_AsksThePage_WhenItOwnsVisibility()
    {
        // With VisibleChanged bound, the message must not hide itself: the page may want to keep
        // it - a validation summary that should stay until the errors are fixed.
        bool? requested = null;
        var cut = Message(p => p
            .Add(x => x.Dismissible, true)
            .Add(x => x.Visible, true)
            .Add(x => x.VisibleChanged, v => requested = v));

        cut.Find("button").Click();

        requested.ShouldBe(false);
        cut.FindAll("button").Count.ShouldBe(1);
    }

    [Fact]
    public void ASelfDismissedMessage_ComesBack_WhenThePageShowsItAgain()
    {
        var cut = Message(p => p.Add(x => x.Dismissible, true));

        cut.Find("button").Click();
        cut.Render(p => p.Add(x => x.Visible, false));
        cut.Render(p => p.Add(x => x.Visible, true));

        cut.FindAll("button").Count.ShouldBe(1);
    }

    [Fact]
    public void Solid_LetsThePanelColourTheText()
    {
        // On a fill, the text must be the fill's -content colour, inherited from the panel. An
        // explicit text-content on the title would put dark text on a dark fill in one palette.
        var cut = Message(p => p
            .Add(x => x.Intent, ZenIntent.Danger)
            .Add(x => x.Variant, ZenVariant.Solid)
            .Add(x => x.Title, "Failed"));

        cut.Find("div").ClassList.ShouldContain("text-danger-content");
        cut.Find("p").ClassList.ShouldNotContain("text-content");
    }
}
