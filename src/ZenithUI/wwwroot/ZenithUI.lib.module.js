/**
 * Blazor JS initializer for ZenithUI.
 *
 * Blazor loads `_content/{PackageId}/{PackageId}.lib.module.js` automatically, so everything here
 * happens with no wiring in the consuming application - which is the point: the problem it fixes
 * is invisible until you navigate, and no consumer should have to know it exists.
 *
 * ## What it fixes
 *
 * Enhanced navigation does not reload the page. It fetches the next document and merges it into
 * the live DOM, attributes on <html> included. The server cannot know the viewer's stored theme -
 * `localStorage` is not sent with a request - so every response carries an <html> with no
 * `data-zen-theme`, and the merge faithfully removes the one the theme service put there.
 *
 * The symptom is precise and misleading: the switcher appears to change only the current page.
 * The preference was stored correctly, the palette was applied correctly, and then a navigation
 * quietly reverted the document to the OS default. Nothing in the component was wrong.
 *
 * `enhancedload` fires after each of those merges - navigations, enhanced form posts and
 * streaming-rendering updates alike - which makes it the one place this can be put right.
 *
 * ## Why re-applying is enough
 *
 * The attribute is the whole of the state. tokens/dark.css keys off it, so writing it back
 * repaints the page with no re-render and no component involvement. The stored preference is
 * untouched by any of this; only the DOM needed correcting.
 */

import { applyStoredMode } from './js/zen-theme.js';

/**
 * Blazor Web App startup hook.
 *
 * Static import above rather than a dynamic one inside the handler: the module has to be ready
 * before the first navigation, not fetched during it, or the first one still flashes.
 *
 * @param {{ addEventListener: (type: string, handler: () => void) => void }} blazor
 */
export function afterWebStarted(blazor) {
    blazor.addEventListener('enhancedload', applyStoredMode);
}
