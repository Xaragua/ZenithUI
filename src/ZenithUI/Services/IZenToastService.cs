using ZenithUI.Core;

namespace ZenithUI.Services;

/// <summary>
/// Shows transient notifications from anywhere, with no markup at the call site.
/// </summary>
/// <remarks>
/// One <see cref="ZenToastHost"/> in the layout renders whatever is showing. Without it the
/// service accepts calls and nothing appears.
/// </remarks>
public interface IZenToastService
{
    /// <summary>The toasts currently on screen, oldest first.</summary>
    IReadOnlyList<ZenToastInstance> Toasts { get; }

    /// <summary>Raised when a toast appears or leaves, so the host can re-render.</summary>
    event Action? Changed;

    /// <summary>Shows a toast.</summary>
    /// <param name="message">The body text.</param>
    /// <param name="intent">Colour role, which also decides the icon and how it is announced.</param>
    /// <param name="options">Duration, action and dismissal options.</param>
    /// <returns>The instance, so the caller can dismiss it early.</returns>
    ZenToastInstance Show(string message, ZenIntent intent = ZenIntent.Neutral, ZenToastOptions? options = null);

    /// <summary>Shows a success toast.</summary>
    /// <param name="message">The body text.</param>
    /// <param name="options">Duration, action and dismissal options.</param>
    ZenToastInstance Success(string message, ZenToastOptions? options = null);

    /// <summary>Shows an informational toast.</summary>
    /// <param name="message">The body text.</param>
    /// <param name="options">Duration, action and dismissal options.</param>
    ZenToastInstance Info(string message, ZenToastOptions? options = null);

    /// <summary>Shows a warning toast.</summary>
    /// <param name="message">The body text.</param>
    /// <param name="options">Duration, action and dismissal options.</param>
    ZenToastInstance Warning(string message, ZenToastOptions? options = null);

    /// <summary>
    /// Shows an error toast, announced assertively and with no auto-dismiss.
    /// </summary>
    /// <param name="message">The body text.</param>
    /// <param name="options">Overrides, including <c>Duration</c> if a timeout really is wanted.</param>
    /// <remarks>
    /// The duration default is deliberately different here. An error that disappears on a timer is
    /// an error the user may never have seen, and there is no way to get it back.
    /// </remarks>
    ZenToastInstance Danger(string message, ZenToastOptions? options = null);

    /// <summary>Dismisses every toast.</summary>
    void Clear();
}
