namespace ZenithUI.Tests.Core;

public class ZenComponentBaseTests
{
    /// <summary>Minimal concrete subject; the base class is abstract and has no markup of its own.</summary>
    private sealed class TestComponent : ZenComponentBase
    {
        public string? Composed => RootClass("zen-test");

        public IReadOnlyDictionary<string, object>? Filtered => AttributesWithoutClass;

        public string Help => SubId("help");
    }

    private static TestComponent Build(Action<TestComponent>? configure = null)
    {
        var component = new TestComponent();
        configure?.Invoke(component);
        return component;
    }

    // ---- Generated id ----------------------------------------------------------------------

    [Fact]
    public void Id_DefaultsToAGeneratedValue() =>
        Build().Id.ShouldStartWith("zen-");

    [Fact]
    public void Id_IsUniquePerInstance() =>
        Build().Id.ShouldNotBe(Build().Id);

    [Fact]
    public void Id_IsAssignedInTheConstructor()
    {
        // The id must exist before any lifecycle method runs. Under prerendering a component
        // renders once on the server and again after hydration; an id generated in OnInitialized
        // or OnAfterRender would differ between the two passes, breaking the label-to-input
        // association and producing a DOM-diff mismatch. Reading it off a freshly constructed
        // instance - no renderer involved - is what pins that down.
        var component = new TestComponent();

        component.Id.ShouldNotBeNullOrWhiteSpace();
        component.Id.Length.ShouldBe(12);
    }

    [Fact]
    public void Id_CanBeOverriddenByTheCaller() =>
        Build(c => c.Id = "my-field").Id.ShouldBe("my-field");

    [Fact]
    public void SubId_DerivesFromTheEffectiveId() =>
        Build(c => c.Id = "my-field").Help.ShouldBe("my-field-help");

    // ---- Class composition -----------------------------------------------------------------

    [Fact]
    public void RootClass_ReturnsComponentClasses_WhenNothingElseIsSupplied() =>
        Build().Composed.ShouldBe("zen-test");

    [Fact]
    public void RootClass_AppendsTheClassParameterAfterComponentClasses() =>
        // Order is the contract: the caller's classes come last so they win on equal-specificity
        // ties, which is what makes `Class="mt-4"` a usable escape hatch.
        Build(c => c.Class = "mt-4").Composed.ShouldBe("zen-test mt-4");

    [Fact]
    public void RootClass_AlsoMergesAClassArrivingThroughSplattedAttributes() =>
        Build(c =>
        {
            c.Class = "mt-4";
            c.AdditionalAttributes = new Dictionary<string, object> { ["class"] = "shadow-lg" };
        })
        .Composed.ShouldBe("zen-test mt-4 shadow-lg");

    // ---- Attribute filtering ---------------------------------------------------------------

    [Fact]
    public void AttributesWithoutClass_RemovesOnlyTheClassEntry()
    {
        var component = Build(c => c.AdditionalAttributes = new Dictionary<string, object>
        {
            ["class"] = "shadow-lg",
            ["data-testid"] = "card",
            ["aria-label"] = "Card",
        });

        var filtered = component.Filtered.ShouldNotBeNull();

        filtered.ShouldNotContainKey("class");
        filtered["data-testid"].ShouldBe("card");
        filtered["aria-label"].ShouldBe("Card");
    }

    [Fact]
    public void AttributesWithoutClass_ReturnsTheSameInstance_WhenThereIsNoClass()
    {
        // Splatting happens on every render of every component; the no-class path is the common
        // one and must not allocate a copy of the dictionary.
        var attributes = new Dictionary<string, object> { ["id"] = "x" };
        var component = Build(c => c.AdditionalAttributes = attributes);

        component.Filtered.ShouldBeSameAs(attributes);
    }

    [Fact]
    public void AttributesWithoutClass_ReturnsNull_WhenThereAreNoAttributes() =>
        Build().Filtered.ShouldBeNull();
}
