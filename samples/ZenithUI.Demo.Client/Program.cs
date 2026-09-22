using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using ZenithUI;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// The client has its own DI container. A component rendered with InteractiveWebAssembly resolves
// IZenThemeService from HERE, not from the server project, so ZenithUI must be registered in both.
builder.Services.AddZenithUI();

await builder.Build().RunAsync();
