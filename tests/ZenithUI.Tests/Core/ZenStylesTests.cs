namespace ZenithUI.Tests.Core;

public class ZenStylesTests
{
    private static readonly ZenIntent[] AllIntents = Enum.GetValues<ZenIntent>();
    private static readonly ZenVariant[] AllVariants = Enum.GetValues<ZenVariant>();

    // ---- Focus treatment --------------------------------------------------------------------

    [Fact]
    public void InputControls_SignalFocusOnTheirOwnBorder()
    {
        // Text fields recolour the border they already have rather than drawing an outline outside
        // it. An outline around an existing border shows as two concentric lines, and in a dense
        // form makes adjacent fields look like they are colliding.
        ZenStyles.InputBase.ShouldContain("zen-focus-border");
        ZenStyles.InputBase.Split(' ').ShouldNotContain("zen-focus");
    }

    [Fact]
    public void CompositeInputs_KeyOffFocusWithin() =>
        // The bordered wrapper never receives focus itself; its descendant input does.
        ZenStyles.InputWrapperBase.ShouldContain("zen-focus-border-within");

    [Fact]
    public void LinkedCards_UseTheSameBorderEmphasis()
    {
        // A linked card and a focused field say the same thing - "this is what you are about to
        // act on" - so they say it the same way. Asserted on the rendered component rather than a
        // constant, because the class is applied conditionally on Href.
        using var ctx = new BunitContext();
        ctx.Renderer.SetRendererInfo(new RendererInfo("Server", isInteractive: true));

        var linked = ctx.Render<ZenCard>(p => p.Add(x => x.Href, "/detail"));
        var plain = ctx.Render<ZenCard>();

        linked.Find("a").ClassList.ShouldContain("zen-interactive-border");
        plain.Find("article").ClassList.ShouldNotContain("zen-interactive-border");
    }

    [Fact]
    public void Buttons_KeepTheRing() =>
        // Buttons have no resting border to recolour, so the ring stays the right signal there.
        ZenStyles.InteractiveBase.Split(' ').ShouldContain("zen-focus");

    [Fact]
    public void InvalidInputs_AreMarkedBeforeTheyAreFocused() =>
        // A failing field must read as failing on sight, not only once the user tabs into it.
        ZenStyles.InputBase.ShouldContain("aria-invalid:border-danger");

    [Fact]
    public void InnerInput_DrawsNoBoxOfItsOwn()
    {
        // The wrapper owns every visual affordance. A background or border here would show as a
        // second box nested inside the first.
        ZenStyles.InputInnerBase.ShouldContain("border-0");
        ZenStyles.InputInnerBase.ShouldContain("bg-transparent");
        ZenStyles.InputInnerBase.ShouldContain("focus:outline-none");
    }

    // ---- Token roles ------------------------------------------------------------------------

    [Theory]
    [MemberData(nameof(IntentCases))]
    public void SoftVariant_UsesStrongForText_NeverThePlainFill(ZenIntent intent)
    {
        // -strong is the only token guaranteed readable on -soft. The plain intent token is a
        // fill; using it as a foreground is the mistake the quartet exists to prevent.
        var classes = ZenStyles.Soft(intent).Split(' ');

        if (intent == ZenIntent.Neutral)
        {
            return;
        }

        classes.ShouldContain($"text-{Name(intent)}-strong");
        classes.ShouldNotContain($"text-{Name(intent)}");
    }

    [Theory]
    [MemberData(nameof(IntentCases))]
    public void SolidVariant_PairsTheFillWithItsContentColour(ZenIntent intent)
    {
        if (intent == ZenIntent.Neutral)
        {
            return;
        }

        var classes = ZenStyles.Solid(intent).Split(' ');

        classes.ShouldContain($"bg-{Name(intent)}");
        classes.ShouldContain($"text-{Name(intent)}-content");
    }

    [Theory]
    [MemberData(nameof(IntentCases))]
    public void Foreground_IsAlwaysTheStrongToken(ZenIntent intent)
    {
        if (intent == ZenIntent.Neutral)
        {
            return;
        }

        ZenStyles.Foreground(intent).ShouldBe($"text-{Name(intent)}-strong");
    }

    // ---- Interactive vs static --------------------------------------------------------------

    [Theory]
    [MemberData(nameof(IntentVariantCases))]
    public void NonInteractive_HasNoHoverState_InAnyCombination(ZenIntent intent, ZenVariant variant) =>
        // Regression: the flag originally only suppressed the hover on Soft, so a Solid or Outline
        // badge still lit up under the pointer and claimed to be clickable.
        ZenStyles.Variant(intent, variant, interactive: false)
            .Split(' ')
            .ShouldNotContain(c => c.StartsWith("hover:", StringComparison.Ordinal));

    [Theory]
    [MemberData(nameof(IntentVariantCases))]
    public void Interactive_HasAHoverState_InEveryCombination(ZenIntent intent, ZenVariant variant) =>
        ZenStyles.Variant(intent, variant, interactive: true)
            .Split(' ')
            .ShouldContain(c => c.StartsWith("hover:", StringComparison.Ordinal));

    // ---- Scanner compatibility --------------------------------------------------------------

    [Theory]
    [MemberData(nameof(IntentVariantCases))]
    public void EveryClassIsALiteral_NotAnInterpolation(ZenIntent intent, ZenVariant variant)
    {
        // Tailwind finds classes as literal text in source files, so a runtime-assembled name such
        // as $"bg-{intent}" would compile fine here and simply never exist in the stylesheet. A
        // stray brace is the visible symptom of that mistake.
        var classes = ZenStyles.Variant(intent, variant);

        classes.ShouldNotContain("{");
        classes.ShouldNotContain("}");
        classes.Trim().ShouldNotBeEmpty();
    }

    public static TheoryData<ZenIntent> IntentCases()
    {
        var data = new TheoryData<ZenIntent>();

        foreach (var intent in AllIntents)
        {
            data.Add(intent);
        }

        return data;
    }

    public static TheoryData<ZenIntent, ZenVariant> IntentVariantCases()
    {
        var data = new TheoryData<ZenIntent, ZenVariant>();

        foreach (var intent in AllIntents)
        {
            foreach (var variant in AllVariants)
            {
                data.Add(intent, variant);
            }
        }

        return data;
    }

    private static string Name(ZenIntent intent) => intent.ToString().ToLowerInvariant();
}
