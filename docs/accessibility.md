# Accessibility

What ZenithUI guarantees, how that is checked, what the checks cannot see, and what the M6 audit
found.

- [The baseline](#the-baseline)
- [How it is verified](#how-it-is-verified)
- [The M6 audit](#the-m6-audit)
- [What the checks cannot see](#what-the-checks-cannot-see)
- [What a consuming application still owns](#what-a-consuming-application-still-owns)

## The baseline

Every component ships with:

- **The right element, or the right role.** A nav menu is a list, a table is a table, a dialog is
  `<dialog>`, a drawer is a `popover`. A role is added only where no element carries the meaning.
- **Keyboard operation per the WAI-ARIA Authoring Practices** for the pattern it implements —
  roving tabindex in the grid, tree and table; `aria-activedescendant` in the combobox; Escape and
  focus restore on every overlay.
- **A visible focus indicator**, always. `outline: none` never appears without a replacement. Text
  entry controls recolour their own border (`zen-focus-border`); buttons, links and toggles take an
  outline ring (`zen-focus`). The two indicators exist because a ring outside a densely packed input
  collides with its neighbours and a recoloured border on a borderless button has nothing to
  recolour.
- **`aria-invalid` and `aria-describedby`** wired from the field to its error and hint text, with
  the error state surviving focus — an invalid field keeps its danger border while focused rather
  than having it replaced by a focus style.
- **≥4.5:1 contrast in both palettes**, audited numerically rather than by eye.
- **No render-mode assumption.** Components degrade to correct, operable HTML with no JavaScript
  attached; see [getting-started.md](getting-started.md#static-ssr-no-interactivity-at-all) for what
  that does and does not include.

## How it is verified

Three independent layers, because each catches what the others cannot.

**1. The contrast audit** (`ThemeTokenTests`, in CI). Parses `tokens/base.css` and
`tokens/dark.css`, converts every OKLCH value to linear sRGB, and asserts the WCAG 2.2 ratio for
each documented pairing plus sRGB gamut containment — a colour outside the gamut is not the colour
that was declared. Every violation is reported per run, with the maximum in-gamut chroma for each
offender, so a palette fix is one edit rather than a guess-and-rerun loop.

**2. Per-component ARIA tests** (bUnit, in CI). Each component asserts the attributes its pattern
requires, under both an interactive renderer and a static one. These are about *structure*: which
element carries which state, which id references which element, how many tab stops a widget has.

**3. An axe-core sweep of the running demo** (manual, per milestone). Every page in both palettes,
plus the states that only exist after interaction: an open modal, a raised toast, an open combobox
listbox, an open popover, the drawer at 390 px, a sorted table with an expanded detail row, an
expanded tree, a submitted invalid form, and an open date-picker calendar.

Layer 3 is the one that finds things. Layers 1 and 2 verify decisions already made; a browser
audit sees the result, including markup no test thought to look at.

## The M6 audit

Ten pages × two palettes, then nine interactive states. Findings, in the order they mattered:

### `aria-selected` on a role that does not support it — *fixed*

`ZenCalendar` put `aria-selected` on the day `<button>`. The attribute is defined for a short list
of roles — `gridcell` among them, `button` not — so on the button it was invalid ARIA that an
assistive technology is free to ignore, and the selected day would then read exactly like every
other day. It now sits on the `<td role="gridcell">` that contains the button, where it is defined.

The alternative reading of the APG pattern — make the `<td>` itself the focusable widget and drop
the inner button — was not taken: a `<td>` cannot be `disabled`, so every out-of-range day would
need `aria-disabled` plus hand-rolled click and keyboard suppression, trading a supported state for
re-implemented semantics.

### A wrapper between the combobox listbox and its options — *fixed*

`ZenCombobox` rendered `role="listbox"` on the popover panel and then a scrolling `<div>` inside it
holding the options. Three things were wrong at once, and all three were invisible to the test
suite because each id resolved to *something*:

- `aria-controls` on the input named the wrapper, an element with no role at all.
- The options were not owned by the listbox that claimed them.
- `scrollItemIntoView` addressed a node the accessibility tree does not contain.

The panel now *is* the listbox: one element carrying the role, the id, the scrolling and the
options. axe's `scrollable-region-focusable` finding — raised against the wrapper — went away with
it, which is the sign it was structural rather than a checker quirk.

`ZenPopover` grew a `PanelId` parameter to make this expressible. A component cannot read its own
child's generated id in the render pass that creates it, so the caller has to be able to supply one.

### Heading order — *fixed, in the demo*

Three demo pages jumped a heading level: a card at `h3` directly under the page `h1`, and a table
detail panel at `h4` under an `h2`. `ZenCard` already takes `TitleLevel`; the demo simply wasn't
using it. Nothing to fix in the library, and worth recording anyway — a component library that
hardcodes a heading level makes this unfixable in a consuming app, which is why the parameter
exists.

### Contrast, in the demo's own markup — *fixed, in the demo*

The token gallery faded the `-content` label on each intent fill with `opacity-90`. Three of the
seven pairs dropped below 4.5:1 — on the page whose subject is that the pairs are calculated. The
library's audit could not see it, because the tokens themselves are correct and it was the demo that
weakened them. Dimming a token is a rebrand, and an untested one.

### Coverage gap — *fixed*

`ZenCalendar` and `ZenDatePicker` shipped in M2 with no tests at all, which is how the invalid
`aria-selected` survived four milestones. They now have seventeen, covering the grid structure, the
single tab stop, the arrow-key arithmetic, month paging with its live region, bounds and per-day
predicates, and the picker's static-SSR prerender.

### Result

Zero violations across all twenty page/palette combinations and all nine interactive states, at
`wcag2a`, `wcag2aa`, `wcag21a`, `wcag21aa`, `wcag22aa` and `best-practice`.

## What the checks cannot see

Stated plainly, because "passes axe" is routinely read as more than it is. Automated rules catch
something like a third of what matters, and none of the three layers above is a user.

- **No screen reader has been run against this library.** The ARIA is correct by specification and
  by audit; how NVDA, JAWS and VoiceOver actually narrate each pattern is untested.
- **Contrast is audited on the tokens, not on the pixels.** A consumer who overrides a token, or
  layers text on an image, is outside what CI can check. So is a demo that fades a token — which is
  exactly how one of the findings above arose.
- **Reflow and zoom** are checked at 360 / 768 / 1440 px by hand. There is no automated 400% zoom or
  400 px reflow test.
- **Motion.** `prefers-reduced-motion` disables the theme transition and the popover fade; nothing
  asserts that automatically.
- **Timing.** Toast dismissal timers pause on hover and on focus, which is the right behaviour for
  WCAG 2.2.1, but "long enough" is a judgement no rule makes.
- **Cognitive load, language, error recovery** — all outside every tool listed here.

## What a consuming application still owns

A component library can supply correct components and nothing more. The page is yours:

- **Heading order.** Components take a heading level (`ZenCard.TitleLevel`); which one fits the
  outline is a page-level decision.
- **Landmarks and labels.** `ZenAppShell` contributes the skip link and the `<main>` landmark; if
  you place several navigation landmarks, name them.
- **Accessible names for your own content.** Icon-only buttons take `AriaLabel`, and an unnamed one
  is a button a screen reader announces as "button".
- **Page titles, language, focus on navigation.**
- **Your own contrast**, if you rebrand. The four intent roles have obligations to each other — see
  [theming.md](theming.md#intent-tokens-come-in-fours).
- **Third-party CSS.** Anything unlayered on your page beats ZenithUI's layered rules, including
  the Bootstrap the project templates ship with. That is a visual problem first and an accessibility
  one second, when it lands on a focus indicator.

## Re-running the sweep

The sweep is a script, not a paragraph of instructions, so a later milestone runs exactly what M6
ran:

```bash
cd samples/ZenithUI.Demo && dotnet run          # one terminal

cd tools/accessibility && npm install
npm run sweep                                   # another
```

It exits non-zero on any violation, and treats a state it failed to reach as a failure — a
"passing" audit of a modal that never opened is worse than no audit. Add a state by appending to
`tools/accessibility/states.json`: a path, and a snippet that drives the page into the state.

It is not a CI step because it needs the demo running and a real rendering engine — which is also
precisely why it finds what the unit tests cannot.
