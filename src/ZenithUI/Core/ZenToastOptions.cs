namespace ZenithUI.Core;

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

    /// <summary>Extra CSS classes for the toast surface.</summary>
    public string? Class { get; set; }
}
