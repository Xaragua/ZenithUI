namespace ZenithUI;

/// <summary>
/// The children of a component that declare themselves rather than render themselves - a
/// breadcrumb's items, a tab set's tabs - kept in document order across renders.
/// </summary>
/// <remarks>
/// <para>
/// The parent renders <c>&lt;CascadingValue Value="this"&gt;@ChildContent&lt;/CascadingValue&gt;</c>
/// followed by its markup inside a <see cref="ZenDefer"/>, calling <see cref="BeginPass"/> before
/// the cascade and <see cref="Merge"/> inside the defer. Each child calls <see cref="Register"/>
/// from <c>OnParametersSet</c> and <see cref="Unregister"/> from <c>Dispose</c>.
/// </para>
/// <para>
/// This is <c>ZenStepper</c>'s mechanism, factored out. A child is not dropped for missing a pass:
/// Blazor re-sets a child's parameters only when one might have changed, so a child with nothing
/// but strings registers once and is then silent. It joins the list the first time it registers
/// and leaves when it is disposed. A pass's registrations only place new children - each goes in
/// front of the next one written after it that the list already knows - and, when every child
/// registered, reorder the list to match.
/// </para>
/// </remarks>
/// <typeparam name="T">The child component type.</typeparam>
internal sealed class ZenChildList<T>
    where T : class
{
    private readonly List<T> _items = [];
    private readonly List<T> _passOrder = [];
    private int _generation;
    private int _collectedGeneration = -1;

    /// <summary>The children, in document order, as of the last <see cref="Merge"/>.</summary>
    public IReadOnlyList<T> Items => _items;

    /// <summary>Starts a render pass of the parent. Call before rendering the children.</summary>
    public void BeginPass() => _generation++;

    /// <summary>Called by a child as it receives its parameters.</summary>
    public void Register(T item)
    {
        if (_collectedGeneration != _generation)
        {
            _collectedGeneration = _generation;
            _passOrder.Clear();
        }

        if (!_passOrder.Contains(item))
        {
            _passOrder.Add(item);
        }
    }

    /// <summary>Called by a child as it is disposed.</summary>
    public void Unregister(T item)
    {
        _items.Remove(item);
        _passOrder.Remove(item);
    }

    /// <summary>Folds this pass's registrations into <see cref="Items"/> and returns it.</summary>
    public IReadOnlyList<T> Merge()
    {
        for (var i = 0; i < _passOrder.Count; i++)
        {
            var item = _passOrder[i];

            if (_items.Contains(item))
            {
                continue;
            }

            var next = _passOrder.Skip(i + 1).FirstOrDefault(_items.Contains);

            if (next is null)
            {
                _items.Add(item);
            }
            else
            {
                _items.Insert(_items.IndexOf(next), item);
            }
        }

        if (_passOrder.Count == _items.Count && !_passOrder.SequenceEqual(_items))
        {
            _items.Clear();
            _items.AddRange(_passOrder);
        }

        return _items;
    }
}
