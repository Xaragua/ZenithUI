using System.Reflection;
using Microsoft.AspNetCore.Components.Forms;

namespace ZenithUI.Tests.Components;

/// <summary>
/// Controls that implement IAsyncDisposable as well as ZenInputBase's IDisposable. The renderer
/// calls only DisposeAsync on such a component, so the base's cleanup has to be reached from it -
/// otherwise a disposed control stays subscribed to its form's EditContext.
/// </summary>
public class ZenDisposalTests : BunitContext
{
    public ZenDisposalTests()
    {
        Renderer.SetRendererInfo(new RendererInfo("Server", isInteractive: true));
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    private static int Subscribers(EditContext context) =>
        (typeof(EditContext)
            .GetField(nameof(EditContext.OnValidationStateChanged), BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(context) as Delegate)?.GetInvocationList().Length ?? 0;

    private async Task AssertUnsubscribesAsync<TComponent>(Action<ComponentParameterCollectionBuilder<TComponent>> parameters)
        where TComponent : IComponent
    {
        var context = new EditContext(new object());

        Render<CascadingValue<EditContext>>(p => p
            .Add(x => x.Value, context)
            .AddChildContent<TComponent>(parameters));

        Subscribers(context).ShouldBe(1);

        await DisposeComponentsAsync();

        Subscribers(context).ShouldBe(0);
    }

    [Fact]
    public Task Lookup_LeavesTheEditContext() =>
        AssertUnsubscribesAsync<ZenLookup<string>>(p => p.Add(x => x.Items, ["a", "b"]));

    [Fact]
    public Task KeyLookup_LeavesTheEditContext() =>
        AssertUnsubscribesAsync<ZenKeyLookup<string, string>>(p => p
            .Add(x => x.Items, ["a", "b"])
            .Add(x => x.ValueField, s => s));

    [Fact]
    public Task Combobox_LeavesTheEditContext() =>
        AssertUnsubscribesAsync<ZenCombobox<string>>(p => p.Add(x => x.Items, ["a", "b"]));
}
