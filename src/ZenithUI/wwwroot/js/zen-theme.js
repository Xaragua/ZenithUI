/**
 * ZenithUI theme module.
 *
 * Owns exactly one piece of DOM state: the `data-zen-theme` attribute on <html>. The CSS in
 * tokens/dark.css does the rest, which is why switching palettes never touches a class list and
 * never re-renders a component tree.
 *
 * Mirrors the logic in the inline FOUC-prevention snippet (see ZenThemeScript.razor). If you
 * change the storage key or the attribute name, change it in both places.
 *
 * ## One module, many .NET listeners
 *
 * A dynamic `import()` of the same URL yields the SAME module instance for the lifetime of the
 * page, so this module's state is shared by every caller. That matters more than it first looks:
 * a Blazor Web App can host an Interactive Server island and an Interactive WebAssembly island on
 * one page, and those run in different .NET runtimes with different DI containers - two separate
 * IZenThemeService instances, both importing this one module.
 *
 * So listeners are kept in a Set rather than a single slot. Storing one reference meant the last
 * island to initialize silently displaced the others, and the first island to dispose tore down
 * the shared media listener for everyone. Broadcasting to the Set also keeps every island's UI in
 * agreement: change the theme from one and the rest update their own selection, instead of sitting
 * on a stale value while the page around them repaints.
 */

const STORAGE_KEY = 'zen-theme';
const ATTRIBUTE = 'data-zen-theme';
const TRANSITION_CLASS = 'zen-theme-transition';
const VALID_MODES = ['system', 'light', 'dark'];

/**
 * DotNetObjectReference instances to notify. A Set, not a single slot - see the note above.
 * @type {Set<object>}
 */
const listeners = new Set();

/** @type {MediaQueryList | null} */
let mediaQuery = null;

/** @type {((event: MediaQueryListEvent) => void) | null} */
let mediaListener = null;

/**
 * Reads the persisted preference.
 *
 * localStorage access is wrapped because it throws outright in a few real situations: Safari in
 * private mode with storage disabled, and any page served in a partitioned or sandboxed iframe.
 * A theme preference is not worth breaking the page over, so failures degrade to 'system'.
 *
 * @returns {'system' | 'light' | 'dark'}
 */
function readStoredMode() {
    try {
        const stored = window.localStorage.getItem(STORAGE_KEY);
        return VALID_MODES.includes(stored) ? stored : 'system';
    } catch {
        return 'system';
    }
}

/**
 * Persists the preference, ignoring storage failures for the reasons above.
 * @param {'system' | 'light' | 'dark'} mode
 */
function writeStoredMode(mode) {
    try {
        if (mode === 'system') {
            window.localStorage.removeItem(STORAGE_KEY);
        } else {
            window.localStorage.setItem(STORAGE_KEY, mode);
        }
    } catch {
        /* the preference simply will not survive a reload */
    }
}

/** @returns {'light' | 'dark'} the operating system's current preference */
function systemTheme() {
    return window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
}

/**
 * @param {'system' | 'light' | 'dark'} mode
 * @returns {'light' | 'dark'} the palette that `mode` resolves to right now
 */
function resolve(mode) {
    return mode === 'system' ? systemTheme() : mode;
}

/**
 * Writes the attribute that the CSS keys off.
 *
 * In 'system' mode the attribute is REMOVED rather than set to the resolved value. That is what
 * keeps the third viewer state working: with no attribute present, the `prefers-color-scheme`
 * media query in tokens/dark.css is in charge, so the page follows the OS live - including a
 * change made while the tab sits in the background.
 *
 * @param {'system' | 'light' | 'dark'} mode
 */
function applyMode(mode) {
    const root = document.documentElement;

    if (mode === 'system') {
        root.removeAttribute(ATTRIBUTE);
    } else {
        root.setAttribute(ATTRIBUTE, mode);
    }
}

