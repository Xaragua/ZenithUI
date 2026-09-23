namespace ZenithUI.Core;

/// <summary>
/// Maps the <see cref="ZenIntent"/> x <see cref="ZenVariant"/> x <see cref="ZenSize"/> matrix onto
/// Tailwind class strings. One place owns the visual language, so a change to how, say, an outline
/// button reads applies everywhere at once.
/// </summary>
/// <remarks>
/// <para>
/// <b>Every returned string is a literal.</b> That is not a style preference - Tailwind's scanner
/// works by finding class names as literal text in source files, so a class assembled at runtime
/// (<c>$"bg-{intent}"</c>) would never be emitted into the stylesheet. Switch expressions returning
/// whole literal strings are what make this file legible to the scanner; the library's Tailwind
/// build includes <c>Core/**/*.cs</c> in its <c>@source</c> list for exactly this reason.
/// </para>
/// <para>
/// Resting appearance and hover state are kept in separate methods. A badge and a button share a
/// palette but not a behaviour: giving a status chip a hover state tells the user it is clickable,
/// which is a lie. <see cref="Variant"/> composes the two, and only adds the hover half when the
/// caller says the element is interactive.
/// </para>
/// <para>
/// The token roles are honoured strictly: <c>-{intent}</c> only ever appears as a background,
/// <c>-content</c> only as text on that background, and <c>-strong</c> only as a foreground on a
/// surface or on <c>-soft</c>. See <c>Styles/tokens/base.css</c> for why that distinction exists.
/// </para>
/// </remarks>
public static class ZenStyles
{
    // ---- Resting appearance -----------------------------------------------------------------

    /// <summary>
    /// A solid fill: the intent colour as background, its <c>-content</c> colour as text.
    /// Highest emphasis.
    /// </summary>
    public static string Solid(ZenIntent intent) => intent switch
    {
        ZenIntent.Primary => "bg-primary text-primary-content",
        ZenIntent.Secondary => "bg-secondary text-secondary-content",
        ZenIntent.Accent => "bg-accent text-accent-content",
        ZenIntent.Success => "bg-success text-success-content",
        ZenIntent.Warning => "bg-warning text-warning-content",
        ZenIntent.Danger => "bg-danger text-danger-content",
        ZenIntent.Info => "bg-info text-info-content",

        // Neutral has no intent colour of its own; it borrows the surface stack, which is what
        // makes it usable as a default button on any background.
        _ => "bg-surface-raised text-content border border-border",
    };

    /// <summary>
    /// A tinted background with <c>-strong</c> text. Medium emphasis - the right default for
    /// badges and status chips.
    /// </summary>
    public static string Soft(ZenIntent intent) => intent switch
    {
        ZenIntent.Primary => "bg-primary-soft text-primary-strong",
        ZenIntent.Secondary => "bg-secondary-soft text-secondary-strong",
        ZenIntent.Accent => "bg-accent-soft text-accent-strong",
        ZenIntent.Success => "bg-success-soft text-success-strong",
        ZenIntent.Warning => "bg-warning-soft text-warning-strong",
        ZenIntent.Danger => "bg-danger-soft text-danger-strong",
        ZenIntent.Info => "bg-info-soft text-info-strong",
        _ => "bg-surface-sunken text-content",
    };

    /// <summary>Transparent with an intent-coloured border and <c>-strong</c> text.</summary>
    public static string Outline(ZenIntent intent) => intent switch
    {
        ZenIntent.Primary => "border border-primary text-primary-strong",
        ZenIntent.Secondary => "border border-secondary text-secondary-strong",
        ZenIntent.Accent => "border border-accent text-accent-strong",
        ZenIntent.Success => "border border-success text-success-strong",
        ZenIntent.Warning => "border border-warning text-warning-strong",
        ZenIntent.Danger => "border border-danger text-danger-strong",
        ZenIntent.Info => "border border-info text-info-strong",
        _ => "border border-border text-content",
    };

