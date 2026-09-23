using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq.Expressions;
using Microsoft.AspNetCore.Components.Forms;

namespace ZenithUI;

/// <summary>
/// Base class for ZenithUI form controls. Provides two-way binding, optional
/// <see cref="Microsoft.AspNetCore.Components.Forms.EditContext"/> integration, and the shared field parameters.
/// </summary>
/// <typeparam name="TValue">The bound value type.</typeparam>
/// <remarks>
/// <para>
/// <b>Why this does not derive from <see cref="InputBase{TValue}"/>.</b> That class declares its
/// <c>EditContext</c> cascading parameter as required and throws from <c>SetParametersAsync</c>
/// when none is supplied:
/// </para>
/// <para>
/// <c>InputBase requires a cascading parameter of type EditContext. For example, you can use
/// InputBase inside an EditForm.</c>
/// </para>
/// <para>
/// That makes every derived control unusable outside an <c>EditForm</c> — so a standalone search
/// box, a filter dropdown in a toolbar, or a quantity stepper in a table row would each need a
/// dummy form wrapped around it. A component library cannot impose that, so the binding machinery
/// is reimplemented here with the <c>EditContext</c> made optional.
/// </para>
/// <para>
/// Everything else is kept deliberately identical to the framework's semantics — particularly
/// <see cref="ValueExpression"/> and <see cref="FieldIdentifier"/> — so third-party validators that
/// write into a <see cref="ValidationMessageStore"/> keyed by field identifier (FluentValidation
/// adapters, <c>DataAnnotationsValidator</c>) work against these controls unmodified.
/// </para>
/// </remarks>
public abstract class ZenInputBase<TValue> : ZenComponentBase, IDisposable
{
    private readonly EventHandler<ValidationStateChangedEventArgs> _validationStateChangedHandler;

    private bool _hasInitializedParameters;
    private EditContext? _subscribedEditContext;
    private Type? _nullableUnderlyingType;
    private CancellationTokenSource? _debounceCts;

    /// <summary>Initializes a new instance.</summary>
    protected ZenInputBase() =>
        _validationStateChangedHandler = (_, _) => _ = InvokeAsync(StateHasChanged);

    /// <summary>
    /// The form context, supplied by an enclosing <c>EditForm</c>. Optional: when absent the
    /// control still binds, it simply has no validation to display.
    /// </summary>
    [CascadingParameter]
    protected EditContext? CascadedEditContext { get; set; }

    /// <summary>The bound value.</summary>
    [Parameter]
    public TValue? Value { get; set; }

    /// <summary>Raised when the value changes. Set implicitly by <c>@bind-Value</c>.</summary>
    [Parameter]
    public EventCallback<TValue?> ValueChanged { get; set; }

    /// <summary>
    /// Identifies the bound model field. Set implicitly by <c>@bind-Value</c>, and required for
    /// validation messages to be matched to this control.
    /// </summary>
    [Parameter]
    public Expression<Func<TValue?>>? ValueExpression { get; set; }

    /// <summary>Visible label. Rendered by the field wrapper.</summary>
    [Parameter]
    public string? Label { get; set; }

    /// <summary>Guidance shown under the control.</summary>
    [Parameter]
    public string? HelpText { get; set; }

    /// <summary>Placeholder text.</summary>
    [Parameter]
    public string? Placeholder { get; set; }

    /// <summary>
    /// Marks the field required: shows the indicator and sets <c>aria-required</c>. This is a
    /// presentation flag — it does not by itself validate anything.
    /// </summary>
    [Parameter]
    public bool Required { get; set; }

    /// <summary>Disables the control.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>Makes the control read-only. Unlike disabled, the value still submits and is focusable.</summary>
    [Parameter]
    public bool ReadOnly { get; set; }

    /// <summary>Control size.</summary>
    [Parameter]
    public ZenSize Size { get; set; } = ZenSize.Medium;

    /// <summary>Content rendered inside the control, before the input.</summary>
    [Parameter]
    public RenderFragment? LeadingContent { get; set; }

    /// <summary>Content rendered inside the control, after the input.</summary>
    [Parameter]
    public RenderFragment? TrailingContent { get; set; }

    /// <summary>
    /// An error message supplied by the caller, shown regardless of <c>EditContext</c> state.
    /// For controls used outside a form.
    /// </summary>
    [Parameter]
    public string? ErrorMessage { get; set; }

    /// <summary>Stretches the control to its container's width. On by default for form fields.</summary>
    [Parameter]
    public bool FullWidth { get; set; } = true;

