using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ZenithUI.Services;

namespace ZenithUI;

/// <summary>Registration helpers for ZenithUI.</summary>
public static class ZenithUIServiceCollectionExtensions
{
    /// <summary>
    /// Registers the services ZenithUI components resolve from DI.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The same collection, for chaining.</returns>
    /// <remarks>
    /// <para>
    /// Call this in every project that renders ZenithUI components. In a Blazor Web App with
    /// WebAssembly interactivity that means <b>both</b> the server project's <c>Program.cs</c> and
    /// the client project's - the two have separate service containers, and a component rendered
    /// on the client resolves from the client's.
    /// </para>
    /// <para>
    /// Everything is registered <c>Scoped</c>, which is the correct lifetime for both hosting
    /// models: one instance per circuit on Blazor Server, one per application on WebAssembly.
    /// Either maps to exactly one document, which is what lets the theme service cache DOM state.
    /// </para>
    /// <para>
    /// <c>TryAdd</c> is used throughout, so an application that has already registered its own
    /// <see cref="IZenThemeService"/> keeps it.
    /// </para>
    /// </remarks>
    public static IServiceCollection AddZenithUI(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddScoped<IZenThemeService, ZenThemeService>();

        return services;
    }
}
