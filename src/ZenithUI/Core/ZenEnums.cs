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

/// <summary>The space between the items of a layout component.</summary>
/// <remarks>
/// A scale rather than a length, for the reason <see cref="ZenContentWidth"/> is one: a consumer
/// who does not run Tailwind cannot add <c>gap-5</c> to a precompiled stylesheet, so every step
/// here is a literal the library's own build emits. Separate from <see cref="ZenSize"/>, which
/// sizes controls rather than the room between them.
/// </remarks>
public enum ZenSpace
{
    /// <summary>No space. Items touch.</summary>
    None,

    /// <summary>0.25rem. An icon and its label.</summary>
    Xs,

    /// <summary>0.5rem. A cluster of badges or small buttons.</summary>
    Sm,

    /// <summary>1rem. Cards in a grid, fields in a form. The default.</summary>
    Md,

    /// <summary>1.5rem. Groups within a section.</summary>
    Lg,

    /// <summary>2rem. Sections of a page.</summary>
    Xl,

    /// <summary>3rem. Major regions of a long page.</summary>
    Xxl,
}

/// <summary>The axis a <c>ZenStack</c> lays its items out along.</summary>
public enum ZenDirection
{
    /// <summary>Top to bottom. The default.</summary>
    Vertical,

    /// <summary>In a row, following the writing direction.</summary>
    Horizontal,
}

/// <summary>
/// A viewport width from which a responsive layout parameter applies. Tailwind's default
/// breakpoints, so they line up with every responsive class inside the library.
/// </summary>
public enum ZenBreakpoint
{
    /// <summary>640px and wider.</summary>
    Sm,

    /// <summary>768px and wider.</summary>
    Md,

    /// <summary>1024px and wider.</summary>
    Lg,

    /// <summary>1280px and wider.</summary>
    Xl,
}

/// <summary>How the items of a <c>ZenStack</c> line up across its axis.</summary>
public enum ZenCrossAlign
{
    /// <summary>Each item fills the stack's cross axis. What a stack does when nothing is set.</summary>
    Stretch,

    /// <summary>Against the start edge.</summary>
    Start,

    /// <summary>Centred. An icon beside a line of text.</summary>
    Center,

    /// <summary>Against the end edge.</summary>
    End,

    /// <summary>On a shared text baseline. Labels and values of different sizes in a row.</summary>
    Baseline,
}

/// <summary>How a <c>ZenStack</c> distributes spare room along its axis.</summary>
public enum ZenJustify
{
    /// <summary>Packed at the start. What a stack does when nothing is set.</summary>
    Start,

    /// <summary>Packed in the middle.</summary>
    Center,

    /// <summary>Packed at the end. A row of dialog actions.</summary>
    End,

    /// <summary>The first item at the start, the last at the end, the rest spread between.</summary>
    Between,
}

/// <summary>The HTML element a layout component renders.</summary>
/// <remarks>
/// A layout component has no role of its own, so the element is what carries the meaning. Pick
/// the one that describes the content: a list of cards is a <see cref="Ul"/>, and then each item
/// must be an <see cref="Li"/>.
/// </remarks>
public enum ZenLayoutElement
{
    /// <summary><c>&lt;div&gt;</c>, no meaning of its own. The default.</summary>
    Div,

    /// <summary><c>&lt;section&gt;</c>, a thematic group, usually with a heading.</summary>
    Section,

    /// <summary><c>&lt;article&gt;</c>, a self-contained piece that could stand alone.</summary>
    Article,

    /// <summary><c>&lt;header&gt;</c>, introductory content for its nearest section.</summary>
    Header,

    /// <summary><c>&lt;footer&gt;</c>, closing content for its nearest section.</summary>
    Footer,

    /// <summary><c>&lt;nav&gt;</c>, a navigation landmark. Give it an <c>aria-label</c>.</summary>
    Nav,

    /// <summary><c>&lt;aside&gt;</c>, content tangential to what surrounds it.</summary>
    Aside,

    /// <summary><c>&lt;ul&gt;</c>, an unordered list. Its items must be <see cref="Li"/>.</summary>
    Ul,

    /// <summary><c>&lt;ol&gt;</c>, an ordered list. Its items must be <see cref="Li"/>.</summary>
    Ol,

    /// <summary><c>&lt;li&gt;</c>, an item of a <see cref="Ul"/> or <see cref="Ol"/>.</summary>
    Li,
}

/// <summary>When a <c>ZenLookup</c> writes the row the user picked into its value.</summary>
public enum ZenLookupCommit
{
    /// <summary>
    /// On the pick itself: a click or Enter selects the row and closes the panel. The default,
    /// and the way ZenCombobox behaves.
    /// </summary>
    Immediate,

    /// <summary>
    /// On a Confirm button. A click or Enter only marks the row; the panel stays open until the
    /// user confirms, and closing it any other way discards the choice. For picks that are
    /// expensive to undo - binding a contract to a vendor, say - where one stray click should
    /// not be enough.
    /// </summary>
    Confirm,
}