    /// <summary>Transparent. Lowest emphasis - toolbar and row actions.</summary>
    public static string Ghost(ZenIntent intent) => intent switch
    {
        ZenIntent.Primary => "text-primary-strong",
        ZenIntent.Secondary => "text-secondary-strong",
        ZenIntent.Accent => "text-accent-strong",
        ZenIntent.Success => "text-success-strong",
        ZenIntent.Warning => "text-warning-strong",
        ZenIntent.Danger => "text-danger-strong",
        ZenIntent.Info => "text-info-strong",
        _ => "text-content-muted",
    };

    // ---- Hover states -----------------------------------------------------------------------

    /// <summary>
    /// Hover for <see cref="Solid"/>: the background moves to <c>-strong</c>.
    /// </summary>
    /// <remarks>
    /// In the light palette that reads as darker and in the dark palette as lighter, which is the
    /// correct direction in both: hovering should increase contrast against the page, not move in
    /// a fixed direction.
    /// </remarks>
    public static string SolidHover(ZenIntent intent) => intent switch
    {
        ZenIntent.Primary => "hover:bg-primary-strong",
        ZenIntent.Secondary => "hover:bg-secondary-strong",
        ZenIntent.Accent => "hover:bg-accent-strong",
        ZenIntent.Success => "hover:bg-success-strong",
        ZenIntent.Warning => "hover:bg-warning-strong",
        ZenIntent.Danger => "hover:bg-danger-strong",
        ZenIntent.Info => "hover:bg-info-strong",
        _ => "hover:bg-surface-sunken",
    };

    /// <summary>Hover for <see cref="Soft"/>: promotes the chip to a solid fill.</summary>
    public static string SoftHover(ZenIntent intent) => intent switch
    {
        ZenIntent.Primary => "hover:bg-primary hover:text-primary-content",
        ZenIntent.Secondary => "hover:bg-secondary hover:text-secondary-content",
        ZenIntent.Accent => "hover:bg-accent hover:text-accent-content",
        ZenIntent.Success => "hover:bg-success hover:text-success-content",
        ZenIntent.Warning => "hover:bg-warning hover:text-warning-content",
        ZenIntent.Danger => "hover:bg-danger hover:text-danger-content",
        ZenIntent.Info => "hover:bg-info hover:text-info-content",
        _ => "hover:bg-surface-overlay",
    };

    /// <summary>Hover for <see cref="Outline"/> and <see cref="Ghost"/>: a soft tint appears.</summary>
    public static string TintHover(ZenIntent intent) => intent switch
    {
        ZenIntent.Primary => "hover:bg-primary-soft",
        ZenIntent.Secondary => "hover:bg-secondary-soft",
        ZenIntent.Accent => "hover:bg-accent-soft",
        ZenIntent.Success => "hover:bg-success-soft",
        ZenIntent.Warning => "hover:bg-warning-soft",
        ZenIntent.Danger => "hover:bg-danger-soft",
        ZenIntent.Info => "hover:bg-info-soft",
        _ => "hover:bg-surface-sunken hover:text-content",
    };

    // ---- Composition ------------------------------------------------------------------------

    /// <summary>Resting appearance for a variant, with no hover state.</summary>
    public static string VariantBase(ZenIntent intent, ZenVariant variant) => variant switch
    {
        ZenVariant.Solid => Solid(intent),
        ZenVariant.Soft => Soft(intent),
        ZenVariant.Outline => Outline(intent),
        _ => Ghost(intent),
    };

    /// <summary>The hover half for a variant.</summary>
    public static string VariantHover(ZenIntent intent, ZenVariant variant) => variant switch
    {
        ZenVariant.Solid => SolidHover(intent),
        ZenVariant.Soft => SoftHover(intent),
        _ => TintHover(intent),
    };

    /// <summary>Resting appearance plus, optionally, the hover state.</summary>
    /// <param name="intent">The semantic colour role.</param>
    /// <param name="variant">The visual weight.</param>
    /// <param name="interactive">
    /// <see langword="false"/> for a static element such as a badge, which must not appear
    /// clickable. Applies to every variant, not just <see cref="ZenVariant.Soft"/>.
    /// </param>
    public static string Variant(ZenIntent intent, ZenVariant variant, bool interactive = true) =>
        interactive
            ? $"{VariantBase(intent, variant)} {VariantHover(intent, variant)}"
            : VariantBase(intent, variant);

