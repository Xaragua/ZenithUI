using System.Collections;
using System.Reflection;
using System.Resources;
using System.Text.RegularExpressions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;

namespace ZenithUI.Tests.Core;

/// <summary>
/// The library's own text: that the resource files agree with each other and with the key list,
/// that the culture chain resolves the way the docs say, and that a component asks the localizer
/// in the right order - its parameter, then the application's localizer, then the built-in one.
/// </summary>
public partial class ZenLocalizationTests : BunitContext
{
    public ZenLocalizationTests()
    {
        Renderer.SetRendererInfo(new RendererInfo("Server", isInteractive: true));
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    private static readonly ResourceManager Resources =
        new(ZenStrings.BaseName, typeof(ZenStrings).Assembly);

    private static Dictionary<string, string> Strings(string culture)
    {
        var set = Resources.GetResourceSet(CultureInfo.GetCultureInfo(culture), createIfNotExists: true, tryParents: false)
            ?? throw new InvalidOperationException($"No resources for '{culture}'.");

        return set.Cast<DictionaryEntry>().ToDictionary(e => (string)e.Key, e => (string)e.Value!);
    }

    private static HashSet<string> Keys() =>
        typeof(ZenStrings).Assembly.GetType("ZenithUI.ZenStringKeys")!
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Select(f => (string)f.GetRawConstantValue()!)
            .ToHashSet();

    [GeneratedRegex(@"\{\d+\}")]
    private static partial Regex Placeholder();

    private static List<string> PlaceholdersOf(string value) =>
        Placeholder().Matches(value).Select(m => m.Value).Order().ToList();

    // ---- The resource files -------------------------------------------------------------------

    [Fact]
    public void TheBaseName_IsTheTypesName()
    {
        // The constant stands in for typeof(ZenStrings).FullName, which cannot be used: as a
        // static field it was read before it was set under WebAssembly. They must still agree,
        // because the SDK names the resources after the type.
        ZenStrings.BaseName.ShouldBe(typeof(ZenStrings).FullName);
        typeof(ZenStrings).Assembly.GetManifestResourceNames().ShouldContain($"{ZenStrings.BaseName}.resources");
    }

    [Fact]
    public void TheNeutralResources_HoldExactlyTheDeclaredKeys() =>
        Strings("").Keys.ToHashSet().SetEquals(Keys()).ShouldBeTrue(
            "ZenStrings.resx and ZenStringKeys.cs list different keys.");

    [Fact]
    public void Spanish_TranslatesEveryKey()
    {
        // es is the fallback for every Spanish culture, so a key missing here is an English word
        // in the middle of a Spanish page.
        Strings("es").Keys.Order().ShouldBe(Keys().Order());
    }

    [Fact]
    public void DominicanSpanish_OverridesOnlyKeysThatExist_AndOnlyWhereItDiffers()
    {
        var es = Strings("es");

        foreach (var (key, value) in Strings("es-DO"))
        {
            es.ShouldContainKey(key);
            value.ShouldNotBe(es[key], $"es-DO repeats the es text for {key}; delete it and let it fall back.");
        }
    }

    [Theory]
    [InlineData("es")]
    [InlineData("es-DO")]
    public void EveryTranslation_KeepsTheNeutralPlaceholders(string culture)
    {
        // A translation that drops {0} loses the field name; one that adds {1} throws a
        // FormatException at render time.
        var neutral = Strings("");

        foreach (var (key, value) in Strings(culture))
        {
            PlaceholdersOf(value).ShouldBe(PlaceholdersOf(neutral[key]), $"{culture}: {key}");
        }
    }

    // ---- The culture chain --------------------------------------------------------------------

    [Theory]
    [InlineData("en-US", "Previous page", "Clear search")]
    [InlineData("es-ES", "Página anterior", "Borrar búsqueda")]
    [InlineData("es-DO", "Página anterior", "Limpiar búsqueda")]
    [InlineData("es-MX", "Página anterior", "Borrar búsqueda")]
    [InlineData("fr-FR", "Previous page", "Clear search")]
    public void TheDefaultLocalizer_FollowsTheUiCultureAndItsParents(string culture, string shared, string regional)
    {
        // es-DO inherits from es for the strings it does not override; any other Spanish culture
        // gets es; an unsupported language gets English rather than key names.
        using var _ = TestCulture.Use(culture);

        ZenStrings.Default["Table_PreviousPage"].Value.ShouldBe(shared);
        ZenStrings.Default["SearchInput_Clear"].Value.ShouldBe(regional);
    }

    [Fact]
    public void FormatStrings_FillTheirArguments() =>
        ZenStrings.Default["Table_PageSummary", 1, 10, 42].Value.ShouldBe("1–10 of 42");

    // ---- Components ---------------------------------------------------------------------------

    private sealed record Row(string Name);

    private IRenderedComponent<ZenTable<Row>> PagedTable(
        Action<ComponentParameterCollectionBuilder<ZenTable<Row>>>? extra = null) =>
        Render<ZenTable<Row>>(p =>
        {
            p.Add(x => x.Items, [new Row("a"), new Row("b"), new Row("c")])
                .Add(x => x.PageSize, 1)
                .Add(x => x.Columns, b =>
                {
                    b.OpenComponent<ZenColumn<Row>>(0);
                    b.AddComponentParameter(1, nameof(ZenColumn<Row>.Title), "Name");
                    b.AddComponentParameter(2, nameof(ZenColumn<Row>.Field), (Func<Row, object?>)(r => r.Name));
                    b.CloseComponent();
                });

            extra?.Invoke(p);
        });

    [Fact]
    public void ATable_RendersItsPagerInSpanish()
    {
        using var _ = TestCulture.Use("es-ES");

        var cut = PagedTable();

        cut.Find("nav").GetAttribute("aria-label").ShouldBe("Paginación");
        cut.Find("nav p").TextContent.ShouldBe("1–1 de 3");
        cut.FindAll("nav button")[0].GetAttribute("aria-label").ShouldBe("Página anterior");
        cut.Find("nav label span").TextContent.ShouldBe("Filas por página");
    }

    [Theory]
    [InlineData("es-ES", "Hoy", "Borrar")]
    [InlineData("es-DO", "Hoy", "Limpiar")]
    public void ACalendar_UsesTheRegionalWording(string culture, string today, string clear)
    {
        using var _ = TestCulture.Use(culture);

        var cut = Render<ZenCalendar>(p => p.Add(x => x.AllowClear, true));

        cut.FindAll("button").Select(b => b.TextContent.Trim()).ShouldContain(today);
        cut.FindAll("button").Select(b => b.TextContent.Trim()).ShouldContain(clear);
    }

    [Fact]
    public void AParameter_BeatsTheLocalizer()
    {
        using var _ = TestCulture.Use("es-ES");

        PagedTable(p => p.Add(x => x.Label, "Pedidos"))
            .Find("nav").GetAttribute("aria-label").ShouldBe("Paginación de Pedidos");

        Render<ZenSpinner>(p => p.Add(x => x.Label, "Un momento"))
            .Find("[role=status]").TextContent.Trim().ShouldBe("Un momento");
    }

    [Fact]
    public void AnEmptyLabel_StillSuppressesTheSpinnersAnnouncement() =>
        Render<ZenSpinner>(p => p.Add(x => x.Label, string.Empty))
            .FindAll("[role=status]").ShouldBeEmpty();
}

/// <summary>
/// Which localizer a component uses when the application registers one. A class of its own
/// because each test registers services, which bUnit only allows before anything has resolved
/// from the container - and setting the renderer info in a constructor already has.
/// </summary>
public class ZenLocalizerOverrideTests : BunitContext
{
    private sealed class Shouting : IStringLocalizer<ZenStrings>
    {
        public LocalizedString this[string name] =>
            new(name, ZenStrings.Default[name].Value.ToUpperInvariant());

