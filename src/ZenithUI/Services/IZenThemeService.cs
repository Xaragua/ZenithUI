namespace ZenithUI;

/// <summary>
/// Reads and writes the active theme, persists the user's choice, and notifies subscribers when
/// the effective palette changes.
/// </summary>
/// <remarks>
/// <para>
/// The service is registered scoped. On Blazor Server that means one instance per circuit (one
/// browser tab); on WebAssembly, one per application. Either way it maps to exactly one document,
/// which is what makes a per-instance cache of the current theme correct.
/// </para>
/// <para>
/// Before <see cref="InitializeAsync"/> has run - during static SSR and the prerender pass -
/// <see cref="Mode"/> reports <see cref="ZenThemeMode.System"/> and <see cref="Theme"/> reports
/// <see cref="ZenTheme.Light"/>. The server genuinely cannot know the viewer's preference, so any
/// UI bound to these values must tolerate being wrong for one render. The FOUC-prevention script
/// rendered by the <c>ZenThemeScript</c> component is what stops that uncertainty from being visible.
/// </para>
/// </remarks>
public interface IZenThemeService
{
    /// <summary>The user's choice: light, dark, or follow the system.</summary>
    ZenThemeMode Mode { get; }

    /// <summary>
    /// The palette actually in effect, with <see cref="ZenThemeMode.System"/> already resolved
    /// against the operating system's preference.
    /// </summary>
    ZenTheme Theme { get; }

    /// <summary>
    /// <see langword="true"/> once the real preference has been read from the browser. While
    /// this is <see langword="false"/>, <see cref="Mode"/> and <see cref="Theme"/> are defaults
    /// rather than facts.
    /// </summary>
    bool IsInitialized { get; }

    /// <summary>
    /// Raised whenever <see cref="Mode"/> or <see cref="Theme"/> changes, including when the
    /// operating system flips its preference while the app is in <see cref="ZenThemeMode.System"/>.
    /// </summary>
    /// <remarks>
    /// Subscribers are responsible for unsubscribing - implement <see cref="IDisposable"/> on the
    /// component and detach in <c>Dispose</c>, or the service (which outlives an individual
    /// component) keeps the component alive.
    /// </remarks>
    event EventHandler<ZenThemeChangedEventArgs>? ThemeChanged;

    /// <summary>
    /// Reads the persisted preference and the system setting from the browser and starts
    /// listening for system changes. Safe to call more than once; safe to call when no JavaScript
    /// runtime is available, in which case it is a no-op.
    /// </summary>
    Task InitializeAsync();

    /// <summary>Applies and persists a new theme choice.</summary>
    /// <param name="mode">The mode to switch to.</param>
    Task SetModeAsync(ZenThemeMode mode);

    /// <summary>
    /// Flips between light and dark. When the current mode is
    /// <see cref="ZenThemeMode.System"/>, switches to the explicit opposite of whatever the
    /// system currently resolves to, which is what a user pressing a toggle expects.
    /// </summary>
    Task ToggleAsync();
}

/// <summary>Describes a theme change.</summary>
/// <param name="Mode">The mode now selected.</param>
/// <param name="Theme">The palette now in effect.</param>
public sealed record ZenThemeChangedEventArgs(ZenThemeMode Mode, ZenTheme Theme);
