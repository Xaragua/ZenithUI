/**
 * Focus management for overlay components: focus restore, scroll locking, a focus trap for the
 * paths the platform does not cover, and scrolling an active item into view.
 *
 * Everything here exists because it reads or mutates state that lives only in the browser - the
 * active element, the scrollbar width, the layout of a scroll container. None of it is behaviour;
 * the decisions about *when* to trap, lock or restore stay in C#.
 *
 * Every function tolerates a missing element. Components call these across render boundaries, and
 * an element can be gone by the time the call lands (a dialog closed, a list re-rendered).
 * Throwing would turn a harmless race into an unhandled interop error.
 */

/**
 * Elements to return focus to, keyed by an opaque token handed back to .NET.
 *
 * A token rather than an ElementReference because the element that opened an overlay is very
 * often not one the overlay component can name - it is whatever had focus when a *service* was
 * asked to show a dialog, which may be in a completely different part of the tree.
 */
const savedFocus = new Map();

/** Active focus traps, keyed by container id, each holding its keydown listener. */
const traps = new Map();

/**
 * `cancel` listeners suppressing the platform's own Escape handling, keyed by dialog id.
 * See {@link showModal} for why this cannot be done from Razor.
 */
const cancelBlockers = new Map();

/**
 * Scroll-lock depth. Modals stack, so the lock is reference counted: the second modal to open
 * must not unlock the page when the first one closes.
 */
let scrollLockDepth = 0;

/** The inline styles `lockScroll` overwrote, restored verbatim when the depth returns to zero. */
let scrollLockPrevious = null;

let tokenCounter = 0;

/**
 * Selector for elements that can hold focus. `:not([tabindex^="-"])` filters out programmatic-only
 * stops, and the `[hidden]` and `disabled` exclusions drop elements that match structurally but
 * cannot actually be focused.
 */
const FOCUSABLE = [
    'a[href]',
    'area[href]',
    'button:not([disabled])',
    'input:not([disabled]):not([type="hidden"])',
    'select:not([disabled])',
    'textarea:not([disabled])',
    'iframe',
    'object',
    'embed',
    'audio[controls]',
    'video[controls]',
    '[contenteditable]:not([contenteditable="false"])',
    '[tabindex]:not([tabindex^="-"])',
].join(',');

/**
 * Visible focusable descendants, in tab order.
 *
 * `offsetParent === null` catches `display: none` subtrees; the `getClientRects` check catches the
 * rest (`visibility: hidden`, zero-size, a collapsed `<details>`). A trap that includes invisible
 * stops sends focus somewhere the user cannot see, which is worse than no trap at all.
 *
 * @param {Element} container
 * @returns {HTMLElement[]}
 */
function focusableWithin(container) {
    return Array.from(container.querySelectorAll(FOCUSABLE)).filter(
        (element) =>
            !element.hasAttribute('inert') &&
            element.offsetParent !== null &&
            element.getClientRects().length > 0);
}

/**
 * Records the currently focused element and returns a token for restoring it later.
 *
 * Returns null when nothing meaningful holds focus, so the caller can tell "nothing to restore"
 * apart from "restore failed". `document.body` is treated as nothing: focusing it explicitly is
 * indistinguishable from the default state and would cancel a focus move the app made itself.
 *
 * @returns {string|null} A token for {@link restoreFocus}, or null.
 */
export function saveFocus() {
    const active = document.activeElement;

    if (!active || active === document.body || active === document.documentElement) {
        return null;
    }

    const token = `zf-${++tokenCounter}`;
    savedFocus.set(token, active);

    return token;
}

/**
 * Returns focus to the element recorded by {@link saveFocus} and releases the token.
 *
 * The element may have been removed while the overlay was open - a dialog that deletes the row
 * whose button opened it is the ordinary case, not an edge case. There is no good answer for where
 * focus should go then, but leaving it on a detached node strands the user at the top of the
 * document on the next Tab, so the attempt is simply skipped and the token cleaned up.
 *
 * @param {string|null} token Token from {@link saveFocus}.
 * @param {boolean} [preventScroll] Restore focus without scrolling it into view.
 * @returns {boolean} Whether focus was actually moved.
 */
export function restoreFocus(token, preventScroll) {
    if (!token) {
        return false;
    }

    const element = savedFocus.get(token);
    savedFocus.delete(token);

    if (!element || !element.isConnected || typeof element.focus !== 'function') {
        return false;
    }

    element.focus({ preventScroll: !!preventScroll });

    return true;
}

/**
 * Discards a saved focus token without restoring it.
 *
 * @param {string|null} token
 */
