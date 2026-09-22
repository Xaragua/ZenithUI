# Changelog

All notable changes to ZenithUI are documented here. The format follows
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/) and the project adheres to
[Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added — M0, the foundation

**Design tokens**
- `--zen-*` custom properties in OKLCH, with complete light and dark palettes.
- Three-state theming: an explicit choice wins over the OS preference, and `System` mode removes
  the `data-zen-theme` attribute entirely so the page keeps following the OS live.
- Each intent is a quartet — `-{intent}`, `-content`, `-soft`, `-strong`. `-strong` exists because
  a fill colour and a text colour have opposite requirements: a usable amber warning fill reads at
  roughly 2:1 as text on white, and darkening the fill until it passed would turn every warning
  button brown.

**Build**
- Tailwind v4 pipeline wired into MSBuild; the stylesheet is regenerated on every build.
- Two entry points: `zenith.css` (with preflight) and `zenith.nopreflight.css`.
- `safelist.css` emits the full semantic utility surface unconditionally, so applications that do
  not run Tailwind can still compose with `bg-primary`, `text-content-muted` and friends.

**Core**
- `ZenComponentBase` — class merging, attribute splatting, and a stable ARIA id assigned in the
  constructor so prerender and hydration agree.
- `ZenJsComponentBase` — prerender-safe module loading gated on `RendererInfo.IsInteractive`, with
  disposal that survives a dropped circuit.
- `CssBuilder` — conditional class composition.

**Theming components**
- `IZenThemeService` / `ZenThemeService`, `ZenThemeProvider`, `ZenThemeToggle` (toggle and
  segmented shapes), `ZenThemeScript` (inline anti-flash snippet).
- `AddZenithUI()` service registration.

**Verification**
- 69 tests: `CssBuilder`, `ZenComponentBase`, `ZenThemeToggle` (ARIA contract, interactive and
  static-SSR paths), plus a contrast audit that converts every OKLCH token to linear sRGB and
  asserts WCAG 2.2 ratios and sRGB gamut containment across both palettes.
- Demo application exercising static SSR, Interactive Server and Interactive WebAssembly on one
  page.
- CI builds, tests, packs, and fails if the stylesheet is missing from the `.nupkg`.

### Fixed during M0

These were all found by running the demo rather than by the test suite, and each has a regression
test now.

- **`ZenThemeToggle` required a `ZenThemeProvider` to work.** Only the provider called
  `InitializeAsync`, so a toggle in an app bar — where a provider cannot be placed, because a
  `RenderFragment` cannot cross a static-to-interactive boundary — moved its own highlight on click
  and never touched the DOM. The toggle now initializes the service itself.
- **Multiple interactive islands displaced each other's theme listener.** A dynamic `import()`
  returns the same module instance for the page, so an Interactive Server island and an Interactive
  WebAssembly island shared one `zen-theme.js`. It stored a single `DotNetObjectReference`, so the
  last island to initialize silently replaced the others, and the first to dispose removed the
  shared media-query listener for everyone. Listeners are now a `Set` and changes are broadcast to
  all of them, which also keeps every island's UI in agreement.
- **Eight palette tokens fell outside the sRGB gamut** and would have been clipped by the browser,
  and four content/surface pairs missed WCAG AA. Found by the contrast audit on its first run.
- **`IsInitialized` was set after the state it described.** A subscriber reacting to `ThemeChanged`
  was handed the real browser-resolved state while still being told initialization had not
  finished.
