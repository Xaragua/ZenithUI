using ZenithUI.Core;

namespace ZenithUI.Services;

/// <summary>
/// Raises dialogs from anywhere, without the calling code owning any markup or state.
/// </summary>
/// <remarks>
/// <para>
/// The alternative is what most applications end up with: every page that needs a dialog declares
/// one in its own markup and owns a <c>bool _isOpen</c>. That works until a confirmation has to be
/// raised from a service, a nested component, or an event handler - at which point the state has
/// to be threaded back up the tree, and the dialog's result has to come back down. A service turns
/// the whole thing into <c>await Modals.ConfirmAsync(...)</c>.
/// </para>
/// <para>
/// One <see cref="ZenModalHost"/> in the layout renders whatever is open. Without it the service
/// accepts calls and nothing appears.
/// </para>
/// </remarks>
public interface IZenModalService
{
    /// <summary>The dialogs currently open, oldest first.</summary>
    /// <remarks>
    /// A list, not a single slot: dialogs stack. A confirmation raised from inside an editor must
    /// appear above it rather than replacing it, or confirming leaves the user staring at a page
    /// they thought they were editing.
    /// </remarks>
    IReadOnlyList<ZenModalInstance> Modals { get; }

    /// <summary>Raised when a dialog opens or closes, so the host can re-render.</summary>
    event Action? Changed;

    /// <summary>
    /// Shows <typeparamref name="TComponent"/> as a dialog and completes when it closes.
    /// </summary>
    /// <typeparam name="TComponent">The component to render as the dialog body.</typeparam>
    /// <param name="parameters">Parameters for that component.</param>
    /// <param name="options">Appearance and dismissal options.</param>
    /// <returns>What the dialog returned.</returns>
    Task<ZenModalResult> ShowAsync<TComponent>(
        IReadOnlyDictionary<string, object?>? parameters = null,
        ZenModalOptions? options = null)
        where TComponent : IComponent;

    /// <summary>
    /// Shows a component type known only at runtime.
    /// </summary>
    /// <param name="componentType">A type implementing <see cref="IComponent"/>.</param>
    /// <param name="parameters">Parameters for that component.</param>
    /// <param name="options">Appearance and dismissal options.</param>
    /// <returns>What the dialog returned.</returns>
    Task<ZenModalResult> ShowAsync(
        Type componentType,
        IReadOnlyDictionary<string, object?>? parameters = null,
        ZenModalOptions? options = null);

    /// <summary>
    /// Asks a yes/no question, with no component to write.
    /// </summary>
    /// <param name="title">The dialog's heading.</param>
    /// <param name="message">The question.</param>
    /// <param name="options">Button labels, intent and initial focus.</param>
    /// <returns><see langword="true"/> only if the user confirmed.</returns>
    Task<bool> ConfirmAsync(string title, string message, ZenConfirmOptions? options = null);

    /// <summary>Closes every open dialog, cancelling each.</summary>
    /// <remarks>
    /// For navigation: a dialog left open across a route change is orphaned over a page that knows
    /// nothing about it. Every awaiting caller receives a cancellation, so none are left hanging.
    /// </remarks>
    void CloseAll();
}