export function releaseFocus(token) {
    if (token) {
        savedFocus.delete(token);
    }
}

/**
 * Moves focus to the first focusable element inside a container, or to the container itself.
 *
 * The container fallback needs `tabindex="-1"` on it to work, which overlay components set. An
 * empty dialog still has to take focus from whatever opened it, or Escape and Tab go to the page
 * behind instead of to the dialog.
 *
 * @param {string} id Container element id.
 * @param {string} [preferredId] Id of an element to focus in preference to the first stop.
 * @returns {boolean} Whether focus was moved.
 */
export function focusFirst(id, preferredId) {
    const container = document.getElementById(id);

    if (!container) {
        return false;
    }

    if (preferredId) {
        const preferred = document.getElementById(preferredId);

        if (preferred && container.contains(preferred)) {
            preferred.focus();
            return true;
        }
    }

    const focusable = focusableWithin(container);

    if (focusable.length > 0) {
        focusable[0].focus();
        return true;
    }

    if (typeof container.focus === 'function') {
        container.focus();
        return true;
    }

    return false;
}

/**
 * Confines Tab within a container until {@link releaseTrap} is called.
 *
 * Native `<dialog>.showModal()` already does this, and ZenModal uses it - the platform's version
 * also makes the background inert, which no amount of key handling can reproduce. This exists for
 * the paths that cannot use a modal dialog: a non-modal drawer, and any fallback where
 * `showModal` is unavailable.
 *
 * Only Tab is intercepted. Trapping arrow keys as well would break every composite widget inside
 * the container, which needs them for its own navigation.
 *
 * @param {string} id Container element id.
 */
export function trapFocus(id) {
    const container = document.getElementById(id);

    if (!container || traps.has(id)) {
        return;
    }

    const onKeyDown = (event) => {
        if (event.key !== 'Tab') {
            return;
        }

        const focusable = focusableWithin(container);

        if (focusable.length === 0) {
            // Nothing to move to, but the page behind must not receive focus either.
            event.preventDefault();
            return;
        }

        const first = focusable[0];
        const last = focusable[focusable.length - 1];
        const active = document.activeElement;

        // Wrapping is only forced at the ends. In between, the browser's own sequential navigation
        // is left alone, so a reordered `tabindex` inside the container still behaves as authored.
        if (event.shiftKey && (active === first || !container.contains(active))) {
            event.preventDefault();
            last.focus();
        } else if (!event.shiftKey && (active === last || !container.contains(active))) {
            event.preventDefault();
            first.focus();
        }
    };

    container.addEventListener('keydown', onKeyDown);
    traps.set(id, onKeyDown);
}

/**
 * Removes a trap installed by {@link trapFocus}.
 *
 * The listener is removed from the live element when it is still in the document, and the registry
 * entry is dropped either way - a container that has already been detached takes its listener with
 * it, but the map entry would leak.
 *
 * @param {string} id Container element id.
 */
export function releaseTrap(id) {
    const onKeyDown = traps.get(id);

    if (!onKeyDown) {
        return;
    }

    document.getElementById(id)?.removeEventListener('keydown', onKeyDown);
    traps.delete(id);
}

/**
 * Prevents the page behind an overlay from scrolling.
 *
 * The scrollbar it removes is replaced with equivalent padding, or the whole page shifts sideways
 * as the overlay opens - a jump that is small, obvious, and reads as a rendering bug. The width is
 * measured rather than assumed because it differs by platform and by pointer type, and is zero for
 * overlay scrollbars.
 *
 * Reference counted: stacked modals each take a lock, and only the last release restores the page.
 */
export function lockScroll() {
    if (scrollLockDepth++ > 0) {
        return;
    }

    const body = document.body;
    const gap = window.innerWidth - document.documentElement.clientWidth;

    scrollLockPrevious = {
        overflow: body.style.overflow,
        paddingInlineEnd: body.style.paddingInlineEnd,
    };

    body.style.overflow = 'hidden';

    if (gap > 0) {
        // Added to whatever padding the page already had, rather than replacing it.
        const existing = parseFloat(window.getComputedStyle(body).paddingInlineEnd) || 0;
        body.style.paddingInlineEnd = `${existing + gap}px`;
    }
}

/**
 * Releases one scroll lock, restoring the page when the last one goes.
 *
 * The saved values are written back verbatim, including empty strings, so a page that never set
 * these properties ends up with no inline style rather than an inert `overflow: visible`.
 */
