namespace ZenithUI;

/// <summary>
/// The ids and ARIA state a control needs in order to be correctly associated with the label,
/// help text and error message rendered around it by <c>ZenField</c>.
/// </summary>
/// <param name="InputId">
/// The id to put on the control. <c>ZenField</c> points its <c>&lt;label for&gt;</c> at this, which
/// is what makes clicking the label focus the control.
/// </param>
/// <param name="DescribedBy">
/// Space-separated ids for <c>aria-describedby</c>, covering whichever of the help text and error
/// message are present. <see langword="null"/> when there is nothing to describe.
/// </param>
/// <param name="Invalid">Whether the field currently has an error.</param>
/// <param name="Required">Whether the field is marked required.</param>
/// <param name="Disabled">Whether the field is disabled.</param>
/// <remarks>
/// Passed to the field's content as a <c>RenderFragment&lt;ZenFieldContext&gt;</c> so that the
/// control itself applies the attributes. The wrapper cannot reach into arbitrary child markup to
/// set them, and guessing at the first descendant input would break the moment a control renders
/// a composite.
/// </remarks>
public readonly record struct ZenFieldContext(
    string InputId,
    string? DescribedBy,
    bool Invalid,
    bool Required,
    bool Disabled)
{
    /// <summary>
    /// The value for <c>aria-invalid</c>: <c>"true"</c> when invalid, otherwise
    /// <see langword="null"/> so the attribute is omitted entirely.
    /// </summary>
    /// <remarks>
    /// Omitting beats <c>aria-invalid="false"</c>: some screen readers announce the explicit
    /// false, which tells the user about a validity state they never asked about.
    /// </remarks>
    public string? AriaInvalid => Invalid ? "true" : null;

    /// <summary>The value for <c>aria-required</c>, or <see langword="null"/> when not required.</summary>
    public string? AriaRequired => Required ? "true" : null;
}
