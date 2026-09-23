/**
 * Small DOM helpers for the handful of things that cannot be expressed in Blazor markup.
 *
 * Everything here exists because the DOM exposes it as a *property* rather than an attribute, or
 * because it is an imperative action with no declarative equivalent. Nothing here implements
 * behaviour - that belongs in C#.
 *
 * Each function is a no-op when the element is missing. Components call these after a render, and
 * an element can be gone by then (a list re-rendered, a dialog closed); throwing would turn a race
 * that does not matter into an unhandled interop error.
 */

/**
 * Sets a checkbox's indeterminate state.
 *
 * `indeterminate` is a property with no attribute equivalent, so it cannot be set from server-
 * rendered HTML - this is the only way to reach it.
 *
 * @param {string} id Element id.
 * @param {boolean} value Whether the box should render as indeterminate.
 */
export function setIndeterminate(id, value) {
    const element = document.getElementById(id);

    if (element) {
        element.indeterminate = !!value;
    }
}

/**
 * Removes focus from an element.
 *
 * Used by ZenNumberInput to stop the browser stepping the value when the wheel turns over a
 * focused field. Dropping focus is preferable to preventDefault on the wheel event, which needs a
 * non-passive listener and blocks page scrolling whenever the pointer is over the control.
 *
 * @param {string} id Element id.
 */
export function blurElement(id) {
    document.getElementById(id)?.blur();
}

/**
 * Whether the currently focused element is inside any of the given containers.
 *
 * Used by popup-style components to decide whether a `focusout` means the user really left. The
 * event's `relatedTarget` cannot answer this from .NET: Blazor marshals it as an opaque reference
 * with no way to test ancestry, so the containment check has to happen here.
 *
 * Several ids, because a control and its popup are no longer one subtree. A panel in the top
 * layer is a DOM sibling of the field that owns it, so a single-container check would report
 * "focus left" the moment focus moved into the panel - closing it on the way in.
 *
 * Returns true when every container is missing - a container that has already been removed is not
 * evidence that the user navigated away, and closing on it would fight the component's own
 * teardown.
 *
 * @param {...string} ids Container element ids.
 * @returns {boolean}
 */
export function containsActiveElement(...ids) {
    const containers = ids
        .flat()
        .map((id) => document.getElementById(id))
        .filter(Boolean);

    if (containers.length === 0) {
        return true;
    }

    return containers.some((container) => container.contains(document.activeElement));
}

/**
 * Focuses an element, optionally selecting its text.
 *
 * @param {string} id Element id.
 * @param {boolean} [select] Select the contents as well, so typing replaces rather than appends.
 */
export function focusElement(id, select) {
    const element = document.getElementById(id);

    if (!element) {
        return;
    }

    element.focus();

    if (select && typeof element.select === 'function') {
        element.select();
    }
}