export function unlockScroll() {
    if (scrollLockDepth === 0) {
        return;
    }

    if (--scrollLockDepth > 0) {
        return;
    }

    if (scrollLockPrevious) {
        document.body.style.overflow = scrollLockPrevious.overflow;
        document.body.style.paddingInlineEnd = scrollLockPrevious.paddingInlineEnd;
        scrollLockPrevious = null;
    }
}

/**
 * Scrolls an item into view inside its scrolling ancestor, without scrolling the page.
 *
 * `scrollIntoView({ block: 'nearest' })` is deliberate: a listbox moving its active option one row
 * should nudge by one row, not centre it. `nearest` also does nothing when the item is already
 * visible, which is what keeps arrow-key navigation from jittering.
 *
 * The item is located by id within the container rather than by id alone, so a stale id from a
 * previous render cannot scroll an unrelated element.
 *
 * @param {string} containerId Scrolling container's element id.
 * @param {string} itemId Element id of the item to reveal.
 */
export function scrollItemIntoView(containerId, itemId) {
    const container = document.getElementById(containerId);
    const item = document.getElementById(itemId);

    if (!container || !item || !container.contains(item)) {
        return;
    }

    item.scrollIntoView({ block: 'nearest', inline: 'nearest' });
}

/**
 * Opens a `<dialog>` as a modal.
 *
 * `showModal()` rather than `show()` or a div with `role="dialog"`, because it brings three things
 * from the platform that are painful to reproduce and easy to get subtly wrong: the top layer, so
 * no ancestor's `overflow` or `z-index` can clip or bury the dialog; `inert` on everything behind
 * it, which removes the background from the tab order *and* from the accessibility tree, something
 * a keydown-based focus trap cannot do; and the `::backdrop` pseudo-element.
 *
 * Escape is deliberately NOT left to the platform. The browser's own handling closes a dialog with
 * no chance to intervene, so the `cancel` event is suppressed here and the key is routed through
 * C# instead - which is what lets a dialog refuse to close while its form is dirty, and what makes
 * ZenModal's CloseOnEscape parameter mean anything.
 *
 * The suppression has to live here rather than in Razor. `oncancel` is a recognised Blazor event,
 * but it is registered without preventDefault support, so `@@oncancel:preventDefault` is not a
 * directive Razor understands - it compiles to a literal HTML attribute that does nothing, and the
 * dialog closes anyway. A listener is the only route that actually works.
 *
 * @param {string} id The dialog element's id.
 * @returns {boolean} Whether the dialog was opened.
 */
export function showModal(id) {
    const dialog = document.getElementById(id);

    if (!dialog || typeof dialog.showModal !== 'function') {
        return false;
    }

    if (dialog.open) {
        return true;
    }

    if (!cancelBlockers.has(id)) {
        const onCancel = (event) => event.preventDefault();

        dialog.addEventListener('cancel', onCancel);
        cancelBlockers.set(id, onCancel);
    }

    try {
        dialog.showModal();
        return true;
    } catch {
        // Already open in another tree, or disconnected between the check and the call.
        return false;
    }
}

/**
 * Closes a `<dialog>` opened by {@link showModal}.
 *
 * @param {string} id The dialog element's id.
 */
export function closeModal(id) {
    const dialog = document.getElementById(id);
    const onCancel = cancelBlockers.get(id);

    if (onCancel) {
        dialog?.removeEventListener('cancel', onCancel);
        cancelBlockers.delete(id);
    }

    if (dialog && dialog.open && typeof dialog.close === 'function') {
        try {
            dialog.close();
        } catch {
            // Already closing.
        }
    }
}

/**
 * Whether a point lies outside the dialog's own box.
 *
 * This is how a backdrop click is detected. The backdrop is a pseudo-element, so it cannot carry a
 * listener of its own, and a click on it targets the `<dialog>` itself - meaning a plain click
 * handler cannot tell "clicked the backdrop" from "clicked the dialog". Comparing the pointer
 * against the dialog's rectangle can.
 *
 * The rectangle is read here rather than passed from C# because the coordinates in a Blazor
 * MouseEventArgs are viewport-relative while the dialog's box is not known to .NET at all.
 *
 * @param {string} id The dialog element's id.
 * @param {number} clientX
 * @param {number} clientY
 * @returns {boolean} True when the point is outside the dialog.
 */
export function isOutsideDialog(id, clientX, clientY) {
    const dialog = document.getElementById(id);

    if (!dialog) {
        return false;
    }

    const rect = dialog.getBoundingClientRect();

    // A zero-sized rect means the dialog is mid-transition. Reporting "outside" then would close
    // it on the click that opened it.
    if (rect.width === 0 && rect.height === 0) {
        return false;
    }

    return clientX < rect.left
        || clientX > rect.right
        || clientY < rect.top
        || clientY > rect.bottom;
}

