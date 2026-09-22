using System.Text.Json.Serialization;

namespace ZenithUI.Core;

/// <summary>
/// The placement options handed to <c>zen-popover.js</c>.
/// </summary>
/// <remarks>
/// A record rather than an anonymous object so the property names are declared once and the
/// JSON contract with the module is visible in one place. Names are camelCase to match what the
/// module reads.
/// </remarks>
/// <param name="Side">One of <c>top</c>, <c>bottom</c>, <c>start</c>, <c>end</c>.</param>
/// <param name="Align">One of <c>start</c>, <c>center</c>, <c>end</c>.</param>
/// <param name="Offset">Gap between anchor and panel, in CSS pixels.</param>
/// <param name="Flip">Whether the panel may move to the opposite side to stay in view.</param>
/// <param name="Shift">Whether the panel may slide along its axis to stay in view.</param>
/// <param name="MatchWidth">Whether the panel takes the anchor's width as its minimum.</param>
public sealed record ZenPopoverOptions(
    [property: JsonPropertyName("side")] string Side,
    [property: JsonPropertyName("align")] string Align,
    [property: JsonPropertyName("offset")] int Offset,
    [property: JsonPropertyName("flip")] bool Flip,
    [property: JsonPropertyName("shift")] bool Shift,
    [property: JsonPropertyName("matchWidth")] bool MatchWidth);

/// <summary>Decomposes a <see cref="ZenPlacement"/> into the side and alignment JS expects.</summary>
public static class ZenPlacementExtensions
{
    /// <summary>The side of the anchor the panel sits on.</summary>
    /// <param name="placement">The placement to decompose.</param>
    /// <returns><c>top</c>, <c>bottom</c>, <c>start</c> or <c>end</c>.</returns>
    public static string Side(this ZenPlacement placement) => placement switch
    {
        ZenPlacement.TopStart or ZenPlacement.Top or ZenPlacement.TopEnd => "top",
        ZenPlacement.StartTop or ZenPlacement.Start or ZenPlacement.StartBottom => "start",
        ZenPlacement.EndTop or ZenPlacement.End or ZenPlacement.EndBottom => "end",
        _ => "bottom",
    };

    /// <summary>How the panel is aligned along that side.</summary>
    /// <param name="placement">The placement to decompose.</param>
    /// <returns><c>start</c>, <c>center</c> or <c>end</c>.</returns>
    public static string Align(this ZenPlacement placement) => placement switch
    {
        ZenPlacement.BottomStart or ZenPlacement.TopStart
            or ZenPlacement.StartTop or ZenPlacement.EndTop => "start",
        ZenPlacement.BottomEnd or ZenPlacement.TopEnd
            or ZenPlacement.StartBottom or ZenPlacement.EndBottom => "end",
        _ => "center",
    };
}
