using ZenithUI.Core;

namespace ZenithUI.Services;

/// <summary>Default <see cref="IZenModalService"/>. Registered scoped, so one stack per document.</summary>
public sealed class ZenModalService : IZenModalService
{
    private readonly List<ZenModalInstance> _modals = [];

    /// <inheritdoc />
    public IReadOnlyList<ZenModalInstance> Modals => _modals;

    /// <inheritdoc />
    public event Action? Changed;

    /// <inheritdoc />
    public Task<ZenModalResult> ShowAsync<TComponent>(
        IReadOnlyDictionary<string, object?>? parameters = null,
        ZenModalOptions? options = null)
        where TComponent : IComponent =>
        ShowAsync(typeof(TComponent), parameters, options);

    /// <inheritdoc />
    public Task<ZenModalResult> ShowAsync(
        Type componentType,
        IReadOnlyDictionary<string, object?>? parameters = null,
        ZenModalOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(componentType);

        if (!typeof(IComponent).IsAssignableFrom(componentType))
        {
            throw new ArgumentException(
                $"{componentType.Name} is not a Blazor component.", nameof(componentType));
        }

        var instance = new ZenModalInstance(
            componentType,
            parameters ?? new Dictionary<string, object?>(),
            options ?? new ZenModalOptions(),
            Remove);

        _modals.Add(instance);
        Changed?.Invoke();

        return instance.Result;
    }

    /// <inheritdoc />
    public async Task<bool> ConfirmAsync(string title, string message, ZenConfirmOptions? options = null)
    {
        var confirm = options ?? new ZenConfirmOptions();

        var result = await ShowAsync<ZenConfirmDialog>(
            new Dictionary<string, object?>
            {
                [nameof(ZenConfirmDialog.Message)] = message,
                [nameof(ZenConfirmDialog.Options)] = confirm,
            },
            new ZenModalOptions
            {
                Title = title,
                Size = confirm.Size,
                // A confirmation is a question with two answers on screen. A backdrop click is a
                // third, ambiguous one - so it is refused here regardless of the global default.
                CloseOnBackdropClick = false,
            });

        return result.Confirmed;
    }

    /// <inheritdoc />
    public void CloseAll()
    {
        // Iterated over a copy: Close calls back into Remove, which mutates the list.
        foreach (var modal in _modals.ToArray())
        {
            modal.Close(ZenModalResult.Cancel());
        }
    }

    private void Remove(ZenModalInstance instance)
    {
        if (_modals.Remove(instance))
        {
            Changed?.Invoke();
        }
    }
}
