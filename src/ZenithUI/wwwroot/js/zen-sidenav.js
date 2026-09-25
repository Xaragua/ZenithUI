/**
 * ZenSideNav's collapsed rail: the toggle, the stored preference, and the labels shown as
 * tooltips while only icons are visible.
 *
 * ## Why this is document-level JavaScript and not component state
 *
 * A side nav lives in a layout, and a layout is always static - `Body` is a RenderFragment and
 * cannot cross a render-mode boundary. A collapse built on @onclick and a bool would work in a
 * demo page and be inert in the only place a side nav goes. So, exactly like the theme, the state
 * is one attribute on <html> - `data-zen-sidenav="collapsed"` - and the CSS in components/base.css
 * does the rest. Nothing here renders; it only flips that attribute. Even the toggle's label is
 * CSS: the button carries both "Collapse" and "Expand" and the inactive one is display:none, which
 * also takes it out of the accessible name - so the name is right from the first paint, before
 * any of this has loaded.
 *
 * The listeners are delegated from `document`, so they work for a nav rendered by static SSR, by
 * an interactive island, or swapped in by enhanced navigation, with no component having to bind
 * anything.
 *
 * ## Staying collapsed across navigations
 *
 * ZenSideNav renders an inline script ahead of the <aside> that restores the attribute before the
 * first paint of a full page load. Enhanced navigation merges <html>'s attributes back to what the
 * server sent, which drops it - the same trap the theme falls into, fixed the same way:
 * ZenithUI.lib.module.js calls `applyStoredSideNav` on every `enhancedload`.
 *
 * The storage key and attribute are duplicated in that inline script (ZenSideNav.razor). Change
 * them in both places.
 */

const STORAGE_KEY = 'zen-sidenav';
const ATTRIBUTE = 'data-zen-sidenav';

/**
 * The width at which the nav is a rail rather than a drawer. Mirrors `ZenSideNav.OverlayQuery`
 * and the media queries in components/base.css. A drawer never collapses, so no tooltip is shown
 * there.
 */
const RAIL_QUERY = '(width >= 64rem)';

/** The elements in a collapsed rail that get a tooltip: links, section headings, the toggle. */
const TIP_TARGETS = '.zen-sidenav-collapsible a, .zen-sidenav-collapsible [role="link"], ' +
    '.zen-sidenav-collapsible summary, .zen-sidenav-collapsible [data-zen-sidenav-toggle]';

/** How long a tooltip survives the pointer leaving its target, so it can be moved onto it. */
const HIDE_DELAY_MS = 120;

let installed = false;

/** @type {HTMLElement | null} */
let tip = null;

/** @type {Element | null} */
let tipTarget = null;

let hideTimer = 0;

function readStored() {
    try {
        return window.localStorage.getItem(STORAGE_KEY) === 'collapsed';
    } catch {
        return false;
    }
}

function write(collapsed) {
    try {
        if (collapsed) {
            window.localStorage.setItem(STORAGE_KEY, 'collapsed');
        } else {
            window.localStorage.removeItem(STORAGE_KEY);
        }
    } catch {
        // Storage unavailable. The rail still collapses; it simply will not be remembered.
    }
}

function isCollapsed() {
    return document.documentElement.getAttribute(ATTRIBUTE) === 'collapsed';
}

function isRail() {
    return window.matchMedia(RAIL_QUERY).matches;
}

function setCollapsed(collapsed) {
    if (collapsed) {
        document.documentElement.setAttribute(ATTRIBUTE, 'collapsed');
    } else {
        document.documentElement.removeAttribute(ATTRIBUTE);
    }

    write(collapsed);
    hideTip();
}

/**
 * Re-applies the stored preference. Called at startup and after every enhanced navigation, which
 * resets <html>'s attributes to the server's.
 */
export function applyStoredSideNav() {
    if (readStored()) {
        document.documentElement.setAttribute(ATTRIBUTE, 'collapsed');
    } else {
        document.documentElement.removeAttribute(ATTRIBUTE);
    }

    hideTip();
}

// ---- Tooltip ----------------------------------------------------------------------------------

/**
 * The one tooltip element, created on first use.
 *
 * A manual popover so it renders in the top layer. The rail scrolls, and `overflow-y: auto`
 * clips horizontally as well, so a tooltip positioned inside it would be cut off at the rail's
 * edge - which is exactly where it has to appear.
 *
 * aria-hidden because it repeats the target's accessible name, which the visually hidden label
 * inside the link already provides. Announcing it as well would read every item twice.
 *
 * Re-attached when missing: an enhanced navigation merges <body> and removes nodes the server did
 * not send, this one included.
 */
