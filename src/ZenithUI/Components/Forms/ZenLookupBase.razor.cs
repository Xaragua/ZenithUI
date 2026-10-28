namespace ZenithUI;

/// <summary>
/// The search field with a table popup behind <see cref="ZenLookup{TItem}"/> and
/// <see cref="ZenKeyLookup{TItem, TValue}"/>. Not used directly.
/// </summary>
/// <typeparam name="TItem">The row type shown in the popup table.</typeparam>
/// <typeparam name="TValue">The bound value type.</typeparam>
public abstract partial class ZenLookupBase<TItem, TValue>
    where TItem : notnull;
