# Localization

Every word ZenithUI renders on its own behalf comes from its resources in the current UI
culture. That covers button labels, accessible names, the table pager, stepper navigation and
validation messages. The library ships three languages:

| Culture | Resources | Notes |
| --- | --- | --- |
| `en-US` | `ZenStrings.resx` (neutral) | The default, and the fallback for any culture without a translation |
| `es-ES` | `ZenStrings.es.resx` | The full Spanish translation, and the fallback for every other `es-*` culture |
| `es-DO` | `ZenStrings.es-DO.resx` | Only the strings where Dominican usage differs, for example *Ingrese*, *monto*, *Limpiar*. The rest come from `es` |

Dates, numbers and currency were culture-aware before 1.1.0 and are unchanged. They follow
`CultureInfo.CurrentCulture`; the text follows `CultureInfo.CurrentUICulture`.

- [What you have to do](#what-you-have-to-do)
- [Blazor Web App](#blazor-web-app)
- [Standalone WebAssembly](#standalone-webassembly)
- [Overriding text](#overriding-text)
- [Adding a language](#adding-a-language)
- [How it works, and why](#how-it-works-and-why)

## What you have to do

**Nothing, for English.** An app that never sets a culture renders exactly what 1.0.0 rendered.

For another language, set the UI culture the way any ASP.NET Core app does. ZenithUI reads
`CurrentUICulture` at render time and does not need a registration of its own beyond the
`AddZenithUI()` you already have.

## Blazor Web App

**Server side.** Request localization sets the culture per request, and an interactive Server
circuit keeps the culture of the request that started it:

```csharp
string[] cultures = ["en-US", "es-ES", "es-DO"];

app.UseRequestLocalization(options => options
    .SetDefaultCulture(cultures[0])
    .AddSupportedCultures(cultures)
    .AddSupportedUICultures(cultures));
```

The cookie provider is on by default, so a "language" switch is a link to an endpoint that writes
`CookieRequestCultureProvider.DefaultCookieName` and redirects back. Give that link
`data-enhance-nav="false"`. Enhanced navigation would patch the page in place, and interactive
islands would keep the old circuit, and with it the old language. The demo's
`CultureSwitcher.razor` and `/culture/set` endpoint are a complete example.

Put the language on the document too, so screen readers pronounce it correctly:

```razor
<html lang="@CultureInfo.CurrentUICulture.Name">
```

**WebAssembly side.** The browser runtime never sees the server's request culture. Set it in the
client project's `Program.cs` **before `RunAsync`**. The host loads satellite assemblies,
ZenithUI's Spanish strings among them, for the culture in force when it starts:

```csharp
var host = builder.Build();

// One way to agree with the server: read the lang attribute it rendered.
var lang = await host.Services.GetRequiredService<IJSRuntime>()
    .InvokeAsync<string?>("document.documentElement.getAttribute", "lang");

if (!string.IsNullOrEmpty(lang))
{
    CultureInfo.DefaultThreadCurrentCulture = CultureInfo.GetCultureInfo(lang);
    CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.GetCultureInfo(lang);
}

await host.RunAsync();
```

The client project also needs the full ICU data, or the runtime throws when the culture changes
at startup:

```xml
<BlazorWebAssemblyLoadAllGlobalizationData>true</BlazorWebAssemblyLoadAllGlobalizationData>
```

An app with `<InvariantGlobalization>true</InvariantGlobalization>` cannot switch culture at all,
and ZenithUI renders English there.

## Standalone WebAssembly

The WebAssembly side of the Web App section above applies on its own. Pick the culture however
the app chooses (stored preference, browser language, a query string) and set it before
`RunAsync`.

## Overriding text

**One component:** every string has a parameter, and a value you pass always wins:

```razor
<ZenTable ... EmptyText="Todavía no hay pedidos" />
<ZenSplitButton ... MenuLabel="Otras formas de guardar" />
```

Parameters that used to default to English text (`EmptyText`, `CloseLabel`, `NextText` and so
on) now default to `null`, which means "the localized default". Their type and name are
unchanged. A few are "empty means none": pass `""` to `ZenSpinner.Label` or
`ZenLookup.NoResultsDescription` to render nothing.

**The whole app:** register your own `IStringLocalizer<ZenStrings>`. `ZenStrings.Default` is the
built-in one, so an override can change a few keys and hand the rest back:

```csharp
public sealed class OurZenStrings : IStringLocalizer<ZenStrings>
{
    public LocalizedString this[string name] => name switch
    {
        "Common_NoData" => new(name, "Nothing here yet"),
        _ => ZenStrings.Default[name],
    };

    public LocalizedString this[string name, params object[] arguments] =>
        ZenStrings.Default[name, arguments];

    public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) =>
        ZenStrings.Default.GetAllStrings(includeParentCultures);
}

builder.Services.AddSingleton<IStringLocalizer<ZenStrings>, OurZenStrings>();
```

Register it before or after `AddZenithUI()`. `AddZenithUI` uses `TryAdd`, so an earlier
registration is kept, and a later one is what the container resolves.

Key names are listed in `src/ZenithUI/Localization/ZenStrings.resx`, grouped by the component that
renders them (`Table_*`, `Stepper_*`, `Validation_*`). Shared keys start with `Common_`.

## Adding a language

Inside the library, add `ZenStrings.<culture>.resx` next to the others with every key translated,
or for a regional variant, only the keys that differ from its parent language. The test suite
enforces this:

- every key in `ZenStringKeys.cs` exists in the neutral and the `es` resources
- a regional file only overrides keys that exist, and only where its text actually differs
- every translation keeps the neutral string's `{0}` placeholders, so none can throw a
  `FormatException` at render time

Outside the library, implement `IStringLocalizer<ZenStrings>` as above and return your
translations for the cultures you add.

## How it works, and why

**The library looks up its own resources.** It does not go through the framework's
`ResourceManagerStringLocalizerFactory`. That factory finds resources through the application's
`ResourcesPath`, which describes the application's folders, not this package's. An app that set
it would silently have lost every ZenithUI translation.

**`AddZenithUI()` registers `IStringLocalizer<ZenStrings>` as a closed type.** `AddLocalization()`
registers an open `IStringLocalizer<>`, which would otherwise resolve for `ZenStrings` too, through
that same factory, and render key names. A closed registration beats the open one whichever is
called first. If an app registers localization without `AddZenithUI()`, components detect the
framework's generic localizer and use the built-in one instead.

**No registration is required at all.** A component asks the container for a localizer and falls
back to `ZenStrings.Default` when there is none. That fallback is why an app, or a bUnit test,
written before 1.1.0 keeps working unchanged.

**Plurals are two keys.** For example, `Table_RowOne` and `Table_RowOther`. English and Spanish
both need only the one/other distinction. A language with more plural forms would need the
component to choose among more keys, so that is a change to the library, not just a new file.
