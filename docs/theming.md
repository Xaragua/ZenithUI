# Theming ZenithUI

Everything visual in ZenithUI resolves through a `--zen-*` custom property. No component writes a
literal colour, so rebranding the library is a stylesheet override rather than a fork, and
switching palettes repaints a statically rendered page without re-rendering a single component.

This document covers the token surface, the three-state light/dark model, and — the part that needs
the most care — what an application running **its own** Tailwind build has to do.

- [The token surface](#the-token-surface)
- [Intent tokens come in fours](#intent-tokens-come-in-fours)
- [Light, dark, and system](#light-dark-and-system)
- [Rebranding](#rebranding)
- [If your app runs its own Tailwind](#if-your-app-runs-its-own-tailwind)
- [Troubleshooting](#troubleshooting)

---

## The token surface

Tokens are declared on `:root` in the light palette and redeclared for dark. Tailwind's theme maps
each one to a utility, so `bg-surface` compiles to `background-color: var(--zen-surface)`.

| Group | Tokens | Utilities |
| --- | --- | --- |
| Surfaces | `--zen-surface`, `-raised`, `-sunken`, `-overlay`, `-hover` | `bg-surface`, `bg-surface-raised`, … |
| Content | `--zen-content`, `-muted`, `-subtle`, `-inverted` | `text-content`, `text-content-muted`, … |
| Borders | `--zen-border`, `--zen-border-strong` | `border-border`, `divide-border-strong`, … |
| Intents | `--zen-{primary,secondary,accent,success,warning,danger,info}` ×4 | see below |
| Focus | `--zen-ring`, `--zen-ring-width` | `ring-ring`, and the `zen-focus*` utilities |
| Shape | `--zen-radius-{sm,md,lg,xl}`, `--zen-shadow-{sm,md,lg}` | `rounded-zen-lg`, `shadow-zen-md` |
| Layout | `--zen-appbar-height`, `--zen-sidenav-width`, `--zen-sidenav-width-collapsed` (the rail with `ZenSideNav Collapsible`) | `h-appbar`, `w-sidenav` |

Two layout properties are written by components rather than read from the palette:
`--zen-sidenav-top` (set by `ZenAppShell`, because CSS gives an element no way to measure a sticky
sibling) and `--zen-slider-fill` (set by `ZenRangeSlider` as the value changes).

## Intent tokens come in fours

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
brown.

So: `bg-warning` + `text-warning-content` for a solid chip; `text-warning-strong` for coloured text
on a page. Never `text-warning` on a surface — the contrast audit in CI will not catch it in *your*
markup, only in ZenithUI's.

## Light, dark, and system

A viewer's theme has three states and only two of them stamp an attribute on `<html>`:

| Stored preference | OS preference | Result |
| --- | --- | --- |
| none (`System`) | light | light |
| none (`System`) | dark | dark, live — follows the OS even in a background tab |
| `light` | dark | light — an explicit choice wins |
| `dark` | light | dark |

In `System` mode the `data-zen-theme` attribute is **removed** rather than set to the resolved
value. That is what keeps the page following the OS instead of freezing against it.

The dark palette is therefore declared twice — once under `@media (prefers-color-scheme: dark)`
guarded against an explicit light choice, once under `[data-zen-theme="dark"]`. A test asserts the
two blocks never drift apart.

`<ZenThemeScript />` must be **synchronous and in `<head>`, before the stylesheet.** `localStorage`
is not sent with an HTTP request, so the server cannot know the stored preference: the first HTML it
produces is always the light palette, and without an inline script a dark-mode viewer sees a white
flash on every navigation. A deferred script or a module runs after first paint and is useless for
this.

For the rare rule that no token can carry — inverting an inline SVG, say — there is a `zen-dark:`
variant matching the same three states. Reach for it only when a token genuinely cannot express the
difference.

## Rebranding

Override the tokens in your own stylesheet, loaded after ZenithUI's:

```css
:root {
  --zen-primary: oklch(0.55 0.2 155);
  --zen-primary-content: oklch(0.99 0 0);
  --zen-primary-soft: oklch(0.955 0.03 155);
  --zen-primary-strong: oklch(0.46 0.13 155);
  --zen-radius-md: 0.125rem;
}

/* The dark palette is a separate declaration, and it takes both forms - the explicit choice
   and the OS preference - exactly as tokens/dark.css does. */
:root[data-zen-theme="dark"] {
  --zen-primary: oklch(0.72 0.16 155);
  --zen-primary-content: oklch(0.16 0.01 155);
  --zen-primary-soft: oklch(0.29 0.05 155);
  --zen-primary-strong: oklch(0.82 0.13 155);
}

@media (prefers-color-scheme: dark) {
  :root:not([data-zen-theme="light"]) {
    --zen-primary: oklch(0.72 0.16 155);
    --zen-primary-content: oklch(0.16 0.01 155);
    --zen-primary-soft: oklch(0.29 0.05 155);
    --zen-primary-strong: oklch(0.82 0.13 155);
  }
}
```

Override the dark palette too, or your brand colour will be the one thing on the page that does not
change when the theme does.

Nothing needs rebuilding: no Tailwind install, no config file, no rebuild of the package. The
utilities ZenithUI shipped already point at the variables you just redefined.

If you override an intent, check the result against both palettes — the four roles above have
contrast obligations to each other, and `-soft` in particular is a background that `-strong` has to
stay legible on.

---

## If your app runs its own Tailwind

You only need this if you write Tailwind classes in your **own** markup. If you don't, link
`_content/ZenithUI/zenith.css` and skip the section entirely — the semantic utilities are all
present, guaranteed by the library's safelist.

### The problem

A utility is compiled by the build that scans the markup using it. ZenithUI's precompiled stylesheet
was built by scanning ZenithUI's components, so it carries `bg-surface` for ZenithUI's markup. It
cannot carry it for yours — Tailwind never saw your `.razor` files, and it cannot: they are compiled
into a DLL inside a `.nupkg`.

There is a second, nastier half. Both stylesheets write into the `utilities` cascade layer, and
**within a layer the later file wins**. Your build emits only what you use, so `.flex` is almost
certainly in your sheet and `.lg\:hidden` almost certainly is not — and a ZenithUI component
written `class="flex lg:hidden"` would then be `display: flex` at every width, in your app and
nowhere else.

### The fix

The package ships a single flattened file, `zenith.theme.css`, and an MSBuild target that copies it
into your project's intermediate directory on build. Import it from your own Tailwind entry point:

```css
@import "tailwindcss";

@source "../Components/**/*.razor";

@import "../obj/zenithui/zenith.theme.css";
```

That import carries three things:

1. The `@theme inline` block, so `bg-surface`, `text-content-muted`, `rounded-zen-lg` and the rest
   compile for your markup. Every one resolves a `var(--zen-*)`, so a token override still rebrands
   the library — the theme emits no colour values of its own.
2. The `zen-dark:` custom variant.
3. Every class candidate ZenithUI's own markup contains. Your sheet and ZenithUI's then define the
   same rules identically, so which of the two the browser parses last stops mattering. It costs
   roughly 40 KB of duplicated utilities before compression, and it is what closes the cascade trap
   above.

Then link the preflight-free build, so the reset is not emitted twice:

```razor
<link rel="stylesheet" href="_content/ZenithUI/zenith.nopreflight.css" />
<link rel="stylesheet" href="app.css" />
```

`samples/ZenithUI.Demo/Styles/app.css` is a working example, importing the identical file by
relative path because it builds from source rather than from the package.

### Build ordering

The copy runs from `ZenithCopyTheme`, before `PrepareForBuild`. If your Tailwind step is an MSBuild
target of your own, make the dependency explicit rather than relying on ordering:

```xml
<Target Name="BuildAppTailwind" DependsOnTargets="ZenithCopyTheme" BeforeTargets="Build">
  <Exec Command="npm run css:build" />
</Target>
```

Three properties adjust it, all optional: `ZenithThemeDirectory` (where the file lands, default
`$(BaseIntermediateOutputPath)zenithui\`), `ZenithThemeDestination` (the full path), and
`ZenithSkipThemeCopy` (`true` disables the target).

Check the copy into source control or don't — it is regenerated on every build from the package, and
lives under `obj/` precisely so you don't have to think about it.

---

## Troubleshooting

**`bg-surface` does nothing in my markup.** Your Tailwind build has no `@theme`. Import
`../obj/zenithui/zenith.theme.css`, and confirm the file exists — if it doesn't, the package's
targets file never ran, which usually means the reference is a `<ProjectReference>` rather than a
`<PackageReference>`.

**A ZenithUI component lays out wrongly, only in my app.** A utility is defined in both sheets and
yours wins with a different value, or the component's own is missing from the sheet that loads last.
Importing the theme file fixes it, because it makes both sheets emit the same rules.

**The page renders but nothing is styled.** Check the `<link>` resolves —
`_content/ZenithUI/zenith.css` is served by `MapStaticAssets`, which bakes content-hashed URLs into
a manifest at build time. A server process still running from a previous build serves stale
fingerprints, and once the files behind them change it serves **empty responses**.

**The theme looks right but library components are missing utilities.** Same cause, different
symptom: a stale pre-compressed copy of the stylesheet behind a healthy-looking `200`. `curl` sends
no `Accept-Encoding`, so it gets the real file; the browser sends one and gets an empty gzip body.
Confirm with `curl -s -H "Accept-Encoding: gzip" <url> --compressed | wc -c` and treat a decoded
length of `0` as proof. Delete the `obj/**/compressed` directories and rebuild.

**A colour is right in light and wrong in dark.** You overrode `:root` only. The dark palette is a
separate declaration; see [Rebranding](#rebranding).

**The theme resets when I navigate — the switcher seems to theme only the current page.** Enhanced
navigation merges the server's document into the live DOM, `<html>` attributes included, and the
server cannot know your stored preference — so its response has no `data-zen-theme` and the merge
removes the one the theme service set. ZenithUI corrects this from a Blazor JS initializer,
`_content/ZenithUI/ZenithUI.lib.module.js`, which re-applies the stored preference on every
`enhancedload`. It is discovered and loaded by Blazor automatically; nothing to wire up. If you do
see the reset, confirm that file is being served — a missing or blocked static asset is the only
way this comes back.
