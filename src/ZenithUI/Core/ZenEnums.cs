namespace ZenithUI;

/// <summary>Control size scale shared by every ZenithUI component that has one.</summary>
public enum ZenSize
{
    /// <summary>Compact: dense tables, toolbars, inline filters.</summary>
    Small,

    /// <summary>The default size for forms and standalone controls.</summary>
    Medium,

    /// <summary>Prominent: primary calls to action, marketing surfaces.</summary>
    Large,
}

/// <summary>
/// Semantic colour role. Maps to the <c>--zen-{intent}</c> token family, never to a literal
/// colour, so a rebrand is a token override rather than a markup change.
/// </summary>
public enum ZenIntent
{
    /// <summary>Inherits the surrounding surface and content colours. The default.</summary>
    Neutral,

    /// <summary>The application's main brand action.</summary>
    Primary,

    /// <summary>A supporting action of lower emphasis than <see cref="Primary"/>.</summary>
    Secondary,

    /// <summary>A highlight colour for emphasis that is not an action.</summary>
    Accent,

    /// <summary>Confirmation of a completed or healthy state.</summary>
    Success,

    /// <summary>A condition that needs attention but is not an error.</summary>
    Warning,

    /// <summary>An error, destructive action, or failed state.</summary>
    Danger,

    /// <summary>Neutral informational emphasis.</summary>
    Info,
}

/// <summary>Visual weight of a control against its surface.</summary>
public enum ZenVariant
{
    /// <summary>Filled with the intent colour; highest emphasis.</summary>
    Solid,

    /// <summary>Tinted with the intent's <c>-soft</c> token; medium emphasis.</summary>
    Soft,

    /// <summary>Transparent with an intent-coloured border.</summary>
    Outline,

    /// <summary>Transparent until hovered; lowest emphasis.</summary>
    Ghost,
}

/// <summary>Shadow depth, suggesting how far a surface sits above the page.</summary>
public enum ZenElevation
{
    /// <summary>Flat. Relies on border and background alone.</summary>
    None,

    /// <summary>A hairline shadow. The default for cards.</summary>
    Low,

    /// <summary>A visible lift, for surfaces that float over content.</summary>
    Medium,

    /// <summary>A pronounced lift, for overlays, popovers and drawers.</summary>
    High,
}

/// <summary>Silhouette drawn by a loading placeholder.</summary>
public enum ZenSkeletonShape
{
    /// <summary>A short bar at text height. Multiple lines render the last one short.</summary>
    Text,

    /// <summary>A circle, for avatars and icon placeholders.</summary>
    Circle,

    /// <summary>A large block, for images, cards and chart areas.</summary>
    Rectangle,
}

/// <summary>Shape drawn by a progress indicator.</summary>
public enum ZenProgressShape
{
    /// <summary>A horizontal bar. The default, and the right choice when there is width to spare.</summary>
    Linear,

    /// <summary>A ring. For tight spaces and inline use.</summary>
    Circular,
}

/// <summary>
/// A corner of an element, in logical terms so the anchor follows the writing direction.
/// </summary>
public enum ZenCorner
{
    /// <summary>Top, on the side text starts from.</summary>
    TopStart,

    /// <summary>Top, on the side text ends at. The default for a notification badge.</summary>
    TopEnd,

    /// <summary>Bottom, on the side text starts from.</summary>
    BottomStart,

    /// <summary>Bottom, on the side text ends at.</summary>
    BottomEnd,
}

/// <summary>Direction of change shown by a stat card's delta.</summary>
public enum ZenTrend
{
    /// <summary>No delta shown.</summary>
    None,

    /// <summary>Value rose.</summary>
    Up,

    /// <summary>Value fell.</summary>
    Down,

    /// <summary>Value held steady.</summary>
    Flat,
}

/// <summary>The theme a user can choose.</summary>
public enum ZenThemeMode
{
    /// <summary>Follow the operating system's <c>prefers-color-scheme</c>. The default.</summary>
    System,

    /// <summary>Force the light palette regardless of the system preference.</summary>
    Light,

    /// <summary>Force the dark palette regardless of the system preference.</summary>
    Dark,
}

/// <summary>The palette actually in effect once <see cref="ZenThemeMode.System"/> is resolved.</summary>
public enum ZenTheme
{
    /// <summary>The light palette from <c>tokens/base.css</c>.</summary>
    Light,

    /// <summary>The dark palette from <c>tokens/dark.css</c>.</summary>
    Dark,
}

