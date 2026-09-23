using ZenithUI.Core;

namespace ZenithUI.Services;

/// <summary>Default <see cref="IZenToastService"/>. Registered scoped, so one stack per document.</summary>
public sealed class ZenToastService : IZenToastService
{
    private readonly List<ZenToastInstance> _toasts = [];

    /// <summary>
    /// How many toasts may be on screen at once.
    /// </summary>
    /// <remarks>
    /// A cap rather than an unbounded list. A loop that fails once per item will raise a toast per
    /// item, and a stack tall enough to cover the page is worse than no notification at all -
    /// it hides the very UI the user needs to fix the problem. The oldest goes when the cap is
    /// reached, because the newest is the one describing what just happened.
    /// </remarks>
    public int MaxVisible { get; set; } = 5;

    /// <inheritdoc />
    public IReadOnlyList<ZenToastInstance> Toasts => _toasts;

    /// <inheritdoc />
    public event Action? Changed;

    /// <inheritdoc />
    public ZenToastInstance Show(
        string message,
        ZenIntent intent = ZenIntent.Neutral,
        ZenToastOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(message);

        var instance = new ZenToastInstance(message, intent, options ?? new ZenToastOptions(), Remove);

        _toasts.Add(instance);

        while (_toasts.Count > MaxVisible)
        {
            // Dismissed rather than removed outright, so its OnDismissed callback still runs and a
            // caller tracking outstanding notifications does not leak one.
            _toasts[0].Dismiss();
        }

        Changed?.Invoke();

        return instance;
    }

    /// <inheritdoc />
    public ZenToastInstance Success(string message, ZenToastOptions? options = null) =>
        Show(message, ZenIntent.Success, options);

    /// <inheritdoc />
    public ZenToastInstance Info(string message, ZenToastOptions? options = null) =>
        Show(message, ZenIntent.Info, options);

    /// <inheritdoc />
    public ZenToastInstance Warning(string message, ZenToastOptions? options = null) =>
        Show(message, ZenIntent.Warning, options);

    /// <inheritdoc />
    public ZenToastInstance Danger(string message, ZenToastOptions? options = null) =>
        Show(message, ZenIntent.Danger, options ?? new ZenToastOptions { Duration = TimeSpan.Zero });

    /// <inheritdoc />
    public void Clear()
    {
        // Over a copy: Dismiss calls back into Remove, which mutates the list.
        foreach (var toast in _toasts.ToArray())
        {
            toast.Dismiss();
        }
    }

    private void Remove(ZenToastInstance instance)
    {
        if (_toasts.Remove(instance))
        {
            Changed?.Invoke();
        }
    }
}
