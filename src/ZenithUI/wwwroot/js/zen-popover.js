/**
 * Anchored positioning for floating panels: place a panel against an anchor, flip it when it would
 * leave the viewport, shift it along its axis to stay inside, and tell .NET when the user clicks
 * away.
 *
 * Why this is not CSS. `position: absolute` inside a relative wrapper - what ZenDatePicker does
 * today - is clipped by any ancestor with `overflow: hidden` and cannot flip when it runs out of
 * room. The CSS Anchor Positioning spec solves both, but is not in Firefox yet, so the panel is
 * placed here instead and rendered into the top layer where nothing can clip it.
 *
 * What is NOT here. Escape, arrow keys, typeahead and every other keyboard concern stay in C#,
 * where the component already handles `@onkeydown`. Only two things genuinely cannot: measuring
 * the viewport, and knowing that a pointer went down outside a subtree.
 */

/** Live popovers keyed by panel element id. */
const popovers = new Map();

/** Gap between anchor and panel when the caller does not specify one, in CSS pixels. */
const DEFAULT_OFFSET = 4;

/** Minimum gap kept between the panel and the viewport edge. */
const VIEWPORT_PADDING = 8;

/**
 * The rectangle to anchor against.
 *
 * ZenPopover's anchor wrapper is `display: contents`, so that it is a positioning handle and
 * nothing else - the caller's own element keeps whatever layout it had. The catch is that a
 * `display: contents` element generates no box at all, so `getBoundingClientRect()` on it returns
 * a zero rect at the origin. Measuring the wrapper directly put every panel in the top-left corner
 * of the viewport regardless of its placement, because every coordinate was computed from (0, 0).
 *
 * So a zero-sized rect means "this element has no box", not "this element is empty", and the
 * measurement descends to the first child that does have one. The recursion handles a caller whose
 * own anchor is itself `display: contents`.
 *
 * @param {Element} element
 * @returns {DOMRect}
 */
function anchorRect(element) {
    const rect = element.getBoundingClientRect();

    if (rect.width > 0 || rect.height > 0) {
        return rect;
    }

    for (const child of element.children) {
        const childRect = anchorRect(child);

        if (childRect.width > 0 || childRect.height > 0) {
            return childRect;
        }
    }

    return rect;
}

/**
 * The element actually worth observing for size changes.
 *
 * Same reason as {@link anchorRect}: a ResizeObserver on a `display: contents` element never
 * fires, because there is no box to observe.
 *
 * @param {Element} element
 * @returns {Element}
 */
function observableAnchor(element) {
    const rect = element.getBoundingClientRect();

    if (rect.width > 0 || rect.height > 0) {
        return element;
    }

    return element.firstElementChild ?? element;
}

/** Physical opposites, used when a placement has to flip. */
const OPPOSITE = {
    top: 'bottom',
    bottom: 'top',
    left: 'right',
    right: 'left',
};

/**
 * Resolves a logical placement to a physical one.
 *
 * C# speaks in `start`/`end` so a component does not have to know the writing direction, and the
 * resolution happens here because the browser is the only party that knows it: the direction can
 * come from a `dir` attribute anywhere up the tree, or from a stylesheet. Reading it off the anchor
 * picks up whichever applies.
 *
 * Only the inline axis flips. `start` as an *alignment* on a top/bottom placement means "the edge
 * text starts from", so it swaps under RTL; `start` on a left/right placement means the top edge,
 * which does not.
 *
 * @param {Element} anchor
 * @param {string} side One of top/bottom/start/end.
 * @param {string} align One of start/center/end.
 * @returns {{side: string, align: string}}
 */
function resolveDirection(anchor, side, align) {
    const rtl = window.getComputedStyle(anchor).direction === 'rtl';

    const physicalSide =
        side === 'start' ? (rtl ? 'right' : 'left')
        : side === 'end' ? (rtl ? 'left' : 'right')
        : side;

    const vertical = physicalSide === 'top' || physicalSide === 'bottom';
    const physicalAlign =
        !rtl || !vertical || align === 'center' ? align
        : align === 'start' ? 'end'
        : 'start';

    return { side: physicalSide, align: physicalAlign };
}

