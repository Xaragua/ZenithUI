# ZenithUI

A TailwindCSS component library for Blazor. Render-mode agnostic, themed entirely through CSS
custom properties, with first-class light and dark palettes.

> **Status: M6 — `1.0.0-rc.1.1`.** Every component in the plan ships: tokens and theming, primitives,
> the full form control set, the overlay family (modal + toast services, popover, combobox, list),
> the data components (table, tree, timeline) and the application shell. The package is consumed
> and verified from a feed by both a Blazor Web App and a standalone WebAssembly app, the
> accessibility sweep is clean, and the API is frozen pending whatever a release candidate turns
> up.

## Why another Blazor component library

- **You keep your Tailwind, or you use none at all.** ZenithUI ships a precompiled stylesheet. A
  consumer's Tailwind `content` globs cannot scan `.razor` files that live compiled inside a NuGet
  package, so relying on that would have been broken by design.
- **Rebranding is a CSS override, not a fork.** Components never write a literal colour.
  `bg-surface` compiles to `background-color: var(--zen-surface)` — redefine the variable and the
  whole library follows.
- **No render-mode assumptions.** The library declares `@rendermode` nowhere. The same component
  works under static SSR, Interactive Server and WebAssembly, and degrades honestly when there is
  no JavaScript runtime attached.
