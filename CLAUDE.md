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

Run the demo before calling a component done — the M0 bugs listed in `docs/CHANGELOG.md` were all
invisible to the test suite and obvious in the browser.
