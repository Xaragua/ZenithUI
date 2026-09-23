namespace ZenithUI;

/// <summary>
/// Composes a CSS class string from conditional fragments, so components never concatenate
/// classes inline in markup.
/// </summary>
/// <remarks>
/// <para>
/// The builder is a mutable <see langword="struct"/> and allocates a <see cref="StringBuilder"/>
/// only when at least one class is actually added, which keeps the common "no extra classes"
/// render path allocation-free.
/// </para>
/// <para>
/// Because it is a struct, always chain the calls or reassign the result - discarding the
/// returned value loses the mutation:
/// <code>
/// var css = CssBuilder.Default("zen-input")
///     .AddClass("zen-input--invalid", isInvalid)
///     .AddClass(Class)
///     .Build();
/// </code>
/// </para>
/// </remarks>
public struct CssBuilder
{
    private StringBuilder? _builder;

    private CssBuilder(string? initial)
    {
        _builder = null;

        if (!string.IsNullOrWhiteSpace(initial))
        {
            _builder = new StringBuilder(initial.Trim());
        }
    }

    /// <summary>Starts a builder seeded with <paramref name="initialClasses"/>.</summary>
    /// <param name="initialClasses">Classes always present on the element; may be null or empty.</param>
    public static CssBuilder Default(string? initialClasses = null) => new(initialClasses);

    /// <summary>Appends <paramref name="value"/> when it is not null or whitespace.</summary>
    public CssBuilder AddClass(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return this;
        }

        _builder ??= new StringBuilder();

        if (_builder.Length > 0)
        {
            _builder.Append(' ');
        }

        _builder.Append(value.Trim());
        return this;
    }

    /// <summary>Appends <paramref name="value"/> only when <paramref name="condition"/> holds.</summary>
    public CssBuilder AddClass(string? value, bool condition) =>
        condition ? AddClass(value) : this;

    /// <summary>
    /// Appends <paramref name="whenTrue"/> or <paramref name="whenFalse"/> depending on
    /// <paramref name="condition"/>. Useful for mutually exclusive state classes.
    /// </summary>
    public CssBuilder AddClass(string? whenTrue, string? whenFalse, bool condition) =>
        AddClass(condition ? whenTrue : whenFalse);

    /// <summary>
    /// Appends the class produced by <paramref name="valueFactory"/>, evaluating it only when
    /// <paramref name="condition"/> holds. Use this when computing the class is not free.
    /// </summary>
    public CssBuilder AddClass(Func<string?> valueFactory, bool condition) =>
        condition ? AddClass(valueFactory()) : this;

    /// <summary>
    /// Appends the <c>class</c> entry from a component's captured unmatched attributes, so that
    /// a caller writing <c>class="mt-4"</c> on a ZenithUI component has it merged in rather than
    /// silently dropped or duplicated.
    /// </summary>
    public CssBuilder AddClassFromAttributes(IReadOnlyDictionary<string, object>? attributes)
    {
        if (attributes is not null
            && attributes.TryGetValue("class", out var value)
            && value is string cssClass)
        {
            return AddClass(cssClass);
        }

        return this;
    }

    /// <summary>
    /// Returns the composed class string, or <see langword="null"/> when nothing was added.
    /// </summary>
    /// <remarks>
    /// Returning null rather than an empty string matters: Blazor omits an attribute whose value
    /// is null, so an empty builder produces no <c>class</c> attribute at all instead of
    /// <c>class=""</c>.
    /// </remarks>
    public readonly string? Build()
    {
        var result = _builder?.ToString();
        return string.IsNullOrWhiteSpace(result) ? null : result;
    }

    /// <inheritdoc />
    public readonly override string ToString() => Build() ?? string.Empty;
}
