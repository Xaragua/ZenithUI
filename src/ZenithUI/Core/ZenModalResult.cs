namespace ZenithUI.Core;

/// <summary>
/// What a dialog returned to whoever opened it.
/// </summary>
/// <remarks>
/// <para>
/// Deliberately not generic. <c>ZenModalResult&lt;T&gt;</c> reads better at the call site right up
/// until a dialog needs to be closed from somewhere that does not know <c>T</c> - the close button
/// in the shared chrome, a backdrop click, Escape. Those paths would each need a non-generic escape
/// hatch, and the type parameter stops paying for itself. The payload is carried as
/// <see cref="object"/> and read back with <see cref="Data{T}"/>.
/// </para>
/// <para>
/// <see cref="Confirmed"/> is the part callers usually branch on, and it is distinct from "there is
/// data": a dialog can be confirmed with nothing to return, and a cancelled dialog may still want
/// to report a draft.
/// </para>
/// </remarks>
public sealed record ZenModalResult
{
    private ZenModalResult(bool confirmed, object? data)
    {
        Confirmed = confirmed;
        Payload = data;
    }

    /// <summary>
    /// Whether the user completed the dialog rather than dismissing it.
    /// </summary>
    /// <remarks>
    /// False for every dismissal route - the close button, Escape, a backdrop click, or the host
    /// tearing the dialog down during navigation. Code that acts on a dialog's outcome should test
    /// this rather than null-checking the payload, so a confirmation that returns nothing is not
    /// mistaken for a cancellation.
    /// </remarks>
    public bool Confirmed { get; }

    /// <summary>The value the dialog returned, if any.</summary>
    public object? Payload { get; }

    /// <summary>A confirmed result carrying no value.</summary>
    public static ZenModalResult Ok() => new(true, null);

    /// <summary>A confirmed result carrying a value.</summary>
    /// <typeparam name="T">Type of the returned value.</typeparam>
    /// <param name="data">The value to hand back to the caller.</param>
    public static ZenModalResult Ok<T>(T data) => new(true, data);

    /// <summary>A dismissal.</summary>
    public static ZenModalResult Cancel() => new(false, null);

    /// <summary>
    /// The payload as <typeparamref name="T"/>, or <see langword="default"/> when it is absent or
    /// of another type.
    /// </summary>
    /// <typeparam name="T">The expected payload type.</typeparam>
    /// <remarks>
    /// Returns default rather than throwing on a type mismatch. A dialog that was cancelled has no
    /// payload at all, so callers have to handle the empty case regardless; making the mismatch
    /// throw would only add a second path that means the same thing.
    /// </remarks>
    public T? Data<T>() => Payload is T value ? value : default;
}
