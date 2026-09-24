namespace ZenithUI;

/// <summary>
/// What a <c>ZenLookup</c> asks its <c>ItemsProvider</c> for: the rows matching what the user
/// typed, one window at a time, in an order.
/// </summary>
/// <param name="Query">
/// The search text, trimmed. Empty when the user has opened the lookup without typing, which
/// should return everything - the lookup is also a browser, not only a search box.
/// </param>
/// <param name="StartIndex">Zero-based index of the first row wanted.</param>
/// <param name="Count">How many rows are wanted, or <see langword="null"/> for all of them.</param>
/// <param name="SortName">The sorted column's <c>SortName</c>, or <see langword="null"/>.</param>
/// <param name="SortDescending">Whether the sort is descending.</param>
/// <param name="CancellationToken">
/// Cancelled when superseded - most often by the next keystroke. Pass it to the query.
/// </param>
public sealed record ZenLookupRequest(
    string Query,
    int StartIndex,
    int? Count,
    string? SortName,
    bool SortDescending,
    CancellationToken CancellationToken);
