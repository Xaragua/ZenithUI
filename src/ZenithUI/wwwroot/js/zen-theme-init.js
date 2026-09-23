/*
 * Anti-flash theme initialiser, for a host page that is a static HTML file rather than a Razor
 * component - a standalone WebAssembly app's wwwroot/index.html, or a non-Blazor host page.
 *
 *     <head>
 *         <script src="_content/ZenithUI/js/zen-theme-init.js"></script>
 *         <link rel="stylesheet" href="_content/ZenithUI/zenith.css" />
 *     </head>
 *
 * A Blazor Web App uses <ZenThemeScript /> in App.razor instead, which inlines this same logic
 * with no request at all. Both exist because the tradeoff genuinely differs: a server-rendered
 * page can emit the script into the HTML it is already sending, and a static index.html cannot.
 *
 * It must be a CLASSIC script with no `defer`, `async` or `type="module"`. All three would run
 * after the first paint, which is precisely the paint this exists to get right. A blocking
 * request for a file of this size, served from the same origin and cached after the first visit,
 * costs less than the white flash it removes.
 *
 * The logic is duplicated in Components/Theming/ZenThemeScript.razor and must stay in lockstep
 * with it and with zen-theme.js - same storage key, same attribute, same "system means no
 * attribute" rule. ZenThemeScriptTests asserts the first two do not drift.
 */
(function () {
    try {
        var mode = localStorage.getItem('zen-theme');

        // 'system' (or anything unrecognised) leaves the attribute off, which hands control
        // to the prefers-color-scheme media query in the stylesheet. Setting the resolved
        // value here instead would freeze the page against later OS changes.
        if (mode === 'light' || mode === 'dark') {
            document.documentElement.setAttribute('data-zen-theme', mode);
        } else {
            document.documentElement.removeAttribute('data-zen-theme');
        }
    } catch (e) {
        // Storage unavailable (private browsing, partitioned iframe). Fall through to the
        // media query; a wrong-but-reasonable palette beats a broken page.
    }
})();
