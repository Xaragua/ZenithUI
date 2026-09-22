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
