using System.Runtime.CompilerServices;

namespace ZenithUI.Tests;

/// <summary>
/// Pins the UI culture the suite runs in.
/// </summary>
/// <remarks>
/// Since 1.1.0 every string ZenithUI renders comes from its resources in
/// <see cref="CultureInfo.CurrentUICulture"/>, and hundreds of assertions here expect the English
/// text. Without this, the suite passes on an English machine and fails on a Spanish one for
/// reasons that have nothing to do with the code. Only the UI culture is pinned: formatting tests
/// set <see cref="CultureInfo.CurrentCulture"/> themselves, and the localization tests switch the
/// UI culture inside a scope.
/// </remarks>
internal static class TestCulture
{
    [ModuleInitializer]
    internal static void PinUiCulture()
    {
        var english = CultureInfo.GetCultureInfo("en-US");

        CultureInfo.DefaultThreadCurrentUICulture = english;
        CultureInfo.CurrentUICulture = english;
    }

    /// <summary>Switches the UI culture until the returned scope is disposed.</summary>
    internal static IDisposable Use(string name)
    {
        var previous = CultureInfo.CurrentUICulture;
        CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(name);

        return new Restore(previous);
    }

    private sealed class Restore(CultureInfo previous) : IDisposable
    {
        public void Dispose() => CultureInfo.CurrentUICulture = previous;
    }
}
