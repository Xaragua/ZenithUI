namespace ZenithUI;

/// <summary>
/// A search field whose suggestions are a table, binding a key of the chosen record - its
/// <see cref="Guid"/>, its id - rather than the record itself.
/// </summary>
/// <typeparam name="TItem">The row type shown in the popup table.</typeparam>
/// <typeparam name="TValue">The key type: <see cref="Guid"/>, <c>Guid?</c>, <see cref="int"/>, <see cref="string"/>.</typeparam>
/// <remarks>
/// <para>
/// A form model holds <c>VendorId</c>, not the vendor. Binding <see cref="ZenLookup{TItem}"/> to
/// it meant a wrapper that looked the record up, mapped it back to its key on every change and
/// re-wired validation by hand. Here <c>@bind-Value="model.VendorId"</c> works directly, and
/// validation comes from the binding as for any other input.
/// </para>
/// <para>
/// The type's default means "nothing chosen": clearing writes <see cref="Guid.Empty"/> to a
/// <see cref="Guid"/>, <see langword="null"/> to a <c>Guid?</c>. Bind a nullable key when an empty
/// value has to fail <c>[Required]</c>.
/// </para>
/// <para>
/// The field shows the text of the record the key names, found in <c>Items</c>. With an
/// <c>ItemsProvider</c> the record may not be on any page loaded yet - a form opened on an existing
/// row - so supply <see cref="ItemResolver"/> to fetch it.
/// </para>
/// </remarks>
public class ZenKeyLookup<TItem, TValue> : ZenLookupBase<TItem, TValue>
    where TItem : notnull
{
    private TItem? _selected;
    private CancellationTokenSource? _resolve;

    /// <summary>The key of a record: <c>v =&gt; v.Id</c>.</summary>
    [Parameter, EditorRequired]
    public Func<TItem, TValue> ValueField { get; set; } = default!;

    /// <summary>
    /// Fetches the record for a key that is not in <c>Items</c>, so the field can show its text.
    /// For use with an <c>ItemsProvider</c>.
    /// </summary>
    /// <remarks>
    /// Called only when the bound key changes from outside and names a record this lookup has not
    /// seen. A pick never calls it: the picked row is already known.
    /// </remarks>
    [Parameter]
    public Func<TValue, CancellationToken, ValueTask<TItem?>>? ItemResolver { get; set; }

    /// <inheritdoc />
    protected override TItem? SelectedItem => _selected;

    /// <inheritdoc />
    /// <remarks>
    /// By key, not by record: a provider returns new instances on every request, and a key needs
    /// no value equality on <typeparamref name="TItem"/> to match.
    /// </remarks>
    protected override bool IsCurrent(TItem item) =>
        EqualityComparer<TValue>.Default.Equals(ValueField(item), CurrentValue);

    /// <inheritdoc />
    protected override void SetSelectedItem(TItem? item)
    {
        _selected = item;
        CurrentValue = item is null ? default : ValueField(item);
    }

    /// <inheritdoc />
    protected override async Task SyncSelectedItemAsync()
    {
        if (ValueField is null)
        {
            throw new InvalidOperationException(
                $"{GetType().Name} requires a {nameof(ValueField)} parameter.");
        }

        if (!HasValue)
        {
            _selected = default;
            return;
        }

        if (_selected is not null && IsCurrent(_selected))
        {
            return;
        }

        _selected = Items is null ? default : Items.FirstOrDefault(IsCurrent);

        if (_selected is not null || ItemResolver is null)
        {
            return;
        }

        _resolve?.Cancel();
        _resolve?.Dispose();
        _resolve = new CancellationTokenSource();

        var key = CurrentValue!;
        TItem? resolved;

        try
        {
            resolved = await ItemResolver(key, _resolve.Token);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        // The value may have moved on while the resolver ran; a stale answer must not land.
        if (EqualityComparer<TValue>.Default.Equals(CurrentValue, key))
        {
            _selected = resolved;
        }
    }

    /// <inheritdoc />
    protected override void DisposeCore()
    {
        _resolve?.Cancel();
        _resolve?.Dispose();
        _resolve = null;

        base.DisposeCore();
    }
}
