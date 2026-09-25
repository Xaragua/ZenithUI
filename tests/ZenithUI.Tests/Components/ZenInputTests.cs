using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Components.Forms;

namespace ZenithUI.Tests.Components;

/// <summary>
/// The text-entry controls. The binding path gets the most attention here, because
/// <c>ZenInputBase</c> reimplements machinery the framework normally provides and a silent
/// difference from <c>InputBase</c> would surface as a validator that mysteriously ignores a field.
/// </summary>
public class ZenInputTests : BunitContext
{
    public ZenInputTests() =>
        Renderer.SetRendererInfo(new RendererInfo("Server", isInteractive: true));

    private sealed class Model
    {
        [Required(ErrorMessage = "Name is required.")]
        public string? Name { get; set; }

        [Range(1, 10, ErrorMessage = "Pick 1 to 10.")]
        public int Quantity { get; set; } = 5;

        public decimal? Amount { get; set; }
    }

    // ---- Standalone use, with no EditForm ---------------------------------------------------

    [Fact]
    public void RendersWithoutAnEditForm()
    {
        // The entire reason ZenInputBase does not derive from InputBase<T>: that class throws
        // without a cascading EditContext, which would make a toolbar filter or a table-cell
        // stepper impossible without wrapping each in a dummy form.
        var act = () => Render<ZenTextInput>(p => p.Add(x => x.Value, "hello"));

        act.ShouldNotThrow();
    }

    [Fact]
    public void BindsWithoutAnEditForm()
    {
        var value = "before";

        var cut = Render<ZenTextInput>(p => p
            .Add(x => x.Value, value)
            .Add(x => x.ValueChanged, v => value = v ?? string.Empty));

        cut.Find("input").Change("after");

        value.ShouldBe("after");
    }

    [Fact]
    public void DoesNotRaiseValueChanged_WhenTheValueIsUnchanged()
    {
        // Re-raising on an identical value re-runs validation and re-renders for nothing, and in a
        // parent that reacts to the callback it can loop.
        var raised = 0;

        var cut = Render<ZenTextInput>(p => p
            .Add(x => x.Value, "same")
            .Add(x => x.ValueChanged, _ => raised++));

        cut.Find("input").Change("same");

        raised.ShouldBe(0);
    }

    // ---- Inside an EditForm -----------------------------------------------------------------

    private IRenderedComponent<EditForm> RenderForm(Model model, RenderFragment<Model> fields) =>
        Render<EditForm>(p => p
            .Add(x => x.Model, model)
            .Add(x => x.ChildContent, _ => builder =>
            {
                builder.OpenComponent<DataAnnotationsValidator>(0);
                builder.CloseComponent();
                builder.AddContent(1, fields(model));
            }));

    [Fact]
    public void ShowsAValidationMessage_FromDataAnnotations()
    {
        var model = new Model();

        var cut = RenderForm(model, m => builder =>
        {
            builder.OpenComponent<ZenTextInput>(0);
            builder.AddComponentParameter(1, nameof(ZenTextInput.Value), m.Name);
            builder.AddComponentParameter(2, nameof(ZenTextInput.ValueChanged),
                EventCallback.Factory.Create<string?>(this, v => m.Name = v));
            builder.AddComponentParameter(3, nameof(ZenTextInput.ValueExpression),
                (Expression<Func<string?>>)(() => m.Name));
            builder.AddComponentParameter(4, nameof(ZenTextInput.Label), "Name");
            builder.CloseComponent();
        });

        cut.Find("form").Submit();

        cut.Find("[role=alert]").TextContent.ShouldContain("Name is required.");
        cut.Find("input").GetAttribute("aria-invalid").ShouldBe("true");
    }

    [Fact]
    public void ClearsTheValidationMessage_WhenTheFieldBecomesValid()
    {
        var model = new Model();

        var cut = RenderForm(model, m => builder =>
        {
            builder.OpenComponent<ZenTextInput>(0);
            builder.AddComponentParameter(1, nameof(ZenTextInput.Value), m.Name);
            builder.AddComponentParameter(2, nameof(ZenTextInput.ValueChanged),
                EventCallback.Factory.Create<string?>(this, v => m.Name = v));
            builder.AddComponentParameter(3, nameof(ZenTextInput.ValueExpression),
                (Expression<Func<string?>>)(() => m.Name));
            builder.CloseComponent();
        });

        cut.Find("form").Submit();
        cut.FindAll("[role=alert]").ShouldNotBeEmpty();

        cut.Find("input").Change("Ada");

        // NotifyFieldChanged is what re-runs validation for the field. Without it the message
        // would linger after the user fixed the problem.
        cut.FindAll("[role=alert]").ShouldBeEmpty();
    }

