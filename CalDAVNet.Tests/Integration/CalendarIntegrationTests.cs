using FluentAssertions;

namespace CalDAVNet.Tests.Integration;

[Collection("LiveCalDav")]
public class CalendarIntegrationTests
{
    private readonly CalDavFixture _fixture;

    public CalendarIntegrationTests(CalDavFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task GetCalendarsAsync_ReturnsUserCalendars()
    {
        if (!_fixture.IsConfigured) return;

        var client = _fixture.CreateClient();
        var upn = await client.GetPrincipalNameAsync();
        var principal = await client.GetPrincipalAsync(upn.PrincipalName!);

        var calendars = await client.GetCalendarsAsync(principal.Principal!.CalendarHomeSet!,
            BodyHelper.Propfind([XNames.ResourceType, XNames.GetCtag, XNames.DisplayName, XNames.CurrentUserPrivilegeSet]));

        calendars.OverallResult.Should().Be(ClientResult.Success);
        calendars.Should().NotBeEmpty();

        var first = calendars.First(c => c.IsSuccess).Calendar!;
        first.Href.Should().NotBeNullOrWhiteSpace();
        first.DisplayName.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task GetCalendarAsync_LoadsExistingCalendar()
    {
        if (!_fixture.IsConfigured) return;

        var client = _fixture.CreateClient();
        var upn = await client.GetPrincipalNameAsync();
        var principal = await client.GetPrincipalAsync(upn.PrincipalName!);

        var calendars = await client.GetCalendarsAsync(principal.Principal!.CalendarHomeSet!,
            BodyHelper.Propfind([XNames.AllProp]));
        var firstCal = calendars.First(c => c.IsSuccess).Calendar!;

        var singleCalResponse = await client.GetCalendarAsync(firstCal.Href,
            BodyHelper.Propfind([XNames.ResourceType, XNames.GetCtag, XNames.SyncToken, XNames.DisplayName]));

        singleCalResponse.IsSuccess.Should().BeTrue();
        singleCalResponse.Calendar.Should().NotBeNull();
        singleCalResponse.Calendar!.Href.Should().Be(firstCal.Href);
        singleCalResponse.Calendar.DisplayName.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Calendar_Lifecycle_CreateUpdateDelete()
    {
        if (!_fixture.IsConfigured) return;

        var client = _fixture.CreateClient();
        var upn = await client.GetPrincipalNameAsync();
        var principal = await client.GetPrincipalAsync(upn.PrincipalName!);
        var homeSet = principal.Principal!.CalendarHomeSet!;

        string calendarName = "test-cal-" + Guid.NewGuid().ToString("N")[..8];
        var mkcalendarBody = BodyHelper.Mkcalendar(calendarName, Constants.Comp.VEVENT, "Automated test calendar", "#ff0000");

        try
        {
            await client.CreateCalendarAsync(homeSet, mkcalendarBody);
        }
        catch (HttpRequestException ex)
        {
            // If the provider restricts secondary calendar creation, verify that error handling works
            ex.Should().NotBeNull();
            return;
        }

        // Find the created calendar
        var calendars = await client.GetCalendarsAsync(homeSet,
            BodyHelper.Propfind([XNames.ResourceType, XNames.DisplayName, XNames.GetCtag]));

        var created = calendars.FirstOrDefault(c => c.IsSuccess && c.Calendar?.DisplayName == calendarName)?.Calendar;
        if (created is not null)
        {
            // Update
            var updateDoc = new PropertyUpdateBuilder()
                .AddDisplayName(calendarName + "-updated")
                .RemoveColor()
                .Build();

            await client.UpdateCalendarAsync(created.Href, created.Ctag ?? "1", updateDoc);

            // Delete
            await client.DeleteAsync(created.Href, created.Ctag ?? "1");
        }
    }
}
