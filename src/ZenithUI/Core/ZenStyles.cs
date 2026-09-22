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
/// The token roles are honoured strictly: <c>-{intent}</c> only ever appears as a background,
/// <c>-content</c> only as text on that background, and <c>-strong</c> only as a foreground on a
/// surface or on <c>-soft</c>. See <c>Styles/tokens/base.css</c> for why that distinction exists.
/// </para>
/// </remarks>
public static class ZenStyles
{
    /// <summary>
    /// A solid fill: the intent colour as background, its <c>-content</c> colour as text.
    /// Highest emphasis.
    /// </summary>
    /// <remarks>
    /// The hover state moves the background to <c>-strong</c>. In the light palette that reads as
    /// darker and in the dark palette as lighter, which is the correct direction in both: a hover
    /// should increase contrast against the page, not move in a fixed direction.
    /// </remarks>
    public static string Solid(ZenIntent intent) => intent switch
    {
        ZenIntent.Primary => "bg-primary text-primary-content hover:bg-primary-strong",
        ZenIntent.Secondary => "bg-secondary text-secondary-content hover:bg-secondary-strong",
        ZenIntent.Accent => "bg-accent text-accent-content hover:bg-accent-strong",
        ZenIntent.Success => "bg-success text-success-content hover:bg-success-strong",
        ZenIntent.Warning => "bg-warning text-warning-content hover:bg-warning-strong",
        ZenIntent.Danger => "bg-danger text-danger-content hover:bg-danger-strong",
        ZenIntent.Info => "bg-info text-info-content hover:bg-info-strong",

        // Neutral has no intent colour of its own; it borrows the surface stack, which is what
        // makes it usable as a default button on any background.
        _ => "bg-surface-raised text-content border border-border hover:bg-surface-sunken",
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
        _ => "bg-surface-sunken text-content-muted",
    };

    /// <summary>Soft, plus a hover state. For interactive elements rather than static chips.</summary>
    public static string SoftInteractive(ZenIntent intent) => intent switch
    {
        ZenIntent.Primary => "bg-primary-soft text-primary-strong hover:bg-primary hover:text-primary-content",
        ZenIntent.Secondary => "bg-secondary-soft text-secondary-strong hover:bg-secondary hover:text-secondary-content",
        ZenIntent.Accent => "bg-accent-soft text-accent-strong hover:bg-accent hover:text-accent-content",
        ZenIntent.Success => "bg-success-soft text-success-strong hover:bg-success hover:text-success-content",
        ZenIntent.Warning => "bg-warning-soft text-warning-strong hover:bg-warning hover:text-warning-content",
        ZenIntent.Danger => "bg-danger-soft text-danger-strong hover:bg-danger hover:text-danger-content",
        ZenIntent.Info => "bg-info-soft text-info-strong hover:bg-info hover:text-info-content",
        _ => "bg-surface-sunken text-content hover:bg-surface-overlay",
    };

    /// <summary>Transparent with an intent-coloured border and <c>-strong</c> text.</summary>
    public static string Outline(ZenIntent intent) => intent switch
    {
        ZenIntent.Primary => "border border-primary text-primary-strong hover:bg-primary-soft",
        ZenIntent.Secondary => "border border-secondary text-secondary-strong hover:bg-secondary-soft",
        ZenIntent.Accent => "border border-accent text-accent-strong hover:bg-accent-soft",
        ZenIntent.Success => "border border-success text-success-strong hover:bg-success-soft",
        ZenIntent.Warning => "border border-warning text-warning-strong hover:bg-warning-soft",
        ZenIntent.Danger => "border border-danger text-danger-strong hover:bg-danger-soft",
        ZenIntent.Info => "border border-info text-info-strong hover:bg-info-soft",
        _ => "border border-border text-content hover:bg-surface-sunken",
    };

    /// <summary>Transparent until hovered. Lowest emphasis - toolbar and row actions.</summary>
    public static string Ghost(ZenIntent intent) => intent switch
    {
        ZenIntent.Primary => "text-primary-strong hover:bg-primary-soft",
        ZenIntent.Secondary => "text-secondary-strong hover:bg-secondary-soft",
        ZenIntent.Accent => "text-accent-strong hover:bg-accent-soft",
        ZenIntent.Success => "text-success-strong hover:bg-success-soft",
        ZenIntent.Warning => "text-warning-strong hover:bg-warning-soft",
        ZenIntent.Danger => "text-danger-strong hover:bg-danger-soft",
        ZenIntent.Info => "text-info-strong hover:bg-info-soft",
        _ => "text-content-muted hover:bg-surface-sunken hover:text-content",
    };

    /// <summary>Dispatches to the matching variant helper.</summary>
    /// <param name="intent">The semantic colour role.</param>
    /// <param name="variant">The visual weight.</param>
    /// <param name="interactive">
    /// When <see langword="true"/>, <see cref="ZenVariant.Soft"/> gains a hover state. Static chips
    /// should pass <see langword="false"/> so they do not appear clickable.
    /// </param>
    public static string Variant(ZenIntent intent, ZenVariant variant, bool interactive = true) => variant switch
    {
        ZenVariant.Solid => Solid(intent),
        ZenVariant.Soft => interactive ? SoftInteractive(intent) : Soft(intent),
        ZenVariant.Outline => Outline(intent),
        _ => Ghost(intent),
    };

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
}
