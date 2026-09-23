namespace ZenithUI;

/// <summary>
/// The element ids <c>ZenAppShell</c> hands to the chrome rendered inside it, so that a menu
/// button in the app bar and the side nav it reveals can be wired together without either
/// component knowing where in the tree the other sits.
/// </summary>
/// <param name="NavId">
/// The side nav's element id. The app bar's menu button points its <c>popovertarget</c> at this.
/// </param>
/// <param name="MainId">
/// The <c>&lt;main&gt;</c> element's id, which the shell's skip link targets.
/// </param>
/// <remarks>
/// <para>
/// <b>There is no open/closed state here, and that is the point.</b> The drawer is a
/// <c>popover</c>, so the browser owns whether it is showing: a <c>&lt;button
/// popovertarget&gt;</c> toggles it, Escape and a click outside dismiss it, and focus returns to
/// the button afterwards - all without a line of JavaScript and without either component being
/// interactive.
/// </para>
/// <para>
/// Sharing Blazor state instead would have made the drawer unusable in the place shells actually
/// live. A layout cannot be interactive, because <c>Body</c> is a <see cref="RenderFragment"/> and
/// a render fragment cannot cross a render-mode boundary; so a shell that toggled its nav through
/// a cascaded <c>bool</c> would have worked in the demo page that showed it off and nowhere else.
/// An id crosses that boundary happily, because <c>popovertarget</c> is resolved by the browser
/// against the whole document rather than by Blazor against a render tree.
/// </para>
/// </remarks>
public sealed record ZenShellContext(string NavId, string MainId);

/// <summary>
/// What a <c>ZenNavMenu</c> tells the <c>ZenNavLink</c>s inside it about how they are being laid
/// out.
/// </summary>
/// <param name="Orientation">The axis the menu stacks its links along.</param>
/// <param name="InList">
/// Whether the menu wrapped its content in a <c>&lt;ul&gt;</c>, in which case each link renders
/// its own <c>&lt;li&gt;</c>. A link used on its own outside a menu renders the anchor alone - an
/// <c>&lt;li&gt;</c> with no list around it is invalid HTML and browsers recover from it
/// inconsistently.
/// </param>
public sealed record ZenNavContext(ZenNavOrientation Orientation, bool InList);
