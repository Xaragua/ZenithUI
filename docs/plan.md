# ZenithUI — implementation plan

> **Status:** M0 through M6 complete (2026-09-23), shipping as `1.0.0-rc.1.3`. The consumer-`@theme`
> gap is closed, the package is verified from a feed by both a Blazor Web App and a standalone
> WebAssembly app, and the accessibility sweep is clean across every page, both palettes and nine
> interactive states. `1.0.0` follows whatever the release candidate turns up.
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
  tools/accessibility/            axe-core sweep of the running demo (M6)
```

### Namespace convention

**Every public type is in `ZenithUI`** — components, the parameter enums, the base classes and the
service interfaces alike — so a consumer needs exactly one `@using` and folders stay free to move.
Each component `.razor` file opens with `@namespace ZenithUI`; the types under `Core/`, `Services/`
and `Extensions/` declare it too, rather than mirroring their folder.

That last part was a correction made in M6, from a scratch app rather than from review: the enums
sat in `ZenithUI.Core` and the services in `ZenithUI.Services`, so `Intent="ZenIntent.Primary"`
failed to build until the consumer had found a second `@using`, with nothing but the source tree to
explain why.

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
`ZenBadge`, `ZenSpinner`, `ZenSkeleton`, `ZenPopover`, `ZenField`, `ZenIndicator`, `ZenProgress`,
`ZenText` (added after `1.0.0-rc.1`).

**Inputs** — `ZenTextInput`, `ZenTextArea`, `ZenDateInput`, `ZenDatePicker` + `ZenCalendar`,
`ZenNumberInput<TValue>`, `ZenCurrencyInput` (culture-aware, **formats on blur in C#** — no JS
masking, which sidesteps caret-position bugs), `ZenSearchInput`, `ZenCheckbox`,
`ZenCheckboxGroup<T>`, `ZenRadioGroup<T>` + `ZenRadio<T>`, `ZenToggle`, `ZenRangeSlider`.

**Overlays and feedback** — `ZenModal` + `IZenModalService`, `ZenToast` + `IZenToastService`.

**Selection** — `ZenSelect<TValue>` (native `<select>`, works in SSR with zero JS),
`ZenCombobox<TItem>` (typeahead, async `ItemsProvider`, single/multi, full WAI-ARIA combobox
pattern), `ZenLookup<TItem>` (added in `1.0.0-rc.1.4`: a search field whose popup is a
`ZenTable`, the combobox-with-grid-popup pattern).

**Data display** — `ZenList<TItem>`, `ZenTable<TItem>` + `ZenColumn<TItem>` (server data,
virtualization and grouping added in `1.0.0-rc.1.4`), `ZenTree<TItem>`,
`ZenTimeline` + `ZenTimelineItem`, `ZenStatCard`, `ZenEmptyState`.

**Containers** — `ZenCard` (`Header` / `Body` / `Footer` / `Actions` slots), `ZenForm`.

**Chrome & layout** — `ZenAppBar`, `ZenNavMenu` + `ZenNavLink` + `ZenNavGroup` (collapsible
sections), `ZenSideNav` (off-canvas below `lg`), `ZenFooter`, `ZenAppShell`, `ZenStepper` +
`ZenStep` (added in `1.0.0-rc.1.4`).

**Page layout** (added in `1.0.0-rc.1.3`) — `ZenStack`, `ZenGrid` + `ZenGridItem`, `ZenContainer`,
`ZenSpacer`.

### Layout components exist because `zenith.css` is not Tailwind

A consumer without Tailwind of their own could theme every component and still could not lay out a
page. `zenith.css` holds only what the library's build scanned, so `md:grid-cols-3` in the
consumer's markup works if some component happens to use it, and otherwise does nothing without any
warning. A layout written that way can also break in a later release that stops using a class.

The components follow `ZenText`'s pattern, which exists for the same reason:

- **Enum-typed parameters, each mapped to a literal class in a switch arm**, where the scanner
  sees it. `ZenSpace` (`None` … `Xxl` → `gap-0` … `gap-12`), `ZenDirection`, `ZenBreakpoint`,
  `ZenCrossAlign`, `ZenJustify`, `ZenLayoutElement`. The widths reuse `ZenContentWidth` and
  `ZenStyles.ContentWidth`, so a `ZenContainer` and a `ZenAppShell` of the same width line up.
- **Responsive values are separate parameters** (`ColumnsMd="3"`), not a responsive struct. They
  read naturally in Razor, IntelliSense lists them, and each maps to one switch.
- **Column counts are 1–6 and 12.** Across the five breakpoints that is 35 classes, rather than 60
  for every count up to 12, and it covers the counts people use. `MinItemWidth` handles everything else with **one** class,
  `grid-cols-[repeat(auto-fill,minmax(min(var(--zen-grid-min),100%),1fr))]`, whose width comes
  from a custom property, so any length costs nothing extra. The `min(…, 100%)` keeps a lone column
  from overflowing a screen narrower than the minimum.
- **An unset parameter emits no class**, so the component adds no rule the caller didn't ask for.
- **Invalid combinations throw**: an unsupported count, `MinItemWidth` with `Columns`, `FullWidth`
  with `Span`, `HorizontalFrom` on a horizontal stack. Otherwise each would be a class that silently
  does nothing, the failure these components exist to remove.
- **No role of their own.** `As` provides the element, as it does on `ZenText`.
- **`ZenGridItem` always carries `min-w-0`.** A grid item will not shrink below its content by
  default, so one long unbroken line would widen its column past the screen. The Typography demo
  page hit this first.
- **Out of scope:** padding and margin props, per-child grow, shrink and order, and arbitrary
  lengths. A component for each utility would amount to a second, weaker Tailwind.

`StylesheetCoverageTests` renders every value of every layout and typography parameter and looks
up each emitted class as a selector in the built `zenith.css`. Before this, nothing checked that
guarantee. The cost was 4.2 KB of minified CSS (62,199 → 66,396 bytes).

### The shell needs no render mode, and that constraint chose its design

A shell lives in a layout. A layout can never be interactive, because `Body` is a `RenderFragment`
and a render fragment cannot cross a render-mode boundary — so a drawer toggled by `@onclick` and a
`bool` would demo beautifully on the page that showed it off and be unusable in the only place a
shell goes.

So `ZenSideNav` renders **one** `<aside>` carrying `popover="auto"`. Above `lg` it is a sticky rail
and the popover is never opened; below `lg` a bare `<button popovertarget>` in the app bar toggles
it. `popovertarget` is resolved by the browser against the document rather than by Blazor against a
render tree, which is also why the button works when the bar and the nav are separate interactive
islands. The platform then supplies the top layer, `::backdrop`, Escape, click-outside dismissal,
focus restore to the invoker, and — verified in-browser — an implicit `expanded` state on the
invoker in the accessibility tree.

One element rather than a rail plus a drawer: two would put every link in the DOM twice, with
duplicate ids and a navigation announced twice on every page.

What a render mode *adds*, through a single `bindDrawer` call: a focus trap (`popover="auto"` does
not confine Tab), a scroll lock, and dismissal when a link inside is followed under enhanced
navigation — which swaps the page with no event the platform treats as a dismissal. All three are
absent without it, not broken.

`ZenAppShell` contributes the skip link, the `<main>` landmark with `tabindex="-1"` for it to aim
at, the cascaded ids that join the bar to the nav, and `--zen-sidenav-top` — how far down the
viewport a sticky rail begins, which is the one measurement neither component can derive, since CSS
gives an element no way to measure a sticky sibling.

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
- **Detail rows:** `RowDetailTemplate` opens a sibling `<tr>` spanning every column, holding
  arbitrary content including another `ZenTable`. A `<tr>` cannot contain another `<tr>`, and a
  panel constrained to one column's width defeats the purpose, so it cannot be nested inside a
  cell. Rendered only while open. The disclosure is a plain `<button aria-expanded>` — valid
  anywhere, so detail rows cost the table no role claim.
- **Pager:** numbered pages with elision, a rows-per-page selector, and `aria-current="page"`. A
  gap of exactly one page is filled rather than elided. Changing the page size keeps the first
  visible row in view instead of jumping to page one.

#### Collecting the columns: `ZenDefer`

A parent builds its entire render tree before any of its child components exist, so a table whose
markup depends on what its columns report sees none of them on the first pass.

The reflex fix — `StateHasChanged` from `OnAfterRender`, render again — fails exactly where it
matters. Static SSR never renders a second time, so a server-rendered table would ship with an
empty `<tbody>`, and the failure is invisible to a test suite that only ever renders interactively.

`ZenDefer` fixes the ordering instead of paying for a second pass. The renderer's queue is FIFO,
and a new child component's parameters are set — so its `OnParametersSet` runs — while the frame
that introduces it is being diffed. A table that renders

```razor
<CascadingValue Value="this" IsFixed="true">@Columns</CascadingValue>
<ZenDefer>…the table markup…</ZenDefer>
```

queues the cascade ahead of the defer; the cascade's diff registers every column; only then does
the deferred body render. One pass, correct under every render mode, with a test for the static
SSR case specifically.

#### What role the table claims

A flat table stays a plain `<table>` even when rows are selectable. `role="grid"` owes the user
arrow-key navigation between cells, and this component does not provide it — the same reason
`ZenList` refuses to call a non-interactive list a listbox. Selection is expressed with real
checkboxes, which are announced, keyboard-operable and form-submitting without any claim being
made.

A hierarchical table has no such option, because HTML cannot express "this row is a child of that
one". So it takes `role="treegrid"` with `aria-level` and `aria-expanded` on rows — and the
keyboard contract that comes with the claim: a roving tabindex over rows, Right to expand and
descend, Left to collapse and climb, Home/End, and focus moved after the render because
ArrowRight descends into a row that did not exist when the key arrived. Rows are the unit of
navigation; cells are not, because Left and Right are spoken for by the hierarchy and interactive
cell content stays reachable with Tab.

`ZenTree` states none of `aria-level`, `aria-setsize` or `aria-posinset` and `ZenTable` states
them all. That is not an inconsistency: a tree's nested `<ul role="group">` supplies them, and
restating them would be a second source of truth that no test can catch disagreeing with the DOM.
A table has no such structure to lean on.

#### Server data, virtualization and grouping (M7)

Every body row is built into **one flat list of display rows**, whatever its kind: data rows, tree
rows and group headers. The plain loop and `<Virtualize>` both render that list through a single
row fragment. So a virtualized table groups, expands and opens detail rows with no second code
path, and a fix made to a row is made to both bodies. Each row carries its index in the whole list,
which keeps ids and `aria-rowindex` correct once only a window of rows exists.

- **`ItemsProvider`** is the answer to "the data is remote". The table asks for a window
  (`ZenTableRequest`: start, count, `SortName`, direction and a cancellation token) and draws the
  pager from the returned total. Sorting sends `ZenColumn.SortName`, and only columns with one
  sort, because a `Field` delegate cannot become a query. Requests are keyed, so a view that arrives
  both as an event and as a bound parameter loads once. A superseded request is cancelled.
- **`Virtualize`** uses the framework's component with `<tr>` spacers **and a `<tr>`
  placeholder**. The default placeholder is a `<div>`, which table layout wraps and does not size.
  `<Virtualize>` subtracts the height it assumed the placeholders had from the gap it measured
  between its spacers, so after a long jump the derived row height collapsed to a few pixels. The
  table's scroll wrapper is also `relative`, for the same arithmetic: without it, absolutely
  positioned text inside a row stays put while the rows scroll and stretches the measured box.
  Virtualizing and paging are exclusive, since they answer the same problem. The stacked mobile
  layout is off while virtualizing. Under static SSR the first 50 rows render as plain markup.
- **`GroupBy`** inserts a `<th scope="rowgroup">` header row per group. Groups collapse, and a
  tri-state checkbox selects the whole group. That checkbox deliberately reaches further than
  select-all, which stops at the page, because the header states its count. Paging counts a
  collapsed group as one row, and a group's header repeats at the top of a page it continues onto.
  Grouping is exclusive with the treegrid, since both nest rows, and with a provider, which never
  has every row.

### `ZenLookup<TItem>`

A search field whose popup is a table, for picking one record out of many by any of its columns.
A combobox option is one line of text. A vendor, an account or a product is several facts, and
telling two similar ones apart means seeing them side by side, sorted by the one that matters.

- **The popup is a `ZenTable`.** An internal `ZenTablePicker<TItem>`, passed down as a cascading
  value rather than as public parameters, puts the table into picker mode. In that mode the table
  claims `role="grid"`, rows carry `aria-selected`, and each row's first cell has an id. A table
  that claims to be a grid owes a keyboard contract, and here the text field keeps it. A page
  cannot switch the mode on and then fail to keep it.
- **Focus stays in the field**, per the APG combobox-with-grid-popup pattern.
  `aria-activedescendant` names the active row's first cell. `aria-controls` names the grid, not
  the panel around it, which also holds a pager and a confirm bar. The panel has no role.
- **The panel is wider than the field** when the columns need it. `PanelWidth` sets it, with the
  field's width as the minimum (`MatchWidth`) and the viewport's as the maximum (`max-width`, plus
  `ZenPopover`'s shift).
- **Closing without a pick never changes the value.** Escape, an outside click and Tab-ing away
  all restore the committed text. `mousedown` is prevented inside the panel. Otherwise a click on a
  row would blur the field first and close the panel before the click arrived.
- **The commit mode** is `Immediate` or `Confirm`. In Confirm mode a pick only marks the row, a live
  footer says which row, and Confirm (or Ctrl+Enter) writes the value.

### `ZenStepper` + `ZenStep`

A wizard. It is **not the tabs pattern**. Tabs promise every panel in any order with the arrow
keys, and a linear wizard exists to refuse that. The header is an `<ol>` in a `<nav>`, the active
step is `aria-current="step"` as in `ZenTimeline`, and each step's state is stated in words. A
reachable step is a button; an unreachable one is text rather than a disabled button.

- Only the active step's content renders. If a step has an `EditContext`, it must validate before
  Next, and `OnLeaving` can then cancel the move. Back never validates, because blocking it traps a
  user who needs to fix an earlier answer.
- On every change, focus moves to the new step's heading, as `FocusOnNavigate` does for a page.
  The heading has `tabindex="-1"`, and the existing rule keeps a ring off it.
- **Steps are collected with `ZenDefer` but are not rebuilt on every pass**, as columns are. Blazor
  only re-sets a child's parameters when one of them might have changed, and a step with nothing
  but a title and a flag has none that can. Such a step registered once and then vanished from the
  header on the next render. Steps now join the list the first time they register and leave it
  when disposed. Each pass's registrations are used only to place new steps.

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

Roving tabindex here, `aria-activedescendant` in `ZenList` — the two are opposites on purpose. A
listbox keeps focus on its container because the thing typing into it may be a combobox's text
field; a tree has no such partner, its items are what the user navigates, and screen-reader tree
mode is built around the focused item being a genuinely focused element. Both are one tab stop.

The focus ring is drawn on the row rather than on the `<li>` that holds focus, because the `<li>`
contains the node's entire subtree and an outline on it would encircle every descendant.

A lazily loaded node is drawn as expandable before anything is known about it. Drawing no chevron
would hide the subtree behind an interaction nobody can discover; the guess corrects itself when
the loader returns nothing and the node becomes a leaf. Supply `HasChildren` whenever the answer
is knowable without fetching.

### JS modules — exactly three

| Module | Responsibility | Status |
| --- | --- | --- |
| `zen-theme.js` | localStorage, `data-zen-theme`, `matchMedia` → broadcast to all .NET listeners | ✅ M0 |
| `zen-popover.js` | Anchored positioning with flip/shift, click-outside, Escape | M3 |
| `zen-focus.js` | Focus trap, `scrollIntoView` for active listbox/tree items, focus restore | ✅ M3 |
| `zen-dom.js` | Properties with no attribute equivalent, and imperative one-shots: `indeterminate`, `focus`, `blur`, active-element containment, scrolling a grid row clear of a sticky header | ✅ M2, M7 |

Currency formatting, debouncing and textarea auto-grow are handled in C#/CSS — no JS.

"Exactly three" turned out to be four. `zen-dom.js` was not foreseen because the gap it fills is
not a behaviour: a checkbox's `indeterminate` is a DOM *property* with no attribute, so it cannot
be expressed in server-rendered markup at all, and `focus()` has no declarative equivalent either.
`ZenTree` and the hierarchical `ZenTable` both use it for exactly one call — moving DOM focus to
the row a roving tabindex has moved to, without which a screen reader stays on the row the user
has left. A flat table loads no JavaScript at all.

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
| **M4** ✅ | `ZenTable<TItem>` + `ZenColumn<TItem>` (sort/page/select/hierarchy/detail rows/responsive collapse), `ZenEmptyState`, `ZenTree<TItem>`, `ZenTimeline` + `ZenTimelineItem` | Demo renders a 3-level hierarchical table and a lazy-loading tree | ✅ |
| **M5** ✅ | `ZenAppBar`, `ZenNavMenu` + `ZenNavLink` + `ZenNavGroup`, `ZenSideNav`, `ZenFooter`, `ZenAppShell` | Shell demo usable at 360 / 768 / 1440 px; drawer traps focus and restores it on close | ✅ |
| **M6** ✅ | Docs, a11y audit, `dotnet pack`, NuGet metadata, **close the consumer-`@theme` gap**, one public namespace | `.nupkg` consumed successfully by a scratch Blazor Server app **and** a Blazor WASM app | ✅ `1.0.0-rc.1` |
| **M7** ✅ | `ZenTable` `ItemsProvider`, virtualization and grouping; `ZenLookup<TItem>`; `ZenStepper` + `ZenStep` | A 10,000-row table scrolls and groups in the demo, a lookup binds from 5,010 server-side rows in both commit modes, and a wizard blocks Next on an invalid step; the sweep covers every new state | ✅ `1.0.0-rc.1.4` |

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
7. **Accessibility sweep** (`tools/accessibility`, added in M6) — axe-core over every demo page in
   both palettes, plus the states that only exist after an interaction: an open modal, a raised
   toast, an open combobox listbox and popover, the drawer at 390 px, a sorted table with an
   expanded detail row, an expanded tree, an invalid submitted form, an open calendar. A state it
   cannot reach counts as a failure, because a passing audit of a modal that never opened is worse
   than no audit. See [`accessibility.md`](accessibility.md).

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
| A fourth JS module, `zen-dom.js` | `indeterminate` is a DOM property with no attribute, and `focus()` has no declarative form. Neither is behaviour, which is why neither was foreseen. |
| `ZenDefer`, and it is public | A parent renders before its children exist, so a column-collecting table needs ordering rather than a second pass — and static SSR has no second pass to give. Public only because Razor resolves markup elements to public component types. |
| A selectable flat table is **not** `role="grid"` | The plan implied a grid role followed from selection. A grid owes arrow-key cell navigation; checkboxes deliver accessible selection while promising nothing. |
| `ZenEmptyState`, not in the original inventory | A blank panel cannot be told apart from a loading one or a broken one. Every collection component needs the distinction, so it is a component rather than a string parameter on each. |
| The table's page-size control is a bare `<select>` | `ZenSelect` is a form control and registers a field in any cascading `EditContext`. A table inside an `EditForm` would have its page size join that form's validation. |
| `ZenTable` sorting is tri-state | The order data arrived in carries information — usually "newest first" from the server — and a two-state toggle leaves no way back to it. |
| `TItem` constrained to `notnull` on `ZenTree` and `ZenTable` | Expansion, lazy-load caching and selection are all keyed by the item. A null node has no identity to key on. |
| The side-nav drawer is a **popover**, not Blazor state | A layout cannot be interactive, so a shell whose nav needed a render mode could not be used as a layout. The platform's `popovertarget` crosses boundaries Blazor's cascade cannot. |
| `ZenNavLink` does not wrap Blazor's `NavLink` | `NavLink` decides the same thing and spends it on a CSS class only — it never sets `aria-current`. The active item was visible to sighted users and silent to everyone else. |
| `ZenNavGroup`, not in the original inventory | `ZenNavMenu` could only render a flat list under a static heading, so a nav with sections had no way to collapse them. It is a `<details>`, for the same reason the drawer is a popover. |
| A group's expansion comes from its own `Href`, not from its children | A parent builds its render tree before any child exists, so `open` is written before a link knows whether it matches. `ZenDefer` cannot help: a nav link renders the markup that goes *inside* the element whose attribute depends on it. The alternative is a second pass, which static SSR does not have. |
| Three elements in the chrome carry **no `display` utility at all** | A consumer running their own Tailwind emits `.flex` and almost certainly not `.lg\:hidden`, and their sheet loads last into the same `utilities` layer. See the note below — this was a real, shipped-looking bug. |
| `ZenSideNav`'s explicit `Id` outranks the shell cascade | The only way to wire a nav the cascade cannot reach: one the consumer made an interactive island under a static shell. |
| The consumer `@theme` ships as a **generated, flattened file plus an MSBuild copy**, not an npm package | The plan offered a companion npm package or a `$(NuGetPackageRoot)` import. The first splits one version across two registries; the second hardcodes a machine path and a version number in a checked-in stylesheet. A targets file in the package puts the file at a stable relative path inside the consumer's own project, which is neither. |
| Every public type moved into `ZenithUI`, not only the components | The plan said "root namespace `ZenithUI`" and the implementation read that as the components only. Consuming the package from a scratch app showed what that costs: three `@using` lines for one library, and a build error for the most obvious line of markup anyone writes. |
| A second copy of the anti-flash script, as `zen-theme-init.js` | `ZenThemeScript` cannot render into a standalone WebAssembly app's static `index.html`, so the one hosting model that could not use it got the flash the component exists to prevent. A test asserts the two copies are the same program. |
| `aria-selected` moved from the calendar's day button to its `gridcell` | The attribute is undefined for `role="button"`. Found by the M6 audit, in markup that had no tests at all — which is also why `ZenCalendar` and `ZenDatePicker` now have seventeen. |
| `ZenDatePicker`'s panel became a `ZenPopover` | The plan had it as "an absolutely positioned panel… ZenPopover will supersede this", and M3 never came back for it. `ZenCard` is `overflow-hidden`, so the calendar clipped inside the container date pickers most often sit in — a shipped bug that no test could see and every screenshot could. |
| The combobox popover panel **is** the listbox | A scrolling wrapper between the panel and the options made `aria-controls` name a roleless element, detached the options from the listbox claiming them, and pointed `scrollItemIntoView` at a node the accessibility tree does not contain. `ZenPopover` gained a `PanelId` parameter because a parent cannot read a child's generated id in the pass that creates it. |
| That file carries the library's **whole candidate list**, not just `@theme` | `@theme` alone fixes only half the problem. The other half is cascade order, and the only way two stylesheets can stop disagreeing about `lg:hidden` is for both to emit it. The list comes from `@tailwindcss/oxide` — Tailwind's own scanner — so it cannot drift from what the library was built with. |
| `ZenText`, not in the original inventory, and its element is **not** its variant | Every heading in the demo was hand-rolled Tailwind, and a consumer without Tailwind could not reach the type scale at all. The element is a separate `As` parameter because tying look to level makes people skip heading levels to get a size — the heading-order failure the M6 audit found in the demo. |
| A table's pager is named after its table | Every pager was `<nav aria-label="Pagination">`, so the first page with two paged tables had two identical landmarks. The sweep found it as soon as M7's demo added the second one. |
| `ZenStepper` keeps its steps across passes; `ZenTable` rebuilds its columns | Blazor skips `SetParametersAsync` for a child whose parameters are all unchanged primitives. A column always has a delegate, so it always re-registers. A disabled placeholder step has nothing but strings and bools, and it silently disappeared on the second render. |
| `ZenLookup` hosts a `ZenTable` through an internal cascade | The alternative was a second grid duplicating sorting, paging, virtualization and the provider. Public parameters for the picker mode would have let a page claim `role="grid"` with nobody keeping the keyboard contract. |
| Layout components, not in the original inventory | The plan left page layout to Tailwind, which assumed every consumer runs it. One who does not can theme every component and cannot place any of them, because `zenith.css` has only the layout classes the library happens to use. See [the section above](#layout-components-exist-because-zenithcss-is-not-tailwind). |

### The cascade-order trap a precompiled component library walks into

Found in the browser during M5, after every test passed.

A library that ships precompiled CSS **alongside** a consumer running their own Tailwind has two
stylesheets writing into the same `utilities` layer, and within a layer the later file wins. The
consumer's build emits only what *they* use. So `.flex` is almost certainly in their sheet and
`.lg\:hidden` almost certainly is not — and a library component written as `class="flex lg:hidden"`
is `display: flex` at every width, in every app but the ones that happened to use `lg:hidden` in
their own markup.

On the drawer the same collision was worse than cosmetic. An unopened popover is hidden by a **UA**
rule, and any author `display` outranks the UA origin whatever the specificity — so `.flex` on the
`<aside>` left the drawer permanently on screen, overlapping the page, with no scrim and no top
layer. It read as a broken component and was a cascade-order accident.

The fix is narrow and the rule is general: **where `display` is decided by a breakpoint or by
popover state, the library states it in its own `@layer components` rule and puts no display
utility on the element.** Hence `.zen-sidenav`, `.zen-sidenav-bar` and `.zen-nav-toggle`.

The same hazard applies in weaker form to any responsive variant competing with an unprefixed
utility (`sm:px-6` against `px-4`), where the cost is cosmetic rather than structural.

M6 closed the class of bug rather than the instances. The generated `tailwind/zenith.theme.css`
carries every candidate the library's own markup contains, so a consumer's Tailwind build emits the
same rules ZenithUI's stylesheet does and the two cannot disagree about a shared utility — whichever
one the browser parses last. The three `@layer components` display rules stay as they are: they are
correct on their own terms, and they also protect an app that links `zenith.css` without ever
importing the theme.

## Known gaps

_None currently open. The consumer-`@theme` gap closed in M6 — see
[`theming.md`](theming.md#if-your-app-runs-its-own-tailwind)._