    // ---- Affixes ----------------------------------------------------------------------------

    [Fact]
    public void PlainInput_CarriesTheBorderItself() =>
        Render<ZenTextInput>().Find("input").ClassList.ShouldContain("zen-focus-border");

    [Fact]
    public void InputWithAnAffix_MovesTheBorderToAWrapper()
    {
        // Otherwise the icon sits outside the field, or inside a second nested box.
        var cut = Render<ZenTextInput>(p => p
            .Add(x => x.LeadingContent, (RenderFragment)(b => b.AddContent(0, "@"))));

        cut.Find("div.zen-focus-border-within").ShouldNotBeNull();
        cut.Find("input").ClassList.ShouldNotContain("zen-focus-border");
        cut.Find("input").ClassList.ShouldContain("bg-transparent");
    }

    // ---- Number -----------------------------------------------------------------------------

    [Fact]
    public void NumberInput_ParsesToItsGenericType()
    {
        var value = 0;

        var cut = Render<ZenNumberInput<int>>(p => p
            .Add(x => x.Value, value)
            .Add(x => x.ValueChanged, v => value = v));

        cut.Find("input").Change("42");

        value.ShouldBe(42);
    }

    [Fact]
    public void NumberInput_ReportsAnUnparseableValue_RatherThanSwallowingIt()
    {
        // Treating "12e" as empty loses the user's input and tells them nothing.
        var cut = Render<ZenNumberInput<int>>(p => p.Add(x => x.Label, "Quantity"));

        cut.Find("input").Change("not-a-number");

        cut.Find("[role=alert]").TextContent.ShouldContain("not valid");
    }

    [Fact]
    public void NullableNumberInput_TreatsEmptyAsNoValue()
    {
        // An empty string on a Nullable<T> means "cleared", not "malformed".
        int? value = 7;

        var cut = Render<ZenNumberInput<int?>>(p => p
            .Add(x => x.Value, value)
            .Add(x => x.ValueChanged, v => value = v));

        cut.Find("input").Change(string.Empty);

        value.ShouldBeNull();
        cut.FindAll("[role=alert]").ShouldBeEmpty();
    }

    [Theory]
    [InlineData(true, "text")]
    [InlineData(false, "numeric")]
    public void NumberInput_PicksAnInputModeThatCanTypeTheValue(bool allowNegative, string expected) =>
        // A type="number" on mobile often offers a keypad with no minus sign, which makes a
        // negative value literally untypeable unless inputmode says otherwise.
        Render<ZenNumberInput<int>>(p => p.Add(x => x.AllowNegative, allowNegative))
            .Find("input").GetAttribute("inputmode").ShouldBe(expected);

    // ---- Currency ---------------------------------------------------------------------------

    [Theory]
    [InlineData("1234.56", 1234.56)]
    [InlineData("$1,234.56", 1234.56)]
    [InlineData("  1,234.56  ", 1234.56)]
    [InlineData("(1,234.56)", -1234.56)]
    [InlineData("-99", -99)]
    public void CurrencyInput_ParsesTheShapesMoneyArrivesIn(string text, decimal expected)
    {
        // Pasting a formatted amount from a spreadsheet or an invoice is the normal case, not an
        // edge case. Parentheses are how accounting exports write a negative.
        ZenCurrencyInput.TryParseAmount(text, CultureInfo.GetCultureInfo("en-US"), out var amount)
            .ShouldBeTrue();

        amount.ShouldBe(expected);
    }

    [Fact]
    public void CurrencyInput_RejectsNonsense() =>
        ZenCurrencyInput.TryParseAmount("abc", CultureInfo.InvariantCulture, out _).ShouldBeFalse();

    [Fact]
    public void CurrencyInput_FormatsWhenNotFocused()
    {
        var cut = Render<ZenCurrencyInput>(p => p
            .Add(x => x.Value, 1234.5m)
            .Add(x => x.Culture, CultureInfo.GetCultureInfo("en-US")));

        cut.Find("input").GetAttribute("value").ShouldBe("1,234.50");
    }

