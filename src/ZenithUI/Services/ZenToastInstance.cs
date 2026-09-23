namespace ZenithUI;

/// <summary>One toast on screen.</summary>
/// <remarks>
/// Unlike <see cref="ZenModalInstance"/> this carries no completion source. A toast is told, not
/// asked: nothing awaits its outcome, and an API that let callers await one would invite exactly
/// the blocking-on-a-notification pattern toasts exist to avoid.
/// </remarks>
public sealed class ZenToastInstance
{
    private readonly Action<ZenToastInstance> _onDismiss;

    internal ZenToastInstance(
        string message,
        ZenIntent intent,
        ZenToastOptions options,
        Action<ZenToastInstance> onDismiss)
    {
        Message = message;
        Intent = intent;
        Options = options;
        _onDismiss = onDismiss;

        Span<char> guidChars = stackalloc char[32];
        _ = Guid.NewGuid().TryFormat(guidChars, out _, "N");
        Id = string.Concat("zen-toast-", guidChars[..8]);
    }

    /// <summary>Stable element id, for ARIA wiring and the dismiss animation.</summary>
    public string Id { get; }

    /// <summary>The body text.</summary>
    public string Message { get; }

    /// <summary>Colour role, which also decides the icon and the ARIA live politeness.</summary>
    public ZenIntent Intent { get; }

    /// <summary>Duration, action and dismissal options.</summary>
    public ZenToastOptions Options { get; }

    /// <summary>Whether this toast has already left.</summary>
    public bool IsDismissed { get; private set; }

    /// <summary>
    /// How an error is announced versus everything else.
    /// </summary>
    /// <remarks>
    /// <c>alert</c> for danger, <c>status</c> for the rest. The distinction is not cosmetic: an
    /// assertive live region interrupts whatever a screen reader is currently reading, which is
    /// correct for a failure and rude for "Saved". Getting this backwards either buries errors or
    /// makes routine confirmations talk over the user.
    /// </remarks>
    public string Role => Intent == ZenIntent.Danger ? "alert" : "status";

    /// <summary>Matching politeness for the live region.</summary>
    public string Politeness => Intent == ZenIntent.Danger ? "assertive" : "polite";

    /// <summary>Removes the toast. Safe to call more than once.</summary>
    /// <remarks>
    /// <para>
    /// The <see cref="ZenToastOptions.OnDismissed"/> callback fires here rather than in
    /// <c>ZenToast</c>. A toast can leave without the component ever having rendered it - evicted
    /// by the visible cap in the same call that created it - and a callback owned by the component
    /// would simply be lost in that case. Dismissal is a fact about the toast, not about its
    /// rendering.
    /// </para>
    /// <para>
    /// The callback is started and not awaited. This is called from event handlers and from a
    /// timer thread, and a caller's slow continuation must not hold up either.
    /// </para>
    /// </remarks>
    public void Dismiss()
    {
        if (IsDismissed)
        {
            return;
        }

        IsDismissed = true;
        _onDismiss(this);

        if (Options.OnDismissed is { } onDismissed)
        {
            _ = onDismissed();
        }
    }
}
