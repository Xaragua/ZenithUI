namespace ZenithUI;

/// <summary>
/// A search field whose suggestions are a table, binding the chosen record itself. To bind a key
/// of the record instead - a <see cref="Guid"/>, an id - use <see cref="ZenKeyLookup{TItem, TValue}"/>.
/// </summary>
/// <typeparam name="TItem">The row type, which is also the bound value type.</typeparam>
/// <remarks>
/// A plain class rather than a <c>.razor</c> file because it adds no markup: a <c>.razor</c> file
/// always generates its own <c>BuildRenderTree</c>, which would replace the base's.
/// </remarks>
public class ZenLookup<TItem> : ZenLookupBase<TItem, TItem>
    where TItem : notnull
{
    /// <inheritdoc />
    protected override TItem? SelectedItem => CurrentValue;

    /// <inheritdoc />
    protected override bool HasValue => CurrentValue is not null;

    /// <inheritdoc />
    protected override void SetSelectedItem(TItem? item) => CurrentValue = item;
}
