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
 * Whether the currently focused element is inside the given container.
 *
 * Used by popup-style components to decide whether a `focusout` means the user really left. The
 * event's `relatedTarget` cannot answer this from .NET: Blazor marshals it as an opaque reference
 * with no way to test ancestry, so the containment check has to happen here.
 *
 * Returns true when the container is missing - a container that has already been removed is not
 * evidence that the user navigated away, and closing on it would fight the component's own
 * teardown.
 *
 * @param {string} id Container element id.
 * @returns {boolean}
 */
export function containsActiveElement(id) {
    const container = document.getElementById(id);

    if (!container) {
        return true;
    }

    return container.contains(document.activeElement);
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
