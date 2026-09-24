using Microsoft.AspNetCore.Components.Rendering;

namespace ZenithUI;

/// <summary>
/// What the layout components share: turning <see cref="ZenLayoutElement"/> into a tag, and
/// writing a root element whose tag is only known at render time.
/// </summary>
/// <remarks>
/// Razor cannot take a tag name from a variable, so each layout component writes its root with the
/// render tree builder, as <c>ZenText</c> does. The classes stay in the components themselves, as
/// literals in switch arms where Tailwind's scanner can see them.
/// </remarks>
internal static class ZenLayout
{
    /// <summary>The tag name for a layout element.</summary>
    public static string TagName(ZenLayoutElement element) => element switch
    {
        ZenLayoutElement.Section => "section",
        ZenLayoutElement.Article => "article",
        ZenLayoutElement.Header => "header",
        ZenLayoutElement.Footer => "footer",
        ZenLayoutElement.Nav => "nav",
        ZenLayoutElement.Aside => "aside",
        ZenLayoutElement.Ul => "ul",
        ZenLayoutElement.Ol => "ol",
        ZenLayoutElement.Li => "li",
        _ => "div",
    };

    /// <summary>Writes the root element.</summary>
    /// <param name="builder">The component's render tree builder.</param>
    /// <param name="tagName">From <see cref="TagName"/>.</param>
    /// <param name="attributes">The component's unmatched attributes, without <c>class</c>.</param>
    /// <param name="className">The merged class string.</param>
    /// <param name="style">The merged inline style.</param>
    /// <param name="id">
    /// Only an id the caller chose. A layout box is an occasional <c>aria-labelledby</c> or anchor
    /// target, but a generated id that nothing points at is noise on every box of the page.
    /// </param>
    /// <param name="childContent">The items.</param>
    public static void RenderRoot(
        RenderTreeBuilder builder,
        string tagName,
        IReadOnlyDictionary<string, object>? attributes,
        string? className,
        string? style,
        string? id,
        RenderFragment? childContent)
    {
        builder.OpenElement(0, tagName);
        builder.AddMultipleAttributes(1, attributes);
        builder.AddAttribute(2, "class", className);
        builder.AddAttribute(3, "style", style);
        builder.AddAttribute(4, "id", id);
        builder.AddContent(5, childContent);
        builder.CloseElement();
    }
}
