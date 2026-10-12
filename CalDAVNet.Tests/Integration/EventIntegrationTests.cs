using FluentAssertions;
using Ical.Net.CalendarComponents;
using Ical.Net.DataTypes;
using Ical.Net.Serialization;

namespace CalDAVNet.Tests.Integration;

[Collection("LiveCalDav")]
public class EventIntegrationTests
{
    private static readonly CalendarSerializer Serializer = new();
    private readonly CalDavFixture _fixture;

    public EventIntegrationTests(CalDavFixture fixture)
    {
        _fixture = fixture;
    }

    private async Task<(CalDAVClient client, Calendar defaultCalendar)> GetClientAndCalendarAsync()
    {
        var client = _fixture.CreateClient();
        var upn = await client.GetPrincipalNameAsync();
        var principal = await client.GetPrincipalAsync(upn.PrincipalName!);
        var calendars = await client.GetCalendarsAsync(principal.Principal!.CalendarHomeSet!,
            BodyHelper.Propfind([XNames.ResourceType, XNames.DisplayName, XNames.GetCtag]));
        var defaultCal = calendars.First(c => c.IsSuccess).Calendar!;
        return (client, defaultCal);
    }

    [Fact]
    public async Task GetEventsAsync_WithFilter_ExecutesReport()
    {
        if (!_fixture.IsConfigured) return;

        var (client, defaultCal) = await GetClientAndCalendarAsync();

        var filter = new FilterBuilder()
            .AddCompFilter(new CompFilterBuilder(Constants.Comp.VEVENT)
                .AddTimeRange(DateTime.UtcNow.AddMonths(-6), DateTime.UtcNow.AddMonths(1))
                .ToXElement);

        var events = await client.GetEventsAsync(defaultCal.Href, BodyHelper.CalendarQuery(filter.ToXElement));
        events.Should().NotBeNull();
        events.OverallResult.Should().Be(ClientResult.Success);
    }

    [Fact]
    public async Task Event_Lifecycle_Create_Read_Update_Delete()
    {
        if (!_fixture.IsConfigured) return;

        var (client, defaultCal) = await GetClientAndCalendarAsync();

        var uid = Guid.NewGuid().ToString("N");
        var eventHref = $"{defaultCal.Href}{uid}.ics";
        var start = new CalDateTime(DateTime.UtcNow.AddDays(2));
        var calEvent = new CalendarEvent
        {
            Uid = uid,
            Summary = $"Integration Event {uid}",
            Description = "Automated integration test description",
            Start = start,
            End = start.AddHours(1)
        };

        var iCal = new Ical.Net.Calendar();
        iCal.Events.Add(calEvent);
        var icsString = Serializer.SerializeToString(iCal)!;

        // 1. Create
        await client.CreateEventAsync(eventHref, icsString);

        // 2. Read single event
        var getResponse = await client.GetEventAsync(defaultCal.Href, eventHref);
        getResponse.IsSuccess.Should().BeTrue();
        getResponse.Event.Should().NotBeNull();
        getResponse.Event!.ICalEvent.Should().NotBeNull();
        getResponse.Event.ICalEvent!.Summary.Should().Be($"Integration Event {uid}");
        getResponse.Event.Etag.Should().NotBeNullOrWhiteSpace();

        // 3. Update
        var etag = getResponse.Event.Etag!;
        getResponse.Event.ICalEvent.Summary = $"Updated Event {uid}";
        var updatedIcs = Serializer.SerializeToString(getResponse.Event.ICalCalendar)!;
        await client.UpdateEventAsync(eventHref, etag, updatedIcs);

        // 4. Verify update
        var updatedResponse = await client.GetEventAsync(defaultCal.Href, eventHref);
        updatedResponse.IsSuccess.Should().BeTrue();
        updatedResponse.Event!.ICalEvent!.Summary.Should().Be($"Updated Event {uid}");

        // 5. Multiget query
        var multigetResponse = await client.GetEventsAsync(defaultCal.Href, BodyHelper.CalendarMultiget([eventHref]));
        multigetResponse.Count.Should().Be(1);
        multigetResponse[0].Event!.ICalEvent!.Summary.Should().Be($"Updated Event {uid}");

        // 6. Delete
        var latestEtag = updatedResponse.Event.Etag ?? etag;
        await client.DeleteAsync(eventHref, latestEtag);
    }
}
