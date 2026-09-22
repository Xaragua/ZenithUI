using ZenithUI.Core;

namespace ZenithUI.Services;

/// <summary>
/// Default <see cref="IZenThemeService"/>. Delegates all DOM and storage work to the
/// <c>zen-theme.js</c> module and caches the resulting state for synchronous reads from markup.
/// </summary>
public sealed class ZenThemeService : IZenThemeService, IAsyncDisposable
{
    private const string ModulePath = "./_content/ZenithUI/js/zen-theme.js";

    private readonly IJSRuntime _js;
    private readonly SemaphoreSlim _initGate = new(1, 1);

    private IJSObjectReference? _module;
    private DotNetObjectReference<ZenThemeService>? _selfRef;
    private bool _disposed;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="js">The JavaScript runtime for the current render mode.</param>
    public ZenThemeService(IJSRuntime js) => _js = js;

    /// <inheritdoc />
    public ZenThemeMode Mode { get; private set; } = ZenThemeMode.System;

    /// <inheritdoc />
    public ZenTheme Theme { get; private set; } = ZenTheme.Light;

    /// <inheritdoc />
    public bool IsInitialized { get; private set; }

    /// <inheritdoc />
    public event EventHandler<ZenThemeChangedEventArgs>? ThemeChanged;

    /// <inheritdoc />
    public async Task InitializeAsync()
    {
        if (IsInitialized || _disposed)
        {
            return;
        }

        // ZenThemeProvider calls this from OnAfterRenderAsync, but an application is free to call
        // it from elsewhere and a page may host more than one consumer. The gate makes concurrent
        // first calls collapse into a single module import instead of racing.
        await _initGate.WaitAsync();

        try
        {
            if (IsInitialized || _disposed)
            {
                return;
            }

            _module = await _js.InvokeAsync<IJSObjectReference>("import", ModulePath);
            _selfRef = DotNetObjectReference.Create(this);

            var state = await _module.InvokeAsync<ZenThemeState>("initialize", _selfRef);

            // IsInitialized is set BEFORE ApplyState, because ApplyState raises ThemeChanged and a
            // subscriber reading IsInitialized from that handler must not be told "still
            // prerendering" while being handed the real, browser-resolved state.
            IsInitialized = true;
            ApplyState(state);
        }
        catch (JSException)
        {
            // Thrown when interop is attempted on a renderer that has no JavaScript runtime -
            // static SSR and the prerender pass. Staying uninitialized is the correct outcome:
            // the inline FOUC script has already painted the right palette, and initialization
            // will succeed on the interactive render that follows.
        }
        catch (InvalidOperationException)
        {
            // Same situation, different exception type depending on the hosting model.
        }
        catch (JSDisconnectedException)
        {
            // The circuit dropped mid-import.
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            _initGate.Release();
        }
    }

    /// <inheritdoc />
    public async Task SetModeAsync(ZenThemeMode mode)
    {
        if (_disposed)
        {
            return;
        }

        if (_module is null)
        {
            // A caller reached a mutation before anything initialized the service. Rather than
            // quietly updating only the in-memory state - which leaves the control looking
            // switched while the page never repaints - try to initialize now. On an interactive
            // renderer this succeeds and the write below lands in the DOM.
            await InitializeAsync();
        }

        if (_module is null)
        {
            // Still no JavaScript: this is a prerender or static SSR pass. Record the intent so
            // markup rendered now is self-consistent; the browser is updated once the module loads.
            UpdateState(mode, Resolve(mode));
            return;
        }

        try
        {
            var state = await _module.InvokeAsync<ZenThemeState>("setMode", ToToken(mode));
            ApplyState(state);
        }
        catch (JSDisconnectedException)
        {
        }
        catch (OperationCanceledException)
        {
        }
    }

    /// <inheritdoc />
    public Task ToggleAsync() =>
        // Toggling out of System resolves to the opposite of what the user currently sees, not
        // the opposite of the mode. Pressing a toggle while following a dark OS should produce
        // light, which "invert the mode" would get wrong.
        SetModeAsync(Theme == ZenTheme.Dark ? ZenThemeMode.Light : ZenThemeMode.Dark);

    /// <summary>
    /// Invoked from JavaScript whenever the theme changes, for any reason. Not part of the public
    /// API - it is public only because <see cref="JSInvokableAttribute"/> requires it.
    /// </summary>
    /// <param name="mode">The mode now selected: <c>"system"</c>, <c>"light"</c> or <c>"dark"</c>.</param>
    /// <param name="theme">The resolved palette: <c>"light"</c> or <c>"dark"</c>.</param>
    /// <remarks>
    /// Two situations reach here. The obvious one is the operating system flipping its preference
    /// while the app follows it. The other is another island on the same page changing the theme:
    /// a Blazor Web App can host an Interactive Server island and an Interactive WebAssembly
    /// island simultaneously, each with its own DI container and therefore its own instance of
    /// this service. They share one JavaScript module, which broadcasts to all of them - without
    /// that, a toggle in one island would keep displaying a stale selection while the page around
    /// it had already repainted.
    /// </remarks>
    [JSInvokable]
    public void OnThemeChangedFromJs(string mode, string theme)
    {
        if (_disposed)
        {
            return;
        }

        // UpdateState is a no-op when nothing actually differs, so the echo of this instance's own
        // SetModeAsync call costs nothing and needs no "who asked" bookkeeping in the module.
        UpdateState(ParseMode(mode), ParseTheme(theme));
    }

    private void ApplyState(ZenThemeState state) =>
        UpdateState(ParseMode(state.Mode), ParseTheme(state.Theme));

    private void UpdateState(ZenThemeMode mode, ZenTheme theme)
    {
        if (Mode == mode && Theme == theme && IsInitialized)
        {
            return;
        }

        Mode = mode;
        Theme = theme;

        ThemeChanged?.Invoke(this, new ZenThemeChangedEventArgs(mode, theme));
    }

    private static ZenTheme Resolve(ZenThemeMode mode) => mode switch
    {
        ZenThemeMode.Dark => ZenTheme.Dark,
        ZenThemeMode.Light => ZenTheme.Light,

        // The server cannot see prefers-color-scheme. Light is the documented pre-initialization
        // assumption; the inline script has already painted the truth.
        _ => ZenTheme.Light,
    };

    private static string ToToken(ZenThemeMode mode) => mode switch
    {
        ZenThemeMode.Light => "light",
        ZenThemeMode.Dark => "dark",
        _ => "system",
    };

    private static ZenThemeMode ParseMode(string? value) => value switch
    {
        "light" => ZenThemeMode.Light,
        "dark" => ZenThemeMode.Dark,
        _ => ZenThemeMode.System,
    };

    private static ZenTheme ParseTheme(string? value) =>
        value == "dark" ? ZenTheme.Dark : ZenTheme.Light;

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        ThemeChanged = null;

        if (_module is not null)
        {
            try
            {
                // Pass this instance's reference so the module unregisters only this listener.
                // Disposing one island must not tear down the shared media-query listener that
                // other islands on the page are still relying on.
                await _module.InvokeVoidAsync("dispose", _selfRef);
                await _module.DisposeAsync();
            }
            catch (JSDisconnectedException)
            {
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                _module = null;
            }
        }

        _selfRef?.Dispose();
        _selfRef = null;
        _initGate.Dispose();
    }

    /// <summary>Shape of the object returned by the JavaScript module.</summary>
    private sealed record ZenThemeState(string Mode, string Theme);
}