    /// <summary>
    /// Update the bound value on every keystroke rather than when the control loses focus.
    /// </summary>
    /// <remarks>
    /// Off by default, and that default is deliberate. Committing on every keystroke re-runs
    /// validation mid-word, so a half-typed email address is reported as invalid while the user is
    /// still typing it. Turn it on for a control whose whole purpose is live response - a search
    /// box, a filter - and pair it with <see cref="DebounceMilliseconds"/>.
    /// </remarks>
    [Parameter]
    public bool Immediate { get; set; }

    /// <summary>
    /// How long to wait after the last keystroke before committing, when <see cref="Immediate"/>
    /// is set. Zero commits synchronously.
    /// </summary>
    [Parameter]
    public int DebounceMilliseconds { get; set; }

    /// <summary>Extra classes for the surrounding field wrapper, as opposed to the control itself.</summary>
    /// <remarks>
    /// <see cref="ZenComponentBase.Class"/> lands on the control, because that is the element a
    /// caller almost always means. This is the escape hatch for the layout around it.
    /// </remarks>
    [Parameter]
    public string? FieldClass { get; set; }

    /// <summary>
    /// The effective <see cref="Microsoft.AspNetCore.Components.Forms.EditContext"/>, or <see langword="null"/> when the control is
    /// used standalone.
    /// </summary>
    protected EditContext? EditContext { get; private set; }

    /// <summary>
    /// Identifies the bound field, derived from <see cref="ValueExpression"/>. Default when there
    /// is no expression.
    /// </summary>
    protected FieldIdentifier FieldIdentifier { get; private set; }

    /// <summary>Validation messages for this field, plus any caller-supplied <see cref="ErrorMessage"/>.</summary>
    protected IEnumerable<string> ValidationMessages
    {
        get
        {
            if (!string.IsNullOrEmpty(ErrorMessage))
            {
                yield return ErrorMessage;
            }

            if (EditContext is not null && ValueExpression is not null)
            {
                foreach (var message in EditContext.GetValidationMessages(FieldIdentifier))
                {
                    yield return message;
                }
            }
        }
    }

    /// <summary><see langword="true"/> when the field has anything to report.</summary>
    protected bool IsInvalid => ValidationMessages.Any();

    /// <summary>
    /// Gets or sets the current value, raising <see cref="ValueChanged"/> and notifying the
    /// <see cref="Microsoft.AspNetCore.Components.Forms.EditContext"/> when it actually changes.
    /// </summary>
    protected TValue? CurrentValue
    {
        get => Value;
        set
        {
            if (EqualityComparer<TValue>.Default.Equals(value, Value))
            {
                return;
            }

            Value = value;
            _ = ValueChanged.InvokeAsync(value);

            // Notifying the field is what re-runs validation and re-renders any ValidationSummary.
            // Skipped without a ValueExpression, since there would be no field to notify about.
            if (EditContext is not null && ValueExpression is not null)
            {
                EditContext.NotifyFieldChanged(FieldIdentifier);
            }
        }
    }

    /// <summary>
    /// The current value as a string, for binding directly to an <c>input</c> element's value.
    /// Assigning an unparseable string records a parse error rather than throwing.
    /// </summary>
    protected string? CurrentValueAsString
    {
        get => FormatValueAsString(CurrentValue);
        set
        {
            // An empty string on a Nullable<T> means "no value", not "parse failure". Without this
            // branch, clearing a nullable number field would surface a spurious format error.
            if (_nullableUnderlyingType is not null && string.IsNullOrEmpty(value))
            {
                ParsingFailed = false;
                CurrentValue = default;
                return;
            }

            if (TryParseValueFromString(value, out var parsed, out var validationErrorMessage))
            {
                ParsingFailed = false;
                CurrentValue = parsed;
            }
            else
            {
                ParsingFailed = true;
                ParsingErrorMessage = validationErrorMessage;

                // The field is still marked changed so that validation re-runs and the rest of the
                // form sees the edit, even though no valid value was produced.
                if (EditContext is not null && ValueExpression is not null)
                {
                    EditContext.NotifyFieldChanged(FieldIdentifier);
                }
            }
        }
    }

    /// <summary><see langword="true"/> when the last string assignment could not be parsed.</summary>
    protected bool ParsingFailed { get; private set; }

    /// <summary>The message describing the last parse failure.</summary>
    protected string? ParsingErrorMessage { get; private set; }

    /// <summary>
    /// Converts <paramref name="value"/> to its display string. Override for culture- or
    /// format-specific rendering.
    /// </summary>
    protected virtual string? FormatValueAsString(TValue? value) => value switch
    {
        null => null,
        string s => s,
        IFormattable formattable => formattable.ToString(null, CultureInfo.CurrentCulture),
        _ => value.ToString(),
    };

