# ZenithUI — working conventions

## Documentation

**All documentation goes in `docs/`.** The single exception is `README.md`, which stays at the
repository root because it is the GitHub landing page.

That means the plan, the changelog, architecture notes, ADRs, migration guides and anything else
written for humans belong under `docs/` — not at the root, and not scattered beside the code.

When adding a document, link it from the **Documentation** table in `README.md` so it is
discoverable.

Non-documentation files keep their conventional locations: `LICENSE`, `.gitignore`,
`.gitattributes`, `global.json`, `Directory.*.props` and `.github/` all stay at the root.

## Code

- Components never hardcode a colour. Use the semantic utilities (`bg-surface`,
  `text-content-muted`, `text-danger-strong`) which resolve to `--zen-*` custom properties.
- Never write `dark:` variants. The palette swaps through custom properties so that statically
  rendered markup repaints without a class change.
- Never declare `@rendermode` inside `src/ZenithUI`. The consuming application decides.
- Every component `.razor` file starts with `@namespace ZenithUI`. Do **not** put that directive in
  a folder-level `_Imports.razor`: two such files both generate a `ZenithUI._Imports` class and the
  build fails with `CS0111`. Shared `@using` directives live in `Components/_Imports.razor`, which
  deliberately has no `@namespace`.
- Guard all JS interop behind `RendererInfo.IsInteractive`, in `OnAfterRenderAsync` only.

See [`docs/plan.md`](docs/plan.md) for the reasoning behind each of these.

## Verifying

`dotnet build` regenerates the Tailwind stylesheets. `dotnet test` includes the contrast audit,
which fails on any WCAG or sRGB-gamut regression in either palette.

Run the demo before calling a component done — the M0 and M1 bugs listed in `docs/CHANGELOG.md`
were all invisible to the test suite and obvious in the browser.

**Restart the demo after every rebuild.** `MapStaticAssets` bakes content-hashed asset URLs into a
manifest at build time. A still-running process keeps serving the old fingerprints, and once the
files behind them change it serves **empty responses** — the page loads with the library stylesheet
applied but none of the app's own CSS, which looks exactly like a catastrophic styling bug and is
not one. If the demo suddenly renders in Times New Roman with underlined links, this is why.

**The sibling failure: a stale gzip of the library stylesheet.** Restarting is not always enough.
The static-asset pipeline caches pre-compressed copies under
`obj/Debug/net10.0/compressed/`, and that cache can go stale against a regenerated
`zenith.nopreflight.css` — leaving an **empty** gzip behind a healthy-looking 200.

It is nastier than the manifest trap because every obvious check passes. `curl` sends no
`Accept-Encoding`, so it gets the real 47 KB file; the browser sends one, gets
`Content-Encoding: gzip` over an empty body, and parses **zero rules**. The page is then styled
entirely by the demo's own `app.css` — which carries the `--zen-*` tokens and every class the demo's
own markup uses, so the theme still looks right — while anything used *only* inside a library
component silently vanishes: `appearance-none`, `size-4`, the `relative` on a select shell. The
symptom is a native select arrow, an enormous unsized icon and collapsed spacing, which reads as a
component bug and is not one.

Confirm it in one command, and treat a decoded length of 0 as proof:

```bash
curl -s -H "Accept-Encoding: gzip" <url> --compressed | wc -c
```

The fix is to delete the `compressed` directories and rebuild. **Verify CSS in a real browser**, not
with `curl`: headless Edge renders and screenshots a page, and
`[...document.styleSheets].map(s => s.cssRules.length)` dumped via `--dump-dom` is what actually
proves a stylesheet applied.

## Focus treatment

Two indicators, chosen by control type:

- **Text-entry controls** (input, textarea, select, combobox) use `zen-focus-border` — the
  control's own border turns the ring colour, with no outline outside it. Compose via
  `ZenStyles.InputBase`. For a composite control whose border sits on a wrapper, use
  `zen-focus-border-within` / `ZenStyles.InputWrapperBase`.
- **Buttons, links and toggles** use `zen-focus` — an outline ring. They have no resting border to
  recolour.

An invalid field keeps its danger border while focused; do not let a focus style replace an error
state.
