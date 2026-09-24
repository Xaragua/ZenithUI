namespace ZenithUI;

/// <summary>
/// What a <see cref="ZenTable{TItem}"/> asks its <c>ItemsProvider</c> for: a window of rows, in an
/// order.
/// </summary>
/// <param name="StartIndex">Zero-based index of the first row wanted.</param>
/// <param name="Count">
/// How many rows are wanted, or <see langword="null"/> for "all of them from
/// <paramref name="StartIndex"/>". A paged table asks for exactly one page; a virtualized table asks
/// for what fits in the viewport plus the overscan.
/// </param>
/// <param name="SortName">
/// The <c>SortName</c> of the column the user sorted by, or <see langword="null"/> for the order
/// the data naturally comes in.
/// </param>
/// <param name="SortDescending">Whether the sort is descending.</param>
/// <param name="CancellationToken">
/// Cancelled when the request is superseded - the user sorted again, paged again, or scrolled past
/// the window before it arrived. Pass it to the query.
/// </param>
public sealed record ZenTableRequest(
    int StartIndex,
    int? Count,
    string? SortName,
    bool SortDescending,
    CancellationToken CancellationToken);

/// <summary>A window of rows, and how many rows exist in total.</summary>
/// <typeparam name="TItem">The row type.</typeparam>
/// <param name="Items">The rows in the requested window.</param>
/// <param name="TotalCount">
/// The number of rows across every page. The table needs it to draw the pager, size the scrollbar
/// of a virtualized body, and state <c>aria-rowcount</c>.
/// </param>
public sealed record ZenTableResult<TItem>(IReadOnlyList<TItem> Items, int TotalCount);

/// <summary>One group of rows in a table with <c>GroupBy</c> set.</summary>
/// <typeparam name="TItem">The row type.</typeparam>
/// <param name="Key">The value the rows were grouped on. May be <see langword="null"/>.</param>
/// <param name="Text">The key as it is shown in the group header.</param>
/// <param name="Items">Every row in the group, in the table's current sort order.</param>
public sealed record ZenTableGroup<TItem>(object? Key, string Text, IReadOnlyList<TItem> Items)
{
    /// <summary>How many rows the group holds.</summary>
    public int Count => Items.Count;
}

/// <summary>
/// Puts a <see cref="ZenTable{TItem}"/> into the role a picker's popup needs: a single-select
/// <c>grid</c> whose rows are chosen from a text field that keeps focus.
/// </summary>
/// <remarks>
/// Cascaded rather than exposed as parameters, because it is not a mode a page should turn on. A
/// table claiming <c>role="grid"</c> owes the user a keyboard contract, and here that contract is
/// implemented by the owner - the text field that holds focus and moves
/// <c>aria-activedescendant</c> through the rows. A table put in this mode with no owner would
/// make the claim and keep none of it.
/// </remarks>
internal sealed class ZenTablePicker<TItem>
{
    /// <summary>The index of the row under the keyboard cursor, or -1 for none.</summary>
    public int ActiveIndex { get; set; } = -1;

    /// <summary>Whether a row is the chosen one - committed, or pending confirmation.</summary>
    public required Func<TItem, bool> IsChosen { get; init; }

    /// <summary>Called when a row is clicked.</summary>
    public required Func<TItem, int, Task> RowClicked { get; init; }

    /// <summary>Called when the pointer enters a row, so keyboard and pointer agree on one row.</summary>
    public required Action<int> RowHovered { get; init; }

    /// <summary>
    /// Called after every render of the table, so the owner can react to rows it did not ask
    /// for - a provider's page arriving after the owner last rendered.
    /// </summary>
    public Action? Rendered { get; init; }
}
