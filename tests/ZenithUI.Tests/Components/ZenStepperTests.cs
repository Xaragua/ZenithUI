using AngleSharp.Dom;
using Microsoft.AspNetCore.Components.Forms;
using System.ComponentModel.DataAnnotations;

namespace ZenithUI.Tests.Components;

/// <summary>
/// ZenStepper. The ARIA group pins the decision not to be a tablist; the rest is the wizard
/// contract - what may be skipped, what must validate, and what happens when a step says no.
/// </summary>
public class ZenStepperTests : BunitContext
{
    public ZenStepperTests()
    {
        Renderer.SetRendererInfo(new RendererInfo("Server", isInteractive: true));
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    private sealed record StepSpec(
        string Title,
        bool Disabled = false,
        string? Error = null,
        bool Optional = false,
        EditContext? Form = null,
        Func<ZenStepLeavingArgs, Task>? OnLeaving = null);

    private int _active;
    private int _finished;

    private IRenderedComponent<ZenStepper> Stepper(
        StepSpec[] steps,
        Action<ComponentParameterCollectionBuilder<ZenStepper>>? extra = null) =>
        Render<ZenStepper>(p =>
        {
            p.Add(x => x.ActiveIndex, _active)
                .Add(x => x.ActiveIndexChanged, i => _active = i)
                .Add(x => x.OnFinish, () => _finished++)
                .Add(x => x.ChildContent, (RenderFragment)(builder =>
                {
                    var seq = 0;

                    foreach (var step in steps)
                    {
                        builder.OpenComponent<ZenStep>(seq++);
                        builder.AddComponentParameter(seq++, nameof(ZenStep.Title), step.Title);
                        builder.AddComponentParameter(seq++, nameof(ZenStep.Disabled), step.Disabled);
                        builder.AddComponentParameter(seq++, nameof(ZenStep.Error), step.Error);
                        builder.AddComponentParameter(seq++, nameof(ZenStep.Optional), step.Optional);
                        builder.AddComponentParameter(seq++, nameof(ZenStep.EditContext), step.Form);
                        builder.AddComponentParameter(seq++, nameof(ZenStep.OnLeaving), step.OnLeaving);
                        builder.AddComponentParameter(seq++, nameof(ZenStep.ChildContent),
                            (RenderFragment)(b => b.AddMarkupContent(0, $"<p class=\"content\">{step.Title} content</p>")));
                        builder.CloseComponent();
                    }
                }));

            extra?.Invoke(p);
        });

    private static readonly StepSpec[] Three = [new("Account"), new("Profile"), new("Review")];

    private static IElement NextButton(IRenderedComponent<ZenStepper> cut) =>
        cut.FindAll("section button").Last();

    private static string Content(IRenderedComponent<ZenStepper> cut) =>
        cut.Find("p.content").TextContent;

    // ---- ARIA ---------------------------------------------------------------------------------

    [Fact]
    public void Header_IsAnOrderedListInANav_NotATablist()
    {
        // A tablist promises every panel is reachable in any order with the arrow keys. A linear
        // wizard exists to refuse exactly that.
        var cut = Stepper(Three);

        cut.Find("nav").GetAttribute("aria-label").ShouldBe("Progress");
        cut.FindAll("nav ol > li").Count.ShouldBe(3);
        cut.FindAll("[role=tablist], [role=tab]").ShouldBeEmpty();
    }

    [Fact]
    public void TheActiveStep_IsTheOnlyOneMarkedCurrent() =>
        Stepper(Three).FindAll("nav li").Select(li => li.GetAttribute("aria-current"))
            .ShouldBe(["step", null, null]);

    [Fact]
    public void TheContent_IsARegionNamedByTheStepTitle()
    {
        var cut = Stepper(Three);

        var section = cut.Find("section");
        var heading = cut.Find($"#{section.GetAttribute("aria-labelledby")}");

        heading.TextContent.Trim().ShouldBe("Account");
        heading.TagName.ShouldBe("H2");
        heading.GetAttribute("tabindex").ShouldBe("-1");
    }

    [Fact]
    public void ChangingStep_MovesFocusToTheNewStepsHeading()
    {
        // Without it, Next leaves focus on a button that now belongs to a different step, and a
        // screen reader user is not told that the content above it was replaced.
        var module = JSInterop.SetupModule("./_content/ZenithUI/js/zen-dom.js");
        module.SetupVoid("focusElement", _ => true).SetVoidResult();

        var cut = Stepper(Three);
        cut.FindAll("section button").Single(b => b.TextContent.Trim() == "Next").Click();

        var heading = cut.Find($"#{cut.Find("section").GetAttribute("aria-labelledby")}");
        heading.TextContent.Trim().ShouldBe("Profile");
        module.VerifyInvoke("focusElement").Arguments[0].ShouldBe(heading.GetAttribute("id"));
    }

    [Fact]
    public void OnlyTheActiveStepsContent_IsRendered()
    {
        var cut = Stepper(Three);

        cut.FindAll("p.content").Count.ShouldBe(1);
        Content(cut).ShouldBe("Account content");
    }

    [Fact]
    public void AStepAlreadyLeftForward_SaysItIsCompleted_InWords()
    {
        var cut = Stepper(Three);

        NextButton(cut).Click();

        var first = cut.FindAll("nav li")[0];
        first.GetAttribute("data-state").ShouldBe("complete");
        first.QuerySelector(".zen-sr-only")!.TextContent.ShouldBe("(completed)");
    }

    [Fact]
    public void AStepWithAnError_SaysSo_InTheHeaderAndAboveItsContent()
    {
        var cut = Stepper([new("Account", Error: "Email is taken"), new("Profile")]);

        cut.FindAll("nav li")[0].GetAttribute("data-state").ShouldBe("error");
        cut.FindAll("nav li")[0].TextContent.ShouldContain("(has errors)");
        cut.Find("section [role=alert]").TextContent.ShouldContain("Email is taken");
    }

    [Fact]
    public void AnOptionalStep_IsLabelledOptional() =>
        Stepper([new("Account"), new("Newsletter", Optional: true)])
            .FindAll("nav li")[1].TextContent.ShouldContain("Optional");

    [Fact]
    public void UnreachableSteps_ArePlainText_NotDisabledButtons()
    {
        // A disabled button is still announced as a control, and is a tab stop in some browsers.
        var cut = Stepper(Three);

        cut.FindAll("nav button").ShouldBeEmpty();
    }

    [Fact]
    public void AStepWithNothingThatCanChange_SurvivesARerender()
    {
        // Blazor only re-sets a child's parameters when one might have changed. Strings and bools
        // that have not changed do not count, so this middle step registers once and never again -
        // and a list rebuilt on every pass dropped it from the header on the second render.
        var cut = Render<ZenStepper>(p => p.Add(x => x.ChildContent, (RenderFragment)(b =>
        {
            b.OpenComponent<ZenStep>(0);
            b.AddComponentParameter(1, nameof(ZenStep.Title), "One");
            b.CloseComponent();
            b.OpenComponent<ZenStep>(2);
            b.AddComponentParameter(3, nameof(ZenStep.Title), "Billing");
            b.AddComponentParameter(4, nameof(ZenStep.Disabled), true);
            b.CloseComponent();
            b.OpenComponent<ZenStep>(5);
            b.AddComponentParameter(6, nameof(ZenStep.Title), "Three");
            b.CloseComponent();
        })));

        cut.Render();

        cut.FindAll("nav li").Select(li => li.TextContent.Trim()).ShouldBe(["1One", "2Billing", "3Three"]);
    }

    [Fact]
    public void AStepAddedByAnIf_LandsWhereItIsWritten()
    {
        var showBilling = false;

        RenderFragment steps = b =>
        {
            AddContentStep(b, 0, "Account");

            if (showBilling)
            {
                AddContentStep(b, 10, "Billing");
            }

            AddContentStep(b, 20, "Review");
        };

        var cut = Render<ZenStepper>(p => p.Add(x => x.ChildContent, steps));

        showBilling = true;
        cut.Render(p => p.Add(x => x.ChildContent, steps));

        cut.FindAll("nav li").Select(li => li.TextContent.Trim()).ShouldBe(["1Account", "2Billing", "3Review"]);

        showBilling = false;
        cut.Render(p => p.Add(x => x.ChildContent, steps));

        cut.FindAll("nav li").Select(li => li.TextContent.Trim()).ShouldBe(["1Account", "2Review"]);

        static void AddContentStep(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder b, int seq, string title)
        {
            b.OpenComponent<ZenStep>(seq);
            b.AddComponentParameter(seq + 1, nameof(ZenStep.Title), title);
            b.AddComponentParameter(seq + 2, nameof(ZenStep.ChildContent), (RenderFragment)(c => c.AddContent(0, title)));
            b.CloseComponent();
        }
    }

    // ---- Moving -------------------------------------------------------------------------------

    [Fact]
    public void Next_MovesForward_AndReportsTheIndex()
    {
        var cut = Stepper(Three);

        NextButton(cut).Click();

        _active.ShouldBe(1);
        Content(cut).ShouldBe("Profile content");
    }

    [Fact]
    public void Back_IsAbsentOnTheFirstStep() =>
        Stepper(Three).FindAll("section button").Count.ShouldBe(1);

    [Fact]
    public void OnTheLastStep_NextBecomesFinish_AndRaisesOnFinish()
    {
        _active = 2;
        var cut = Stepper(Three);

        NextButton(cut).TextContent.Trim().ShouldBe("Finish");
        NextButton(cut).Click();

        _finished.ShouldBe(1);
    }

    [Fact]
    public void Next_SkipsADisabledStep()
    {
        var cut = Stepper([new("Account"), new("Billing", Disabled: true), new("Review")]);

        NextButton(cut).Click();

        _active.ShouldBe(2);
    }

    [Fact]
    public void Linear_TheHeaderOffersOnlyStepsAlreadyReached()
    {
        var cut = Stepper(Three);

        NextButton(cut).Click();
        NextButton(cut).Click();

        // On Review: Account and Profile have been reached and are buttons; Review is current.
        cut.FindAll("nav li").Select(li => li.QuerySelector("button") is not null).ShouldBe([true, true, false]);

        cut.FindAll("nav button")[0].Click();
        _active.ShouldBe(0);

        // Back at the start, Review is still reachable - it was reached once.
        cut.FindAll("nav li")[2].QuerySelector("button").ShouldNotBeNull();
    }

    [Fact]
    public void NonLinear_EveryStepIsReachable()
    {
        var cut = Stepper(Three, p => p.Add(x => x.Linear, false));

        cut.FindAll("nav li")[2].QuerySelector("button")!.Click();

        _active.ShouldBe(2);
    }

    [Fact]
    public void ActiveIndex_FromTheParameter_SelectsTheStep()
    {
        _active = 1;

        Content(Stepper(Three)).ShouldBe("Profile content");
    }

    // ---- Validation ---------------------------------------------------------------------------

    private sealed class Account
    {
        [Required]
        public string? Email { get; set; }
    }

    private EditContext ValidatedForm(Account model)
    {
        var form = new EditContext(model);
        form.EnableDataAnnotationsValidation(Services);
        return form;
    }

    [Fact]
    public void AnInvalidForm_BlocksNext()
    {
        var cut = Stepper([new("Account", Form: ValidatedForm(new Account())), new("Profile")]);

        NextButton(cut).Click();

        _active.ShouldBe(0);
        Content(cut).ShouldBe("Account content");
    }

    [Fact]
    public void AValidForm_LetsNextThrough()
    {
        var cut = Stepper([new("Account", Form: ValidatedForm(new Account { Email = "a@b.c" })), new("Profile")]);

        NextButton(cut).Click();

        _active.ShouldBe(1);
    }

    [Fact]
    public void AnInvalidForm_DoesNotBlockBack()
    {
        // Blocking Back until the step is valid traps a user who needs to change an earlier
        // answer this step depends on.
        _active = 1;
        var cut = Stepper([new("Account"), new("Profile", Form: ValidatedForm(new Account()))]);

        cut.FindAll("section button")[0].Click();

        _active.ShouldBe(0);
    }

    [Fact]
    public void OnLeaving_CanCancel()
    {
        var cut = Stepper([new("Account", OnLeaving: args => { args.Cancel = true; return Task.CompletedTask; }), new("Profile")]);

        NextButton(cut).Click();

        _active.ShouldBe(0);
    }

    [Fact]
    public void OnLeaving_IsToldTheDirection()
    {
        ZenStepLeavingArgs? seen = null;
        _active = 1;

        var cut = Stepper([new("Account"), new("Profile", OnLeaving: args => { seen = args; return Task.CompletedTask; })]);

        cut.FindAll("section button")[0].Click();

        seen.ShouldNotBeNull();
        seen.IsForward.ShouldBeFalse();
        seen.ToIndex.ShouldBe(0);
    }

    [Fact]
    public void Finish_ValidatesTheLastStepToo()
    {
        var cut = Stepper([new("Review", Form: ValidatedForm(new Account()))]);

        NextButton(cut).Click();

        _finished.ShouldBe(0);
    }

    // ---- Custom navigation --------------------------------------------------------------------

    [Fact]
    public void NavigationTemplate_ReplacesTheButtons_AndCanDriveTheStepper()
    {
        var cut = Stepper(Three, p => p.Add(x => x.NavigationTemplate, context => builder =>
        {
            builder.OpenElement(0, "button");
            builder.AddAttribute(1, "class", "custom-next");
            builder.AddAttribute(2, "onclick", EventCallback.Factory.Create(this, context.NextAsync));
            builder.AddContent(3, $"Step {context.Index + 1} of {context.Count}");
            builder.CloseElement();
        }));

        cut.Find("button.custom-next").TextContent.ShouldBe("Step 1 of 3");
        cut.Find("button.custom-next").Click();

        _active.ShouldBe(1);
    }

    [Fact]
    public void UnderStaticRendering_TheHeaderAndActiveStepRender()
    {
        Renderer.SetRendererInfo(new RendererInfo("Static", isInteractive: false));

        var cut = Stepper(Three);

        cut.FindAll("nav li").Count.ShouldBe(3);
        Content(cut).ShouldBe("Account content");
    }
}
