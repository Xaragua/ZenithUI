namespace ZenithUI.Core;

/// <summary>
/// The inline SVG paths ZenithUI needs for its own components.
/// </summary>
/// <remarks>
/// <para>
/// This is not an icon library and is not trying to become one. It exists so that installing
/// ZenithUI does not drag in an icon font or a second package: a chevron on a tree node and a
/// spinner on a loading button are part of the components, not decoration the consumer chose.
/// Everything here is drawn on a 24x24 grid with <c>currentColor</c> strokes, so it inherits
/// colour and size from whatever it sits in.
/// </para>
/// <para>
/// For application icons, pass your own markup to <c>ZenIcon</c>'s <c>ChildContent</c>, or use any
/// icon set you like. Nothing in the library assumes these.
/// </para>
/// </remarks>
public static class ZenIcons
{
    /// <summary>Checkmark. Selection, success states.</summary>
    public const string Check = """<path d="m5 13 4 4L19 7" />""";

    /// <summary>Cross. Dismiss, clear, close.</summary>
    public const string Close = """<path d="M18 6 6 18M6 6l12 12" />""";

    /// <summary>Chevron pointing down. Collapsed disclosure, select affordance.</summary>
    public const string ChevronDown = """<path d="m6 9 6 6 6-6" />""";

    /// <summary>Chevron pointing right. Collapsed tree node, breadcrumb separator.</summary>
    public const string ChevronRight = """<path d="m9 6 6 6-6 6" />""";

    /// <summary>Chevron pointing up.</summary>
    public const string ChevronUp = """<path d="m6 15 6-6 6 6" />""";

    /// <summary>Chevron pointing left. Previous month, previous page.</summary>
    public const string ChevronLeft = """<path d="m15 6-6 6 6 6" />""";

    /// <summary>Calendar. The date picker trigger.</summary>
    public const string Calendar = """<rect x="3" y="4.5" width="18" height="16" rx="2" /><path d="M3 9.5h18M8 3v3m8-3v3" />""";

    /// <summary>Magnifier. Search inputs.</summary>
    public const string Search = """<circle cx="11" cy="11" r="7" /><path d="m20 20-3.5-3.5" />""";

    /// <summary>Sun. Light theme.</summary>
    public const string Sun = """<circle cx="12" cy="12" r="4" /><path d="M12 2v2m0 16v2M4.93 4.93l1.41 1.41m11.32 11.32 1.41 1.41M2 12h2m16 0h2M4.93 19.07l1.41-1.41m11.32-11.32 1.41-1.41" />""";

    /// <summary>Moon. Dark theme.</summary>
    public const string Moon = """<path d="M21 12.79A9 9 0 1 1 11.21 3a7 7 0 0 0 9.79 9.79Z" />""";

    /// <summary>Monitor. Follow the system theme.</summary>
    public const string Monitor = """<rect x="2" y="3" width="20" height="14" rx="2" /><path d="M8 21h8m-4-4v4" />""";

    /// <summary>Circled "i". Informational callouts.</summary>
    public const string Info = """<circle cx="12" cy="12" r="9" /><path d="M12 11v5m0-8.5v.5" />""";

    /// <summary>Triangle with an exclamation. Warnings.</summary>
    public const string Warning = """<path d="M10.3 3.9 2.4 17a2 2 0 0 0 1.7 3h15.8a2 2 0 0 0 1.7-3L13.7 3.9a2 2 0 0 0-3.4 0Z" /><path d="M12 9v4m0 3.5v.5" />""";

    /// <summary>Circled exclamation. Errors and destructive confirmations.</summary>
    public const string Danger = """<circle cx="12" cy="12" r="9" /><path d="M12 8v5m0 3.5v.5" />""";

    /// <summary>Circled checkmark. Success confirmations.</summary>
    public const string Success = """<circle cx="12" cy="12" r="9" /><path d="m8.5 12.5 2.5 2.5 4.5-5" />""";

    /// <summary>Upward arrow. A positive trend on a stat card.</summary>
    public const string TrendUp = """<path d="M12 19V5m0 0-6 6m6-6 6 6" />""";

    /// <summary>Downward arrow. A negative trend on a stat card.</summary>
    public const string TrendDown = """<path d="M12 5v14m0 0 6-6m-6 6-6-6" />""";

    /// <summary>Horizontal dash. A flat trend on a stat card.</summary>
    public const string TrendFlat = """<path d="M5 12h14" />""";

    /// <summary>
    /// Spinner ring. A three-quarter arc rather than a full circle, because a full circle has no
    /// visible orientation and appears motionless while it rotates.
    /// </summary>
    public const string Spinner = """<path d="M21 12a9 9 0 1 1-6.22-8.56" />""";
}
