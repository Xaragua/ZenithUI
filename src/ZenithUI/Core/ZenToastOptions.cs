namespace ZenithUI;

/// <summary>How one toast behaves and what it offers.</summary>
public sealed class ZenToastOptions
{
    /// <summary>Optional bold heading above the message.</summary>
    public string? Title { get; set; }

    /// <summary>
    /// How long the toast stays before dismissing itself. <see cref="TimeSpan.Zero"/> means it
    /// stays until dismissed.
    /// </summary>
    /// <remarks>
    /// Five seconds by default, which is short for a sentence and far too short for a paragraph.
    /// A toast carrying anything a user must actually read, or any action they must take, should
    /// set this to zero and let them close it.
    /// </remarks>
    public TimeSpan Duration { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>Show the dismiss button.</summary>
    public bool ShowCloseButton { get; set; } = true;

    /// <summary>Label on an optional action button.</summary>
    public string? ActionText { get; set; }

    /// <summary>Invoked when the action button is pressed.</summary>
    /// <remarks>
    /// The toast dismisses itself after this runs. A toast whose action leaves it on screen reads
    /// as a failed click.
    /// </remarks>
    public Func<Task>? OnAction { get; set; }

    /// <summary>Invoked when the toast leaves the screen, however it goes.</summary>
    public Func<Task>? OnDismissed { get; set; }

    /// <summary>
    /// Tint the toast with its intent's <c>-soft</c> background instead of the neutral overlay
    /// surface.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Off by default, and that default is a judgement rather than an accident. A stack of tinted
    /// toasts is louder than a stack of neutral ones, and the icon already carries the intent; the
    /// tint is a second signal for cases where it earns its place - a single error on a busy page,
    /// or a design that leans on colour.
    /// </para>
    /// <para>
    /// <see cref="ZenIntent.Neutral"/> has no <c>-soft</c> token, so it keeps the overlay surface
    /// either way.
    /// </para>
    /// </remarks>
    public bool Soft { get; set; }

    /// <summary>Extra CSS classes for the toast surface.</summary>
    public string? Class { get; set; }
}
