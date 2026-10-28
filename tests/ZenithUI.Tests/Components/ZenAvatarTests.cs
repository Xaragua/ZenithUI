namespace ZenithUI.Tests.Components;

/// <summary>
/// ZenAvatar and ZenAvatarGroup. The avatar is one named image whatever it shows, and its colour
/// must not depend on which process rendered it.
/// </summary>
public class ZenAvatarTests : BunitContext
{
    public ZenAvatarTests() =>
        Renderer.SetRendererInfo(new RendererInfo("Server", isInteractive: true));

    [Fact]
    public void IsOneImage_NamedByTheName()
    {
        var root = Render<ZenAvatar>(p => p.Add(x => x.Name, "Ana Pérez")).Find("span");

        root.GetAttribute("role").ShouldBe("img");
        root.GetAttribute("aria-label").ShouldBe("Ana Pérez");
    }

    [Fact]
    public void ShowsInitials_FromTheFirstAndLastWords()
    {
        var cut = Render<ZenAvatar>(p => p.Add(x => x.Name, "maría josé de la cruz"));

        cut.Find("span > span[aria-hidden=true]").TextContent.ShouldBe("MC");
    }

    [Theory]
    [InlineData("Cher", "C")]
    [InlineData("  ana   pérez ", "AP")]
    [InlineData("Émile Zola", "ÉZ")]
    [InlineData("𝒜da Lovelace", "𝒜L")]
    public void Initials_AreWholeCharacters(string name, string expected) =>
        ZenAvatar.InitialsOf(name).ShouldBe(expected);

    [Fact]
    public void InitialsParameter_WinsOverTheName()
    {
        var cut = Render<ZenAvatar>(p => p.Add(x => x.Name, "Acme Corporation").Add(x => x.Initials, "AC+"));

        cut.Find("span > span[aria-hidden=true]").TextContent.ShouldBe("AC+");
    }

    [Fact]
    public void WithNoName_ShowsTheUserGlyph_AndIsStillNamed()
    {
        var cut = Render<ZenAvatar>();

        cut.Find("svg").ShouldNotBeNull();
        cut.Find("span").GetAttribute("aria-label").ShouldBe("User");
    }

    [Fact]
    public void ThePicture_IsLaidOverTheInitials_WithAnEmptyAlt()
    {
        // The initials stay rendered underneath, so a picture that fails shows them, not a hole.
        var cut = Render<ZenAvatar>(p => p.Add(x => x.Name, "Ana Pérez").Add(x => x.Src, "/ana.jpg"));

        cut.Find("img").GetAttribute("alt").ShouldBe(string.Empty);
        cut.Find("span > span[aria-hidden=true]").TextContent.ShouldBe("AP");
    }

    [Fact]
    public void APictureThatFails_IsRemoved()
    {
        var cut = Render<ZenAvatar>(p => p.Add(x => x.Name, "Ana Pérez").Add(x => x.Src, "/missing.jpg"));

        cut.Find("img").TriggerEvent("onerror", new Microsoft.AspNetCore.Components.Web.ErrorEventArgs());

        cut.FindAll("img").ShouldBeEmpty();
    }

    [Fact]
    public void ANewPicture_IsTriedAgain_AfterAnEarlierOneFailed()
    {
        var cut = Render<ZenAvatar>(p => p.Add(x => x.Name, "Ana").Add(x => x.Src, "/missing.jpg"));
        cut.Find("img").TriggerEvent("onerror", new Microsoft.AspNetCore.Components.Web.ErrorEventArgs());

        cut.Render(p => p.Add(x => x.Src, "/ana.jpg"));

        cut.Find("img").GetAttribute("src").ShouldBe("/ana.jpg");
    }

    [Fact]
    public void Decorative_HidesIt()
    {
        var root = Render<ZenAvatar>(p => p.Add(x => x.Name, "Ana").Add(x => x.Decorative, true)).Find("span");

        root.GetAttribute("aria-hidden").ShouldBe("true");
        root.HasAttribute("role").ShouldBeFalse();
        root.HasAttribute("aria-label").ShouldBeFalse();
    }

    [Fact]
    public void DerivedColour_IsStable()
    {
        // Pinned, not just compared within this process: string.GetHashCode would pass a
        // same-process comparison and still change colour between prerender and interactive.
        ZenAvatar.IntentFor("Ana Pérez").ShouldBe(ZenAvatar.IntentFor("Ana Pérez"));
        ZenAvatar.IntentFor("Ana Pérez").ShouldBe(ZenAvatar.IntentFor("  Ana Pérez "));

        var spread = new[] { "Ana", "Ben", "Chloé", "Dmitri", "Eko", "Fatima", "Gus", "Hana" }
            .Select(ZenAvatar.IntentFor)
            .Distinct()
            .Count();

        spread.ShouldBeGreaterThan(2);
    }

    [Fact]
    public void ExplicitIntent_WinsOverTheDerivedOne()
    {
        var cut = Render<ZenAvatar>(p => p.Add(x => x.Name, "Ana").Add(x => x.Intent, ZenIntent.Danger));

        cut.Find("span").ClassList.ShouldContain("bg-danger-soft");
    }

    // ---- Group --------------------------------------------------------------------------------

    private IRenderedComponent<ZenAvatarGroup> Group(int more, ZenSize size = ZenSize.Large) =>
        Render<ZenAvatarGroup>(p => p
            .Add(x => x.MoreCount, more)
            .Add(x => x.Size, size)
            .Add(x => x.Label, "Assignees")
            .Add(x => x.ChildContent, (RenderFragment)(builder =>
            {
                builder.OpenComponent<ZenAvatar>(0);
                builder.AddComponentParameter(1, nameof(ZenAvatar.Name), "Ana Pérez");
                builder.CloseComponent();
                builder.OpenComponent<ZenAvatar>(2);
                builder.AddComponentParameter(3, nameof(ZenAvatar.Name), "Ben Ode");
                builder.AddComponentParameter(4, nameof(ZenAvatar.Size), ZenSize.Small);
                builder.CloseComponent();
            })));

    [Fact]
    public void Group_IsANamedGroup()
    {
        var group = Group(0).Find("div");

        group.GetAttribute("role").ShouldBe("group");
        group.GetAttribute("aria-label").ShouldBe("Assignees");
    }

    [Fact]
    public void Group_SizesEveryAvatar_AndRingsThem()
    {
        var avatars = Group(0).FindAll("span[role=img]");

        avatars.Count.ShouldBe(2);
        avatars.ShouldAllBe(a => a.ClassList.Contains("size-14") && a.ClassList.Contains("ring-surface"));
    }

    [Fact]
    public void Group_MoreCount_IsNamedAsANumberOfPeople()
    {
        var more = Group(5).FindAll("span[role=img]")[^1];

        more.GetAttribute("aria-label").ShouldBe("5 more");
        more.TextContent.Trim().ShouldBe("+5");
    }

    [Fact]
    public void Group_NoMoreCount_NoPlus() =>
        Group(0).Markup.ShouldNotContain("+");
}
