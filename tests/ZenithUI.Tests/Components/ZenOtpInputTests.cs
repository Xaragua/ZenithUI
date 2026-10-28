namespace ZenithUI.Tests.Components;

/// <summary>
/// ZenOtpInput: one real input drawn as boxes. These pin the attributes that make autofill, paste
/// and a screen reader work, and the cleaning of what is typed.
/// </summary>
public class ZenOtpInputTests : BunitContext
{
    public ZenOtpInputTests()
    {
        Renderer.SetRendererInfo(new RendererInfo("Server", isInteractive: true));
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    private string? _code;
    private readonly List<string> _completed = [];

    private IRenderedComponent<ZenOtpInput> Otp(Action<ComponentParameterCollectionBuilder<ZenOtpInput>>? extra = null) =>
        Render<ZenOtpInput>(p =>
        {
            p.Add(x => x.Value, _code)
                .Add(x => x.ValueChanged, v => _code = v)
                .Add(x => x.OnComplete, c => _completed.Add(c));

            extra?.Invoke(p);
        });

    private static List<string> Cells(IRenderedComponent<ZenOtpInput> cut) =>
        cut.FindAll("span[aria-hidden=true].font-mono").Select(c => c.TextContent).ToList();

    [Fact]
    public void IsOneInput_ForOneTimeCodes()
    {
        var cut = Otp();

        var inputs = cut.FindAll("input");
        inputs.Count.ShouldBe(1);

        var input = inputs[0];
        input.GetAttribute("autocomplete").ShouldBe("one-time-code");
        input.GetAttribute("inputmode").ShouldBe("numeric");
        input.GetAttribute("maxlength").ShouldBe("6");
    }

    [Fact]
    public void IsLabelled_ByDefault()
    {
        var cut = Otp();

        cut.Find($"label[for={cut.Find("input").Id}]").TextContent.ShouldContain("Verification code");
    }

    [Fact]
    public void DrawsOneBoxPerCharacter_HiddenFromScreenReaders() =>
        Cells(Otp(p => p.Add(x => x.Length, 4))).Count.ShouldBe(4);

    [Fact]
    public void APastedCode_FillsEveryBox_AndCompletes()
    {
        var cut = Otp();

        cut.Find("input").Input("123456");

        cut.WaitForAssertion(() => _code.ShouldBe("123456"));
        Cells(cut).ShouldBe(["1", "2", "3", "4", "5", "6"]);
        _completed.ShouldBe(["123456"]);
    }

    [Fact]
    public void Letters_AreDropped_WhenNumeric()
    {
        var cut = Otp();

        cut.Find("input").Input("12a");

        // A rejected character costs a second render, after a yield.

        cut.WaitForAssertion(() => _code.ShouldBe("12"));
        cut.Find("input").GetAttribute("value").ShouldBe("12");
    }

    [Fact]
    public void APasteWithSpacesOrDashes_IsCleaned()
    {
        var cut = Otp();

        cut.Find("input").Input("123 - 456 789");

        cut.WaitForAssertion(() => _code.ShouldBe("123456"));
    }

    [Fact]
    public void NonNumeric_KeepsLetters_AndTheKeyboardIsText()
    {
        var cut = Otp(p => p.Add(x => x.Numeric, false));

        cut.Find("input").GetAttribute("inputmode").ShouldBe("text");
        cut.Find("input").Input("AB12CD");

        _code.ShouldBe("AB12CD");
    }

    [Fact]
    public void OnComplete_FiresOnce_UntilTheCodeIsEditedAgain()
    {
        var cut = Otp();
        var input = cut.Find("input");

        input.Input("123456");
        cut.Find("input").Input("123456");
        _completed.Count.ShouldBe(1);

        cut.Find("input").Input("12345");
        cut.Find("input").Input("123457");
        _completed.ShouldBe(["123456", "123457"]);
    }

    [Fact]
    public void Mask_HidesTheCharacters()
    {
        _code = "12";

        Cells(Otp(p => p.Add(x => x.Mask, true))).Take(2).ShouldAllBe(c => c == "•");
    }

    [Fact]
    public void TheNextBox_TakesTheFocusTreatment()
    {
        _code = "12";
        var cut = Otp();

        var cells = cut.FindAll("span[aria-hidden=true].font-mono");
        cells[2].ClassList.ShouldContain("peer-focus:border-ring");
        cells[1].ClassList.ShouldNotContain("peer-focus:border-ring");
    }

    [Fact]
    public void Invalid_KeepsEveryBoxDanger_EvenFocused()
    {
        var cut = Otp(p => p.Add(x => x.ErrorMessage, "That code has expired."));

        var cells = cut.FindAll("span[aria-hidden=true].font-mono");
        cells.ShouldAllBe(c => c.ClassList.Contains("border-danger"));
        cells[0].ClassList.ShouldContain("peer-focus:border-danger");
        cut.Find("input").GetAttribute("aria-invalid").ShouldBe("true");
    }

    [Fact]
    public void SeparatorAfter_DrawsADash()
    {
        var cut = Otp(p => p.Add(x => x.SeparatorAfter, 3));

        cut.FindAll("span.w-3[aria-hidden=true]").Count.ShouldBe(1);
    }

    [Fact]
    public void AnOverlongValue_IsTrimmedForDisplay()
    {
        _code = "12345678";

        Otp().Find("input").GetAttribute("value").ShouldBe("123456");
    }
}