/// <summary>
/// Where a floating panel sits relative to the element it is anchored to.
/// </summary>
/// <remarks>
/// <para>
/// Named logically, so a component does not have to know the writing direction: <c>Start</c> and
/// <c>End</c> are the sides text begins and ends at, which swap under <c>dir="rtl"</c>. The
/// resolution to physical sides happens in <c>zen-popover.js</c>, because the browser is the only
/// party that knows the direction in force - it can come from a <c>dir</c> attribute anywhere up
/// the tree or from a stylesheet.
/// </para>
/// <para>
/// The second word is the alignment along the chosen side. A bare side centres.
/// </para>
/// <para>
/// A placement is a preference, not a guarantee. A panel that would leave the viewport flips to
/// the opposite side and shifts along its axis to stay visible.
/// </para>
/// </remarks>
public enum ZenPlacement
{
    /// <summary>Below the anchor, leading edges aligned. The default for menus and listboxes.</summary>
    BottomStart,

    /// <summary>Below the anchor, centred.</summary>
    Bottom,

    /// <summary>Below the anchor, trailing edges aligned.</summary>
    BottomEnd,

    /// <summary>Above the anchor, leading edges aligned.</summary>
    TopStart,

    /// <summary>Above the anchor, centred.</summary>
    Top,

    /// <summary>Above the anchor, trailing edges aligned.</summary>
    TopEnd,

    /// <summary>On the side text starts from, top edges aligned.</summary>
    StartTop,

    /// <summary>On the side text starts from, centred.</summary>
    Start,

    /// <summary>On the side text starts from, bottom edges aligned.</summary>
    StartBottom,

    /// <summary>On the side text ends at, top edges aligned.</summary>
    EndTop,

    /// <summary>On the side text ends at, centred.</summary>
    End,

    /// <summary>On the side text ends at, bottom edges aligned.</summary>
    EndBottom,
}

/// <summary>How wide a modal dialog is allowed to grow.</summary>
/// <remarks>
/// A maximum rather than a fixed width: every size still shrinks to fit a narrow viewport, so a
/// dialog never overflows a phone screen. The scale is separate from <see cref="ZenSize"/>, which
/// describes control height and text size - a dialog has neither.
/// </remarks>
public enum ZenModalSize
{
    /// <summary>Narrow. Confirmations and single-field prompts.</summary>
    Small,

    /// <summary>The default. A short form.</summary>
    Medium,

    /// <summary>A longer form, or content with a table in it.</summary>
    Large,

    /// <summary>Close to the viewport width, for editors and detail views.</summary>
    ExtraLarge,

    /// <summary>The whole viewport. For an immersive editing surface on small screens.</summary>
    Full,
}

/// <summary>Which corner (or edge) toasts stack in.</summary>
/// <remarks>
/// Logical inline sides, so a right-to-left document moves the stack to the other side without the
/// consumer changing anything. The block axis stays physical: top is top in every writing mode
/// this library targets.
/// </remarks>
public enum ZenToastPosition
{
    /// <summary>Top, on the side text starts from.</summary>
    TopStart,

    /// <summary>Top, centred.</summary>
    TopCenter,

    /// <summary>Top, on the side text ends at. The common default for desktop.</summary>
    TopEnd,

    /// <summary>Bottom, on the side text starts from.</summary>
    BottomStart,

    /// <summary>Bottom, centred. Reads well on narrow screens, near the thumb.</summary>
    BottomCenter,

    /// <summary>Bottom, on the side text ends at.</summary>
    BottomEnd,
}

/// <summary>
/// Horizontal alignment of a table column's contents or of a block of text.
/// </summary>
/// <remarks>
/// Logical rather than physical, so a right-to-left document does not need every numeric column
/// or centred heading re-specified. <see cref="End"/> is the correct choice for numbers in both
/// directions: digits line up by place value only when they share a trailing edge.
/// </remarks>
public enum ZenAlign
{
    /// <summary>The side text starts from. The default, and right for text.</summary>
    Start,

    /// <summary>Centred. For short status values and icons.</summary>
    Center,

    /// <summary>The side text ends at. For numbers, amounts and dates.</summary>
    End,
}

/// <summary>
/// How wide a centred content column may grow before it stops and gutters take over.
/// </summary>
/// <remarks>
/// An enum rather than a caller-supplied class, because a consumer who does not run Tailwind has
/// no way to add <c>max-w-5xl</c> to a precompiled stylesheet. The values here are literals the
/// library's own build emits, so they exist in <c>zenith.css</c> whatever the consumer does.
/// </remarks>
public enum ZenContentWidth
{
    /// <summary>Edge to edge. The right choice for a dashboard or an app with its own side rail.</summary>
    Full,

    /// <summary>A reading measure. Documentation, settings, a single form.</summary>
    Narrow,

