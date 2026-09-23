using Microsoft.Extensions.DependencyInjection;

namespace ZenithUI.Tests.Components;

/// <summary>
/// ZenToast, ZenToastService and ZenToastHost.
/// </summary>
/// <remarks>
/// The auto-dismiss timer is real wall-clock time, so the tests that touch it use durations short
/// enough to wait on rather than mocking <see cref="Timer"/>. Everything else - the stack, the cap,
/// the announcement semantics - is synchronous.
/// </remarks>
public class ZenToastTests : BunitContext
{
    private readonly ZenToastService _toasts = new();

    public ZenToastTests()
    {
        Services.AddSingleton<IZenToastService>(_toasts);
        Renderer.SetRendererInfo(new RendererInfo("Server", isInteractive: true));
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    // ---- Announcement semantics ---------------------------------------------------------------

    [Fact]
    public void Errors_Interrupt_EverythingElseWaits()
    {
        // Not cosmetic. An assertive live region cuts off whatever a screen reader is currently
        // reading, which is right for a failure and rude for "Saved". Backwards, this either
        // buries errors or talks over the user constantly.
        var danger = _toasts.Danger("Upload failed");
        var success = _toasts.Success("Saved");

        danger.Role.ShouldBe("alert");
        danger.Politeness.ShouldBe("assertive");

        success.Role.ShouldBe("status");
        success.Politeness.ShouldBe("polite");
    }

    [Fact]
    public void Errors_DoNotAutoDismiss()
    {
        // An error that disappears on a timer is an error the user may never have seen, and there
        // is no way to bring it back.
        _toasts.Danger("Upload failed").Options.Duration.ShouldBe(TimeSpan.Zero);

        // Still overridable for a caller who really wants one.
        _toasts.Danger("Transient", new ZenToastOptions { Duration = TimeSpan.FromSeconds(2) })
            .Options.Duration.ShouldBe(TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void OtherIntents_AutoDismissByDefault() =>
        _toasts.Success("Saved").Options.Duration.ShouldBe(TimeSpan.FromSeconds(5));

    // ---- The stack ----------------------------------------------------------------------------

    [Fact]
    public void Toasts_Stack_OldestFirst()
    {
        _toasts.Info("first");
        _toasts.Info("second");

        _toasts.Toasts.Select(t => t.Message).ShouldBe(["first", "second"]);
    }

    [Fact]
    public void TheStack_IsCapped_DroppingTheOldest()
    {
        // A loop that fails once per item raises a toast per item. A stack tall enough to cover the
        // page hides the very UI needed to fix the problem.
        _toasts.MaxVisible = 3;

        for (var i = 1; i <= 6; i++)
        {
            _toasts.Info($"message {i}");
        }

        _toasts.Toasts.Count.ShouldBe(3);
        _toasts.Toasts.Select(t => t.Message).ShouldBe(["message 4", "message 5", "message 6"]);
    }

    [Fact]
    public void EvictedToasts_StillRunTheirDismissCallback()
    {
        // Evicted, not deleted. A caller tracking outstanding notifications would otherwise leak
        // one every time the cap bites.
        var dismissed = false;
        _toasts.MaxVisible = 1;

        _toasts.Info("first", new ZenToastOptions { OnDismissed = () => { dismissed = true; return Task.CompletedTask; } });
        _toasts.Info("second");

        dismissed.ShouldBeTrue();
    }

    [Fact]
    public void DismissingTwice_IsIgnored()
    {
        var count = 0;
        var toast = _toasts.Info("once", new ZenToastOptions { OnDismissed = () => { count++; return Task.CompletedTask; } });

        toast.Dismiss();
        toast.Dismiss();

        count.ShouldBe(1);
        toast.IsDismissed.ShouldBeTrue();
    }

    [Fact]
    public void Clear_RemovesEverything()
    {
        _toasts.Info("a");
        _toasts.Info("b");

        _toasts.Clear();

        _toasts.Toasts.ShouldBeEmpty();
    }

    // ---- The host -----------------------------------------------------------------------------

    [Fact]
    public void Host_RendersNothingWhenIdle() =>
        Render<ZenToastHost>().FindAll(".zen-toast").ShouldBeEmpty();

    [Fact]
    public void Host_RendersEachToastWithItsRole()
    {
        var host = Render<ZenToastHost>();

        _toasts.Danger("Upload failed");
        _toasts.Success("Saved");

        host.Render();

        host.FindAll(".zen-toast").Count.ShouldBe(2);
        host.Find("[role='alert']").TextContent.ShouldContain("Upload failed");
        host.Find("[role='status']").TextContent.ShouldContain("Saved");
    }

    [Fact]
    public void Host_LetsClicksThroughItsEmptyArea()
    {
        // The container is fixed and full-width. Without pointer-events-none on it (and auto back
        // on each toast) an invisible column sits over the page swallowing clicks on whatever is
        // behind it - a bug nobody reports as "the toast host", they report it as "this button
        // sometimes does nothing".
        var host = Render<ZenToastHost>();

        _toasts.Info("hello");
        host.Render();

        host.Find(".zen-toast-host").ClassList.ShouldContain("pointer-events-none");
        host.Find(".zen-toast").ClassList.ShouldContain("pointer-events-auto");
    }

    [Fact]
    public void Soft_TintsTheSurfaceWithItsIntent()
    {
        var host = Render<ZenToastHost>();

        _toasts.Success("Saved", new ZenToastOptions { Soft = true });
        host.Render();

        var classes = host.Find(".zen-toast").ClassList;

        classes.ShouldContain("bg-success-soft");
        classes.ShouldNotContain("bg-surface-overlay");
    }

    [Fact]
    public void TheNeutralSurface_IsTheDefault()
    {
        var host = Render<ZenToastHost>();

        _toasts.Success("Saved");
        host.Render();

        host.Find(".zen-toast").ClassList.ShouldContain("bg-surface-overlay");
    }

    [Fact]
    public void ANeutralToast_StaysOnTheOverlaySurfaceEvenWhenSoft()
    {
        // ZenIntent.Neutral has no -soft token, so there is nothing to tint with. Falling through
        // to ZenStyles.SoftSurface would hand back bg-surface-sunken - a *recessed* surface for a
        // panel that floats above the page.
        var host = Render<ZenToastHost>();

        _toasts.Show("Plain", ZenIntent.Neutral, new ZenToastOptions { Soft = true });
        host.Render();

        host.Find(".zen-toast").ClassList.ShouldContain("bg-surface-overlay");
    }

    [Fact]
    public void CloseButton_DismissesTheToast()
    {
        var host = Render<ZenToastHost>();

        _toasts.Info("hello");
        host.Render();
        host.Find("button[aria-label='Dismiss']").Click();

        _toasts.Toasts.ShouldBeEmpty();
    }

    [Fact]
    public void ActionButton_RunsAndThenDismisses()
    {
        // A toast that stays put once its button is pressed reads as a click that did not register.
        var ran = false;
        var host = Render<ZenToastHost>();

        _toasts.Info("Item deleted", new ZenToastOptions
        {
            ActionText = "Undo",
            OnAction = () => { ran = true; return Task.CompletedTask; },
        });

        host.Render();
        host.FindAll("button").First(b => b.TextContent.Contains("Undo")).Click();

        ran.ShouldBeTrue();
        _toasts.Toasts.ShouldBeEmpty();
    }

    // ---- Auto-dismiss -------------------------------------------------------------------------

    [Fact]
    public async Task AToast_DismissesItselfWhenItsTimeIsUp()
    {
        var host = Render<ZenToastHost>();

        _toasts.Info("brief", new ZenToastOptions { Duration = TimeSpan.FromMilliseconds(80) });
        host.Render();

        await Task.Delay(400);

        _toasts.Toasts.ShouldBeEmpty();
    }

    [Fact]
    public async Task AZeroDuration_StaysPut()
    {
        var host = Render<ZenToastHost>();

        _toasts.Info("sticky", new ZenToastOptions { Duration = TimeSpan.Zero });
        host.Render();

        await Task.Delay(300);

        _toasts.Toasts.ShouldHaveSingleItem();
    }

    [Fact]
    public async Task Focus_PausesTheTimer_NotJustHover()
    {
        // Hover alone is the version most libraries ship, and it fails the two users who need it
        // most: someone reading with a screen reader, and someone tabbing to the action button.
        // Both hold focus with no pointer over the toast, and both would watch it vanish.
        var host = Render<ZenToastHost>();

        _toasts.Info("reading this", new ZenToastOptions { Duration = TimeSpan.FromMilliseconds(120) });
        host.Render();

        host.Find(".zen-toast").FocusIn();

        await Task.Delay(400);

        _toasts.Toasts.ShouldHaveSingleItem();
    }

    [Fact]
    public async Task LeavingTheToast_RestartsTheClock()
    {
        var host = Render<ZenToastHost>();

        _toasts.Info("hover me", new ZenToastOptions { Duration = TimeSpan.FromMilliseconds(120) });
        host.Render();

        var element = host.Find(".zen-toast");
        element.MouseEnter();
        await Task.Delay(250);

        // Still there: the timer was cancelled on enter.
        _toasts.Toasts.ShouldHaveSingleItem();

        element.MouseLeave();
        await Task.Delay(400);

        _toasts.Toasts.ShouldBeEmpty();
    }
}