/**
 * Places `panel` against `anchor` for one placement, returning viewport coordinates and whether
 * the result fits.
 *
 * Fit is reported rather than acted on, so the caller can compare candidates before committing.
 *
 * @param {DOMRect} box The anchor rectangle, from {@link anchorRect}.
 * @param {DOMRect} panelRect
 * @param {string} side One of top/bottom/left/right.
 * @param {string} align One of start/center/end.
 * @param {number} offset
 * @returns {{x: number, y: number, fits: boolean}}
 */
function place(box, panelRect, side, align, offset) {
    let x = 0;
    let y = 0;

    if (side === 'top' || side === 'bottom') {
        y = side === 'top'
            ? box.top - panelRect.height - offset
            : box.bottom + offset;

        x = align === 'start' ? box.left
            : align === 'end' ? box.right - panelRect.width
            : box.left + (box.width - panelRect.width) / 2;
    } else {
        x = side === 'left'
            ? box.left - panelRect.width - offset
            : box.right + offset;

        y = align === 'start' ? box.top
            : align === 'end' ? box.bottom - panelRect.height
            : box.top + (box.height - panelRect.height) / 2;
    }

    const fits =
        y >= VIEWPORT_PADDING &&
        x >= VIEWPORT_PADDING &&
        y + panelRect.height <= window.innerHeight - VIEWPORT_PADDING &&
        x + panelRect.width <= window.innerWidth - VIEWPORT_PADDING;

    return { x, y, fits };
}

/**
 * Pulls a coordinate back inside the viewport.
 *
 * Clamped at the low end last, so that a panel taller or wider than the viewport overflows at the
 * bottom/right rather than the top/left - the top of an over-long menu is the part worth keeping,
 * because that is where its first item is.
 *
 * @param {number} value
 * @param {number} size Panel extent along this axis.
 * @param {number} viewport Viewport extent along this axis.
 * @returns {number}
 */
function clamp(value, size, viewport) {
    return Math.max(VIEWPORT_PADDING, Math.min(value, viewport - size - VIEWPORT_PADDING));
}

/**
 * Measures and positions one popover.
 *
 * Runs on open, on scroll, on resize, and whenever the anchor or panel changes size. Reading
 * layout on every scroll frame is the cost of not having anchor positioning; it is confined to two
 * `getBoundingClientRect` calls and a style write, and only while a panel is actually open.
 *
 * @param {object} state
 */
function position(state) {
    const anchor = document.getElementById(state.anchorId);
    const panel = document.getElementById(state.panelId);

    if (!anchor || !panel) {
        return;
    }

    const anchorBox = anchorRect(anchor);

    // Width matching is applied before the panel is measured: a listbox widened to match its field
    // may wrap differently, and measuring first would use a height that is about to change.
    if (state.matchWidth) {
        panel.style.minWidth = `${anchorBox.width}px`;
    }

    const panelRect = panel.getBoundingClientRect();
    const resolved = resolveDirection(anchor, state.side, state.align);

    let best = place(anchorBox, panelRect, resolved.side, resolved.align, state.offset);
    let side = resolved.side;

    // Flip to the opposite side when the preferred one does not fit, but only if the opposite
    // actually fits - flipping into an equally bad position just makes the panel jump.
    if (!best.fits && state.flip) {
        const flipped = place(anchorBox, panelRect, OPPOSITE[resolved.side], resolved.align, state.offset);

        if (flipped.fits) {
            best = flipped;
            side = OPPOSITE[resolved.side];
        }
    }

    const x = state.shift ? clamp(best.x, panelRect.width, window.innerWidth) : best.x;
    const y = state.shift ? clamp(best.y, panelRect.height, window.innerHeight) : best.y;

    // Fixed positioning in viewport coordinates. The panel is in the top layer, whose containing
    // block is the viewport, so no scroll offset is added - doing so is the classic popover bug
    // that makes a panel drift as the page scrolls.
    panel.style.position = 'fixed';
    panel.style.left = `${Math.round(x)}px`;
    panel.style.top = `${Math.round(y)}px`;
    panel.style.margin = '0';

    // Exposed so the panel can point its arrow and animate from the correct edge.
    panel.dataset.zenSide = side;
}

/**
 * Opens a popover: positions it, keeps it positioned, and watches for clicks outside.
 *
 * @param {string} anchorId Element the panel is placed against.
 * @param {string} panelId The panel itself.
 * @param {object} options Placement options from C#.
 * @param {object} [dotNetRef] Receives `OnOutsideClick` when the pointer goes down elsewhere.
 */
