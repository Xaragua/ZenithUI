using System.Globalization;
using System.Resources;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;

namespace ZenithUI;

/// <summary>
/// The text ZenithUI renders on its own behalf: button labels, accessible names, pager text,
/// validation messages. Also the type argument that names the library's localizer,
/// <c>IStringLocalizer&lt;ZenStrings&gt;</c>.
/// </summary>
/// <remarks>
/// <para>
/// Ships in en-US (the neutral resources), Spanish (<c>es</c>, written for es-ES) and Dominican
/// Spanish (<c>es-DO</c>, which overrides only the strings where its wording differs and falls
/// back to <c>es</c> for the rest). The culture is <see cref="CultureInfo.CurrentUICulture"/> at
/// render time, so whatever the host already does to set it - request localization middleware on
/// the server, <c>CultureInfo.DefaultThreadCurrentUICulture</c> in WebAssembly - applies.
/// </para>
/// <para>
/// To replace or add strings app-wide, register your own <c>IStringLocalizer&lt;ZenStrings&gt;</c>.
/// <see cref="Default"/> is the built-in one, for an implementation that overrides a few keys and
/// hands the rest back. A parameter set on a component always wins over either.
/// </para>
/// <para>
/// The lookup is the library's own rather than the framework's <c>ResourceManagerStringLocalizerFactory</c>.
/// That factory finds resources through the application's <c>ResourcesPath</c>, which describes
/// the application's folders - not this assembly's - so an app that sets it would have silently
/// lost every ZenithUI translation.
/// </para>
/// </remarks>
public sealed class ZenStrings
{
    private ZenStrings()
    {
    }

    /// <summary>The built-in localizer, reading the resources compiled into ZenithUI.</summary>
    public static IStringLocalizer<ZenStrings> Default { get; } = new ResourceLocalizer();

    /// <summary>
    /// The localizer a component should use: the application's, when it registered one, and the
    /// built-in one otherwise.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Optional on purpose. An application that never calls <c>AddZenithUI</c>, and every bUnit
    /// test written before 1.1.0, has no registration - and must keep rendering English rather
    /// than throwing for a service it was never told it needed.
    /// </para>
    /// <para>
    /// The framework's own <c>StringLocalizer&lt;T&gt;</c> is passed over. It is what an
    /// application calling <c>AddLocalization()</c> gets for every <c>IStringLocalizer&lt;T&gt;</c>
    /// it did not register itself, and it looks for this library's resources under the
    /// application's <c>ResourcesPath</c>, finds nothing, and renders key names.
    /// <c>AddZenithUI</c> registers the closed type so that never arises; this is the backstop for
    /// an application that registers localization without it.
    /// </para>
    /// </remarks>
    internal static IStringLocalizer Resolve(IServiceProvider? services) =>
        services?.GetService<IStringLocalizer<ZenStrings>>() is { } registered
            && registered.GetType().Assembly != typeof(StringLocalizer<>).Assembly
            ? registered
            : Default;

    /// <summary>
    /// The resources' manifest name. The SDK names a .resx after the class in the .cs file
    /// beside it, not after its folder, so this follows the type wherever the files move.
    /// </summary>
    /// <remarks>
    /// A constant, not <c>typeof(ZenStrings).FullName</c> in a static field. <see cref="Default"/>
    /// is initialised above this line, and constructing it touches the localizer's own statics,
    /// which read this name. The server's JIT happened to defer that until first use, by which
    /// time a static field would have been set; the WebAssembly runtime does not, read null, and
    /// every component that rendered text failed with a TypeInitializationException. A constant
    /// has no initialisation order to get wrong.
    /// </remarks>
    internal const string BaseName = nameof(ZenithUI) + "." + nameof(ZenStrings);

    private sealed class ResourceLocalizer : IStringLocalizer<ZenStrings>
    {
        private static readonly ResourceManager Resources =
            new(BaseName, typeof(ZenStrings).Assembly);

        public LocalizedString this[string name]
        {
            get
            {
                var value = Resources.GetString(name, CultureInfo.CurrentUICulture);
                return new LocalizedString(name, value ?? name, resourceNotFound: value is null, searchedLocation: BaseName);
            }
        }

        public LocalizedString this[string name, params object[] arguments]
        {
            get
            {
                var format = this[name];

                // Numbers inside a message format in the formatting culture, which is not
                // necessarily the UI language: an English UI can still want 1.234,56.
                return format.ResourceNotFound
                    ? format
                    : new LocalizedString(name, string.Format(CultureInfo.CurrentCulture, format.Value, arguments), false, BaseName);
            }
        }

        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);
            var culture = CultureInfo.CurrentUICulture;

            while (true)
            {
                var set = Resources.GetResourceSet(culture, createIfNotExists: true, tryParents: false);

                if (set is not null)
                {
                    foreach (System.Collections.DictionaryEntry entry in set)
                    {
                        if (entry.Key is string key && entry.Value is string value && seen.Add(key))
                        {
                            yield return new LocalizedString(key, value, false, BaseName);
                        }
                    }
                }

                if (!includeParentCultures || culture.Equals(CultureInfo.InvariantCulture))
                {
                    yield break;
                }

                culture = culture.Parent;
            }
        }
    }
}
