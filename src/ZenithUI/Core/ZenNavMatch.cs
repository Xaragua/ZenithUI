using Microsoft.AspNetCore.Components.Routing;

namespace ZenithUI;

/// <summary>
/// Decides whether a navigation target covers the current location.
/// </summary>
/// <remarks>
/// <para>
/// Shared by <c>ZenNavLink</c>, which spends the answer on <c>aria-current</c>, and by
/// <c>ZenNavGroup</c>, which spends it on whether a collapsed section starts open. The two must
/// agree: a group that disagreed with its own children would either hide the current page behind a
/// closed disclosure or open a section containing nothing the user asked for.
/// </para>
/// <para>
/// The rules are Blazor's own <c>NavLink</c> rules, kept deliberately compatible so that swapping
/// one component for the other does not silently change which item highlights.
/// </para>
/// </remarks>
internal static class ZenNavMatch
{
    /// <summary>
    /// Whether <paramref name="hrefAbsolute"/> is the current location, or contains it.
    /// </summary>
    /// <param name="currentUri">The current absolute location.</param>
    /// <param name="hrefAbsolute">The target, already resolved to an absolute URI.</param>
    /// <param name="match">Exact, or prefix.</param>
    /// <remarks>
    /// The query and fragment are dropped from the current location unless the target itself
    /// carries one. Otherwise <c>/orders</c> would stop matching the moment the page added
    /// <c>?page=2</c> - and a paged table deselecting its own nav item is a bug nobody attributes
    /// to the nav.
    /// </remarks>
    public static bool IsActive(string currentUri, string hrefAbsolute, NavLinkMatch match)
    {
        var current = hrefAbsolute.AsSpan().IndexOfAny('?', '#') >= 0
            ? currentUri
            : TrimQueryAndFragment(currentUri);

        if (EqualsIgnoringTrailingSlash(current, hrefAbsolute))
        {
            return true;
        }

        return match == NavLinkMatch.Prefix && IsStrictlyPrefixWithSeparator(current, hrefAbsolute);
    }

    /// <summary>
    /// The match mode to use when the caller named none: exact for a target that is the
    /// application root, prefix for anything else.
    /// </summary>
    /// <remarks>
    /// A root link defaulting to prefix matches every page in the application, which is how a
    /// hand-written nav ends up with two items highlighted at once. Nobody wants that, so the root
    /// is the one place the default flips.
    /// </remarks>
    public static NavLinkMatch DefaultMatch(string hrefAbsolute, string baseUri) =>
        EqualsIgnoringTrailingSlash(hrefAbsolute, baseUri) ? NavLinkMatch.All : NavLinkMatch.Prefix;

    /// <summary>The URI up to, but not including, any query string or fragment.</summary>
    public static string TrimQueryAndFragment(string uri)
    {
        var cut = uri.AsSpan().IndexOfAny('?', '#');

        return cut < 0 ? uri : uri[..cut];
    }

    /// <summary>
    /// Compares two absolute URIs, treating a trailing slash as insignificant.
    /// </summary>
    /// <remarks>
    /// <c>NavigationManager.BaseUri</c> always ends in a slash and <c>ToAbsoluteUri("")</c> does
    /// not, so the application root would never match itself without this.
    /// </remarks>
    public static bool EqualsIgnoringTrailingSlash(string left, string right) =>
        string.Equals(TrimEndSlash(left), TrimEndSlash(right), StringComparison.OrdinalIgnoreCase);

    private static string TrimEndSlash(string value) =>
        value.Length > 1 && value[^1] == '/' ? value[..^1] : value;

    /// <summary>
    /// Whether <paramref name="value"/> continues <paramref name="prefix"/> at a path boundary.
    /// </summary>
    /// <remarks>
    /// The separator test is what stops <c>/order</c> matching <c>/orders</c>. A plain
    /// <c>StartsWith</c> highlights the wrong item in any nav whose routes share a stem, which is
    /// most of them.
    /// </remarks>
    private static bool IsStrictlyPrefixWithSeparator(string value, string prefix)
    {
        var trimmed = TrimEndSlash(prefix);

        if (value.Length <= trimmed.Length
            || !value.StartsWith(trimmed, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return value[trimmed.Length] is '/' or '?' or '#';
    }
}
