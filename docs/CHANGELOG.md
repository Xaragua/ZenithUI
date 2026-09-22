# Changelog

All notable changes to ZenithUI are documented here. The format follows
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/) and the project adheres to
[Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Changed — focus treatment for input controls

Text-entry controls now signal focus by recolouring **their own border** to the ring colour, with no
outline drawn outside them. Buttons and links are unchanged and keep the ring.

- New `zen-focus-border` utility for a control that carries its own border.
- New `zen-focus-border-within` for a composite control, where the border sits on a wrapper and the
  real input is nested inside it — `:focus-within`, because the bordered element never receives
  focus itself.
- `ZenStyles.InputBase`, `InputWrapperBase`, `InputInnerBase`, `InputSize` and `TextAreaSize`
  codify the treatment so the M2 controls inherit it rather than each re-deciding.

Why the two differ: a button is a solid shape with no resting border, so a ring reads cleanly. A
text field already has a border, and an outline outside it renders as two concentric lines — in a
dense form that makes adjacent fields look like they are colliding.

Two details worth knowing:

- The focused border is reinforced with an **inset** shadow, bringing it to `--zen-ring-width`
  total. A 1px colour change is the bare minimum WCAG 2.4.7 accepts and falls short of the 2px
  perimeter WCAG 2.4.13 asks for. Inset rather than outset means the control's outer dimensions
  never change, so a focused field does not nudge its neighbours.
- An invalid field **keeps its danger border while focused**. An error outranks a focus hint;
  turning a failing field primary-coloured the moment it is focused hides the state the user is
  trying to fix.

### Added — M1, the primitives

**Components**
- `ZenIcon` — inline SVG wrapper, decorative by default and only announced when given a `Title`.
- `ZenButton` — intent x variant x size, loading state, leading/trailing icons, icon-only, and
  renders an `<a>` when `Href` is set.
- `ZenBadge` — status chips, soft by default, with an optional decorative dot.
- `ZenSpinner` — `role="status"` with an accessible name, suppressible when an ancestor already
  announces its busy state.
- `ZenSkeleton` — text/circle/rectangle placeholders, always `aria-hidden`, honouring
  `prefers-reduced-motion`.
- `ZenCard` — header/body/footer/actions slots, elevation, and an optional whole-card link.
- `ZenStatCard` — KPI tile with a trend indicator whose direction and sentiment are separate, so
  rising churn reads as an up arrow in a bad colour.
- `ZenField` — label/help/error wrapper owning the `aria-describedby` and `aria-invalid` wiring.

**Core**
- `ZenStyles` — the intent x variant x size matrix as literal Tailwind class strings, so the
  scanner can see them. One place owns the visual language.
- `ZenIcons` — the inline SVG paths the library needs, so installing ZenithUI does not drag in an
  icon font.
- `ZenInputBase<TValue>` — binding, validation and shared field parameters, with an **optional**
  `EditContext`. Deliberately not derived from `InputBase<TValue>`, which throws without a
  cascading `EditContext` and would make every control unusable outside an `EditForm`.

**Design decisions worth knowing**
- `ZenButton` defaults `type="button"`. An HTML button inside a form defaults to `submit`, so a
  "Cancel" button with no explicit type submits the form.
- A disabled `ZenButton` with an `Href` drops the `href` entirely. A disabled link is not a thing
  in HTML — the browser follows it regardless — so rendering one that looks dead and still
  navigates is worse than rendering no link.
- While loading, the button label stays in the DOM at zero opacity rather than being swapped for
  the spinner, so the control does not resize under the pointer mid-click.
- An icon-only button without an accessible name throws. It is a development-time mistake with no
  runtime recovery.

**Verification**
- 127 tests, up from 69.

### Fixed during M1

- **The per-folder `@namespace` convention did not survive a second folder.** Razor generates a
  class per `_Imports.razor`, so two folders both declaring `@namespace ZenithUI` produced two
  `ZenithUI._Imports` classes and a `CS0111`. The directive moved into each component file.
- **`ZenField` treated a whitespace-only validation message as real**, producing an alert region
  that was announced but contained nothing.
- **A non-interactive `ZenBadge` still carried hover classes in three of four variants.** The
  `interactive: false` flag only suppressed the hover on `Soft`, so a Solid or Outline badge lit up
  under the pointer and told the user it was clickable. `ZenStyles` now keeps resting appearance and
  hover state in separate methods and composes them, so the flag applies uniformly.

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