/**
 * Applies a mode with a brief cross-fade instead of a hard snap.
 *
 * The transition class is added for the duration of the change only. Leaving it on permanently
 * would make every unrelated hover and focus state on the page animate its colours, which reads
 * as lag. `prefers-reduced-motion` disables the transition in CSS.
 *
 * @param {'system' | 'light' | 'dark'} mode
 */
function applyModeAnimated(mode) {
    const root = document.documentElement;
    root.classList.add(TRANSITION_CLASS);
    applyMode(mode);

    window.setTimeout(() => root.classList.remove(TRANSITION_CLASS), 220);
}

/**
 * Tells every registered .NET listener about the current state.
 *
 * A reference whose owning runtime has gone away throws on invoke; those are pruned rather than
 * allowed to break the broadcast for the listeners that are still alive. `invokeMethodAsync`
 * returns a promise, so the rejection path needs catching too.
 *
 * @param {'system' | 'light' | 'dark'} mode
 * @param {'light' | 'dark'} theme
 */
function broadcast(mode, theme) {
    for (const listener of [...listeners]) {
        try {
            const result = listener.invokeMethodAsync('OnThemeChangedFromJs', mode, theme);

            if (result && typeof result.catch === 'function') {
                result.catch(() => listeners.delete(listener));
            }
        } catch {
            listeners.delete(listener);
        }
    }
}

/**
 * Registers a .NET listener and returns the current state.
 *
 * Safe to call from several islands; each gets its own registration.
 *
 * @param {object} dotNetRef A DotNetObjectReference exposing [JSInvokable] OnThemeChangedFromJs.
 * @returns {{ mode: string, theme: string }} the state at the moment of registration
 */
export function initialize(dotNetRef) {
    listeners.add(dotNetRef);

    const mode = readStoredMode();

    // Re-apply on init. The inline FOUC script already did this before first paint, but a page
    // restored from the back/forward cache may not have run it, and re-applying is idempotent.
    applyMode(mode);

    if (!mediaQuery) {
        mediaQuery = window.matchMedia('(prefers-color-scheme: dark)');

        mediaListener = () => {
            // Only meaningful in 'system' mode; an explicit choice is not overridden by the OS.
            if (readStoredMode() !== 'system') {
                return;
            }

            // The DOM needs no update here - with no attribute set, CSS has already reacted. The
            // broadcast exists so .NET-side UI (a toggle icon, a chart palette) can follow.
            broadcast('system', systemTheme());
        };

        mediaQuery.addEventListener('change', mediaListener);
    }

    return { mode, theme: resolve(mode) };
}

/**
 * Sets and persists the theme, then tells every listener.
 *
 * @param {'system' | 'light' | 'dark'} mode
 * @returns {{ mode: string, theme: string }} the state after the change
 */
export function setMode(mode) {
    const next = VALID_MODES.includes(mode) ? mode : 'system';
    const theme = resolve(next);

    writeStoredMode(next);
    applyModeAnimated(next);

    // Including the caller in the broadcast is harmless - the service compares against its cached
    // state and ignores a no-op - and leaving it out would mean maintaining a "who asked" channel
    // for no benefit.
    broadcast(next, theme);

    return { mode: next, theme };
}

/** @returns {{ mode: string, theme: string }} the current state without changing it */
export function getState() {
    const mode = readStoredMode();
    return { mode, theme: resolve(mode) };
}

/**
 * Unregisters a listener. The shared media listener is detached only once the last one leaves.
 *
 * @param {object} [dotNetRef] The reference passed to initialize. Omitted, this clears everything.
 */
export function dispose(dotNetRef) {
    if (dotNetRef) {
        listeners.delete(dotNetRef);
    } else {
        listeners.clear();
    }

    if (listeners.size > 0) {
        return;
    }

    if (mediaQuery && mediaListener) {
        mediaQuery.removeEventListener('change', mediaListener);
    }

    mediaQuery = null;
    mediaListener = null;
}