        public LocalizedString this[string name, params object[] arguments] =>
            new(name, ZenStrings.Default[name, arguments].Value.ToUpperInvariant());

        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) =>
            ZenStrings.Default.GetAllStrings(includeParentCultures);
    }

    [Fact]
    public void AnApplicationsLocalizer_BeatsTheBuiltInOne()
    {
        Services.AddSingleton<IStringLocalizer<ZenStrings>, Shouting>();

        Render<ZenSpinner>().Find("[role=status]").TextContent.Trim().ShouldBe("LOADING");
    }

    [Fact]
    public void AddZenithUI_RegistersTheBuiltInLocalizer_WithoutReplacingTheApplications()
    {
        new ServiceCollection().AddZenithUI().BuildServiceProvider()
            .GetRequiredService<IStringLocalizer<ZenStrings>>().ShouldBeSameAs(ZenStrings.Default);

        var mine = new Shouting();
        new ServiceCollection().AddSingleton<IStringLocalizer<ZenStrings>>(mine).AddZenithUI().BuildServiceProvider()
            .GetRequiredService<IStringLocalizer<ZenStrings>>().ShouldBeSameAs(mine);
    }

    private sealed class EmptyFactory : IStringLocalizerFactory
    {
        public IStringLocalizer Create(Type resourceSource) => new KeyEcho();

        public IStringLocalizer Create(string baseName, string location) => new KeyEcho();

        private sealed class KeyEcho : IStringLocalizer
        {
            public LocalizedString this[string name] => new(name, name, resourceNotFound: true);

            public LocalizedString this[string name, params object[] arguments] => this[name];

            public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) => [];
        }
    }

    [Fact]
    public void TheFrameworksGenericLocalizer_IsPassedOver()
    {
        // What AddLocalization() resolves IStringLocalizer<ZenStrings> to when nothing closed is
        // registered. Its factory looks under the application's ResourcesPath, finds nothing and
        // echoes key names - so a component must not take it for an override.
        Services.AddSingleton<IStringLocalizer<ZenStrings>>(new StringLocalizer<ZenStrings>(new EmptyFactory()));

        Render<ZenSpinner>().Find("[role=status]").TextContent.Trim().ShouldBe("Loading");
    }
}
