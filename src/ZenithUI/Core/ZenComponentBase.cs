namespace ZenithUI;

/// <summary>
/// Base class for every ZenithUI component. Provides class and attribute pass-through plus a
/// stable element id for ARIA wiring.
/// </summary>
public abstract class ZenComponentBase : ComponentBase
{
    /// <summary>
    /// Additional CSS classes appended after the component's own classes, so they win on
    /// equal-specificity ties and can be used for layout tweaks at the call site.
    /// </summary>
    [Parameter]
    public string? Class { get; set; }

    /// <summary>Inline styles merged onto the component's root element.</summary>
    [Parameter]
    public string? Style { get; set; }

    /// <summary>
    /// Any attribute not matched by a declared parameter, splatted onto the root element.
    /// This is how callers pass <c>data-*</c>, <c>aria-*</c>, <c>title</c> and event handlers
    /// through to the underlying HTML.
    /// </summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    /// <summary>
    /// A unique, stable element id used to wire <c>label for</c>, <c>aria-describedby</c> and
    /// <c>aria-controls</c>. Set it explicitly to take control of the value.
    /// </summary>
    /// <remarks>
    /// The fallback is generated in the constructor rather than in a lifecycle method on purpose.
    /// Under prerendering a component instance renders once on the server and again when the
    /// circuit or WebAssembly runtime takes over; an id generated during <c>OnInitialized</c> or
    /// <c>OnAfterRender</c> would differ between the two passes, which breaks the
    /// <c>label</c>-to-input association after hydration and produces a DOM-diff mismatch.
    /// </remarks>
    [Parameter]
    public string Id { get; set; }

    /// <summary>The id fallback generated for this instance, independent of <see cref="Id"/>.</summary>
    private protected string GeneratedId { get; }

    /// <summary>Initializes a new instance and assigns its generated element id.</summary>
    protected ZenComponentBase()
    {
        // 8 hex chars from a GUID: short enough to keep the DOM readable, wide enough that a
        // collision within a single page is not a practical concern.
        Span<char> guidChars = stackalloc char[32];
        _ = Guid.NewGuid().TryFormat(guidChars, out _, "N");

        GeneratedId = string.Concat("zen-", guidChars[..8]);
        Id = GeneratedId;
    }

    /// <summary>
    /// Derives an id for a satellite element (help text, error message, listbox) from this
    /// component's <see cref="Id"/>, so related nodes share a readable prefix.
    /// </summary>
    /// <param name="suffix">A short role name, for example <c>"help"</c> or <c>"listbox"</c>.</param>
    protected string SubId(string suffix) => $"{Id}-{suffix}";

    /// <summary>
    /// Builds the root element's class attribute: the component's own classes, then
    /// <see cref="Class"/>, then any <c>class</c> arriving through
    /// <see cref="AdditionalAttributes"/>.
    /// </summary>
    /// <param name="componentClasses">Classes intrinsic to the component.</param>
    /// <returns>The merged class string, or <see langword="null"/> when there is nothing to emit.</returns>
    protected string? RootClass(string? componentClasses) =>
        CssBuilder.Default(componentClasses)
            .AddClass(Class)
            .AddClassFromAttributes(AdditionalAttributes)
            .Build();

    /// <summary>
    /// <see cref="AdditionalAttributes"/> with <c>class</c> removed, for splatting onto an
    /// element whose class attribute is rendered separately.
    /// </summary>
    /// <remarks>
    /// Blazor applies a splatted <c>class</c> over an explicitly written one, so without this
    /// filter a caller's <c>class="mt-4"</c> would replace the component's own classes instead of
    /// being merged by <see cref="RootClass"/>. Returns the original dictionary untouched when no
    /// <c>class</c> is present, avoiding an allocation on the common path.
    /// </remarks>
    protected IReadOnlyDictionary<string, object>? AttributesWithoutClass
    {
        get
        {
            if (AdditionalAttributes is null || !AdditionalAttributes.ContainsKey("class"))
            {
                return AdditionalAttributes;
            }

            var filtered = new Dictionary<string, object>(AdditionalAttributes.Count, StringComparer.Ordinal);

            foreach (var pair in AdditionalAttributes)
            {
                if (!string.Equals(pair.Key, "class", StringComparison.Ordinal))
                {
                    filtered[pair.Key] = pair.Value;
                }
            }

            return filtered;
        }
    }
}