function ensureTip() {
    if (!tip) {
        tip = document.createElement('div');
        tip.className = 'zen-tooltip';
        tip.setAttribute('popover', 'manual');
        tip.setAttribute('aria-hidden', 'true');
        tip.addEventListener('pointerenter', () => window.clearTimeout(hideTimer));
        tip.addEventListener('pointerleave', scheduleHide);
    }

    if (!tip.isConnected) {
        document.body.append(tip);
    }

    return tip;
}

/**
 * The target's visible label. The first one that is not display:none, because the collapse toggle
 * carries two and hides whichever does not apply.
 */
function labelFor(target) {
    const label = [...target.querySelectorAll('.zen-nav-label')]
        .find((element) => getComputedStyle(element).display !== 'none');
    const text = (label ? label.textContent : target.getAttribute('aria-label')) ?? '';

    return text.trim();
}

function showTip(target) {
    window.clearTimeout(hideTimer);

    if (!isCollapsed() || !isRail()) {
        return;
    }

    const text = labelFor(target);

    if (!text) {
        return;
    }

    const element = ensureTip();
    element.textContent = text;
    tipTarget = target;

    if (!element.matches(':popover-open')) {
        element.showPopover();
    }

    // Beside the item, on the side away from the rail, vertically centred on it.
    const box = target.getBoundingClientRect();
    const rtl = getComputedStyle(target).direction === 'rtl';
    const gap = 8;

    element.style.top = `${box.top + box.height / 2 - element.offsetHeight / 2}px`;
    element.style.left = rtl
        ? `${box.left - gap - element.offsetWidth}px`
        : `${box.right + gap}px`;
}

function hideTip() {
    window.clearTimeout(hideTimer);
    tipTarget = null;

    if (tip?.isConnected && tip.matches(':popover-open')) {
        tip.hidePopover();
    }
}

function scheduleHide() {
    window.clearTimeout(hideTimer);
    hideTimer = window.setTimeout(hideTip, HIDE_DELAY_MS);
}

// ---- Wiring -------------------------------------------------------------------------------------

function handleClick(event) {
    const toggle = event.target.closest?.('[data-zen-sidenav-toggle]');

    if (toggle) {
        setCollapsed(!isCollapsed());
        return;
    }

    // A section heading clicked in a collapsed rail. Its links have nowhere to open in a column
    // four rems wide, so the rail expands to show them, with the section open.
    const summary = event.target.closest?.('.zen-sidenav-collapsible summary');

    if (summary && isCollapsed() && isRail()) {
        setCollapsed(false);

        // Opened here rather than left to the default action, which toggles - and would close a
        // section that was already open, hiding the very links the user asked to see.
        event.preventDefault();
        summary.parentElement.open = true;
    }
}

/**
 * Installs the document-level listeners. Idempotent: called from both the Blazor Web App and the
 * standalone WebAssembly startup hooks, and a second call must not double every handler.
 */
export function installSideNav() {
    if (installed) {
        return;
    }

    installed = true;

    document.addEventListener('click', handleClick);

    document.addEventListener('pointerover', (event) => {
        const target = event.target.closest?.(TIP_TARGETS);

        if (target) {
            showTip(target);
        }
    });

    document.addEventListener('pointerout', (event) => {
        const target = event.target.closest?.(TIP_TARGETS);

        if (target && !target.contains(event.relatedTarget)) {
            scheduleHide();
        }
    });

    // Keyboard users get the same label on focus: a row of bare icons is no easier to identify
    // by Tab than by pointer.
    document.addEventListener('focusin', (event) => {
        const target = event.target.closest?.(TIP_TARGETS);

        if (target && target.matches(':focus-visible')) {
            showTip(target);
        }
    });

    document.addEventListener('focusout', (event) => {
        if (tipTarget && event.target === tipTarget) {
            hideTip();
        }
    });

    // Dismissible without moving the pointer or focus, as WCAG 1.4.13 asks of content that
    // appears on hover or focus.
    document.addEventListener('keydown', (event) => {
        if (event.key === 'Escape' && tipTarget) {
            hideTip();
        }
    });

    // A tooltip positioned for where its target was is wrong once anything scrolls.
    document.addEventListener('scroll', hideTip, { capture: true, passive: true });
    window.matchMedia(RAIL_QUERY).addEventListener('change', hideTip);

    applyStoredSideNav();
}