    // ---- Standalone helpers -----------------------------------------------------------------

    /// <summary>
    /// The intent's foreground colour on a plain surface - for icons, links and coloured text.
    /// Always the <c>-strong</c> token, which is the only one guaranteed readable there.
    /// </summary>
    public static string Foreground(ZenIntent intent) => intent switch
    {
        ZenIntent.Primary => "text-primary-strong",
        ZenIntent.Secondary => "text-secondary-strong",
        ZenIntent.Accent => "text-accent-strong",
        ZenIntent.Success => "text-success-strong",
        ZenIntent.Warning => "text-warning-strong",
        ZenIntent.Danger => "text-danger-strong",
        ZenIntent.Info => "text-info-strong",
        _ => "text-content",
    };

    /// <summary>
    /// The intent's solid fill alone, without a text colour - for a shape that carries no text of
    /// its own: a timeline marker, a status dot, a chart swatch.
    /// </summary>
    /// <remarks>
    /// Neutral falls back to <c>border-strong</c> rather than a surface token. A neutral dot has to
    /// be visible ON a surface, and every surface token is by definition the same lightness as the
    /// surface it would be drawn against.
    /// </remarks>
    public static string Fill(ZenIntent intent) => intent switch
    {
        ZenIntent.Primary => "bg-primary",
        ZenIntent.Secondary => "bg-secondary",
        ZenIntent.Accent => "bg-accent",
        ZenIntent.Success => "bg-success",
        ZenIntent.Warning => "bg-warning",
        ZenIntent.Danger => "bg-danger",
        ZenIntent.Info => "bg-info",
        _ => "bg-border-strong",
    };

    /// <summary>The intent's tinted background alone, without a text colour.</summary>
    public static string SoftSurface(ZenIntent intent) => intent switch
    {
        ZenIntent.Primary => "bg-primary-soft",
        ZenIntent.Secondary => "bg-secondary-soft",
        ZenIntent.Accent => "bg-accent-soft",
        ZenIntent.Success => "bg-success-soft",
        ZenIntent.Warning => "bg-warning-soft",
        ZenIntent.Danger => "bg-danger-soft",
        ZenIntent.Info => "bg-info-soft",
        _ => "bg-surface-sunken",
    };

    // ---- Sizing -----------------------------------------------------------------------------

    /// <summary>Square icon dimensions matching a control size.</summary>
    public static string IconSize(ZenSize size) => size switch
    {
        ZenSize.Small => "size-4",
        ZenSize.Large => "size-6",
        _ => "size-5",
    };

    /// <summary>Height, horizontal padding, gap and text size for a control with a text label.</summary>
    public static string ControlSize(ZenSize size) => size switch
    {
        ZenSize.Small => "h-8 gap-1.5 px-2.5 text-sm",
        ZenSize.Large => "h-11 gap-2.5 px-5 text-base",
        _ => "h-9 gap-2 px-3.5 text-sm",
    };

    /// <summary>Square dimensions for an icon-only control, matching <see cref="ControlSize"/> heights.</summary>
    public static string SquareControlSize(ZenSize size) => size switch
    {
        ZenSize.Small => "size-8",
        ZenSize.Large => "size-11",
        _ => "size-9",
    };

    /// <summary>Corner radius matching a control size.</summary>
    public static string ControlRadius(ZenSize size) => size switch
    {
        ZenSize.Small => "rounded-zen-sm",
        ZenSize.Large => "rounded-zen-lg",
        _ => "rounded-zen-md",
    };

    /// <summary>
    /// Classes shared by every interactive control: the focus ring, a colour transition, and the
    /// disabled treatment.
    /// </summary>
    /// <remarks>
    /// <c>disabled:pointer-events-none</c> matters for more than cosmetics - without it a disabled
    /// control still fires hover styles, and a disabled anchor (which the browser will happily
    /// follow) still navigates.
    /// </remarks>
    public const string InteractiveBase =
        "zen-focus inline-flex shrink-0 items-center justify-center font-medium transition-colors " +
        "disabled:pointer-events-none disabled:opacity-50 aria-disabled:pointer-events-none aria-disabled:opacity-50";