    [Fact]
    public void CurrencyInput_ShowsBareDigitsWhileFocused()
    {
        // Nothing is rewritten under the caret while the user types - which is the whole reason
        // this control does not mask.
        JSInterop.SetupModule("./_content/ZenithUI/js/zen-dom.js").SetupVoid("selectIfFocused", _ => true);

        var cut = Render<ZenCurrencyInput>(p => p
            .Add(x => x.Value, 1234.5m)
            .Add(x => x.Culture, CultureInfo.GetCultureInfo("en-US")));

        cut.Find("input").Focus();

        cut.Find("input").GetAttribute("value").ShouldBe("1234.5");
    }

    [Fact]
    public void CurrencyInput_ReselectsTheBareDigitsAfterFocus()
    {
        // Swapping the text on focus collapses the selection the browser made on Tab, so typing
        // appended to the old amount instead of replacing it. The new text is selected again, and
        // the expected value is passed so a keystroke that beat the render is left alone.
        var module = JSInterop.SetupModule("./_content/ZenithUI/js/zen-dom.js");
        module.SetupVoid("selectIfFocused", _ => true).SetVoidResult();

        var cut = Render<ZenCurrencyInput>(p => p
            .Add(x => x.Value, 1234.5m)
            .Add(x => x.Culture, CultureInfo.GetCultureInfo("en-US")));

        cut.Find("input").Focus();

        var call = module.VerifyInvoke("selectIfFocused");
        call.Arguments[0].ShouldBe(cut.Find("input").Id);
        call.Arguments[1].ShouldBe("1234.5");
    }

    // ---- Date -------------------------------------------------------------------------------

