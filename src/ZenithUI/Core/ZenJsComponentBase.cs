namespace ZenithUI.Core;

/// <summary>
/// Base class for ZenithUI components backed by a JavaScript module. Encodes the rules that keep
/// the library render-mode agnostic, so individual components cannot get them wrong.
/// </summary>
/// <remarks>
/// <para>
/// Three hazards this class exists to remove:
/// </para>
/// <list type="number">
/// <item>
/// <description>
/// <b>Calling JS during prerender.</b> Under static SSR or the prerender pass of an interactive
/// render mode there is no JavaScript runtime attached, and any interop call throws. The module
/// is therefore imported in <see cref="OnAfterRenderAsync"/> and only after
/// <see cref="ComponentBase.RendererInfo"/> reports an interactive renderer.
/// </description>
/// </item>
/// <item>
/// <description>
/// <b>Leaking module references.</b> Every imported module is disposed with the component.
/// </description>
/// </item>
/// <item>
/// <description>
/// <b>Throwing on teardown.</b> When a Blazor Server circuit drops, disposal races the
/// disconnect and interop throws <see cref="JSDisconnectedException"/>; on WebAssembly a
/// navigating page produces the same shape of failure. Both are swallowed - there is nothing to
/// clean up on a runtime that is already gone.
/// </description>
/// </item>
/// </list>
/// <para>
/// A component deriving from this class must still render meaningful HTML before
/// <see cref="OnModuleReadyAsync"/> ever runs: that markup is what a search engine, a
/// no-JavaScript client, and the prerender pass all see.
/// </para>
/// </remarks>
public abstract class ZenJsComponentBase : ZenComponentBase, IAsyncDisposable
{
    private IJSObjectReference? _module;
    private bool _disposing;
    private bool _disposed;

    /// <summary>The JavaScript runtime for the current render mode.</summary>
    [Inject]
    protected IJSRuntime JS { get; set; } = default!;

    /// <summary>
    /// Path of the ES module backing this component, for example
    /// <c>./_content/ZenithUI/js/zen-popover.js</c>. Return <see langword="null"/> to opt out of
    /// module loading entirely.
    /// </summary>
    protected abstract string? ModulePath { get; }

    /// <summary>
    /// The imported module, or <see langword="null"/> before the first interactive render.
    /// Always null-check: a component may render several times before the module resolves, and
    /// under static SSR it never resolves at all.
    /// </summary>
    protected IJSObjectReference? Module => _module;

    /// <summary>
    /// <see langword="true"/> once the module has loaded and JavaScript may be called.
    /// </summary>
    protected bool IsJsReady => _module is not null && !_disposed;

    /// <summary>
    /// Called once, after the module has been imported, on the first interactive render.
    /// Override to perform initial setup such as registering listeners or measuring the DOM.
    /// </summary>
    protected virtual Task OnModuleReadyAsync() => Task.CompletedTask;

    /// <summary>
    /// Called from <see cref="DisposeAsync"/> before the module reference is released, while
    /// interop is still usable. Override to tear down anything registered in
    /// <see cref="OnModuleReadyAsync"/>.
    /// </summary>
    protected virtual Task OnModuleDisposingAsync() => Task.CompletedTask;

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        if (!firstRender || _disposed)
        {
            return;
        }

        // RendererInfo.IsInteractive is false during static SSR and during the prerender pass of
        // an interactive render mode. Bailing out here is what lets the same component be used
        // under InteractiveServer, InteractiveWebAssembly, InteractiveAuto and no render mode at
        // all without the caller configuring anything.
        if (!RendererInfo.IsInteractive)
        {
            return;
        }

        var path = ModulePath;

        if (string.IsNullOrEmpty(path))
        {
            await OnModuleReadyAsync();
            return;
        }

        try
        {
            _module = await JS.InvokeAsync<IJSObjectReference>("import", path);
        }
        catch (JSDisconnectedException)
        {
            // The circuit went away while the import was in flight.
            return;
        }
        catch (OperationCanceledException)
        {
            // The component was disposed, or the page navigated, mid-import.
            return;
        }

        if (_disposed)
        {
            return;
        }

        await OnModuleReadyAsync();
        StateHasChanged();
    }

    /// <summary>
    /// Invokes a module function, silently skipping the call when JavaScript is unavailable
    /// (prerender, static SSR, or a torn-down circuit).
    /// </summary>
    /// <param name="identifier">The exported function name.</param>
    /// <param name="args">Arguments to pass.</param>
    protected async ValueTask InvokeModuleVoidAsync(string identifier, params object?[] args)
    {
        if (_module is null || _disposed)
        {
            return;
        }

        try
        {
            await _module.InvokeVoidAsync(identifier, args);
        }
        catch (JSDisconnectedException)
        {
        }
        catch (OperationCanceledException)
        {
        }
    }

    /// <summary>
    /// Invokes a module function that returns a value, yielding <see langword="default"/> when
    /// JavaScript is unavailable.
    /// </summary>
    /// <typeparam name="TResult">The expected return type.</typeparam>
    /// <param name="identifier">The exported function name.</param>
    /// <param name="args">Arguments to pass.</param>
    protected async ValueTask<TResult?> InvokeModuleAsync<TResult>(string identifier, params object?[] args)
    {
        if (_module is null || _disposed)
        {
            return default;
        }

        try
        {
            return await _module.InvokeAsync<TResult>(identifier, args);
        }
        catch (JSDisconnectedException)
        {
            return default;
        }
        catch (OperationCanceledException)
        {
            return default;
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await DisposeAsyncCore();
        GC.SuppressFinalize(this);
    }

    /// <summary>Releases the module reference and any component-specific resources.</summary>
    protected virtual async ValueTask DisposeAsyncCore()
    {
        if (_disposed || _disposing)
        {
            return;
        }

        // OnModuleDisposingAsync is allowed to call back into JavaScript, so `_disposed` is not
        // set until after it runs - otherwise InvokeModuleVoidAsync would short-circuit and a
        // derived component could never tear down what it registered. `_disposing` guards
        // against re-entry in the meantime.
        _disposing = true;

        if (_module is null)
        {
            _disposed = true;
            return;
        }

        try
        {
            await OnModuleDisposingAsync();
            _disposed = true;
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
}
