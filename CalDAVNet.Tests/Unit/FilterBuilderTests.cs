using FluentAssertions;

namespace CalDAVNet.Tests.Unit;

public class FilterBuilderTests
{
    [Fact]
    public void FilterBuilder_InitializesWithVCalendarCompFilter()
    {
        var builder = new FilterBuilder();
        var element = builder.ToXElement;

        element.Name.Should().Be(XNames.Filter);
        var vcalendar = element.Element(XNames.CompFilter);
        vcalendar.Should().NotBeNull();
        vcalendar!.Attribute("name")!.Value.Should().Be(Constants.Comp.VCALENDAR);
    }

    [Fact]
    public void FilterBuilder_AddCompFilter_AddsChildToVCalendar()
    {
        var compBuilder = new CompFilterBuilder(Constants.Comp.VEVENT);
        var filter = new FilterBuilder().AddCompFilter(compBuilder.ToXElement);

        var vcalendar = filter.ToXElement.Element(XNames.CompFilter)!;
        var vevent = vcalendar.Element(XNames.CompFilter);
        vevent.Should().NotBeNull();
        vevent!.Attribute("name")!.Value.Should().Be(Constants.Comp.VEVENT);
    }

    [Fact]
    public void CompFilterBuilder_AddTimeRange_FormatsUtcCorrectly()
    {
        var start = new DateTime(2026, 1, 15, 10, 30, 0, DateTimeKind.Utc);
        var end = new DateTime(2026, 1, 15, 11, 30, 0, DateTimeKind.Utc);

        var compBuilder = new CompFilterBuilder(Constants.Comp.VEVENT)
            .AddTimeRange(start, end);

        var timeRange = compBuilder.ToXElement.Element(XNames.TimeRange);
        timeRange.Should().NotBeNull();
        timeRange!.Attribute("start")!.Value.Should().Be("20260115T103000Z");
        timeRange.Attribute("end")!.Value.Should().Be("20260115T113000Z");
    }

    [Fact]
    public void CompFilterBuilder_AddTextMatch_Basic()
    {
        var compBuilder = new CompFilterBuilder(Constants.Comp.VEVENT)
            .AddTextMatch(Constants.PropFilter.SUMMARY, "Team Meeting");

        var propFilter = compBuilder.ToXElement.Element(XNames.PropFilter);
        propFilter.Should().NotBeNull();
        propFilter!.Attribute("name")!.Value.Should().Be(Constants.PropFilter.SUMMARY);

        var textMatch = propFilter.Element(XNames.TextMatch);
        textMatch.Should().NotBeNull();
        textMatch!.Value.Should().Be("Team Meeting");
        textMatch.Attribute("negate").Should().BeNull();
        textMatch.Attribute("collation").Should().BeNull();
    }

    [Fact]
    public void CompFilterBuilder_AddTextMatch_WithNegateAndCollation()
    {
        var compBuilder = new CompFilterBuilder(Constants.Comp.VEVENT)
            .AddTextMatch(Constants.PropFilter.DESCRIPTION, "Secret", negate: true, collation: "i;unicode-casemap");

        var propFilter = compBuilder.ToXElement.Element(XNames.PropFilter)!;
        var textMatch = propFilter.Element(XNames.TextMatch)!;

        textMatch.Value.Should().Be("Secret");
        textMatch.Attribute("negate")!.Value.Should().Be("yes");
        textMatch.Attribute("collation")!.Value.Should().Be("i;unicode-casemap");
    }
}
