namespace ZenithUI.Tests.Components;

/// <summary>
/// In-memory <see cref="IZenThemeService"/> for component tests.
/// </summary>
/// <remarks>
/// Substituting the service rather than mocking its JavaScript module keeps these tests about the
/// components: what they render, which ARIA attributes they carry, and which service calls a click
/// produces. The real service's interop behaviour is a separate concern.
/// </remarks>
public sealed class FakeThemeService : IZenThemeService
{
    /// <summary>Every mode passed to <see cref="SetModeAsync"/>, in order.</summary>
    public List<ZenThemeMode> SetModeCalls { get; } = [];

    /// <summary>How many times <see cref="ToggleAsync"/> was called.</summary>
    public int ToggleCalls { get; private set; }

    /// <summary>How many times <see cref="InitializeAsync"/> was called.</summary>
    public int InitializeCalls { get; private set; }

    /// <inheritdoc />
    public ZenThemeMode Mode { get; private set; } = ZenThemeMode.System;

    /// <inheritdoc />
    public ZenTheme Theme { get; private set; } = ZenTheme.Light;

    /// <inheritdoc />
    public bool IsInitialized { get; private set; }

    /// <inheritdoc />
    public event EventHandler<ZenThemeChangedEventArgs>? ThemeChanged;

    /// <inheritdoc />
    public Task InitializeAsync()
    {
        InitializeCalls++;
        IsInitialized = true;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task SetModeAsync(ZenThemeMode mode)
    {
        SetModeCalls.Add(mode);
        Apply(mode, mode switch
        {
            ZenThemeMode.Dark => ZenTheme.Dark,
            ZenThemeMode.Light => ZenTheme.Light,
            _ => ZenTheme.Light,
        });

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task ToggleAsync()
    {
        ToggleCalls++;
        return SetModeAsync(Theme == ZenTheme.Dark ? ZenThemeMode.Light : ZenThemeMode.Dark);
    }

    /// <summary>Drives a change from outside, as the real service does on a system preference change.</summary>
    public void Apply(ZenThemeMode mode, ZenTheme theme)
    {
        Mode = mode;
        Theme = theme;
        ThemeChanged?.Invoke(this, new ZenThemeChangedEventArgs(mode, theme));
    }

    /// <summary><see langword="true"/> when every subscriber has detached.</summary>
    public bool HasNoSubscribers => ThemeChanged is null;
}
