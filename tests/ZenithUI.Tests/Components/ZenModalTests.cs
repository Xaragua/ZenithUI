using Microsoft.Extensions.DependencyInjection;

namespace ZenithUI.Tests.Components;

/// <summary>
/// ZenModal, ZenModalService and ZenModalHost.
/// </summary>
/// <remarks>
/// The dialog's modality, focus trap and backdrop all come from the platform's
/// <c>&lt;dialog&gt;</c>, which bUnit does not implement - <c>showModal()</c> is an interop call
/// into a runtime that is not there. So these tests cover the parts that are ours: the awaitable
/// contract, the stack, the dismissal routes, and the markup and ARIA wiring.
/// </remarks>
public class ZenModalTests : BunitContext
{
    private readonly ZenModalService _modals = new();

    public ZenModalTests()
    {
        // Registration first: touching Renderer resolves from the provider, and bUnit seals it as
        // soon as anything has been retrieved.
        Services.AddSingleton<IZenModalService>(_modals);

        Renderer.SetRendererInfo(new RendererInfo("Server", isInteractive: true));

        // ZenModal imports zen-focus.js on its first interactive render and calls showModal().
        // Neither exists here, so interop is loose - bUnit records the calls and returns defaults
        // rather than failing the render.
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    private IZenModalService Modals => _modals;

    // ---- The awaitable contract ---------------------------------------------------------------

    [Fact]
    public void ShowAsync_DoesNotCompleteUntilTheDialogCloses()
    {
        // The whole point of the service. If this task completed early, every caller would read a
        // result the user has not given yet.
        var pending = Modals.ShowAsync<ZenConfirmDialog>();

        pending.IsCompleted.ShouldBeFalse();

        Modals.Modals.Single().Confirm();

        pending.IsCompletedSuccessfully.ShouldBeTrue();
    }

    [Fact]
    public async Task ConfirmedDialog_ReturnsItsPayload()
    {
        var pending = Modals.ShowAsync<ZenConfirmDialog>();

        Modals.Modals.Single().Confirm(42);

        var result = await pending;

        result.Confirmed.ShouldBeTrue();
        result.Data<int>().ShouldBe(42);
    }

    [Fact]
    public async Task CancelledDialog_IsNotConfirmed()
    {
        var pending = Modals.ShowAsync<ZenConfirmDialog>();

        Modals.Modals.Single().Cancel();

        (await pending).Confirmed.ShouldBeFalse();
    }

    [Fact]
    public async Task PayloadOfTheWrongType_ReadsAsDefaultRatherThanThrowing()
    {
        // A cancelled dialog has no payload at all, so callers must handle the empty case anyway.
        // Making a type mismatch throw would add a second path meaning the same thing.
        var pending = Modals.ShowAsync<ZenConfirmDialog>();

        Modals.Modals.Single().Confirm("not a number");

        (await pending).Data<int>().ShouldBe(0);
    }

    [Fact]
    public async Task ClosingTwice_IsIgnored()
    {
        // An ordinary race, not a defensive flourish: a dialog closed by its own button while a
        // backdrop click is already in flight reaches Close twice.
        var pending = Modals.ShowAsync<ZenConfirmDialog>();
        var modal = Modals.Modals.Single();

        modal.Confirm(1);
        modal.Confirm(2);

        (await pending).Data<int>().ShouldBe(1);
    }

    // ---- The stack ----------------------------------------------------------------------------

    [Fact]
    public void Dialogs_Stack()
    {
        // A confirmation raised from inside an editor has to appear above it, not replace it.
        _ = Modals.ShowAsync<ZenConfirmDialog>();
        _ = Modals.ShowAsync<ZenConfirmDialog>();

        Modals.Modals.Count.ShouldBe(2);
    }

    [Fact]
    public void ClosingOne_LeavesTheRest()
    {
        _ = Modals.ShowAsync<ZenConfirmDialog>();
        _ = Modals.ShowAsync<ZenConfirmDialog>();

        var top = Modals.Modals[1];
        top.Close();

        Modals.Modals.ShouldHaveSingleItem();
        Modals.Modals[0].ShouldNotBe(top);
    }

    [Fact]
    public async Task CloseAll_CancelsEveryAwaitingCaller()
    {
        // Navigation away from a page with a dialog open. Leaving callers awaiting forever is how
        // a "harmless" orphaned dialog turns into a stuck request.
        var first = Modals.ShowAsync<ZenConfirmDialog>();
        var second = Modals.ShowAsync<ZenConfirmDialog>();

        Modals.CloseAll();

        (await first).Confirmed.ShouldBeFalse();
        (await second).Confirmed.ShouldBeFalse();
        Modals.Modals.ShouldBeEmpty();
    }

    [Fact]
    public void Show_RejectsATypeThatIsNotAComponent() =>
        Should.Throw<ArgumentException>(() => Modals.ShowAsync(typeof(string)));

    // ---- The host -----------------------------------------------------------------------------

    [Fact]
    public void Host_RendersNothingUntilADialogIsShown() =>
        Render<ZenModalHost>().FindAll("dialog").ShouldBeEmpty();

    [Fact]
    public void Host_RendersAnOpenDialog()
    {
        var host = Render<ZenModalHost>();

        _ = Modals.ShowAsync<ZenConfirmDialog>(
            new Dictionary<string, object?> { ["Message"] = "Delete this order?" },
            new ZenModalOptions { Title = "Are you sure?" });

        host.Render();

        host.Markup.ShouldContain("Delete this order?");
        host.Markup.ShouldContain("Are you sure?");
    }

    [Fact]
    public void Host_RendersOneDialogPerOpenModal()
    {
        var host = Render<ZenModalHost>();

        _ = Modals.ShowAsync<ZenConfirmDialog>();
        _ = Modals.ShowAsync<ZenConfirmDialog>();

        host.Render();

        host.FindAll("dialog").Count.ShouldBe(2);
    }

    [Fact]
    public void Host_DropsNullParametersRatherThanCrashing()
    {
        // DynamicComponent.Parameters is IDictionary<string, object> - non-null values - while a
        // caller passing a null parameter is ordinary. An omitted parameter and one explicitly set
        // to null mean the same thing to a component.
        var host = Render<ZenModalHost>();

        _ = Modals.ShowAsync<ZenConfirmDialog>(
            new Dictionary<string, object?> { ["Message"] = "Hello", ["Options"] = null });

        Should.NotThrow(() => host.Render());
        host.Markup.ShouldContain("Hello");
    }

    // ---- Dismissal ----------------------------------------------------------------------------

    [Fact]
    public void Escape_ClosesByDefault()
    {
        var closed = false;

        var cut = Render<ZenModal>(p => p
            .Add(x => x.Open, true)
            .Add(x => x.OpenChanged, open => closed = !open));

        cut.Find("dialog").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        closed.ShouldBeTrue();
    }

    [Fact]
    public void Escape_IsRefusedWhenTheCallerSaysSo()
    {
        // CloseOnEscape="false" has to actually hold. The platform closes a <dialog> on Escape by
        // itself, so the component cancels that event and routes the key through C# - without
        // which this parameter would be a lie and the DOM would drift out of sync with the state.
        var closed = false;

        var cut = Render<ZenModal>(p => p
            .Add(x => x.Open, true)
            .Add(x => x.CloseOnEscape, false)
            .Add(x => x.OpenChanged, open => closed = !open));

        cut.Find("dialog").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        closed.ShouldBeFalse();
    }

    [Fact]
    public void CloseButton_AsksTheOwnerToClose()
    {
        var closed = false;

        var cut = Render<ZenModal>(p => p
            .Add(x => x.Open, true)
            .Add(x => x.Title, "Settings")
            .Add(x => x.OpenChanged, open => closed = !open));

        cut.Find("button[aria-label='Close']").Click();

        closed.ShouldBeTrue();
    }

    [Fact]
    public async Task ConfirmDialog_ReturnsTrueOnlyWhenConfirmed()
    {
        var host = Render<ZenModalHost>();
        var pending = Modals.ConfirmAsync("Delete", "This cannot be undone.");

        host.Render();
        host.Find("dialog button[id$='-confirm']").Click();

        (await pending).ShouldBeTrue();
    }

    [Fact]
    public async Task ConfirmDialog_CancelReturnsFalse()
    {
        var host = Render<ZenModalHost>();
        var pending = Modals.ConfirmAsync("Delete", "This cannot be undone.");

        host.Render();
        host.Find("dialog button[id$='-cancel']").Click();

        (await pending).ShouldBeFalse();
    }

    [Fact]
    public void ConfirmDialog_FocusesTheSafeButtonByDefault()
    {
        // A user who hits Enter out of habit, or who was mid-keystroke when the dialog appeared,
        // should not thereby delete something. The dialog asked a question; the safe answer is the
        // default.
        var host = Render<ZenModalHost>();

        _ = Modals.ConfirmAsync("Delete", "This cannot be undone.");
        host.Render();

        Modals.Modals.Single().Options.InitialFocusId.ShouldEndWith("-cancel");
    }

    // ---- Markup and ARIA ----------------------------------------------------------------------

    [Fact]
    public void Modal_IsANativeDialog()
    {
        // Not a div with role="dialog". showModal() brings the top layer, `inert` on everything
        // behind it, and ::backdrop - and the inert part is the one a keydown focus trap cannot
        // reproduce, because it also removes the background from the accessibility tree.
        var cut = Render<ZenModal>(p => p.Add(x => x.Open, true));

        cut.Find("dialog").ShouldNotBeNull();
    }

    [Fact]
    public void Modal_IsLabelledByItsVisibleTitle()
    {
        var cut = Render<ZenModal>(p => p
            .Add(x => x.Open, true)
            .Add(x => x.Title, "Edit order"));

        var labelledBy = cut.Find("dialog").GetAttribute("aria-labelledby");

        labelledBy.ShouldNotBeNull();
        cut.Find($"#{labelledBy}").TextContent.Trim().ShouldBe("Edit order");
    }

    [Fact]
    public void Modal_FallsBackToAriaLabelWithoutAVisibleTitle()
    {
        var cut = Render<ZenModal>(p => p
            .Add(x => x.Open, true)
            .Add(x => x.Label, "Quick actions"));

        var dialog = cut.Find("dialog");

        dialog.GetAttribute("aria-label").ShouldBe("Quick actions");
        dialog.HasAttribute("aria-labelledby").ShouldBeFalse();
    }

    [Fact]
    public void Modal_RendersItsFooterSlot()
    {
        var cut = Render<ZenModal>(p => p
            .Add(x => x.Open, true)
            .Add(x => x.Footer, (RenderFragment)(b => b.AddMarkupContent(0, "<button>Save</button>"))));

        cut.Markup.ShouldContain("Save");
    }
}