    [Fact]
    public void DateInput_UsesIsoRegardlessOfCulture()
    {
        // The native control's value is always ISO. Formatting it with the current culture is the
        // classic bug: fine in en-US, broken everywhere the separator differs.
        var previous = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");

        try
        {
            var cut = Render<ZenDateInput<DateOnly>>(p => p
                .Add(x => x.Value, new DateOnly(2026, 3, 9)));

            cut.Find("input").GetAttribute("value").ShouldBe("2026-03-09");
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }

    [Fact]
    public void DateInput_ParsesIsoIntoDateOnly()
    {
        DateOnly value = default;

        var cut = Render<ZenDateInput<DateOnly>>(p => p
            .Add(x => x.Value, value)
            .Add(x => x.ValueChanged, v => value = v));

        cut.Find("input").Change("2026-03-09");

        value.ShouldBe(new DateOnly(2026, 3, 9));
    }

    [Fact]
    public void DateInput_WithTime_RendersDateTimeLocal() =>
        Render<ZenDateInput<DateTime?>>(p => p.Add(x => x.IncludeTime, true))
            .Find("input").GetAttribute("type").ShouldBe("datetime-local");

    // ---- Search -----------------------------------------------------------------------------

    [Fact]
    public void SearchInput_IsLiveByDefault()
    {
        // A search box that only reacts on blur is not a search box.
        var cut = Render<ZenSearchInput>();

        cut.Instance.Immediate.ShouldBeTrue();
        cut.Instance.DebounceMilliseconds.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void SearchInput_ShowsAClearButtonOnlyWhenThereIsSomethingToClear()
    {
        Render<ZenSearchInput>().FindAll("button").ShouldBeEmpty();

        Render<ZenSearchInput>(p => p.Add(x => x.Value, "blazor"))
            .Find("button").GetAttribute("aria-label").ShouldBe("Clear search");
    }

    [Fact]
    public void SearchInput_ClearButtonEmptiesTheValue()
    {
        var value = "blazor";

        var cut = Render<ZenSearchInput>(p => p
            .Add(x => x.Value, value)
            .Add(x => x.ValueChanged, v => value = v ?? string.Empty));

        cut.Find("button").Click();

        value.ShouldBe(string.Empty);
    }

    [Fact]
    public void SearchInput_ClearButtonCannotSubmitAnEnclosingForm() =>
        Render<ZenSearchInput>(p => p.Add(x => x.Value, "x"))
            .Find("button").GetAttribute("type").ShouldBe("button");

    // ---- Text area --------------------------------------------------------------------------

    [Fact]
    public void TextArea_AutoGrowsWithoutJavaScript() =>
        // field-sizing rather than writing scrollHeight back on every keystroke: the JS approach
        // does nothing during prerender, so a textarea with content renders at one row until the
        // circuit connects.
        Render<ZenTextArea>().Find("textarea").ClassList.ShouldContain("field-sizing-content");

    // ---- Focus ------------------------------------------------------------------------------

    [Fact]
    public void AutoFocus_FocusesTheInputOnFirstRender()
    {
        // The order-entry case: a line added to a grid, whose field has no reference to call
        // FocusAsync on until the render that creates it.
        var module = JSInterop.SetupModule("./_content/ZenithUI/js/zen-dom.js");
        module.SetupVoid("focusElement", _ => true).SetVoidResult();

        var cut = Render<ZenTextInput>(p => p.Add(x => x.AutoFocus, true));

        module.VerifyInvoke("focusElement").Arguments[0].ShouldBe(cut.Find("input").Id);
    }

    [Fact]
    public void AutoFocus_IsAppliedOnlyOnce()
    {
        // Left on, it must not pull focus back every time the page re-renders.
        var module = JSInterop.SetupModule("./_content/ZenithUI/js/zen-dom.js");
        module.SetupVoid("focusElement", _ => true).SetVoidResult();

        var cut = Render<ZenTextInput>(p => p.Add(x => x.AutoFocus, true));
        cut.Render(p => p.Add(x => x.Value, "changed"));

        module.VerifyInvoke("focusElement", calledTimes: 1);
    }

    [Fact]
    public void AutoFocus_ReachesControlsThatOverrideTheRenderHook()
    {
        // NumberInput has its own OnAfterRenderAsync. Forgetting to call the base there would
        // silently disable AutoFocus for that one control.
        var module = JSInterop.SetupModule("./_content/ZenithUI/js/zen-dom.js");
        module.SetupVoid("focusElement", _ => true).SetVoidResult();

        var cut = Render<ZenNumberInput<int>>(p => p.Add(x => x.AutoFocus, true));

        module.VerifyInvoke("focusElement").Arguments[0].ShouldBe(cut.Find("input").Id);
    }

    [Fact]
    public async Task FocusAsync_CanSelectTheText()
    {
        var module = JSInterop.SetupModule("./_content/ZenithUI/js/zen-dom.js");
        module.SetupVoid("focusElement", _ => true).SetVoidResult();

        var cut = Render<ZenTextInput>(p => p.Add(x => x.Value, "hello"));
        await cut.InvokeAsync(() => cut.Instance.FocusAsync(selectAll: true));

        var call = module.VerifyInvoke("focusElement");
        call.Arguments[0].ShouldBe(cut.Find("input").Id);
        call.Arguments[1].ShouldBe(true);
    }

    [Fact]
    public void TextArea_AutoGrowKeepsItsRowsAsAFloor()
    {
        // field-sizing: content ignores rows, so without a min-height an empty auto-grow field
        // rendered one line tall whatever Rows said, and only grew once the user pressed Enter.
        var cut = Render<ZenTextArea>(p => p.Add(x => x.Rows, 4));

        cut.Find("textarea").GetAttribute("style")!.ShouldContain("min-height:calc(4lh");
    }

    [Fact]
    public void TextArea_FixedHeightLeavesSizingToRows()
    {
        var cut = Render<ZenTextArea>(p => p.Add(x => x.AutoGrow, false));

        cut.Find("textarea").HasAttribute("style").ShouldBeFalse();
    }

    [Fact]
    public void TextArea_CounterRequiresALimit()
    {
        var act = () => Render<ZenTextArea>(p => p.Add(x => x.ShowCounter, true));

        act.ShouldThrow<InvalidOperationException>().Message.ShouldContain(nameof(ZenTextArea.MaxLength));
    }

    // ---- Static SSR -------------------------------------------------------------------------

    [Fact]
    public void StaticSsr_RendersACompleteField()
    {
        Renderer.SetRendererInfo(new RendererInfo("Static", isInteractive: false));

        var cut = Render<ZenTextInput>(p => p
            .Add(x => x.Label, "Email")
            .Add(x => x.HelpText, "We never share it.")
            .Add(x => x.Value, "ada@example.com"));

        cut.Find("label").GetAttribute("for").ShouldBe(cut.Find("input").GetAttribute("id"));
        cut.Find("input").GetAttribute("value").ShouldBe("ada@example.com");
        cut.Find("input").GetAttribute("aria-describedby").ShouldNotBeNullOrEmpty();
    }
}
