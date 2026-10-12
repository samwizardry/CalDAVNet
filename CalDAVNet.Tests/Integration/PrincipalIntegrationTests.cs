using FluentAssertions;

namespace CalDAVNet.Tests.Integration;

[Collection("LiveCalDav")]
public class PrincipalIntegrationTests
{
    private readonly CalDavFixture _fixture;

    public PrincipalIntegrationTests(CalDavFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task GetPrincipalNameAsync_ReturnsSuccessAndPrincipalName()
    {
        if (!_fixture.IsConfigured) return;

        var client = _fixture.CreateClient();
        var response = await client.GetPrincipalNameAsync();

        response.IsSuccess.Should().BeTrue();
        response.PrincipalName.Should().NotBeNullOrWhiteSpace();
        response.PrincipalName.Should().StartWith("/principals/");
    }

    [Fact]
    public async Task GetPrincipalAsync_ReturnsPrincipalDetails()
    {
        if (!_fixture.IsConfigured) return;

        var client = _fixture.CreateClient();
        var upnResponse = await client.GetPrincipalNameAsync();
        upnResponse.IsSuccess.Should().BeTrue();

        var principalResponse = await client.GetPrincipalAsync(upnResponse.PrincipalName!);
        principalResponse.IsSuccess.Should().BeTrue();
        principalResponse.Principal.Should().NotBeNull();
        principalResponse.Principal!.CalendarHomeSet.Should().NotBeNullOrWhiteSpace();
        principalResponse.Principal.DisplayName.Should().NotBeNullOrWhiteSpace();
    }
}
