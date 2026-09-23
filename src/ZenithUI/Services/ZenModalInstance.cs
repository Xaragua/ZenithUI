namespace ZenithUI;

/// <summary>
/// One open dialog: what to render, how, and the promise the opener is awaiting.
/// </summary>
/// <remarks>
/// <para>
/// Cascaded to the dialog's content, so a component rendered inside a modal can close itself with
/// a result without knowing anything about the host or the service:
/// <c>[CascadingParameter] ZenModalInstance Modal { get; set; }</c> then
/// <c>Modal.Close(ZenModalResult.Ok(order))</c>.
/// </para>
/// <para>
/// The completion source is the whole point of the design. It is what turns "raise a dialog, wait
/// for a state change, read a field" into <c>var result = await Modals.ShowAsync&lt;X&gt;()</c>.
/// </para>
/// </remarks>
public sealed class ZenModalInstance
{
    // RunContinuationsAsynchronously matters here. Without it, the continuation of the awaiting
    // caller runs inline on whichever thread completed the task - which is the renderer's thread,
    // in the middle of an event handler. That lets the caller's post-dialog work interleave with
    // the host's own teardown render, and the symptom is a dialog that occasionally fails to
    // disappear.
    private readonly TaskCompletionSource<ZenModalResult> _completion =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    private readonly Action<ZenModalInstance> _onClose;

    internal ZenModalInstance(
        Type componentType,
        IReadOnlyDictionary<string, object?> parameters,
        ZenModalOptions options,
        Action<ZenModalInstance> onClose)
    {
        ComponentType = componentType;
        Parameters = parameters;
        Options = options;
        _onClose = onClose;

        Span<char> guidChars = stackalloc char[32];
        _ = Guid.NewGuid().TryFormat(guidChars, out _, "N");
        Id = string.Concat("zen-modal-", guidChars[..8]);
    }

    /// <summary>Stable element id for this dialog, used for ARIA wiring and interop.</summary>
    public string Id { get; }

    /// <summary>The component type rendered as the dialog's body.</summary>
    public Type ComponentType { get; }

    /// <summary>Parameters passed to that component.</summary>
    public IReadOnlyDictionary<string, object?> Parameters { get; }

    /// <summary>Appearance and dismissal options.</summary>
    public ZenModalOptions Options { get; }

    /// <summary>Completes when the dialog closes, with whatever it returned.</summary>
    public Task<ZenModalResult> Result => _completion.Task;

    /// <summary>Whether this dialog has already closed.</summary>
    public bool IsClosed => _completion.Task.IsCompleted;

    /// <summary>Closes the dialog, handing <paramref name="result"/> back to the opener.</summary>
    /// <param name="result">What to return. Defaults to a cancellation.</param>
    /// <remarks>
    /// Safe to call more than once; later calls are ignored. That is not defensive padding - a
    /// dialog closed by its own button while a backdrop click is already in flight is an ordinary
    /// race, and <c>TrySetResult</c> is what keeps it from throwing.
    /// </remarks>
    public void Close(ZenModalResult? result = null)
    {
        if (!_completion.TrySetResult(result ?? ZenModalResult.Cancel()))
        {
            return;
        }

        _onClose(this);
    }

    /// <summary>Closes the dialog with a confirmed result carrying a value.</summary>
    /// <typeparam name="T">Type of the returned value.</typeparam>
    /// <param name="data">The value to hand back.</param>
    public void Confirm<T>(T data) => Close(ZenModalResult.Ok(data));

    /// <summary>Closes the dialog with a confirmed result carrying no value.</summary>
    public void Confirm() => Close(ZenModalResult.Ok());

    /// <summary>Closes the dialog as a dismissal.</summary>
    public void Cancel() => Close(ZenModalResult.Cancel());
}
