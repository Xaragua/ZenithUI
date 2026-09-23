# Changelog

All notable changes to ZenithUI are documented here. The format follows
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/) and the project adheres to
[Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added — M4, data display

**`ZenTable<TItem>` + `ZenColumn<TItem>`** — the milestone's hard component, and the one whose
mechanism is worth reading before its features.

Columns are child components, so the table's markup depends on what its own children report — and
a parent builds its entire render tree before any child exists. The reflex fix is
`StateHasChanged` from `OnAfterRender` and a second pass, which fails precisely where it matters:
static SSR never renders twice, so a server-rendered table would ship with an empty `<tbody>`, and
nothing in a test suite that only renders interactively would notice.

`ZenDefer` fixes the ordering instead. The renderer's queue is first-in-first-out, and a new child
component's parameters are set — so `OnParametersSet` runs — while the frame that introduces it is
being diffed. A cascade queued ahead of a deferred body therefore registers every column before
that body renders. One pass, every render mode, and a test that renders under a static renderer
specifically.

- **What role the table claims is decided by what it can honour.** A flat table stays a plain
  `<table>` even when rows are selectable. `role="grid"` owes the user arrow-key navigation
  between cells and this component does not provide it — the same reason `ZenList` refuses to call
  a static list a listbox. Selection is expressed with real checkboxes, which are announced
  correctly, operable from the keyboard and submit with a form, without anything being promised.
- **A hierarchical table has no such option**, because HTML cannot say "this row is a child of
  that one". So `role="treegrid"`, `aria-level` and `aria-expanded` on rows, and the keyboard
  contract that comes with the claim: a roving tabindex, Right to expand then descend, Left to
  collapse then climb, Home/End, and focus moved after the render because ArrowRight descends into
  a row that did not exist when the key arrived.
- **Sorting is tri-state**: ascending, descending, then back to the order the data arrived in.
  That order carries information — very often "newest first" from the server — and a two-state
  toggle leaves the user no way back to it short of reloading the page. `aria-sort="none"` is
  stated on every sortable column and omitted on the rest, so the attribute is the affordance
  rather than a decoration on the one column already sorted.
- **Paging counts top-level rows**, so a subtree travels with its parent. Paging after flattening
  would let one expanded row push its siblings onto the next page, and opening a row would look
  like it had deleted the ones below it.
- **Select-all governs the page, not the dataset.** The table only knows the items it was handed,
  and a box that silently selects rows the user has never seen is how a bulk action takes out more
  than it was meant to.
- **Below `md` each row becomes a stacked card**, labelled from `data-label` and driven entirely by
  CSS. The alternative on a phone is a horizontal scrollbar that hides whichever column the user
  came for. The label is an attribute rather than a duplicated element, so the value exists once in
  the DOM: the real header association survives, and a screen reader does not hear it twice.
  `content: attr()` text is not exposed to the accessibility tree, which is usually a drawback and
  is exactly what is wanted here.

**`ZenTree<TItem>`** — the WAI-ARIA tree pattern, with real nesting.

- **Roving tabindex, not `aria-activedescendant` — the opposite of `ZenList`, deliberately.** A
  listbox keeps focus on its container because the thing typing into it may be a combobox's text
  field. A tree has no such partner: its items are what the user navigates, and screen-reader tree
  mode is built around the focused item being a genuinely focused element. Both remain one tab
  stop.
- **The structure states the depth, so the attributes do not.** A `treeitem` holds its children in
  a nested `<ul role="group">`, which supplies level, set size and position in set. Writing
  `aria-level` by hand as well would be a second source of truth, and an attribute that merely
  disagrees with the DOM is invisible to every test that could have caught it. `ZenTable` states
  all three because it has no such structure — the two components differ because their markup
  does, not because they disagree.
- **The flat navigation order is built by the same pass that renders**, so the keyboard cannot move
  to a node that is not on screen — the failure mode of a cached flat list that every expand and
  collapse has to remember to invalidate.
- **A lazily loaded node is drawn as expandable before its children are known.** Drawing no chevron
  until they arrive hides a subtree behind an interaction nobody can discover. The guess corrects
  itself: a node whose loader returns nothing becomes a leaf.
- Type-ahead expires by elapsed time rather than on a timer, so there is nothing to dispose and no
  callback that can fire after the component is gone.

**`ZenTimeline` + `ZenTimelineItem`** — an `<ol>`, because a timeline is ordered by definition.
That is what makes a screen reader announce "3 of 7", which is the position a sighted reader takes
from the connecting line; an unordered list throws it away.

- The connector lives in the marker's flex column with `flex-1`, so it stretches to whatever height
  the entry turns out to be rather than guessing at a fixed length.
- The gap between entries is padding on the **body**, not on the `<li>`. A flex child cannot grow
  into its parent's padding, so a gap on the item would stop every connector short of the next
  marker and leave a dotted-looking ladder.
- Both the trailing line and the trailing gap are removed by `:last-child` rules. An item cannot
  know it is last and the container cannot count a `RenderFragment`, so CSS is the only party that
  holds the fact.
- A timestamp renders as `<time datetime>` carrying the round-trip instant while the text carries
  the culture's formatting. Relative phrasing with no instant behind it renders as a plain span
  instead: a `<time>` without a valid `datetime` advertises a parseable value it does not have.

**Core** — `ZenStyles.Fill` (an intent's solid fill with no text colour, for a shape that carries
no text of its own), `ZenAlign`, and `ZenDefer`. `ZenDefer` is public only because the Razor
compiler resolves markup elements to public component types; nothing in a page has a reason to
write it.

**Verification** — 500 tests, up from 466.

### Fixed during M4

- **The sort affordance was invisible until hover.** The indicator was `opacity-0` with a
  `group-hover` reveal, which meant nothing on the page said the table could be sorted at all: a
  pointer user had to guess, and a touch user, who has no hover, could not have found out. Caught
  by looking at the rendered page, not by any test — every assertion about `aria-sort` passed
  throughout. The indicator now rests at low opacity, with hover and the active sort as the two
  states above it.
- **`ZenTree` passed `Silent="true"` to a `ZenSpinner` that has no such parameter.** It compiled
  and rendered, because `ZenComponentBase` captures unmatched values and splats them — so the
  intent to suppress a duplicate announcement became a stray `silent="true"` attribute on a
  `<span>`, and the spinner went on announcing "Loading" over a treeitem already carrying
  `aria-busy`. The suppression is `Label=""`, which is what the component documents.

### Changed

- The plan's "exactly three JS modules" is four. `zen-dom.js` had already landed in M2 and is now
  used by `ZenTree` and the hierarchical `ZenTable` as well. It was not foreseen because what it
  fills is not behaviour: `indeterminate` is a DOM property with no attribute and so cannot be
  expressed in server-rendered markup, and `focus()` has no declarative equivalent. A flat
  `ZenTable` loads no JavaScript at all.

### Added — M3, overlays and selection

**`ZenPopover`** — an anchored floating panel, and the positioning `ZenCombobox` and the rest of
M3 build on.

`position: absolute` inside a relative wrapper — what `ZenDatePicker` still does — has two failure
modes no stylesheet can reach: any ancestor with `overflow: hidden` clips the panel, and it cannot
move when it runs out of room at the viewport edge. The `popover` attribute puts the panel in the
top layer, where neither applies. CSS Anchor Positioning would remove the JavaScript too, but is
not in Firefox yet, so `zen-popover.js` computes the coordinates.

- `popover="manual"`, not `"auto"`. Auto brings its own light-dismiss and Escape handling, which
  takes both decisions away from the owning component — a combobox needs Escape to clear its active
  option before it closes, and light-dismiss fires after the click has already landed on whatever
  was underneath. Dismissal is on `pointerdown` instead, which also handles the drag-out case a
  click listener gets wrong.
- The attribute is emitted only once the renderer is interactive. A `[popover]` element is
  `display: none` until `showPopover()` runs, so emitting it during prerender would render an open
  panel nobody can see.
- Placements stay logical as far as the browser. Resolving `start`/`end` in C# would need the
  writing direction, which can come from a `dir` attribute anywhere up the tree or from a
  stylesheet — only the browser knows which applies.

**`ZenModal` + `IZenModalService`** — a dialog reads as a function call:
`var result = await Modals.ShowAsync<Editor>()`, or `await Modals.ConfirmAsync(title, message)`.

- Native `<dialog>` with `showModal()`, for the top layer, `inert` on everything behind, and
  `::backdrop`. The `inert` part is the one a keydown focus trap cannot reproduce: it removes the
  background from the accessibility tree as well as the tab order, so a screen reader user cannot
  browse the page behind a "trapped" dialog.
- Escape is taken back from the platform and routed through C#, which is what lets
  `CloseOnEscape="false"` mean something.
- Focus restore is explicit rather than left to `<dialog>`, whose own restore only runs while the
  element is still in the document — and a service-raised dialog is removed in the same render that
  closes it.
- Dialogs stack, keyed by instance. A confirmation raised from inside an editor appears above it.

**`ZenToast` + `IZenToastService`** — `Toasts.Success("Order saved.")`, with no markup at the call
site.

- Errors are announced assertively (`role="alert"`) and everything else politely
  (`role="status"`). An assertive region interrupts whatever a screen reader is reading, which is
  right for a failure and rude for "Saved".
- Errors do not auto-dismiss. One that vanishes on a timer is one the user may never have seen,
  with no way to bring it back.
- The dismiss timer pauses on **focus** as well as hover. Hover alone is the usual implementation
  and it fails the two users who most need the pause — someone reading with a screen reader, and
  someone tabbing to the action button.
- The stack is capped at five, dropping the oldest. A loop that fails once per item raises a toast
  per item, and a column tall enough to cover the page hides the UI needed to fix the problem.

**`ZenToast` soft tints** — `new ZenToastOptions { Soft = true }` paints the toast with its
intent's `-soft` background instead of the neutral overlay. Off by default: a stack of tinted
toasts is louder than a stack of neutral ones, and the icon already carries the intent. The
contrast audit gained a case for it — body text on a `-soft` tint was not a pairing it covered
until this made it reachable. A tinted toast carries no visible border: a neutral line on a
tinted panel sits at the same lightness as the tint in dark, so it reads as a grey smear rather
than an edge. The tint defines the panel and the shadow lifts it.

**`ZenCombobox<TItem>`** — the three things a native `<select>` cannot do: filter, load
asynchronously, and show more than a line per option. Everything native *can* do is still left to
`ZenSelect`.

- Focus never leaves the text input; the active option is named with `aria-activedescendant`. That
  is what lets the user keep typing while arrowing through results.
- The first option is highlighted but **not** selected, and Tab commits nothing. Those are the two
  behaviours autocompletes are most often complained about for: committing a value the user never
  chose, and changing a field's value on the way out of it.
- `ItemsProvider` receives a `CancellationToken` and every keystroke supersedes the request in
  flight, so a slow response for `ab` cannot land after a fast one for `abcd`.
- Prerenders as a closed, labelled text field carrying its value — the M3 exit criterion.

**`ZenList<TItem>`** — a list that stays on the page. Plain `<ul>` when it is just a list;
`role="listbox"` with arrow-key navigation when it is selectable. Giving a non-interactive list
listbox semantics promises a widget the user cannot operate.

**JavaScript** — `zen-popover.js` and `zen-focus.js` land, completing the three modules the plan
allows for. Nothing in either implements behaviour: they measure the viewport, move focus, and
report a pointer going down outside a subtree. Every decision stays in C#.

### Fixed during M3

- **Every popover opened in the top-left corner, whatever its placement.** The anchor wrapper is
  `display: contents`, so that it is a positioning handle and nothing else — the caller's own
  element keeps whatever layout it had. But a `display: contents` element generates no box, so
  `getBoundingClientRect()` on it returns a zero rect at the origin, and every coordinate was
  computed from (0, 0). The measurement now treats a zero-sized rect as "this element has no box"
  rather than "this element is empty" and descends to the first child that does have one. The
  `ResizeObserver` had the same blind spot — it never fires for an element with no box.
- **The modal backdrop lightened the dark page instead of dimming it.** The scrim was written as
  `oklch(from var(--zen-content) l c h / 0.45)`, reasoning that a backdrop should follow the
  palette. `--zen-content` is near-white in dark, so the dim became a white veil at 45%. A
  backdrop dims by definition: it is dark in *both* palettes, and dark needs the heavier value,
  not the lighter one, because the page underneath is already dark. Now a stated `--zen-backdrop`
  token per palette, with a test asserting its lightness sits below every surface it covers.
- **`ZenModal`'s Escape suppression was a literal HTML attribute.** The dialog carried
  `@oncancel:preventDefault="true"` so the browser could not close it behind the component's back.
  `oncancel` is a recognised Blazor event but is registered *without* preventDefault support, so
  that is not a directive the Razor compiler understands — it passed through as literal text, did
  nothing, and left `CloseOnEscape` silently a lie. It compiled, it rendered, and every test
  passed; only the served HTML showed it. The suppression moved to a `cancel` listener in
  `zen-focus.js`, and a test now asserts no `@on`/`@bind` text survives into a component's markup.
- **`ZenList`'s keyboard handling was silently absent.** A Razor comment sat between attributes
  inside the `<ul>` tag. A comment there swallows every attribute that follows it — `@onkeydown`
  included — and the component renders looking entirely correct. Caught by the arrow-key tests;
  the comment now sits above the element and says why.
- **A combobox with a selection could not be changed without clearing it first.** The field shows
  the selected item's own text, so opening the list filtered by that text and matched only the
  item already chosen. The list now filters from the first keystroke rather than from the first
  open.
- **`ZenList` drew a keyboard cursor on a list nobody had touched.** The cursor starts on the first
  row, so an untouched list sat there with a focus ring on it, which reads as a rendering bug. It
  is now gated on the listbox actually holding focus.
- **Combobox options had no hover treatment at all.** Pointing at a row gave no sign it was about
  to be clicked — `ZenList` had a hover style and the combobox simply did not. Hovering now also
  moves the keyboard cursor onto that row, so the pointer and the cursor cannot highlight two
  different options while `Enter` commits a third.
- **Row hover moved the wrong way in the dark palette.** It used `surface-sunken`, which is the
  only neutral surface with usable delta — and at L 0.15 against an overlay panel at L 0.25 it made
  a hovered row look *recessed* rather than live. There is no neutral surface lighter than the
  overlay in dark, so this was a gap in the palette rather than a component bug: a new
  `--zen-surface-hover` token now moves **away** from the page in each palette, darker in light and
  lighter in dark. `ZenList` picks up the same fix.
- **The combobox chevron did nothing when clicked.** It was a decorative icon with
  `pointer-events-none`, copied from `ZenSelect` — where that is right, because a native
  `<select>` fills the wrapper underneath and catches the click. In the combobox the chevron sits
  beside the input in a flex row, so the click landed on the wrapper and nothing happened: the
  affordance every pointer user reaches for first was inert. It is now a real button, kept out of
  the tab order since the input already opens the list on focus and `ArrowDown`.
- **`ZenButton` never rendered its `id`.** `ZenComponentBase` gives every component an `Id`
  parameter with a generated fallback, but a component that does not emit it makes that parameter a
  silent no-op — on the one primitive most often referenced by id, for `aria-controls` from a
  popover trigger or a dialog naming which button to focus first. Found writing the confirm-dialog
  focus test, which could not work until it was fixed.

### Changed — the primary intent is emerald

The default palette's primary quartet moved from blue (hue 264) to emerald (hue 163) in both light
and dark. `--zen-ring` follows `--zen-primary`, so focus rings move with it; nothing else was
touched.

The chroma could not come across unchanged. sRGB holds far less green than blue at these
lightnesses: the audit put the ceiling at 0.117 for `--zen-primary` (L 0.54) and 0.105 for
`--zen-primary-strong` (L 0.48), against the 0.19 the blue carried. They are set to 0.112 and 0.1 —
just inside, with room for a hue nudge. Lightness is unchanged, which is what keeps the contrast
ratios intact: in OKLCH the `L` component predicts contrast, and hue barely moves it.

Primary now sits 8° from `--zen-success` (hue 155). They are distinguishable side by side but no
longer carry different meaning on their own, so a green button is not self-evidently a "confirm".

### Added — `ZenCalendar` and `ZenDatePicker`

A themed calendar, replacing the browser's own date dropdown.

`<input type="date">` is a good control in every respect but one: its calendar is painted by the
browser and cannot be styled at all, so on a dark page it is a white rectangle no stylesheet can
reach. `ZenDateInput` remains for cases where the native experience wins — mobile especially, where
the OS picker beats any web calendar — but `ZenDatePicker` is now the default in the demo.

- `ZenCalendar` is the month grid on its own, usable inline. A real `<table role="grid">` with one
  tab stop and a roving cursor, so Tab leaves the calendar instead of walking all 42 cells. Arrow
  keys move by day, PageUp/PageDown by month, with Shift by year, Home/End to the week's ends.
- The month heading is an `aria-live` region. Without it a screen reader user paging by month hears
  the newly focused day but never learns the month moved.
- Six rows are always rendered, so the panel does not change height between months and shift under
  the pointer.
- Out-of-range and predicate-disabled days stay visible but unselectable. Hiding them makes the grid
  harder to scan, and skipping them breaks arrow-key travel.
- `ZenDatePicker` returns focus to its trigger on close, and the text field still accepts typing, so
  the calendar is an enhancement rather than the only way in.
- The panel sizes to its grid (`w-max`) rather than to a fixed width. It was `w-[17.5rem]` — 280px,
  exactly seven 40px cells — but that measures the border box, so the 24px of padding came out of
  the same budget and Saturday was clipped. Any hardcoded width has to be re-derived whenever the
  cell size, the padding or the font changes; asking the grid how wide it is cannot drift.

### Fixed — native controls ignored the dark palette

- **`color-scheme` was never declared.** That property is what tells the browser to paint its *own*
  chrome to match: scrollbars, number-input spinners, the select popup, the native date picker.
  None of it is reachable from CSS, so its absence showed up as light widgets on a dark page with
  nothing in the stylesheet to blame. Declared in every palette block — light too, or an explicit
  light choice on a dark OS inherits dark chrome.
- **Checkbox and radio are now drawn by the library.** They previously relied on the UA's rendering
  plus `accent-color`, which left them white in dark. `color-scheme` alone would have made them the
  operating system's grey, which is not this library's surface colour — and the two sitting side by
  side in a form read as a mistake. Both are now `appearance: none` with the box, checkmark, dot and
  indeterminate bar drawn from `--zen-*` tokens.
- **The tick and the dot are overlaid siblings, not pseudo-elements.** The first version of the
  rule above hung the mark on `.zen-check::before`, which produced a correctly coloured box with
  nothing drawn inside it: an `<input>` is a replaced element, and replaced elements are not
  required to render `::before` or `::after`. The usual fallback — a `background-image` data URI —
  cannot read a custom property, so the mark colour would have to be baked in, and
  `--zen-primary-content` is near-white in one palette and near-black in the other. A sibling
  element inherits `currentColor`, needs no asset, and is revealed through the `~` combinator.
- **The circular indeterminate progress ring did not animate correctly.** `animate-spin` sat on the
  `<circle>`, where a CSS transform overrides its `rotate(-90 18 18)` presentation attribute, and an
  SVG child rotates about the SVG origin rather than its own centre — so the arc jumped position and
  orbited the top-left corner. The spin moved to the `<svg>`, which is a replaced element in normal
  layout and transforms like anything else.

### Added — M2, form controls and feedback primitives

**Text entry** — `ZenTextInput`, `ZenTextArea`, `ZenNumberInput<T>`, `ZenCurrencyInput`,
`ZenSearchInput`, `ZenDateInput<T>`.

**Choice** — `ZenCheckbox`, `ZenRadioGroup<T>` + `ZenRadio<T>`, `ZenSelect<T>`, `ZenToggle`.

**Other** — `ZenRangeSlider`, `ZenProgress`, `ZenIndicator`, `ZenForm`.

`ZenInputBase<T>` gained `Immediate` and `DebounceMilliseconds` (each keystroke cancels the
previous pending commit), a `FieldClass` escape hatch for the wrapper, and disposal of any pending
debounce.

**Decisions worth knowing**

- **Native elements wherever one exists.** Checkbox, radio, select, date and range all wrap the
  platform control rather than rebuilding it. Each brings keyboard handling, form participation and
  correct announcement for free, and the re-implementations that replace them usually drop at least
  one of the three. `ZenCombobox` (M3) exists for what native genuinely cannot do.
- **`ZenCurrencyInput` does not mask.** Rewriting the value on every keystroke to insert separators
  means restoring the caret afterwards, and every implementation gets that wrong somewhere — typing
  mid-number, replacing a selection, undo. Instead the field shows plain digits while focused and
  formats on blur, so nothing moves under the caret. Parsing accepts symbols, separators and
  accounting-style parentheses for negatives.
- **`ZenTextArea` auto-grows with CSS `field-sizing`,** not by writing `scrollHeight` back on each
  keystroke. The JS approach does nothing during prerender, so a textarea with existing content
  renders at one row until the circuit connects.
- **`ZenNumberInput` blurs on wheel.** A focused `type="number"` steps its value while the user
  scrolls the page past it, silently corrupting a field they were not looking at.
- **`ZenToggle` is `<button role="switch">`,** not a restyled checkbox. A switch takes effect
  immediately; a checkbox is a value to be submitted later, and screen readers announce them
  differently.
- **`ZenProgress` omits `aria-valuenow` when indeterminate.** That omission is precisely how ARIA
  says "in progress, amount unknown"; setting zero announces "0 percent", claiming a fact that is
  not known and reading as a stalled operation.
- **`ZenDateInput` parses and formats with the invariant culture.** The native control's value is
  always ISO regardless of what the user sees — using the current culture works in `en-US` and
  breaks wherever the separator differs.

**Verification** — 327 tests, up from 260.

### Fixed during M2

- **`ZenForm` passed both `EditContext` and `Model` to `EditForm`,** which rejects the combination
  outright, so every page using it returned a 500. Every unit test passed, because none of them had
  rendered a `ZenForm` — caught on first load of the demo page.
- **`ZenIndicator`'s fill was sliced out of `ZenStyles.Solid()`,** whose `Neutral` background is
  `surface-raised`. A neutral badge therefore rendered white on a white page. A marker's entire job
  is to be noticed.

### Fixed — focus and hover border was 1px, not 2px

The border-emphasis utilities offset their outline by `-1px`, which laid the 1px outline directly
on top of the 1px border instead of beside it. The band stayed 1px wide regardless of
`--zen-ring-width`.

A CSS outline paints *outward* from the outline edge, which sits at `outline-offset` from the
border-box edge. For a band spanning `[0, W]` inward, where the border already covers `[0, 1]`, the
offset must be `-W` and the width `W - 1px`. `-1px` is the plausible-looking value that silently
halves the indicator.

`BorderEmphasisTests` now parses `components/base.css` and asserts the arithmetic. bUnit has no
layout engine, so no rendering test can measure how thick a focus ring actually appears — asserting
on the CSS text is the only guard available, and it was verified to fail on the old value.

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

- The focused border is reinforced with an outline pulled *inside* the element by a negative
  offset, bringing the band to `--zen-ring-width` total. A 1px colour change is the bare minimum
  WCAG 2.4.7 accepts and falls short of the 2px perimeter WCAG 2.4.13 asks for. Drawing inward
  rather than outward means the control's outer dimensions never change, so a focused field does
  not nudge its neighbours — and it leaves `box-shadow` free, so the same treatment composes with
  `ZenCard`'s elevation.
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