    /// <summary>
    /// Parses a display string back into <typeparamref name="TValue"/>.
    /// </summary>
    /// <param name="value">The string from the DOM.</param>
    /// <param name="result">The parsed value on success.</param>
    /// <param name="validationErrorMessage">A message describing the failure.</param>
    /// <returns><see langword="true"/> when parsing succeeded.</returns>
    protected virtual bool TryParseValueFromString(
        string? value,
        [MaybeNullWhen(false)] out TValue result,
        [NotNullWhen(false)] out string? validationErrorMessage)
    {
        var targetType = _nullableUnderlyingType ?? typeof(TValue);

        try
        {
            if (targetType == typeof(string))
            {
                result = (TValue)(object)(value ?? string.Empty);
                validationErrorMessage = null;
                return true;
            }

            if (string.IsNullOrEmpty(value))
            {
                result = default!;
                validationErrorMessage = null;
                return _nullableUnderlyingType is not null || !targetType.IsValueType;
            }

            if (targetType.IsEnum)
            {
                result = (TValue)Enum.Parse(targetType, value, ignoreCase: true);
                validationErrorMessage = null;
                return true;
            }

            result = (TValue)Convert.ChangeType(value, targetType, CultureInfo.CurrentCulture);
            validationErrorMessage = null;
            return true;
        }
        catch (Exception ex) when (ex is FormatException or InvalidCastException or OverflowException or ArgumentException)
        {
            result = default!;
            validationErrorMessage = $"The {DisplayName} field is not valid.";
            return false;
        }
    }

    /// <summary>
    /// Handler for the DOM <c>change</c> event: commits when the control loses focus.
    /// </summary>
    protected void HandleChange(ChangeEventArgs args) =>
        CurrentValueAsString = args.Value?.ToString();

    /// <summary>
    /// Handler for the DOM <c>input</c> event: commits on each keystroke, optionally debounced.
    /// </summary>
    /// <remarks>
    /// Each keystroke cancels the previous pending commit. The cancellation is swallowed rather
    /// than propagated - a superseded keystroke is the normal case here, not an error - and the
    /// value is re-checked after the delay so a commit that lost the race never lands.
    /// </remarks>
    protected async Task HandleInputAsync(ChangeEventArgs args)
    {
        var value = args.Value?.ToString();

        if (DebounceMilliseconds <= 0)
        {
            CurrentValueAsString = value;
            return;
        }

        _debounceCts?.Cancel();
        _debounceCts?.Dispose();

        var cts = new CancellationTokenSource();
        _debounceCts = cts;

        try
        {
            await Task.Delay(DebounceMilliseconds, cts.Token);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        if (cts.IsCancellationRequested)
        {
            return;
        }

        CurrentValueAsString = value;
        StateHasChanged();
    }

    /// <summary>The DOM event a control should bind to, given <see cref="Immediate"/>.</summary>
    protected string ValueEventName => Immediate ? "oninput" : "onchange";

    /// <summary>The field's human name, used in generated validation messages.</summary>
    protected string DisplayName =>
        Label ?? (ValueExpression is not null ? FieldIdentifier.FieldName : "value");

    /// <inheritdoc />
    public override Task SetParametersAsync(ParameterView parameters)
    {
        parameters.SetParameterProperties(this);

        if (!_hasInitializedParameters)
        {
            if (ValueExpression is not null)
            {
                FieldIdentifier = FieldIdentifier.Create(ValueExpression);
            }

            _nullableUnderlyingType = Nullable.GetUnderlyingType(typeof(TValue));
            EditContext = CascadedEditContext;
            SubscribeToValidationStateChanges(EditContext);

            _hasInitializedParameters = true;
        }
        else if (!ReferenceEquals(CascadedEditContext, EditContext))
        {
            // A control cannot be moved between forms mid-life: its FieldIdentifier and validation
            // subscription belong to the context it was created in. Failing loudly beats silently
            // reporting another form's validation state.
            throw new InvalidOperationException(
                $"{GetType().Name} does not support changing the EditContext dynamically.");
        }

        return base.SetParametersAsync(ParameterView.Empty);
    }

    private void SubscribeToValidationStateChanges(EditContext? editContext)
    {
        if (ReferenceEquals(_subscribedEditContext, editContext))
        {
            return;
        }

        if (_subscribedEditContext is not null)
        {
            _subscribedEditContext.OnValidationStateChanged -= _validationStateChangedHandler;
        }

        if (editContext is not null)
        {
            editContext.OnValidationStateChanged += _validationStateChangedHandler;
        }

        _subscribedEditContext = editContext;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        SubscribeToValidationStateChanges(null);
        DisposeCore();
        GC.SuppressFinalize(this);
    }

    /// <summary>Releases resources held by a derived control.</summary>
    protected virtual void DisposeCore()
    {
        // A pending debounce holds a timer and a callback into this component. Left running, it
        // fires against a disposed component and its renderer.
        _debounceCts?.Cancel();
        _debounceCts?.Dispose();
        _debounceCts = null;
    }
}
