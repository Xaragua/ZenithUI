namespace ZenithUI;

/// <summary>
/// How a service-raised dialog looks and what dismisses it.
/// </summary>
/// <remarks>
/// A mutable class with initialisers rather than a record with a long positional list: callers set
/// one or two of these and inherit the rest, and a positional record would make every call site
/// name properties it does not care about.
/// </remarks>
public sealed class ZenModalOptions
{
    /// <summary>Heading shown in the dialog's title bar. Also its accessible name.</summary>
    public string? Title { get; set; }

    /// <summary>How wide the dialog may grow.</summary>
    public ZenModalSize Size { get; set; } = ZenModalSize.Medium;

    /// <summary>Show the "X" in the title bar.</summary>
    public bool ShowCloseButton { get; set; } = true;

    /// <summary>
    /// Whether Escape dismisses the dialog.
    /// </summary>
    /// <remarks>
    /// Set false only for a dialog that must not be lost by accident - an unsaved editor, a
    /// required choice. A dialog that cannot be dismissed at all is a trap, so leave at least one
    /// route out.
    /// </remarks>
    public bool CloseOnEscape { get; set; } = true;

    /// <summary>Whether clicking the backdrop dismisses the dialog.</summary>
    /// <remarks>
    /// Off by default, unlike Escape. A backdrop click is easy to do by accident - a mis-aimed
    /// click on a control near the edge - and losing a half-filled form to one is worse than
    /// having to aim for the close button.
    /// </remarks>
    public bool CloseOnBackdropClick { get; set; }

    /// <summary>Lock scrolling of the page behind the dialog.</summary>
    public bool LockScroll { get; set; } = true;

    /// <summary>
    /// Id of the element inside the dialog to focus on open, instead of the first focusable one.
    /// </summary>
    /// <remarks>
    /// Worth setting for a destructive confirmation, where the first focusable control is usually
    /// the one you do not want a stray Enter to activate.
    /// </remarks>
    public string? InitialFocusId { get; set; }

    /// <summary>Extra CSS classes for the dialog surface.</summary>
    public string? Class { get; set; }
}

/// <summary>Text and intent for the built-in confirmation dialog.</summary>
public sealed class ZenConfirmOptions
{
    /// <summary>Label on the confirming button.</summary>
    public string ConfirmText { get; set; } = "Confirm";

    /// <summary>Label on the dismissing button.</summary>
    public string CancelText { get; set; } = "Cancel";

    /// <summary>
    /// Colour of the confirming button. <see cref="ZenIntent.Danger"/> for anything destructive.
    /// </summary>
    public ZenIntent ConfirmIntent { get; set; } = ZenIntent.Primary;

    /// <summary>
    /// Focus the cancel button rather than the confirm button when the dialog opens.
    /// </summary>
    /// <remarks>
    /// Defaults to true for the same reason the confirm button is not autofocused anywhere else: a
    /// user who hits Enter out of habit, or who was mid-keystroke when the dialog appeared, should
    /// not thereby delete something. The dialog asked a question; the safe answer is the default.
    /// </remarks>
    public bool FocusCancel { get; set; } = true;

    /// <summary>How wide the confirmation may grow.</summary>
    public ZenModalSize Size { get; set; } = ZenModalSize.Small;
}
