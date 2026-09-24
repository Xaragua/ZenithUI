namespace ZenithUI;

/// <summary>
/// Raised by a <c>ZenStep</c> as the user tries to leave it. Set <see cref="Cancel"/> to stay.
/// </summary>
/// <param name="FromIndex">The step being left.</param>
/// <param name="ToIndex">The step the user is heading for.</param>
public sealed class ZenStepLeavingArgs(int FromIndex, int ToIndex)
{
    /// <summary>The step being left.</summary>
    public int FromIndex { get; } = FromIndex;

    /// <summary>The step the user is heading for.</summary>
    public int ToIndex { get; } = ToIndex;

    /// <summary>Whether the move is forward, towards Finish.</summary>
    /// <remarks>
    /// Worth checking before validating: blocking Back until the current step is valid traps a
    /// user who went forward to look at something and wants to fix an earlier answer.
    /// </remarks>
    public bool IsForward => ToIndex > FromIndex;

    /// <summary>Set to <see langword="true"/> to keep the user on this step.</summary>
    public bool Cancel { get; set; }
}

/// <summary>
/// What a <c>ZenStepper</c>'s <c>NavigationTemplate</c> is given to build its own buttons with.
/// </summary>
public sealed class ZenStepperContext
{
    private readonly Func<Task> _next;
    private readonly Func<Task> _back;
    private readonly Func<int, Task> _goTo;

    internal ZenStepperContext(
        int index,
        int count,
        bool canGoBack,
        bool isLast,
        bool busy,
        Func<Task> next,
        Func<Task> back,
        Func<int, Task> goTo)
    {
        Index = index;
        Count = count;
        CanGoBack = canGoBack;
        IsLast = isLast;
        Busy = busy;
        _next = next;
        _back = back;
        _goTo = goTo;
    }

    /// <summary>The active step, zero-based.</summary>
    public int Index { get; }

    /// <summary>How many steps there are.</summary>
    public int Count { get; }

    /// <summary>Whether there is an earlier step to go back to.</summary>
    public bool CanGoBack { get; }

    /// <summary>Whether this is the last step, where Next becomes Finish.</summary>
    public bool IsLast { get; }

    /// <summary>Whether a step change is being validated. Disable the buttons while it is.</summary>
    public bool Busy { get; }

    /// <summary>Validates the step and moves forward - or finishes, on the last step.</summary>
    public Task NextAsync() => _next();

    /// <summary>Moves back one step. Never validates.</summary>
    public Task BackAsync() => _back();

    /// <summary>Moves to a step, if the stepper's rules allow it.</summary>
    public Task GoToAsync(int index) => _goTo(index);
}
