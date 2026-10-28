namespace ZenithUI.Tests.Components;

/// <summary>
/// ZenDivider. Which element, and which role, depends on whether the line has text in it.
/// </summary>
public class ZenDividerTests : BunitContext
{
    [Fact]
    public void Horizontal_IsAnHr()
    {
        var hr = Render<ZenDivider>().Find("hr");

        hr.ClassList.ShouldContain("border-border");
        hr.ClassList.ShouldContain("my-4");
    }

    [Fact]
    public void Vertical_IsAVerticalSeparator()
    {
        var line = Render<ZenDivider>(p => p.Add(x => x.Vertical, true)).Find("div");

        line.GetAttribute("role").ShouldBe("separator");
        line.GetAttribute("aria-orientation").ShouldBe("vertical");
        line.ClassList.ShouldContain("mx-4");
    }

    [Fact]
    public void Labelled_IsNotASeparator_SoItsTextIsRead()
    {
        // A separator's children are presentational: the role would silence the label.
        var cut = Render<ZenDivider>(p => p.Add(x => x.Label, "or"));

        cut.FindAll("[role=separator]").ShouldBeEmpty();
        cut.FindAll("hr").ShouldBeEmpty();
        cut.Find("div").TextContent.Trim().ShouldBe("or");
        cut.FindAll("span[aria-hidden=true]").Count.ShouldBe(2);
    }

    [Fact]
    public void Decorative_IsHidden()
    {
        Render<ZenDivider>(p => p.Add(x => x.Decorative, true)).Find("hr").GetAttribute("aria-hidden").ShouldBe("true");

        var vertical = Render<ZenDivider>(p => p.Add(x => x.Vertical, true).Add(x => x.Decorative, true)).Find("div");
        vertical.GetAttribute("aria-hidden").ShouldBe("true");
        vertical.HasAttribute("role").ShouldBeFalse();
    }

    [Fact]
    public void Dashed_AndSpacing()
    {
        var hr = Render<ZenDivider>(p => p.Add(x => x.Dashed, true).Add(x => x.Spacing, ZenSpace.None)).Find("hr");

        hr.ClassList.ShouldContain("border-dashed");
        hr.ClassList.ShouldContain("my-0");
    }
}