/**
 * Drawers currently bound by {@link bindDrawer}, keyed by element id, each holding the listeners
 * to remove and whether it is presently holding a scroll lock.
 */
const drawers = new Map();

/**
 * Upgrades a `popover` side nav with the two things the popover API does not provide.
 *
 * WHAT THE PLATFORM ALREADY DOES, and is therefore absent here: showing and hiding on a
 * `popovertarget` button, Escape, light dismiss, the top layer, `::backdrop`, and returning focus
 * to the invoker on close. All of that works with no JavaScript at all, which is the whole reason
 * ZenSideNav is a popover - an app shell lives in a layout, and a layout cannot be interactive.
 *
 * WHAT IS MISSING. `popover="auto"` does not confine Tab, so a keyboard user tabs off the end of
 * the drawer into the page it is covering; and it does not stop that page scrolling underneath.
 *
 * WHY THE TOGGLE LISTENER LIVES HERE rather than an `@ontoggle` handler in Razor. The trap and the
 * lock have to be installed in the same task as the state change, and a Blazor event on a Server
 * circuit is a round trip - so the drawer would open, and a moment later become trapped. The
 * *policy* is still C#'s: whether to trap, whether to lock, when to close, and the breakpoint
 * below which any of it applies are all passed in.
 *
 * The media query is re-evaluated on every toggle rather than watched. It is only read at the
 * moment the drawer opens, so a listener would buy nothing; and re-reading means a resize past the
 * breakpoint while the drawer is open cannot leave a trap installed over a nav that has gone back
 * to being an ordinary column.
 *
 * @param {string} id The drawer element's id.
 * @param {object} options
 * @param {boolean} [options.trap] Confine Tab within the drawer while it is open.
 * @param {boolean} [options.lockScroll] Prevent the page behind from scrolling.
 * @param {boolean} [options.closeOnNavigate] Hide the drawer when a link inside it is activated.
 * @param {string} [options.query] Media query that must match for any of this to apply.
 */
export function bindDrawer(id, options) {
    const drawer = document.getElementById(id);

    if (!drawer || drawers.has(id)) {
        return;
    }

    const settings = options || {};
    const state = { locked: false };

    const isOverlay = () =>
        !settings.query || window.matchMedia(settings.query).matches;

    const onToggle = (event) => {
        if (event.newState === 'open') {
            if (!isOverlay()) {
                return;
            }

            if (settings.trap) {
                // Ahead of the trap: an empty tab ring would otherwise leave focus on the invoker,
                // outside the container the trap is about to confine Tab to.
                focusFirst(id);
                trapFocus(id);
            }

            if (settings.lockScroll) {
                lockScroll();
                state.locked = true;
            }

            return;
        }

        releaseTrap(id);

        // Guarded by the flag rather than by `settings.lockScroll`, so a drawer that opened while
        // the layout was wide - and therefore took no lock - cannot release someone else's.
        if (state.locked) {
            unlockScroll();
            state.locked = false;
        }
    };

    /**
     * Closing on navigation is not cosmetic. Blazor's enhanced navigation swaps the page content
     * without a document load, so nothing dismisses the drawer: it stays in the top layer, over
     * the page the user just asked for.
     */
    const onClick = (event) => {
        if (!settings.closeOnNavigate || !drawer.matches(':popover-open')) {
            return;
        }

        const link = event.target.closest?.('a[href]');

        // A link opening elsewhere leaves this page - and this drawer - exactly where it was.
        if (link && drawer.contains(link) && link.target !== '_blank') {
            drawer.hidePopover();
        }
    };

    drawer.addEventListener('toggle', onToggle);
    drawer.addEventListener('click', onClick);
    drawers.set(id, { onToggle, onClick, state });
}

/**
 * Removes a binding installed by {@link bindDrawer}.
 *
 * A drawer disposed while open still holds a scroll lock and a trap, and neither goes away with
 * the element - the lock is on `document.body` and would leave the page permanently unscrollable.
 *
 * @param {string} id The drawer element's id.
 */
export function unbindDrawer(id) {
    const entry = drawers.get(id);

    if (!entry) {
        return;
    }

    const drawer = document.getElementById(id);

    drawer?.removeEventListener('toggle', entry.onToggle);
    drawer?.removeEventListener('click', entry.onClick);

    releaseTrap(id);

    if (entry.state.locked) {
        unlockScroll();
    }

    drawers.delete(id);
}
