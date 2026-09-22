namespace ZenithUI.Tests.Theming;

/// <summary>Locates the token stylesheets copied next to the test assembly.</summary>
/// <remarks>
/// The .csproj links <c>src/ZenithUI/Styles/tokens/*.css</c> into <c>Tokens/</c> in the output
/// directory. Copying rather than walking up to the repository root keeps the tests runnable from
/// a published test bundle, where no source tree exists.
/// </remarks>
public static class TokenPaths
{
    /// <summary>The light palette (<c>tokens/base.css</c>).</summary>
    public static string Base { get; } = Resolve("base.css");

    /// <summary>The dark palette (<c>tokens/dark.css</c>).</summary>
    public static string Dark { get; } = Resolve("dark.css");

    private static string Resolve(string fileName)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Tokens", fileName);

        if (!File.Exists(path))
        {
            throw new FileNotFoundException(
                $"Token stylesheet '{fileName}' was not copied to the test output. Check the " +
                "<Content Include=\"..\\..\\src\\ZenithUI\\Styles\\tokens\\*.css\" /> item in " +
                "ZenithUI.Tests.csproj.",
                path);
        }

        return path;
    }
}
