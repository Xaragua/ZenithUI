# ZenithUI

A TailwindCSS component library for Blazor. Render-mode agnostic, themed entirely through CSS
custom properties, with first-class light and dark palettes.

> **Status: M2 — form controls.** Tokens, theming, core base types, primitives and the full form
> control set are in place. Modal and toast services land in M3; the table, tree and app shell in M4–M5.

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
  and every component's ARIA contract has unit tests.

## Getting started

```bash
dotnet add package ZenithUI
```

**1. Register the services** — in every project that renders ZenithUI components. A Blazor Web App
with WebAssembly interactivity has two service containers, so both `Program.cs` files need it:

```csharp
builder.Services.AddZenithUI();
```

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
| Runs Tailwind | Link `zenith.nopreflight.css`, and import ZenithUI's `@theme` into your own entry so `bg-surface` compiles for your markup too. |

`samples/ZenithUI.Demo/Styles/app.css` is a working example of the second case.

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

### The contrast audit

`ThemeTokenTests` parses `tokens/base.css` and `tokens/dark.css`, converts every OKLCH value to
linear sRGB, and asserts the WCAG 2.2 ratios documented at the top of `base.css` — plus that no
token falls outside the sRGB gamut, since a clipped colour is not the colour that was declared.

Failures report **every** violation at once, with the maximum in-gamut chroma for each offender, so
a palette fix is one edit rather than a guess-and-rerun loop:

```
[dark] tokens outside the sRGB gamut:
  --zen-primary = oklch(0.72 0.16 264) (max in-gamut chroma at this L/H is about 0.145)
```

## Roadmap

| Milestone | Contents |
| --- | --- |
| **M0** ✅ | Tokens, theming, `ZenComponentBase`, `ZenJsComponentBase`, `CssBuilder`, CI |
| **M1** ✅ | `ZenIcon`, `ZenButton`, `ZenBadge`, `ZenSpinner`, `ZenSkeleton`, `ZenCard`, `ZenStatCard`, `ZenField`, `ZenInputBase<T>` |
| **M2** ✅ | Text, textarea, number, currency, date, search, checkbox, radio, select, `ZenToggle`, `ZenRangeSlider`, `ZenForm`, `ZenProgress`, `ZenIndicator` |
| **M3** | `ZenModal` + modal service, `ZenToast` + toast service, `ZenPopover`, `ZenCombobox`, `ZenList` |
| **M4** | `ZenTable` (sorting, paging, selection, hierarchy), `ZenTree`, `ZenTimeline` |
| **M5** | `ZenAppBar`, `ZenNavMenu`, `ZenSideNav`, `ZenFooter`, `ZenAppShell` |
| **M6** | Docs, accessibility audit, NuGet publish, v1.0.0 |

## Documentation

All project documentation lives in [`docs/`](docs/). This README is the only document at the
repository root, because it is the GitHub landing page.

| Document | Contents |
| --- | --- |
| [`docs/plan.md`](docs/plan.md) | The implementation plan of record — architecture, component inventory, milestones, and the design decisions behind them |
| [`docs/CHANGELOG.md`](docs/CHANGELOG.md) | What shipped, and what was fixed along the way |

## License

MIT