    /// <summary>The default for a content page with a header and a footer.</summary>
    Medium,

    /// <summary>Roomy, but still bounded on a very wide display.</summary>
    Wide,
}

/// <summary>The axis a navigation menu lays its links out along.</summary>
public enum ZenNavOrientation
{
    /// <summary>Stacked. The side nav case, and the default.</summary>
    Vertical,

    /// <summary>In a row. An app bar's section links.</summary>
    Horizontal,
}

/// <summary>The visual style of a run of text: its size, weight, leading and tracking.</summary>
/// <remarks>
/// A look, not an element. <c>ZenText</c> derives a sensible element from the variant, and its
/// <c>As</c> parameter overrides that, so a heading can sit at the level the document outline
/// needs while looking the size the design needs.
/// </remarks>
public enum ZenTextVariant
{
    /// <summary>Hero text. Larger than any heading; one per page at most.</summary>
    Display,

    /// <summary>A page title.</summary>
    H1,

    /// <summary>A major section title.</summary>
    H2,

    /// <summary>A subsection title.</summary>
    H3,

    /// <summary>A card or panel title.</summary>
    H4,

    /// <summary>A small group title.</summary>
    H5,

    /// <summary>The smallest heading, at body-small size.</summary>
    H6,

    /// <summary>An introductory paragraph, larger and quieter than body text.</summary>
    Lead,

    /// <summary>Running text. The default.</summary>
    Body,

    /// <summary>Secondary text at a smaller size.</summary>
    Small,

    /// <summary>A short annotation: a figure caption, a timestamp, a footnote.</summary>
    Caption,

    /// <summary>A small uppercase label that sits above a heading.</summary>
    Overline,
}

/// <summary>The HTML element a <c>ZenText</c> renders.</summary>
public enum ZenTextElement
{
    /// <summary><c>&lt;h1&gt;</c>.</summary>
    H1,

    /// <summary><c>&lt;h2&gt;</c>.</summary>
    H2,

    /// <summary><c>&lt;h3&gt;</c>.</summary>
    H3,

    /// <summary><c>&lt;h4&gt;</c>.</summary>
    H4,

    /// <summary><c>&lt;h5&gt;</c>.</summary>
    H5,

    /// <summary><c>&lt;h6&gt;</c>.</summary>
    H6,

    /// <summary><c>&lt;p&gt;</c>, a paragraph.</summary>
    P,

    /// <summary><c>&lt;span&gt;</c>, inline text with no meaning of its own.</summary>
    Span,

    /// <summary><c>&lt;div&gt;</c>, a block with no meaning of its own.</summary>
    Div,

    /// <summary><c>&lt;label&gt;</c>. Pair it with a <c>for</c> attribute.</summary>
    Label,

    /// <summary><c>&lt;strong&gt;</c>, text of strong importance.</summary>
    Strong,

    /// <summary><c>&lt;em&gt;</c>, stressed emphasis.</summary>
    Em,

    /// <summary><c>&lt;small&gt;</c>, side comments and fine print.</summary>
    Small,

    /// <summary><c>&lt;blockquote&gt;</c>, a quotation from another source.</summary>
    Blockquote,
}

/// <summary>The colour role of a run of text.</summary>
/// <remarks>
/// The intent tones map to the <c>--zen-{intent}-strong</c> token, the one role the contrast audit
/// guarantees as a foreground on every surface. Never the bare intent, which is a fill.
/// </remarks>
public enum ZenTextTone
{
    /// <summary>The primary content colour.</summary>
    Default,

    /// <summary>Secondary text: descriptions, supporting copy.</summary>
    Muted,

    /// <summary>Tertiary text: placeholders, meta information.</summary>
    Subtle,

    /// <summary>Text on an inverted surface.</summary>
    Inverted,

    /// <summary>No colour of its own; takes the colour of the surrounding element.</summary>
    Inherit,

    /// <summary>The primary intent, as text.</summary>
    Primary,

    /// <summary>The secondary intent, as text.</summary>
    Secondary,

    /// <summary>The accent intent, as text.</summary>
    Accent,

    /// <summary>The success intent, as text.</summary>
    Success,

    /// <summary>The warning intent, as text.</summary>
    Warning,

    /// <summary>The danger intent, as text.</summary>
    Danger,

    /// <summary>The info intent, as text.</summary>
    Info,
}

/// <summary>Font weight of a run of text.</summary>
public enum ZenTextWeight
{
    /// <summary>400.</summary>
    Regular,

    /// <summary>500.</summary>
    Medium,

    /// <summary>600.</summary>
    Semibold,

    /// <summary>700.</summary>
    Bold,
}
