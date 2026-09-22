namespace ZenithUI.Core;

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
