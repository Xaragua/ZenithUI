# ZenithUI — implementation plan

> **Status:** M0, M1 and M2 complete (2026-09-22). M3 next: ZenModal + modal service, ZenToast + toast service.
> This is the plan of record. It is kept current: where implementation contradicted the original
> plan, the plan was corrected and the change noted under [Deviations](#deviations-from-the-original-plan).

## Context

A greenfield, reusable Blazor component library built on TailwindCSS, published as a NuGet package,
living at `c:\code\2026\ZenithUI`. It is not tied to any consuming application.

The first release covers the controls most web apps need: text / textarea / date / numeric /
currency inputs, select, combobox, list, table with hierarchical rows, tree, card, form, nav menu,
side navigation, app bar, footer, search input, radio, checkbox, stat card, and timeline — with
light/dark palettes and a responsive layout out of the box.

**Naming:** package, assembly and root namespace `ZenithUI`; component prefix `Zen`
(`ZenTextInput`, `ZenTable`); CSS token namespace `--zen-*`; static assets served from
`_content/ZenithUI/`.

### Decisions

| Decision | Choice |
| --- | --- |
| Packaging | Standalone repo producing a NuGet package, with its own demo/docs app |
| Render mode | Agnostic — static SSR, Interactive Server, Interactive WebAssembly |
| JavaScript | Minimal, colocated ES modules. No third-party JS dependencies |
| Theming | CSS custom properties + Tailwind semantic tokens. Components never hardcode colours |

### Toolchain

.NET SDK **10.0.401**, Node **v24.15.0**, npm **12.0.2**. Target framework `net10.0`, **Tailwind
v4** (CSS-first `@theme`, native CSS variables).

---

## CSS strategy (the load-bearing decision)

A consumer's Tailwind `content` globs **cannot scan a Razor Class Library's `.razor` files** — they
are compiled into the DLL and shipped inside a `.nupkg`. So ZenithUI **precompiles its own
stylesheet** and ships it as a static web asset.

- The library build runs Tailwind v4 over `src/ZenithUI/**/*.razor` → `wwwroot/zenith.css`,
  reaching consumers at `_content/ZenithUI/zenith.css`.
- Two entry files: `zenith.css` (includes Tailwind preflight) and `zenith.nopreflight.css` (for
  apps already emitting preflight, avoiding a double reset).
- `Styles/safelist.css` emits the full semantic utility surface unconditionally. Without it, a
  utility the library's own components happen not to use would be absent from the shipped CSS, and
  an app that does not run Tailwind would have no way to add it. Costs ~6 KB.
- Consumers rebrand by **overriding `--zen-*` custom properties** — no Tailwind install, no config
  file, no rebuild of the package.
- All library rules land in `@layer components` / `@layer utilities` so a consumer's own utilities
  win on specificity ties.

### What this means for consumers

| Consuming app | What it does |
| --- | --- |
| No Tailwind of its own | Links `zenith.css`. Semantic utilities are all present. |
| Runs Tailwind | Links `zenith.nopreflight.css`, and imports ZenithUI's `@theme` into its own entry so `bg-surface` compiles for its markup too. |

`samples/ZenithUI.Demo/Styles/app.css` is a working example of the second case.

### MSBuild wiring

```xml
<Target Name="BuildTailwind"
        DependsOnTargets="ZenithRestoreNpm"
        BeforeTargets="ResolveStaticWebAssetsInputs;Build"
        Inputs="@(ZenithStyle);@(RazorComponent)"
        Outputs="wwwroot\zenith.css;wwwroot\zenith.nopreflight.css">
  <Exec Command="npm run css:build" WorkingDirectory="$(MSBuildProjectDirectory)" />
</Target>
```

`wwwroot/*.css` is generated and gitignored; CI builds it before `dotnet pack`.

---

## Repository layout

```
c:\code\2026\ZenithUI\
  global.json                     pin SDK 10.0.401
  Directory.Build.props           net10.0, nullable, shared NuGet metadata
  Directory.Packages.props        central package management
  ZenithUI.slnx
  .github/workflows/ci.yml        restore -> npm ci -> build -> test -> pack
  README.md                       the only doc at root (GitHub landing page)
  docs/                           everything else: this plan, the changelog
  src/ZenithUI/                   Microsoft.NET.Sdk.Razor — the package
    package.json                  tailwindcss v4 + @tailwindcss/cli only
    Styles/
      zenith.css                  entry, with preflight
      zenith.nopreflight.css      entry, without preflight
      safelist.css                unconditional semantic utility surface
      tokens/base.css             --zen-* light palette on :root
      tokens/dark.css             dark overrides (3-state, see Theming)
      tokens/theme.css            @theme inline mapping to Tailwind
      components/base.css         rules utilities cannot express
    Core/                         ZenComponentBase, ZenJsComponentBase, CssBuilder, enums
    Services/                     IZenThemeService + implementation
    Extensions/                   AddZenithUI()
    Components/<Category>/*.razor each opening with @namespace ZenithUI (see below)
    wwwroot/                      GENERATED css + js/
  samples/ZenithUI.Demo/          Blazor Web App host
  samples/ZenithUI.Demo.Client/   WebAssembly half — proves render-mode agnosticism
  tests/ZenithUI.Tests/           xUnit + bUnit, including the contrast audit
```

### Namespace convention

Every component `.razor` file opens with `@namespace ZenithUI`, which flattens all components into
one namespace so consumers need exactly one `@using` and folders stay free to move.

The directive goes in each **file**, not in a folder-level `_Imports.razor`. Razor generates a class
per `_Imports.razor`, so two folders both declaring `@namespace ZenithUI` produce two
`ZenithUI._Imports` classes and the build fails with `CS0111`. The convention only looks like it
works while the library has a single component folder.

Shared `@using` directives live in `Components/_Imports.razor`, which has no `@namespace` of its own
and therefore generates the unique `ZenithUI.Components._Imports`.

---

## Core abstractions

### `Core/ZenComponentBase.cs`
`ComponentBase` plus `Class`, `Style`, `AdditionalAttributes` (captured unmatched), and a stable
`Id`.

The id fallback is assigned **in the constructor**. Under prerendering a component renders once on
the server and again after hydration; an id generated in `OnInitialized` or `OnAfterRender` would
differ between the two passes, breaking the `label`-to-input association and producing a DOM-diff
mismatch.

`RootClass()` merges component classes, then `Class`, then any `class` arriving through splatted
attributes. `AttributesWithoutClass` filters the splat so a caller's `class` is merged rather than
replacing the component's own.

### `Core/CssBuilder.cs`
Fluent conditional class composition. Returns `null` rather than `""` when empty, so Blazor omits
the attribute entirely.

### `Core/ZenInputBase<TValue>.cs` — **do not inherit `InputBase<TValue>`**

`Microsoft.AspNetCore.Components.Forms.InputBase<T>` **throws** when no cascading `EditContext` is
present, which would make every input unusable outside an `EditForm`. Write an equivalent base with
an **optional** `[CascadingParameter] EditContext?`:

- Replicate `CurrentValue` / `CurrentValueAsString` / `TryParseValueFromString` / `FieldIdentifier`.
- `EditContext == null` → plain two-way binding, no validation UI.
- `EditContext != null` → `NotifyFieldChanged` on write, messages via `GetValidationMessages`.
- Keep `ValueExpression` handling exactly standard so third-party validators writing into a
  `ValidationMessageStore` keyed by `FieldIdentifier` work unmodified.
- Shared parameters: `Label`, `HelpText`, `Placeholder`, `Required`, `Disabled`, `ReadOnly`,
  `Size`, `LeadingIcon` / `TrailingIcon`, `ValidationState`.

### `Core/ZenJsComponentBase.cs`

```csharp
protected override async Task OnAfterRenderAsync(bool firstRender)
{
    if (!firstRender || !RendererInfo.IsInteractive) return;
    _module = await JS.InvokeAsync<IJSObjectReference>("import", ModulePath);
}
```

Hard rules: no JS before first render; **no `@rendermode` anywhere inside the library**; every
component emits meaningful static HTML during SSR/prerender; `IAsyncDisposable` on every module
reference, swallowing `JSDisconnectedException`.

---

## Theming

### Token namespace `--zen-*`, OKLCH

| Group | Tokens |
| --- | --- |
| Surface | `--zen-surface`, `-raised`, `-sunken`, `-overlay` |
| Content | `--zen-content`, `-muted`, `-subtle`, `-inverted` |
| Border | `--zen-border`, `--zen-border-strong` |
| Intent | `--zen-{primary,secondary,accent,success,warning,danger,info}` × `{"", -content, -soft, -strong}` |
| Focus | `--zen-ring`, `--zen-ring-offset`, widths |
| Shape | `--zen-radius-{sm,md,lg,xl}`, `--zen-shadow-{sm,md,lg}` |
| Layout | `--zen-indent`, `--zen-sidenav-width`, `--zen-appbar-height` |

### Intents are quartets, and the roles are not interchangeable

| Token | Role |
| --- | --- |
| `--zen-{intent}` | A **fill** — solid button backgrounds, badges, progress bars |
| `--zen-{intent}-content` | The text that goes **on** that fill |
| `--zen-{intent}-soft` | A tinted **background** for low-emphasis chips and callouts |
| `--zen-{intent}-strong` | A **foreground** — text, icons, borders on surfaces or on `-soft` |

`-strong` exists because a fill colour and a text colour have opposite requirements, and for some
hues no single value serves both. A usable amber warning fill sits near L 0.76, which reads at about
2:1 as text on white; darkening the fill until the text passed would turn every warning button
brown. So `bg-warning` + `text-warning-content` for a solid chip, `text-warning-strong` for coloured
text on a page. Never `text-warning` on a surface.

### Three-state dark mode

```css
:root { /* complete light palette — every token defined here */ }

@media (prefers-color-scheme: dark) {
  :root:not([data-zen-theme="light"]) { /* dark overrides */ }
}

:root[data-zen-theme="dark"] { /* same overrides; explicit choice wins */ }
```

No token may have its only definition inside a media or attribute block. The two dark blocks are
byte-identical by contract, enforced by
`ThemeTokenTests.DarkPalette_MediaQueryAndAttributeBlocks_AreIdentical`.

In `System` mode the attribute is **removed**, not set to the resolved value — that is what keeps
the page following the OS live rather than freezing against it.

### Theme service

`IZenThemeService` / `ZenThemeService`, `ZenThemeProvider`, `ZenThemeToggle` (toggle and segmented
shapes), `ZenThemeScript` (inline anti-flash snippet), `AddZenithUI()`.

Two constraints discovered in implementation, both load-bearing:

1. **A toggle must initialize the service itself.** Its most common home is an app bar, which
   usually lives in a static layout where a `ZenThemeProvider` cannot be placed at all — a
   `RenderFragment` cannot cross a static-to-interactive boundary.
2. **`zen-theme.js` must broadcast to a `Set` of listeners.** A dynamic `import()` of the same URL
   yields the same module instance for the page, and a Blazor Web App can host an Interactive
   Server island and an Interactive WebAssembly island simultaneously — two .NET runtimes, two DI
   containers, two service instances, one module.

`ZenThemeScript` must be inline and synchronous in `<head>`: `localStorage` is not sent with the
request, so the server cannot know the stored preference, and a deferred script would run after
first paint.

---

## Component inventory

The requested set, plus the primitives the rest depend on.

**Primitives** — `ZenIcon` (slot-based, so no icon font is forced on consumers), `ZenButton`,
`ZenBadge`, `ZenSpinner`, `ZenSkeleton`, `ZenPopover`, `ZenField`, `ZenIndicator`, `ZenProgress`.

**Inputs** — `ZenTextInput`, `ZenTextArea`, `ZenDateInput`, `ZenDatePicker` + `ZenCalendar`,
`ZenNumberInput<TValue>`, `ZenCurrencyInput` (culture-aware, **formats on blur in C#** — no JS
masking, which sidesteps caret-position bugs), `ZenSearchInput`, `ZenCheckbox`,
`ZenCheckboxGroup<T>`, `ZenRadioGroup<T>` + `ZenRadio<T>`, `ZenToggle`, `ZenRangeSlider`.

**Overlays and feedback** — `ZenModal` + `IZenModalService`, `ZenToast` + `IZenToastService`.

**Selection** — `ZenSelect<TValue>` (native `<select>`, works in SSR with zero JS),
`ZenCombobox<TItem>` (typeahead, async `ItemsProvider`, single/multi, full WAI-ARIA combobox
pattern).

**Data display** — `ZenList<TItem>`, `ZenTable<TItem>`, `ZenTree<TItem>`, `ZenTimeline` +
`ZenTimelineItem`, `ZenStatCard`.

**Containers** — `ZenCard` (`Header` / `Body` / `Footer` / `Actions` slots), `ZenForm`.

**Chrome & layout** — `ZenAppBar`, `ZenNavMenu` + `ZenNavLink`, `ZenSideNav` (off-canvas below
`lg`), `ZenFooter`, `ZenAppShell`.

### `ZenTable<TItem>` — the hardest component

- Columns are child components registering through `CascadingValue<ZenTable<TItem>>`:
  `<ZenColumn TItem="Order" Title="Amount" Field="o => o.Amount" />`, with an optional
  `CellTemplate`.
- Sorting (`aria-sort`), paging, row selection, sticky header, loading skeleton rows.
- **Hierarchy:** `Func<TItem, IEnumerable<TItem>?>? ChildrenProvider` plus an expansion set.
  Flatten to `(Item, Depth, HasChildren, IsExpanded)`; indent via the `zen-indent` utility driven
  by a `--zen-depth` custom property; `aria-level` / `aria-expanded` on `<tr>`, `role="treegrid"`
  when hierarchical.
- **Responsive default:** below `md`, collapse to a stacked card list driven by `data-label`
  attributes from column titles — pure CSS, no JS, no media-query service.

### `ZenModal` and `IZenModalService`

The service exists because the alternative is worse. Without it every page that needs a dialog
declares a `ZenModal` in its own markup and owns a `bool _isOpen` — so a confirmation prompt cannot
be raised from a service, a nested component, or an event handler without threading state back up
the tree. The service lets any code say `await Modals.ConfirmAsync(...)` and get an answer.

- `IZenModalService.ShowAsync<TComponent>(parameters)` returns a `ZenModalResult` the caller awaits,
  so a dialog reads as a function call rather than a state machine.
- `ConfirmAsync(title, message, options)` for the common case, so a yes/no prompt needs no component.
- A single `ZenModalHost` placed once in the layout renders whatever the service has open. Modals
  stack; the host tracks a list, not a single slot.
- Native `<dialog>` with `showModal()`, which gets the top layer, inert background, and Escape
  handling from the platform rather than from re-implemented JavaScript.
- Focus moves to the dialog on open and **returns to the element that opened it** on close —
  the part most hand-rolled modals miss, and the one that strands keyboard users at the top of the
  page.
- `zen-focus.js` supplies the focus trap for the fallback path and the scroll lock.

### `ZenToast` and `IZenToastService`

- `IZenToastService.Show(message, intent, options)`, plus `Success` / `Warning` / `Danger` / `Info`
  shorthands.
- One `ZenToastHost` in the layout, positioned by the consumer (corner, duration, max visible).
- `role="status"` for informational toasts and `role="alert"` for errors: the first must not
  interrupt what a screen reader is reading, the second must.
- Auto-dismiss pauses on hover and on focus. A toast that vanishes while being read, or while the
  user is reaching for its action, is a toast that failed at its one job.
- `prefers-reduced-motion` drops the slide animation but not the toast.

### `ZenProgress`

Determinate and indeterminate. `role="progressbar"` with `aria-valuenow` / `aria-valuemin` /
`aria-valuemax`, omitting `aria-valuenow` when indeterminate — which is exactly how a screen reader
is told "in progress, amount unknown". Linear and circular shapes.

### `ZenIndicator`

A small dot or count anchored to the corner of another element — unread badges, status dots on
avatars. Wraps its child rather than requiring the caller to manage positioning. The count is real
text, not a background image, so it is announced; a bare dot is `aria-hidden` and the meaning has to
live in the child's accessible name.

### `ZenToggle`

A switch. Rendered as `<button role="switch" aria-checked>` rather than a styled checkbox: a switch
takes effect immediately, a checkbox is a value to be submitted, and screen readers announce the two
differently. `ZenCheckbox` remains the right control inside a form.

### `ZenRangeSlider`

Native `<input type="range">` underneath, restyled — the platform already gives correct keyboard
handling, touch targets and `aria-valuetext`, and every hand-rolled slider gets at least one of
those wrong. Single value first; a dual-thumb range variant only if a real need appears, since it
requires abandoning the native element.

### `ZenTree<TItem>`
WAI-ARIA tree pattern: `role="tree"` / `treeitem` / `group`, roving `tabindex`, arrow-key navigation
(Left collapses or moves to parent, Right expands or moves to first child), Home/End, type-ahead.
Lazy children via `Func<TItem, Task<IEnumerable<TItem>>>`.

### JS modules — exactly three

| Module | Responsibility | Status |
| --- | --- | --- |
| `zen-theme.js` | localStorage, `data-zen-theme`, `matchMedia` → broadcast to all .NET listeners | ✅ M0 |
| `zen-popover.js` | Anchored positioning with flip/shift, click-outside, Escape | M3 |
| `zen-focus.js` | Focus trap, `scrollIntoView` for active listbox/tree items, focus restore | M3 |

Currency formatting, debouncing and textarea auto-grow are handled in C#/CSS — no JS.

---

## Accessibility baseline

Correct semantic element or ARIA role; keyboard operation per the WAI-ARIA Authoring Practices
pattern; a visible focus ring using `--zen-ring` (never `outline: none` without a replacement);
`aria-invalid` / `aria-describedby` on invalid fields; ≥4.5:1 contrast in both palettes. Each
component's bUnit test asserts its required ARIA attributes.

---

## Milestones

| # | Scope | Done when | Status |
| --- | --- | --- | --- |
| **M0** | Repo scaffold, Tailwind v4 pipeline, tokens, theme service/provider/toggle, `ZenComponentBase`, `ZenJsComponentBase`, `CssBuilder`, demo, tests, CI | Demo runs; light/dark/system repaints via CSS variables alone; CI green | ✅ |
| **M1** | `ZenIcon`, `ZenButton`, `ZenBadge`, `ZenSpinner`, `ZenSkeleton`, `ZenCard`, `ZenStatCard`, `ZenField`, `ZenInputBase<T>` | A demo page per primitive, verified in both palettes | ✅ |
| **M2** ✅ | Text, textarea, number, currency, date, search, checkbox (+group), radio group, native select, `ZenToggle`, `ZenRangeSlider`, `ZenForm`, plus the feedback primitives `ZenProgress` and `ZenIndicator` | A demo form binds an `EditForm` + `DataAnnotationsValidator` and shows per-field errors; the same inputs also work **without** an `EditForm` | ✅ |
| **M3** ✅ | `ZenModal` + `IZenModalService`, `ZenToast` + `IZenToastService`, `ZenPopover`, `ZenCombobox<TItem>`, `ZenList<TItem>` | A dialog can be raised and awaited from a service with no markup on the page; focus returns to the opener on close; combobox passes keyboard + ARIA tests and prerenders as a closed labelled field with JS disabled | ✅ |
| **M4** | `ZenTable<TItem>` (sort/page/select/hierarchy/responsive collapse), `ZenTree<TItem>`, `ZenTimeline` | Demo renders a 3-level hierarchical table and a lazy-loading tree | |
| **M5** | `ZenAppBar`, `ZenNavMenu`, `ZenSideNav`, `ZenFooter`, `ZenAppShell` | Shell demo usable at 360 / 768 / 1440 px; drawer traps focus and restores it on close | |
| **M6** | Docs, a11y audit, `dotnet pack`, NuGet metadata, v1.0.0, **close the consumer-`@theme` gap** | `.nupkg` consumed successfully by a scratch Blazor Server app **and** a Blazor WASM app | |

---

## Verification

1. **Demo app** (`samples/ZenithUI.Demo`) — `dotnet watch`; one page per component, a global
   light/dark/system toggle, and a route group rendered as pure static SSR.
2. **bUnit tests** — per component: expected markup, parameters apply, two-way binding raises
   `ValueChanged`, validation messages surface, required ARIA attributes present. Renderer is set
   explicitly per test via `Renderer.SetRendererInfo`, so both the interactive and static-SSR paths
   are covered.
3. **Render-mode proof** — the demo renders the same component type under `InteractiveServer`,
   `InteractiveWebAssembly`, and no render mode at all, on one page.
4. **Responsive & visual check** — drive the running demo with `claude-in-chrome` at 360 / 768 /
   1440 px in both palettes.
5. **Packaging check** — `dotnet pack`, then install from a local feed into throwaway Server and
   WASM apps. CI additionally fails if `zenith.css` is missing from the `.nupkg`, since a silently
   no-op Tailwind target still produces a package that builds and restores.
6. **Contrast audit** — parses the token stylesheets, converts every OKLCH value to linear sRGB, and
   asserts WCAG 2.2 ratios plus sRGB gamut containment. Reports **every** violation per run with the
   max in-gamut chroma for each offender, so a palette fix is one edit rather than a
   guess-and-rerun loop.

---

## Deviations from the original plan

Recorded because each changed the design rather than merely the code.

| Change | Why |
| --- | --- |
| Intents became **quartets**; added `--zen-{intent}-strong` | The contrast audit proved no single amber serves as both fill and text. The planned trio was insufficient. |
| Added `Styles/safelist.css` | Tailwind emits only what it scans, so a consumer not running Tailwind would have had no semantic utilities at all. |
| The demo grew **its own** Tailwind build | Precompiled library CSS cannot contain a consuming app's own utilities. The demo has to model the real consumer path, not a shortcut. |
| `Components/_Imports.razor` sits one level down, not at the project root | A root `_Imports.razor` collides with the folder-level `@namespace ZenithUI` files. |
| `ZenThemeToggle` initializes the service itself | A provider cannot be placed in a static layout, which is where app bars live. |
| `zen-theme.js` keeps a `Set` of listeners | One module instance is shared by every interactive island on the page. |

## Known gaps

- **Consumer `@theme` import from NuGet.** An app that runs its own Tailwind needs to import
  ZenithUI's `@theme` so `bg-surface` compiles in its markup. The demo does this by relative path,
  which only works from a source checkout. The real fix is a companion npm package or a documented
  `$(NuGetPackageRoot)` import. Scheduled for M6.
