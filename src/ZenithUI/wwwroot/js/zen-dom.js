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