export function open(anchorId, panelId, options, dotNetRef) {
    close(panelId);

    const panel = document.getElementById(panelId);

    if (!panel) {
        return;
    }

    const state = {
        anchorId,
        panelId,
        dotNetRef: dotNetRef ?? null,
        side: options?.side ?? 'bottom',
        align: options?.align ?? 'start',
        offset: typeof options?.offset === 'number' ? options.offset : DEFAULT_OFFSET,
        flip: options?.flip !== false,
        shift: options?.shift !== false,
        matchWidth: !!options?.matchWidth,
    };

    // The top layer is what makes the panel immune to `overflow: hidden` and to z-index stacking
    // contexts further up the tree. `popover` is used rather than `<dialog>` because this panel is
    // explicitly non-modal: the page behind it stays interactive, which is the whole point for a
    // combobox whose input keeps focus.
    if (typeof panel.showPopover === 'function' && panel.hasAttribute('popover')) {
        try {
            panel.showPopover();
        } catch {
            // Already open, or disconnected between the null check and here. Position anyway.
        }
    }

    position(state);

    // Re-position rather than close on scroll. Closing is the easier behaviour and the wrong one:
    // a panel that vanishes because the user nudged the wheel feels broken.
    //
    // `capture: true` catches scrolls in any ancestor scroll container, not just the page - a
    // panel anchored inside a scrolling pane has to track it.
    state.onScroll = () => position(state);
    window.addEventListener('scroll', state.onScroll, { capture: true, passive: true });
    window.addEventListener('resize', state.onScroll, { passive: true });

    // Catches the cases scroll and resize miss: the anchor moving because content above it
    // changed, and the panel growing as a filtered list re-renders.
    if (typeof ResizeObserver !== 'undefined') {
        state.resizeObserver = new ResizeObserver(() => position(state));
        state.resizeObserver.observe(panel);

        const anchor = document.getElementById(anchorId);

        if (anchor) {
            state.resizeObserver.observe(observableAnchor(anchor));
        }
    }

    if (state.dotNetRef) {
        // pointerdown, not click: a click fires after the pointer is released, by which time a
        // drag that started inside the panel and ended outside would close it. pointerdown also
        // beats focusout, so the panel closes before the click lands on whatever is underneath.
        state.onPointerDown = (event) => {
            const anchor = document.getElementById(anchorId);

            // The composed path rather than `contains(target)`: an element inside a shadow root
            // nested in the panel is visually inside it but fails a `contains` check, because
            // `target` is retargeted to the shadow host. The path crosses those boundaries.
            const path = event.composedPath?.() ?? [event.target];

            // The anchor is excluded so a trigger button's own click can toggle the panel closed
            // itself, rather than being closed here and immediately reopened by its handler.
            if (path.includes(panel) || (anchor && path.includes(anchor))) {
                return;
            }

            state.dotNetRef.invokeMethodAsync('OnOutsideClick').catch(() => {
                // The component was disposed between the event and the call.
            });
        };

        document.addEventListener('pointerdown', state.onPointerDown, true);
    }

    popovers.set(panelId, state);
}

/**
 * Closes a popover and removes every listener it installed.
 *
 * Safe to call for a panel that was never opened, and safe to call twice - both happen during
 * teardown races, where a component disposes while a close is already in flight.
 *
 * @param {string} panelId
 */
export function close(panelId) {
    const state = popovers.get(panelId);

    if (!state) {
        return;
    }

    if (state.onScroll) {
        window.removeEventListener('scroll', state.onScroll, { capture: true });
        window.removeEventListener('resize', state.onScroll);
    }

    if (state.onPointerDown) {
        document.removeEventListener('pointerdown', state.onPointerDown, true);
    }

    state.resizeObserver?.disconnect();

    const panel = document.getElementById(panelId);

    if (panel && typeof panel.hidePopover === 'function' && panel.hasAttribute('popover')) {
        try {
            panel.hidePopover();
        } catch {
            // Already hidden.
        }
    }

    popovers.delete(panelId);
}

/**
 * Re-positions an open popover immediately.
 *
 * For the changes no observer sees: a panel whose content was replaced by .NET in the same frame,
 * where the ResizeObserver callback would arrive one frame late and show the panel in its old
 * place first.
 *
 * @param {string} panelId
 */
export function reposition(panelId) {
    const state = popovers.get(panelId);

    if (state) {
        position(state);
    }
}
