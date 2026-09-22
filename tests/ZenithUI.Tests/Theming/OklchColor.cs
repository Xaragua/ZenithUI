using System.Globalization;
using System.Text.RegularExpressions;

namespace ZenithUI.Tests.Theming;

/// <summary>
/// An OKLCH colour, with the conversions needed to compute WCAG contrast.
/// </summary>
/// <param name="L">Perceptual lightness, 0-1.</param>
/// <param name="C">Chroma.</param>
/// <param name="H">Hue in degrees.</param>
/// <param name="Alpha">Alpha, 0-1.</param>
public readonly partial record struct OklchColor(double L, double C, double H, double Alpha = 1.0)
{
    /// <summary>
    /// Matches <c>oklch(L C H)</c> and <c>oklch(L C H / A)</c>, accepting both the fractional and
    /// percentage spellings of L that CSS allows.
    /// </summary>
    [GeneratedRegex(
        @"oklch\(\s*(?<l>[\d.]+)(?<lpct>%?)\s+(?<c>[\d.]+)\s+(?<h>[\d.]+)(?:\s*/\s*(?<a>[\d.]+))?\s*\)",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex OklchPattern { get; }

    /// <summary>Parses a CSS <c>oklch(...)</c> function.</summary>
    /// <param name="css">The colour value, for example <c>oklch(0.54 0.19 264)</c>.</param>
    /// <returns>The parsed colour, or <see langword="null"/> when <paramref name="css"/> is not OKLCH.</returns>
    public static OklchColor? TryParse(string css)
    {
        var match = OklchPattern.Match(css);

        if (!match.Success)
        {
            return null;
        }

        var l = double.Parse(match.Groups["l"].Value, CultureInfo.InvariantCulture);

        if (match.Groups["lpct"].Value == "%")
        {
            l /= 100.0;
        }

        var c = double.Parse(match.Groups["c"].Value, CultureInfo.InvariantCulture);
        var h = double.Parse(match.Groups["h"].Value, CultureInfo.InvariantCulture);

        var alpha = match.Groups["a"].Success
            ? double.Parse(match.Groups["a"].Value, CultureInfo.InvariantCulture)
            : 1.0;

        return new OklchColor(l, c, h, alpha);
    }

    /// <summary>
    /// Converts to linear-light sRGB, clamped into gamut.
    /// </summary>
    /// <remarks>
    /// Oklab -> LMS -> linear sRGB, using Bjorn Ottosson's published matrices. The result is
    /// clamped rather than gamut-mapped: every token in the palette is inside sRGB by design, and
    /// a clamp that silently changes a colour is exactly what the out-of-gamut test checks for.
    /// </remarks>
    public (double R, double G, double B) ToLinearRgb()
    {
        var hueRadians = H * Math.PI / 180.0;
        var a = C * Math.Cos(hueRadians);
        var b = C * Math.Sin(hueRadians);

        var lRoot = L + (0.3963377774 * a) + (0.2158037573 * b);
        var mRoot = L - (0.1055613458 * a) - (0.0638541728 * b);
        var sRoot = L - (0.0894841775 * a) - (1.2914855480 * b);

        var lms = (L: lRoot * lRoot * lRoot, M: mRoot * mRoot * mRoot, S: sRoot * sRoot * sRoot);

        var r = (+4.0767416621 * lms.L) - (3.3077115913 * lms.M) + (0.2309699292 * lms.S);
        var g = (-1.2684380046 * lms.L) + (2.6097574011 * lms.M) - (0.3413193965 * lms.S);
        var bl = (-0.0041960863 * lms.L) - (0.7034186147 * lms.M) + (1.7076147010 * lms.S);

        return (Math.Clamp(r, 0, 1), Math.Clamp(g, 0, 1), Math.Clamp(bl, 0, 1));
    }

    /// <summary>
    /// <see langword="true"/> when the colour falls outside the sRGB gamut and would be clipped
    /// by the browser, changing its appearance from what the token declares.
    /// </summary>
    public bool IsOutOfGamut()
    {
        var hueRadians = H * Math.PI / 180.0;
        var a = C * Math.Cos(hueRadians);
        var b = C * Math.Sin(hueRadians);

        var lRoot = L + (0.3963377774 * a) + (0.2158037573 * b);
        var mRoot = L - (0.1055613458 * a) - (0.0638541728 * b);
        var sRoot = L - (0.0894841775 * a) - (1.2914855480 * b);

        var lms = (L: lRoot * lRoot * lRoot, M: mRoot * mRoot * mRoot, S: sRoot * sRoot * sRoot);

        var r = (+4.0767416621 * lms.L) - (3.3077115913 * lms.M) + (0.2309699292 * lms.S);
        var g = (-1.2684380046 * lms.L) + (2.6097574011 * lms.M) - (0.3413193965 * lms.S);
        var bl = (-0.0041960863 * lms.L) - (0.7034186147 * lms.M) + (1.7076147010 * lms.S);

        // A hair of tolerance: the matrices are published to ten digits, so an in-gamut colour can
        // land a rounding error outside [0,1].
        const double Tolerance = 0.001;

        return r < -Tolerance || r > 1 + Tolerance
            || g < -Tolerance || g > 1 + Tolerance
            || bl < -Tolerance || bl > 1 + Tolerance;
    }

    /// <summary>
    /// The largest chroma that stays inside sRGB at this colour's lightness and hue.
    /// </summary>
    /// <remarks>
    /// Found by bisection rather than solved analytically: the sRGB gamut boundary in OKLCH has no
    /// closed form, and 24 iterations resolve it far more precisely than the three decimal places
    /// a token is written to. Reported in failure messages so a palette fix is a single edit
    /// rather than a guess-and-rerun loop.
    /// </remarks>
    public double MaxInGamutChroma()
    {
        var low = 0.0;
        var high = 0.5;

        for (var i = 0; i < 24; i++)
        {
            var mid = (low + high) / 2;

            if (new OklchColor(L, mid, H, Alpha).IsOutOfGamut())
            {
                high = mid;
            }
            else
            {
                low = mid;
            }
        }

        return low;
    }

    /// <summary>
    /// WCAG 2.2 relative luminance.
    /// </summary>
    /// <remarks>
    /// The sRGB components are already linear-light here, so the gamma-expansion step in the WCAG
    /// formula has effectively been done by the Oklab conversion; only the luminance weighting
    /// remains.
    /// </remarks>
    public double RelativeLuminance()
    {
        var (r, g, b) = ToLinearRgb();
        return (0.2126 * r) + (0.7152 * g) + (0.0722 * b);
    }

    /// <summary>WCAG 2.2 contrast ratio between two colours, from 1:1 to 21:1.</summary>
    public static double ContrastRatio(OklchColor a, OklchColor b)
    {
        var la = a.RelativeLuminance();
        var lb = b.RelativeLuminance();

        var lighter = Math.Max(la, lb);
        var darker = Math.Min(la, lb);

        return (lighter + 0.05) / (darker + 0.05);
    }
}
