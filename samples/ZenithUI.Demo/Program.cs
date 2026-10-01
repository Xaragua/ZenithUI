using Microsoft.AspNetCore.Localization;
using ZenithUI;
using ZenithUI.Demo.Components;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddZenithUI();

// The demo's own localization. AddLocalization registers an open IStringLocalizer<> that would
// look for ZenithUI's strings under this app's ResourcesPath and render key names - which is why
// AddZenithUI registers IStringLocalizer<ZenStrings> closed. The order of the two calls does not
// matter.
builder.Services.AddLocalization();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

// The cultures ZenithUI ships text for. The cookie provider is what the switcher in the app bar
// writes; an interactive Server circuit takes its culture from the request that started it, so a
// change needs a full page load to reach the islands - see CultureSwitcher.razor.
string[] cultures = ["en-US", "es-ES", "es-DO"];

app.UseRequestLocalization(options => options
    .SetDefaultCulture(cultures[0])
    .AddSupportedCultures(cultures)
    .AddSupportedUICultures(cultures));

app.UseAntiforgery();

app.MapGet("/culture/set", (string culture, string? redirectUri, HttpContext context) =>
{
    if (cultures.Contains(culture))
    {
        context.Response.Cookies.Append(
            CookieRequestCultureProvider.DefaultCookieName,
            CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
            new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1), IsEssential = true, SameSite = SameSiteMode.Lax });
    }

    // LocalRedirect, so the parameter cannot bounce a visitor to another site.
    return Results.LocalRedirect(string.IsNullOrEmpty(redirectUri) ? "~/" : redirectUri);
});

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(ZenithUI.Demo.Client._Imports).Assembly);

app.Run();