    // ---- Form controls ----------------------------------------------------------------------

    /// <summary>
    /// Classes shared by every text-entry control: surface, border, placeholder colour, and the
    /// focus and invalid treatments.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Note <c>zen-focus-border</c> rather than <c>zen-focus</c>. Input controls signal focus by
    /// recolouring their own border, not by drawing a ring outside it - a field already has a
    /// border in its resting state, and an outline around that produces two concentric lines.
    /// Buttons and links keep the ring, where there is no border to recolour.
    /// </para>
    /// <para>
    /// <c>aria-invalid:border-danger</c> is what makes a failing field read as failing before it is
    /// ever focused; <c>zen-focus-border</c> then keeps that danger colour through focus rather
    /// than replacing it with the ring colour.
    /// </para>
    /// </remarks>
    public const string InputBase =
        "zen-focus-border block w-full border border-border bg-surface text-content transition-colors " +
        "placeholder:text-content-subtle aria-invalid:border-danger " +
        "disabled:cursor-not-allowed disabled:opacity-50 read-only:bg-surface-sunken";

    /// <summary>
    /// The same treatment for a composite control whose border sits on a wrapper element, with the
    /// real input nested inside alongside icons or affixes.
    /// </summary>
    public const string InputWrapperBase =
        "zen-focus-border-within flex w-full items-center border border-border bg-surface text-content " +
        "transition-colors has-aria-invalid:border-danger " +
        "has-disabled:cursor-not-allowed has-disabled:opacity-50";

    /// <summary>The bare input inside an <see cref="InputWrapperBase"/> wrapper.</summary>
    /// <remarks>
    /// Transparent and borderless: the wrapper owns every visual affordance, so a background or
    /// border here would show as a second box inside the first.
    /// </remarks>
    public const string InputInnerBase =
        "min-w-0 flex-1 border-0 bg-transparent p-0 text-inherit placeholder:text-content-subtle " +
        "focus:outline-none disabled:cursor-not-allowed";

    /// <summary>Height, horizontal padding and text size for a text-entry control.</summary>
    public static string InputSize(ZenSize size) => size switch
    {
        ZenSize.Small => "h-8 px-2.5 text-sm",
        ZenSize.Large => "h-11 px-4 text-base",
        _ => "h-9 px-3 text-sm",
    };

    /// <summary>Padding and text size for a multi-line control, which has no fixed height.</summary>
    public static string TextAreaSize(ZenSize size) => size switch
    {
        ZenSize.Small => "px-2.5 py-1.5 text-sm",
        ZenSize.Large => "px-4 py-3 text-base",
        _ => "px-3 py-2 text-sm",
    };

    /// <summary>Corner radius matching an input size. Mirrors <see cref="ControlRadius"/>.</summary>
    public static string InputRadius(ZenSize size) => ControlRadius(size);

    /// <summary>Maximum width of a centred content column.</summary>
    /// <remarks>
    /// Returned alongside <c>mx-auto</c> by the shell components, never on its own - a max-width
    /// with no auto margin pins the column to the start edge, which on a wide display looks like a
    /// layout that forgot to finish.
    /// </remarks>
    public static string ContentWidth(ZenContentWidth width) => width switch
    {
        ZenContentWidth.Narrow => "max-w-3xl",
        ZenContentWidth.Medium => "max-w-5xl",
        ZenContentWidth.Wide => "max-w-7xl",
        _ => "max-w-none",
    };

    /// <summary>Shadow for a surface raised above the page.</summary>
    /// <param name="elevation">How far above the page the surface sits.</param>
    /// <returns>A shadow utility, or <see langword="null"/> for <see cref="ZenElevation.None"/>.</returns>
    public static string? Elevation(ZenElevation elevation) => elevation switch
    {
        ZenElevation.None => null,
        ZenElevation.Medium => "shadow-zen-md",
        ZenElevation.High => "shadow-zen-lg",
        _ => "shadow-zen-sm",
    };
}
