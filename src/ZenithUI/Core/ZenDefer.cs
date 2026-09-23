using Microsoft.AspNetCore.Components.Rendering;

namespace ZenithUI.Core;

/// <summary>
/// Renders its content one step later in the render queue than the component that declares it.
/// </summary>
/// <remarks>
/// <para>
/// Infrastructure, not a control. It is public only because the Razor compiler resolves markup
/// elements to public component types; nothing in a page has a reason to write it.
/// </para>
/// <para>
/// It exists for one problem: a component whose markup depends on what its own child components
/// report. <c>ZenTable</c> is the case - its columns are child components that register
/// themselves, and a parent builds its entire render tree before any child has been instantiated,
/// so the table's first pass would see no columns at all.
/// </para>
/// <para>
/// The usual fix is to call <c>StateHasChanged</c> from <c>OnAfterRender</c> and render a second
/// time. That fails exactly where it matters most: under static SSR there is no second render, so
/// a server-rendered table would ship to the browser with an empty body. Rendering is also
/// happening twice on every pass to work around an ordering problem.
/// </para>
/// <para>
/// Deferring is ordering, not re-rendering. The renderer processes its queue first-in-first-out,
/// and a new child component's parameters are set - and therefore its <c>OnParametersSet</c> runs
/// - while the frame that introduces it is being diffed. So a parent that renders
/// <c>&lt;CascadingValue&gt;columns&lt;/CascadingValue&gt;</c> followed by
/// <c>&lt;ZenDefer&gt;the markup&lt;/ZenDefer&gt;</c> queues the cascade ahead of the defer; the
/// cascade's diff registers every column, and only then does the deferred content render. One
/// pass, correct in every render mode.
/// </para>
/// </remarks>
public sealed class ZenDefer : ComponentBase
{
    /// <summary>The content to render a step late.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <inheritdoc />
    protected override void BuildRenderTree(RenderTreeBuilder builder) =>
        builder.AddContent(0, ChildContent);
}
