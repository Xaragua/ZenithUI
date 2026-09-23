namespace ZenithUI.Tests.Components;

/// <summary>
/// ZenTimeline: the semantics that make a list of markers readable without seeing the line.
/// </summary>
public class ZenTimelineTests : BunitContext
{
    public ZenTimelineTests() =>
        Renderer.SetRendererInfo(new RendererInfo("Server", isInteractive: true));

    private static readonly DateTimeOffset Moment =
        new(2026, 3, 14, 9, 30, 0, TimeSpan.FromHours(-4));

    private IRenderedComponent<ZenTimeline> RenderTimeline(Action<ZenTimelineItemSpec>[] items) =>
        Render<ZenTimeline>(p => p.Add(x => x.ChildContent, (RenderFragment)(builder =>
        {
            var seq = 0;

            foreach (var configure in items)
            {
                var spec = new ZenTimelineItemSpec();
                configure(spec);

                builder.OpenComponent<ZenTimelineItem>(seq++);
                builder.AddComponentParameter(seq++, nameof(ZenTimelineItem.Title), spec.Title);

                if (spec.Timestamp is not null)
                {
                    builder.AddComponentParameter(seq++, nameof(ZenTimelineItem.Timestamp), spec.Timestamp);
                }

                if (spec.TimestampText is not null)
                {
                    builder.AddComponentParameter(seq++, nameof(ZenTimelineItem.TimestampText), spec.TimestampText);
                }

                if (spec.Current)
                {
                    builder.AddComponentParameter(seq++, nameof(ZenTimelineItem.Current), true);
                }

                builder.CloseComponent();
            }
        })));

    [Fact]
    public void ATimeline_IsAnOrderedList()
    {
        // <ol> rather than <ul> is what makes a screen reader announce "3 of 7" - the position a
        // sighted reader gets from the connecting line. An unordered list discards it.
        var cut = RenderTimeline([s => s.Title = "Created", s => s.Title = "Shipped"]);

        cut.Find("ol").ShouldNotBeNull();
        cut.FindAll("ol > li").Count.ShouldBe(2);
    }

    [Fact]
    public void TheCurrentEntry_IsTheOnlyOneMarkedAsTheCurrentStep()
    {
        var cut = RenderTimeline([
            s => s.Title = "Created",
            s => { s.Title = "Packed"; s.Current = true; },
            s => s.Title = "Delivered",
        ]);

        cut.FindAll("li")
            .Select(li => li.GetAttribute("aria-current"))
            .ShouldBe([null, "step", null]);
    }

    [Fact]
    public void ATimestamp_IsMachineReadableAndFormattedForTheReader()
    {
        // The <time> element carries the round-trip instant in `datetime` while the text carries
        // the culture's rendering, so the value stays parseable however it is displayed.
        var cut = RenderTimeline([s => { s.Title = "Shipped"; s.Timestamp = Moment; }]);

        var time = cut.Find("time");

        DateTimeOffset.Parse(time.GetAttribute("datetime")!, CultureInfo.InvariantCulture)
            .ShouldBe(Moment);

        time.TextContent.ShouldBe(Moment.ToString("g", CultureInfo.CurrentCulture));
    }

    [Fact]
    public void RelativeTimestampText_DoesNotClaimToBeMachineReadable()
    {
        // "3 days ago" has no instant behind it. A <time> without a valid datetime is worse than a
        // span: it advertises a parseable value to assistive tech and to crawlers, and has none.
        var cut = RenderTimeline([s => { s.Title = "Shipped"; s.TimestampText = "3 days ago"; }]);

        cut.FindAll("time").ShouldBeEmpty();
        cut.Markup.ShouldContain("3 days ago");
    }

    [Fact]
    public void TimestampText_WinsOverTheFormattedInstant_ButKeepsIt()
    {
        // Both given: the reader sees the phrasing, the machine still gets the instant.
        var cut = RenderTimeline([s =>
        {
            s.Title = "Shipped";
            s.Timestamp = Moment;
            s.TimestampText = "yesterday";
        }]);

        var time = cut.Find("time");

        time.TextContent.ShouldBe("yesterday");
        time.HasAttribute("datetime").ShouldBeTrue();
    }

    [Fact]
    public void TheMarker_IsHiddenFromAssistiveTechnology()
    {
        // A colour and a glyph are not an accessible name. Anything the marker means has to be in
        // the text as well, so announcing the marker itself only adds noise.
        var cut = RenderTimeline([s => s.Title = "Created"]);

        cut.Find("li > div > span").GetAttribute("aria-hidden").ShouldBe("true");
    }

    [Fact]
    public void EveryEntry_DrawsAConnector_AndCssRemovesTheLastOne()
    {
        // The line is rendered unconditionally because an item cannot know it is last. The final
        // one is hidden by a `:last-child` rule in components/base.css; this asserts the hook the
        // rule depends on is actually emitted.
        var cut = RenderTimeline([s => s.Title = "A", s => s.Title = "B"]);

        cut.FindAll(".zen-timeline-line").Count.ShouldBe(2);
        cut.Find("ol").ClassList.ShouldContain("zen-timeline");
    }

    private sealed class ZenTimelineItemSpec
    {
        public string? Title { get; set; }

        public DateTimeOffset? Timestamp { get; set; }

        public string? TimestampText { get; set; }

        public bool Current { get; set; }
    }
}
