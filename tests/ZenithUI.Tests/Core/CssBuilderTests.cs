namespace ZenithUI.Tests.Core;

public class CssBuilderTests
{
    [Fact]
    public void Build_ReturnsNull_WhenNothingWasAdded() =>
        // Null rather than "" matters: Blazor omits an attribute whose value is null, so an empty
        // builder produces no class attribute at all instead of class="".
        CssBuilder.Default().Build().ShouldBeNull();

    [Fact]
    public void Build_ReturnsNull_WhenOnlyBlankValuesWereAdded() =>
        CssBuilder.Default(" ").AddClass(null).AddClass("").AddClass("   ").Build().ShouldBeNull();

    [Fact]
    public void Build_JoinsValuesWithSingleSpaces() =>
        CssBuilder.Default("a").AddClass("b").AddClass("c").Build().ShouldBe("a b c");

    [Fact]
    public void Build_TrimsEachFragment() =>
        CssBuilder.Default("  a  ").AddClass("  b  ").Build().ShouldBe("a b");

    [Fact]
    public void AddClass_SkipsTheValue_WhenConditionIsFalse() =>
        CssBuilder.Default("base").AddClass("extra", condition: false).Build().ShouldBe("base");

    [Fact]
    public void AddClass_AppendsTheValue_WhenConditionIsTrue() =>
        CssBuilder.Default("base").AddClass("extra", condition: true).Build().ShouldBe("base extra");

    [Theory]
    [InlineData(true, "base on")]
    [InlineData(false, "base off")]
    public void AddClass_ChoosesBetweenTwoValues(bool condition, string expected) =>
        CssBuilder.Default("base").AddClass("on", "off", condition).Build().ShouldBe(expected);

    [Fact]
    public void AddClass_DoesNotEvaluateTheFactory_WhenConditionIsFalse()
    {
        var evaluated = false;

        CssBuilder.Default("base")
            .AddClass(
                () =>
                {
                    evaluated = true;
                    return "expensive";
                },
                condition: false)
            .Build();

        evaluated.ShouldBeFalse("the factory overload exists precisely to avoid this work.");
    }

    [Fact]
    public void AddClassFromAttributes_MergesTheCallersClass()
    {
        var attributes = new Dictionary<string, object> { ["class"] = "mt-4", ["id"] = "x" };

        CssBuilder.Default("zen-card")
            .AddClassFromAttributes(attributes)
            .Build()
            .ShouldBe("zen-card mt-4");
    }

    [Fact]
    public void AddClassFromAttributes_IsANoOp_WhenThereIsNoClassEntry() =>
        CssBuilder.Default("zen-card")
            .AddClassFromAttributes(new Dictionary<string, object> { ["id"] = "x" })
            .Build()
            .ShouldBe("zen-card");

    [Fact]
    public void AddClassFromAttributes_IsANoOp_WhenAttributesAreNull() =>
        CssBuilder.Default("zen-card").AddClassFromAttributes(null).Build().ShouldBe("zen-card");

    [Fact]
    public void AddClassFromAttributes_IgnoresANonStringClassValue() =>
        // Splatted attributes are object-typed; a caller binding class to a non-string should not
        // crash the render.
        CssBuilder.Default("zen-card")
            .AddClassFromAttributes(new Dictionary<string, object> { ["class"] = 42 })
            .Build()
            .ShouldBe("zen-card");

    [Fact]
    public void ToString_ReturnsEmptyString_ForAnEmptyBuilder() =>
        CssBuilder.Default().ToString().ShouldBe(string.Empty);
}
