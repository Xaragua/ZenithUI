# Getting started

Five minutes to a themed, working page, for each of the three hosting models ZenithUI supports.
Every step below was walked through against a package installed from a feed — the sharp edges
called out are ones a scratch app actually hit, not ones imagined for the document.

- [Install](#install)
- [Blazor Web App](#blazor-web-app-server-or-webassembly-interactivity)
- [Standalone WebAssembly](#standalone-webassembly)
- [Static SSR, no interactivity at all](#static-ssr-no-interactivity-at-all)
- [Laying out a page without Tailwind](#laying-out-a-page-without-tailwind)
- [If your app runs its own Tailwind](#if-your-app-runs-its-own-tailwind)
- [Things that bite](#things-that-bite)

## Install

```bash
dotnet add package ZenithUI
```

Everything public lives in one namespace. Add it once, in `_Imports.razor`:

```razor
@using ZenithUI
```

That covers the components, the parameter enums (`ZenIntent`, `ZenSize`, `ZenThemeToggleMode`) and
the service interfaces (`IZenModalService`, `IZenToastService`, `IZenThemeService`).

## Blazor Web App (Server or WebAssembly interactivity)

**1. Register the services** in `Program.cs`:

```csharp
using ZenithUI;

builder.Services.AddZenithUI();
```

With **WebAssembly** interactivity there are two service containers, so both `Program.cs` files
need the call — the server project's and the client project's. A component rendered on the client
resolves from the client's container, and a missing registration there fails at render time rather
than at build time.

**2. Wire up `App.razor`:**

```razor
<head>
    @* Synchronous, and before the stylesheet. See below. *@
    <ZenThemeScript />
    <link rel="stylesheet" href="_content/ZenithUI/zenith.css" />
</head>

<body class="zen-root">
```

`zen-root` is what puts the page on the palette: the base surface colour, the base text colour and
the font smoothing. Without it the components are themed and the page behind them is not.

**3. Drop in a theme switcher** wherever it belongs:

```razor
<ZenThemeToggle />                                      @* light <-> dark *@
<ZenThemeToggle Mode="ZenThemeToggleMode.Segmented" />  @* System / Light / Dark *@
```

## Standalone WebAssembly

The host page is a static `wwwroot/index.html`, which no Razor component can write into — so
`<ZenThemeScript />` has nowhere to go. The package ships the same logic as a file for exactly this
case:

```html
<head>
    <script src="_content/ZenithUI/js/zen-theme-init.js"></script>
    <link rel="stylesheet" href="_content/ZenithUI/zenith.css" />
</head>

<body class="zen-root">
```

Leave that script **classic and blocking**. `defer`, `async` and `type="module"` all move it past
the first paint, which is the paint it exists to get right.

`Program.cs` is the same `builder.Services.AddZenithUI();` as everywhere else.

## Static SSR, no interactivity at all

Nothing extra. The library declares `@rendermode` nowhere, so every component renders as plain
server-side HTML, correctly themed, and the ones that would otherwise need JavaScript degrade
honestly rather than breaking.

What works with no render mode anywhere on the page:

- **The palette**, including a stored light/dark preference — `ZenThemeScript` is an inline script,
  independent of Blazor entirely.
- **The side-nav drawer.** It is a `popover` with a `popovertarget` button, so the browser supplies
  the top layer, the backdrop, Escape and click-outside dismissal. This is why `ZenAppShell` can be
  a layout: a layout can never be interactive.
- **Collapsible nav sections.** `ZenNavGroup` is a `<details>`.
- **Every form control as a real, labelled input.** A combobox prerenders as a closed, labelled
  field rather than as a broken listbox. Two-way binding is an event, so it needs a render mode;
  for a plain form post, splat a `name` onto the control and read it with
  `[SupplyParameterFromForm]`.

What needs a render mode, because the behaviour is an event handler: sorting and paging a table,
expanding a tree, opening a modal or raising a toast from a service, live search debouncing, and the
theme toggle's own click. Add `@rendermode` per page or per component when you want those — nothing
in the markup changes.

## Laying out a page without Tailwind

`zenith.css` is not the whole of Tailwind. It holds the classes ZenithUI's own components use, plus
the semantic surface (`bg-surface`, `text-content-muted`, `rounded-zen-md`, …). A layout class you
write in your own markup, such as `md:grid-cols-3`, `gap-5` or `max-w-4xl`, works only if some
component happens to use that exact class. Nothing warns you when it doesn't: the class does
nothing, and a later release that stops using it can break your layout without any change on your
side.

The layout components cover that gap. Every value they accept is a class the library's own build
emits, and a test checks that each one is present in `zenith.css`:

```razor
<ZenContainer MaxWidth="ZenContentWidth.Wide">
    <ZenStack Gap="ZenSpace.Xl">
        @* A toolbar: title at the start, actions pushed to the end. *@
        <ZenStack Direction="ZenDirection.Horizontal" Align="ZenCrossAlign.Center">
            <ZenText Variant="ZenTextVariant.H1">Dashboard</ZenText>
            <ZenSpacer />
            <ZenButton>New</ZenButton>
        </ZenStack>

        @* One column on a phone, two from sm, four from lg. *@
        <ZenGrid Columns="1" ColumnsSm="2" ColumnsLg="4">
            <ZenStatCard Label="Revenue" Value="$48,210" />
            <ZenStatCard Label="Orders" Value="1,284" />
            <ZenStatCard Label="Refunds" Value="23" />
            <ZenStatCard Label="Customers" Value="912" />
            <ZenGridItem SpanSm="2" SpanLg="3">
                <ZenCard Title="Revenue by week">…</ZenCard>
            </ZenGridItem>
            <ZenCard Title="Top customer">…</ZenCard>
        </ZenGrid>

        @* As many columns as fit, none narrower than 16rem. *@
        <ZenGrid MinItemWidth="16rem">…</ZenGrid>
    </ZenStack>
</ZenContainer>
```

| Component | What it is |
| --- | --- |
| `ZenStack` | A flex column or row. `Gap`, `Align`, `Justify`, `Wrap`, and `HorizontalFrom` to stack on a phone and switch to a row from a breakpoint. |
| `ZenGrid` | A grid. Either `Columns` plus `ColumnsSm` … `ColumnsXl` (1–6 or 12), or `MinItemWidth` for as many columns as fit. |
| `ZenGridItem` | A grid cell that spans columns: `Span` plus `SpanSm` … `SpanXl`, or `FullWidth`. A plain child of the grid needs no wrapper. |
| `ZenContainer` | A centred column with gutters, at the same widths as `ZenAppShell`'s `MaxWidth`. For pages without the shell. |
| `ZenSpacer` | Fills the spare room in a stack, pushing what follows it to the far end. |

Every one of them takes `As`, which sets the element (`Section`, `Nav`, `Ul`, `Li`, …) because a
layout box has no role of its own. A grid rendered as a `Ul` needs its items to be `Li`, either with
`ZenGridItem As="ZenLayoutElement.Li"` or with plain `<li>` elements.

An unsupported value, such as `Columns="7"` or `MinItemWidth` combined with `Columns`, throws when the
component renders. That is deliberate: it replaces a class that would otherwise do nothing, with no
warning.

For anything these don't cover, run your own Tailwind (next section). Padding, margins and
arbitrary sizes are deliberately left out: a component for each utility would amount to a second,
weaker Tailwind.

## If your app runs its own Tailwind

Only needed if you write Tailwind classes in your **own** markup — see
[`theming.md`](theming.md#if-your-app-runs-its-own-tailwind) for the full explanation. The short
version:

```css
/* Styles/app.css - your Tailwind entry point */
@import "tailwindcss";

@source "../Components/**/*.razor";

/* Dropped here on build by the package's targets file. */
@import "../obj/zenithui/zenith.theme.css";
```

and link `zenith.nopreflight.css` instead of `zenith.css`, so the reset is not emitted twice:

```razor
<link rel="stylesheet" href="_content/ZenithUI/zenith.nopreflight.css" />
<link rel="stylesheet" href="app.css" />
```

## Things that bite

**The project templates ship Bootstrap, and Bootstrap wins.** Both `blazor` and `blazorwasm` link
`bootstrap.min.css`, which defines `.bg-primary` among others. Its rules are **unlayered**, and an
unlayered rule beats a layered one whatever the order or specificity — so ZenithUI's utilities,
which live in `@layer utilities` by design, lose every collision. A `ZenButton` comes out Bootstrap
blue. Remove the template's Bootstrap link; keep `app.css` if you want its layout.

**A `NavMenu` that is not ZenithUI's.** The templates' sidebar comes with its own colours in
`NavMenu.razor.css`. Scoped CSS is unlayered too, so the same rule applies: it will not follow your
theme. Replace it with `ZenNavMenu` or accept that one region ignores the palette.

**Nothing is styled at all, and the page is in Times New Roman.** A server process still running
from a previous build is serving stale asset fingerprints: `MapStaticAssets` bakes content-hashed
URLs into a manifest at build time, and once the files behind them change it serves **empty
responses**. Restart the app after rebuilding.

**A component looks broken and the theme looks fine.** Almost always a stale pre-compressed copy of
the stylesheet — an empty gzip behind a healthy `200`. `curl` sends no `Accept-Encoding` and gets
the real file, so the obvious check passes; the browser sends one and parses zero rules. Confirm
with `curl -s -H "Accept-Encoding: gzip" <url> --compressed | wc -c`, treat `0` as proof, delete
the `obj/**/compressed` directories and rebuild.

**A dark-mode flash on every navigation.** `ZenThemeScript` is missing, deferred, or after the
stylesheet. `localStorage` is not sent with an HTTP request, so the server cannot know the viewer's
preference: the first HTML is always the light palette, and only a synchronous inline script can
correct it before the first frame.
