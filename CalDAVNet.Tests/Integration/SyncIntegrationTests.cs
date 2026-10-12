using FluentAssertions;

namespace CalDAVNet.Tests.Integration;

[Collection("LiveCalDav")]
public class SyncIntegrationTests
{
    private readonly CalDavFixture _fixture;

    public SyncIntegrationTests(CalDavFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task SyncCalendarItemsAsync_ReturnsSyncTokenAndChanges()
    {
        if (!_fixture.IsConfigured) return;

        var client = _fixture.CreateClient();
        var upn = await client.GetPrincipalNameAsync();
        var principal = await client.GetPrincipalAsync(upn.PrincipalName!);
        var calendars = await client.GetCalendarsAsync(principal.Principal!.CalendarHomeSet!,
            CalDAVPayloads.Propfind([XNames.ResourceType, XNames.DisplayName, XNames.GetCtag]));
        var defaultCal = calendars.First(c => c.IsSuccess).Calendar!;

        var initialSync = await client.SyncCalendarItemsAsync(defaultCal.Href, null);

        initialSync.Should().NotBeNull();
        initialSync.SyncToken.Should().NotBeNullOrWhiteSpace();

        // Subsequent sync with the obtained token
        var secondSync = await client.SyncCalendarItemsAsync(defaultCal.Href, initialSync.SyncToken);
        secondSync.Should().NotBeNull();
        secondSync.SyncToken.Should().NotBeNullOrWhiteSpace();
    }
}
