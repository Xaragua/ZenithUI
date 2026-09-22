using System.Text.RegularExpressions;

namespace ZenithUI.Tests.Theming;

/// <summary>
/// Reads the <c>--zen-*</c> declarations out of the token stylesheets so the palette can be
/// asserted against in C#.
/// </summary>
/// <remarks>
/// This is a deliberately small, purpose-built parser rather than a real CSS parser. It only has
/// to handle the shape the token files are written in - one <c>--zen-name: value;</c> per
/// declaration, inside a known block - and failing loudly on anything else is the desired
/// behaviour: a token file that has drifted into a shape this cannot read is a token file that
/// needs a second look.
/// </remarks>
public sealed partial class TokenSheet
{
    private readonly Dictionary<string, string> _tokens;

    private TokenSheet(Dictionary<string, string> tokens) => _tokens = tokens;

    /// <summary>Every <c>--zen-*</c> token name found, without the leading dashes.</summary>
    public IReadOnlyCollection<string> Names => _tokens.Keys;

    /// <summary>The raw declared value of a token.</summary>
    public string this[string name] => _tokens[name];

    /// <summary><see langword="true"/> when the sheet declares <paramref name="name"/>.</summary>
    public bool Contains(string name) => _tokens.ContainsKey(name);

    /// <summary>
    /// Resolves a token to a colour, following <c>var(--zen-other)</c> indirection.
    /// </summary>
    /// <param name="name">Token name without the leading dashes, for example <c>zen-primary</c>.</param>
    /// <returns>The colour, or <see langword="null"/> when the token is not an OKLCH value.</returns>
    public OklchColor? ResolveColor(string name)
    {
        // Indirection is shallow by construction (--zen-ring: var(--zen-primary)), but the depth
        // limit stops a future circular alias from hanging the test run.
        for (var depth = 0; depth < 8; depth++)
        {
            if (!_tokens.TryGetValue(name, out var value))
            {
                return null;
            }

            var alias = VarReferencePattern.Match(value);

            if (!alias.Success)
            {
                return OklchColor.TryParse(value);
            }

            name = alias.Groups["ref"].Value;
        }

        throw new InvalidOperationException($"Token '{name}' has circular var() indirection.");
    }

    /// <summary>Parses every declaration in a file, regardless of which block it sits in.</summary>
    /// <param name="path">Path to the CSS file.</param>
    public static TokenSheet Load(string path) => Parse(File.ReadAllText(path));

    /// <summary>Parses every <c>--zen-*</c> declaration in a CSS string.</summary>
    public static TokenSheet Parse(string css)
    {
        var tokens = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (Match match in DeclarationPattern.Matches(StripComments(css)))
        {
            // Later declarations win, mirroring the cascade within a single block.
            tokens[match.Groups["name"].Value] = Normalize(match.Groups["value"].Value);
        }

        return new TokenSheet(tokens);
    }

    /// <summary>
    /// Extracts the declarations of a single rule, identified by a substring of its selector.
    /// </summary>
    /// <param name="css">The stylesheet text.</param>
    /// <param name="selectorFragment">
    /// Text that uniquely identifies the opening of the block, for example
    /// <c>:root[data-zen-theme="dark"]</c>.
    /// </param>
    public static string ExtractBlock(string css, string selectorFragment)
    {
        css = StripComments(css);

        var selectorIndex = css.IndexOf(selectorFragment, StringComparison.Ordinal);

        if (selectorIndex < 0)
        {
            throw new InvalidOperationException($"No block matching '{selectorFragment}' was found.");
        }

        var open = css.IndexOf('{', selectorIndex);

        if (open < 0)
        {
            throw new InvalidOperationException($"Block '{selectorFragment}' has no opening brace.");
        }

        var depth = 0;

        for (var i = open; i < css.Length; i++)
        {
            if (css[i] == '{')
            {
                depth++;
            }
            else if (css[i] == '}')
            {
                depth--;

                if (depth == 0)
                {
                    return css[(open + 1)..i];
                }
            }
        }

        throw new InvalidOperationException($"Block '{selectorFragment}' is unterminated.");
    }

    /// <summary>
    /// Collapses whitespace so that declarations written across multiple lines (the multi-layer
    /// shadows) compare equal to their single-line equivalents.
    /// </summary>
    public static string Normalize(string value) =>
        WhitespacePattern.Replace(value, " ").Trim();

    private static string StripComments(string css) =>
        CommentPattern.Replace(css, string.Empty);

    [GeneratedRegex(@"--(?<name>zen-[a-z0-9-]+)\s*:\s*(?<value>[^;}]+)", RegexOptions.IgnoreCase)]
    private static partial Regex DeclarationPattern { get; }

    [GeneratedRegex(@"var\(\s*--(?<ref>[a-z0-9-]+)\s*\)", RegexOptions.IgnoreCase)]
    private static partial Regex VarReferencePattern { get; }

    [GeneratedRegex(@"/\*.*?\*/", RegexOptions.Singleline)]
    private static partial Regex CommentPattern { get; }

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespacePattern { get; }
}