- **Accessibility is tested, not claimed.** Contrast ratios are audited in CI against both palettes,
  every component's ARIA contract has unit tests, and an axe-core sweep drives the running demo —
  every page in both palettes, plus the states that only exist after an interaction. See
  [`docs/accessibility.md`](https://github.com/Xaragua/ZenithUI/blob/main/docs/accessibility.md), including what those checks cannot see.

## Getting started

```bash
dotnet add package ZenithUI
```

**1. Register the services** — in every project that renders ZenithUI components. A Blazor Web App
with WebAssembly interactivity has two service containers, so both `Program.cs` files need it:

```csharp
builder.Services.AddZenithUI();
```

Everything public is in one namespace, so `_Imports.razor` needs one line — `@using ZenithUI` —
covering the components, the parameter enums and the service interfaces alike.

**2. Reference the stylesheet and the anti-flash script** in `App.razor`:

```razor
<head>
    @* Must be synchronous and before the stylesheet - see below. *@
    <ZenThemeScript />
    <link rel="stylesheet" href="_content/ZenithUI/zenith.css" />
</head>
<body class="zen-root">
```

If your application already runs Tailwind, use `zenith.nopreflight.css` instead so the reset is not
emitted twice.

### Do you need Tailwind in your app?

Only if you write Tailwind classes in your own markup — and if you do, nothing changes about how
you'd set it up.

`zenith.css` contains the utilities ZenithUI's own components use, plus the full semantic surface
(`bg-primary`, `text-content-muted`, `rounded-zen-lg`, …) that `safelist.css` guarantees is always
emitted. It cannot contain `grid` or `max-w-6xl` for *your* pages: Tailwind only emits a class it
finds in a file it scanned, and the library's build never sees your `.razor` files.

So:

| Your app | What to do |
| --- | --- |
| No Tailwind of its own | Link `zenith.css`. Semantic utilities are all there. |
| Runs Tailwind | Link `zenith.nopreflight.css`, and add one import to your own Tailwind entry point (below). |

The package ships a flattened `zenith.theme.css` and an MSBuild target that copies it into your
project's `obj/` on build, so the import is a stable relative path rather than a version-stamped
path into your NuGet cache:

```css
@import "tailwindcss";
@source "../Components/**/*.razor";
@import "../obj/zenithui/zenith.theme.css";
```

That is what makes `bg-surface` compile for *your* markup — and it carries every class ZenithUI's
own components use, so the two stylesheets cannot disagree about a shared utility. See
[`docs/theming.md`](https://github.com/Xaragua/ZenithUI/blob/main/docs/theming.md) for why that second half matters;
`samples/ZenithUI.Demo/Styles/app.css` is a working example.

**3. Add a theme switcher** wherever it belongs:

```razor
<ZenThemeToggle />                                      @* light <-> dark *@
<ZenThemeToggle Mode="ZenThemeToggleMode.Segmented" />  @* System / Light / Dark *@
```

### Why `ZenThemeScript` has to be inline

`localStorage` is not sent with an HTTP request, so the server cannot know the viewer's stored
preference — the first HTML it produces is always the light palette. Without a synchronous inline
script in `<head>`, a dark-mode user sees a white flash on every navigation until the circuit
connects. A deferred script or a module would run after first paint and be useless for this.

## Theming

Three states are handled, because a viewer's theme has three states and only two of them stamp an
attribute on `<html>`:

| Stored preference | OS preference | Result |
| --- | --- | --- |
| none (`System`) | light | light |
| none (`System`) | dark | dark, live — follows the OS even in a background tab |
| `light` | dark | light — an explicit choice wins |
| `dark` | light | dark |

In `System` mode the `data-zen-theme` attribute is **removed** rather than set to the resolved
value. That is what keeps the page following the OS instead of freezing against it.

### Rebranding

Override the tokens in your own stylesheet, loaded after ZenithUI's:

```css
:root {
  --zen-primary: oklch(0.55 0.2 155);
  --zen-primary-content: oklch(0.99 0 0);
  --zen-primary-soft: oklch(0.955 0.03 155);
  --zen-primary-strong: oklch(0.46 0.13 155);
  --zen-radius-md: 0.125rem;
}
```

### Intent tokens come in fours

The four roles are not interchangeable:

| Token | Role |
| --- | --- |
| `--zen-{intent}` | A **fill** — solid button backgrounds, badges, progress bars |
| `--zen-{intent}-content` | The text that goes **on** that fill |
| `--zen-{intent}-soft` | A tinted **background** for low-emphasis chips and callouts |
| `--zen-{intent}-strong` | A **foreground** — text, icons and borders on surfaces or on `-soft` |

`-strong` exists because a fill colour and a text colour have opposite requirements, and for some
hues no single value serves both. A usable amber warning fill sits near L 0.76, which reads at about
2:1 as text on white; darkening the fill until the text passed would turn every warning button
brown. So: `bg-warning` + `text-warning-content` for a solid chip, `text-warning-strong` for
coloured text on a page. Never `text-warning` on a surface.

## Repository layout

```
src/ZenithUI/          The package. Razor Class Library + Tailwind v4 build.
samples/ZenithUI.Demo/ Live showcase. Server + WebAssembly + static SSR, one page per component.
tests/ZenithUI.Tests/  xUnit + bUnit, including the contrast audit.
tools/accessibility/   The axe-core sweep. Needs the demo running; not a CI step.
```

## Development

```bash
dotnet restore
dotnet build                                   # runs the Tailwind build via MSBuild
dotnet test
dotnet watch --project samples/ZenithUI.Demo   # demo at https://localhost:7xxx
```

The stylesheet is rebuilt by the `BuildTailwind` MSBuild target on every build; `npm ci` runs
automatically the first time. To iterate on CSS alone:

```bash
cd src/ZenithUI && npm run css:watch
```

### The accessibility sweep

Not a CI step, because it needs the demo running and a real rendering engine — which is exactly why
it catches what the unit tests cannot:

```bash
cd samples/ZenithUI.Demo && dotnet run            # one terminal
cd tools/accessibility && npm install && npm run sweep
```

### The contrast audit

`ThemeTokenTests` parses `tokens/base.css` and `tokens/dark.css`, converts every OKLCH value to
linear sRGB, and asserts the WCAG 2.2 ratios documented at the top of `base.css` — plus that no
token falls outside the sRGB gamut, since a clipped colour is not the colour that was declared.

Failures report **every** violation at once, with the maximum in-gamut chroma for each offender, so
a palette fix is one edit rather than a guess-and-rerun loop:

```
[light] tokens outside the sRGB gamut:
  --zen-primary = oklch(0.54 0.13 163) (max in-gamut chroma at this L/H is about 0.117)
```

## Roadmap

| Milestone | Contents |
| --- | --- |
| **M0** ✅ | Tokens, theming, `ZenComponentBase`, `ZenJsComponentBase`, `CssBuilder`, CI |
| **M1** ✅ | `ZenIcon`, `ZenButton`, `ZenBadge`, `ZenSpinner`, `ZenSkeleton`, `ZenCard`, `ZenStatCard`, `ZenField`, `ZenInputBase<T>` |
| **M2** ✅ | Text, textarea, number, currency, date, search, checkbox, radio, select, `ZenToggle`, `ZenRangeSlider`, `ZenForm`, `ZenProgress`, `ZenIndicator`, `ZenDatePicker` |
| **M3** ✅ | `ZenModal` + modal service, `ZenToast` + toast service, `ZenPopover`, `ZenCombobox`, `ZenList` |
| **M4** ✅ | `ZenTable` + `ZenColumn` (sorting, paging, selection, hierarchy, detail rows, responsive collapse), `ZenEmptyState`, `ZenTree`, `ZenTimeline` |
| **M5** ✅ | `ZenAppBar`, `ZenNavMenu` + `ZenNavLink` + `ZenNavGroup` (collapsible sections), `ZenSideNav` (off-canvas drawer needing no render mode), `ZenFooter`, `ZenAppShell` |
| **M6** ✅ | The consumer `@theme` artifact, one public namespace, the getting-started / theming / accessibility docs, the axe sweep, and `1.0.0-rc.1` verified from a feed by a Blazor Web App and a standalone WebAssembly app |
| **rc.1.1** ✅ | `ZenText` — the type scale as a component: heading and body variants, a separate `As` element, logical alignment, token-backed tones |

## Documentation

All project documentation lives in [`docs/`](https://github.com/Xaragua/ZenithUI/tree/main/docs/). This README is the only document at the
repository root, because it is the GitHub landing page.

| Document | Contents |
| --- | --- |
| [`docs/getting-started.md`](https://github.com/Xaragua/ZenithUI/blob/main/docs/getting-started.md) | Install and wire-up for each hosting model, what works with no render mode, and the template defaults that fight the library |
| [`docs/theming.md`](https://github.com/Xaragua/ZenithUI/blob/main/docs/theming.md) | The token surface, the three-state light/dark model, rebranding, and what an app running its own Tailwind has to do |
| [`docs/plan.md`](https://github.com/Xaragua/ZenithUI/blob/main/docs/plan.md) | The implementation plan of record — architecture, component inventory, milestones, and the design decisions behind them |
| [`docs/accessibility.md`](https://github.com/Xaragua/ZenithUI/blob/main/docs/accessibility.md) | What the library guarantees, the three layers that verify it, the M6 audit findings, and what automated checks cannot see |
| [`docs/CHANGELOG.md`](https://github.com/Xaragua/ZenithUI/blob/main/docs/CHANGELOG.md) | What shipped, and what was fixed along the way |

## License

MIT
