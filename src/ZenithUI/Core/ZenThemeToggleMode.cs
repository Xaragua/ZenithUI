namespace ZenithUI.Core;

/// <summary>Shape rendered by the theme switcher.</summary>
public enum ZenThemeToggleMode
{
    /// <summary>
    /// A single button that flips between light and dark. Compact enough for an app bar, but it
    /// cannot express "follow the system" - use <see cref="Segmented"/> where that matters.
    /// </summary>
    Toggle,

    /// <summary>
    /// Three mutually exclusive buttons in a radiogroup: System, Light, Dark. The right choice
    /// for a settings page, where following the operating system must be selectable explicitly.
    /// </summary>
    Segmented,
}
