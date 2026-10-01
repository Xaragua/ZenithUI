using System.Globalization;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.JSInterop;
using ZenithUI;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// The client has its own DI container. A component rendered with InteractiveWebAssembly resolves
// IZenThemeService from HERE, not from the server project, so ZenithUI must be registered in both.
builder.Services.AddZenithUI();

var host = builder.Build();

// The browser runtime does not see the server's request culture, so it takes the language the
// server rendered into <html lang>. Set before RunAsync: the host loads the satellite assemblies
// - ZenithUI's Spanish strings among them - for the culture in force when it starts, and that
// needs BlazorWebAssemblyLoadAllGlobalizationData in the project file.
var lang = await host.Services.GetRequiredService<IJSRuntime>()
    .InvokeAsync<string?>("document.documentElement.getAttribute", "lang");

if (!string.IsNullOrEmpty(lang))
{
    var culture = CultureInfo.GetCultureInfo(lang);

    CultureInfo.DefaultThreadCurrentCulture = culture;
    CultureInfo.DefaultThreadCurrentUICulture = culture;
}

await host.RunAsync();
