using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Components.Forms;

namespace ZenithUI.Tests.Components;

/// <summary>
/// ZenSegmentedButton: looks like buttons, is a native radio group - which is what makes it work
/// with no script and be announced as a choice of one.
/// </summary>
public class ZenSegmentedButtonTests : BunitContext
{
    public ZenSegmentedButtonTests() =>
        Renderer.SetRendererInfo(new RendererInfo("Server", isInteractive: true));

    private string? _view = "list";

    private IRenderedComponent<ZenSegmentedButton<string>> Segmented(
        Action<ComponentParameterCollectionBuilder<ZenSegmentedButton<string>>>? extra = null,
        bool boardDisabled = false) =>
        Render<ZenSegmentedButton<string>>(p =>
        {
            p.Add(x => x.Label, "View")
                .Add(x => x.Value, _view)
                .Add(x => x.ValueChanged, v => _view = v)
                .Add(x => x.ChildContent, (RenderFragment)(builder =>
                {
                    Segment(builder, 0, "list", "List");
                    Segment(builder, 10, "grid", "Grid");
                    Segment(builder, 20, "board", "Board", boardDisabled);
                }));

            extra?.Invoke(p);
        });

    private static void Segment(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder, int seq, string value, string label, bool disabled = false)
    {
        builder.OpenComponent<ZenSegment<string>>(seq);
        builder.AddComponentParameter(seq + 1, nameof(ZenSegment<string>.Value), value);
        builder.AddComponentParameter(seq + 2, nameof(ZenSegment<string>.Label), label);
        builder.AddComponentParameter(seq + 3, nameof(ZenSegment<string>.Disabled), disabled);
        builder.CloseComponent();
    }

    [Fact]
    public void IsAFieldsetOfRadios_SharingOneName()
    {
        var cut = Segmented();

        cut.Find("fieldset > legend").TextContent.Trim().ShouldBe("View");

        var radios = cut.FindAll("input[type=radio]");
        radios.Count.ShouldBe(3);
        radios.Select(r => r.GetAttribute("name")).Distinct().Count().ShouldBe(1);
    }

    [Fact]
    public void EachRadio_IsLabelledByItsSegment()
    {
        // The label wraps the radio, which is what names it.
        var cut = Segmented();

        cut.FindAll("label").Select(l => (l.QuerySelector("input") is not null, l.TextContent.Trim()))
            .ShouldBe([(true, "List"), (true, "Grid"), (true, "Board")]);
    }

    [Fact]
    public void TheValue_IsChecked()
    {
        var radios = Segmented().FindAll("input[type=radio]");

        radios[0].HasAttribute("checked").ShouldBeTrue();
        radios[1].HasAttribute("checked").ShouldBeFalse();
    }

    [Fact]
    public void ChoosingASegment_WritesTheValue()
    {
        var cut = Segmented();

        cut.FindAll("input[type=radio]")[1].Change(true);

        _view.ShouldBe("grid");
    }

    [Fact]
    public void ADisabledSegment_DisablesItsRadio()
    {
        var radios = Segmented(boardDisabled: true).FindAll("input[type=radio]");

        radios[2].HasAttribute("disabled").ShouldBeTrue();
        radios[0].HasAttribute("disabled").ShouldBeFalse();
    }

    [Fact]
    public void ReadOnly_LeavesOnlyTheChosenSegmentReachable()
    {
        var radios = Segmented(p => p.Add(x => x.ReadOnly, true)).FindAll("input[type=radio]");

        radios[0].HasAttribute("disabled").ShouldBeFalse();
        radios[1].HasAttribute("disabled").ShouldBeTrue();
    }

    [Fact]
    public void HideLabel_KeepsTheLegendForScreenReaders() =>
        Segmented(p => p.Add(x => x.HideLabel, true)).Find("legend").ClassList.ShouldContain("zen-sr-only");

    [Fact]
    public void TheSelectedFill_FollowsTheIntent()
    {
        var label = Segmented(p => p.Add(x => x.Intent, ZenIntent.Primary)).Find("label");

        label.ClassList.ShouldContain("has-checked:bg-primary");
        label.ClassList.ShouldContain("zen-focus-visible-within");
    }

    [Fact]
    public void IsInlineByDefault() =>
        Segmented().Find("fieldset").ClassList.ShouldContain("w-fit");

    [Fact]
    public void AnIconOnlySegment_IsNamedByItsTitle()
    {
        var cut = Render<ZenSegmentedButton<string>>(p => p
            .Add(x => x.Label, "View")
            .Add(x => x.ChildContent, (RenderFragment)(builder =>
            {
                builder.OpenComponent<ZenSegment<string>>(0);
                builder.AddComponentParameter(1, nameof(ZenSegment<string>.Value), "grid");
                builder.AddComponentParameter(2, nameof(ZenSegment<string>.Icon), ZenIcons.Menu);
                builder.AddComponentParameter(3, nameof(ZenSegment<string>.Title), "Grid view");
                builder.CloseComponent();
            })));

        cut.Find("input").GetAttribute("aria-label").ShouldBe("Grid view");
    }

    [Fact]
    public void AnIconOnlySegment_WithoutATitle_Throws()
    {
        var ex = Should.Throw<InvalidOperationException>(() => Render<ZenSegmentedButton<string>>(p => p
            .Add(x => x.ChildContent, (RenderFragment)(builder =>
            {
                builder.OpenComponent<ZenSegment<string>>(0);
                builder.AddComponentParameter(1, nameof(ZenSegment<string>.Value), "grid");
                builder.AddComponentParameter(2, nameof(ZenSegment<string>.Icon), ZenIcons.Menu);
                builder.CloseComponent();
            }))));

        ex.Message.ShouldContain("Title");
    }

    [Fact]
    public void UnderStaticRendering_TheRadiosAreStillThere()
    {
        // No script is needed: the browser checks the radio and the form submits it.
        Renderer.SetRendererInfo(new RendererInfo("Static", isInteractive: false));

        var radios = Segmented().FindAll("input[type=radio]");

        radios.Count.ShouldBe(3);
        radios.Select(r => r.GetAttribute("value")).ShouldBe(["list", "grid", "board"]);
    }

    private sealed class Prefs
    {
        [Required(ErrorMessage = "Pick a view.")]
        public string? View { get; set; }
    }

    [Fact]
    public void InAForm_ItValidates()
    {
        var prefs = new Prefs();

        var cut = Render<EditForm>(p => p
            .Add(x => x.Model, prefs)
            .Add(x => x.ChildContent, _ => builder =>
            {
                builder.OpenComponent<DataAnnotationsValidator>(0);
                builder.CloseComponent();

                builder.OpenComponent<ZenSegmentedButton<string>>(1);
                builder.AddComponentParameter(2, "Label", "View");
                builder.AddComponentParameter(3, "Value", prefs.View);
                builder.AddComponentParameter(4, "ValueChanged", EventCallback.Factory.Create<string?>(this, v => prefs.View = v));
                builder.AddComponentParameter(5, "ValueExpression", (Expression<Func<string?>>)(() => prefs.View));
                builder.AddComponentParameter(6, "ChildContent", (RenderFragment)(b => Segment(b, 0, "list", "List")));
                builder.CloseComponent();
            }));

        cut.Find("form").Submit();

        cut.Find("[role=alert]").TextContent.ShouldContain("Pick a view.");
        cut.Find("fieldset").GetAttribute("aria-invalid").ShouldBe("true");
    }
}
