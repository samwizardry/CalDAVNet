using System.Collections;
using System.Xml.Linq;
using FluentAssertions;

namespace CalDAVNet.Tests.Unit;

public class ResponseTests
{
    [Theory]
    [InlineData(200, ClientError.NoError, null)]
    [InlineData(201, ClientError.NoError, null)]
    [InlineData(204, ClientError.NoError, null)]
    [InlineData(299, ClientError.NoError, null)]
    [InlineData(404, ClientError.NotFound, "Resource not found.")]
    [InlineData(400, ClientError.Failure, "Failed to get resource.")]
    [InlineData(500, ClientError.Failure, "Failed to get resource.")]
    public void ResponseMappings_MapsStatusCodes(int code, ClientError expectedError, string? expectedMessage)
    {
        ResponseMappings.StatusCodeToClientError(code).Should().Be(expectedError);
        ResponseMappings.StatusCodeToErrorMessage(code).Should().Be(expectedMessage);
    }

    [Fact]
    public void ClientResponse_PropertiesAndIsSuccess()
    {
        var ex = new InvalidOperationException("boom");
        var resp = new ClientResponse
        {
            Result = ClientResult.Success,
            ErrorCode = ClientError.NoError,
            ErrorMessage = null,
            Exception = ex
        };

        resp.IsSuccess.Should().BeTrue();
        resp.Result.Should().Be(ClientResult.Success);
        resp.ErrorCode.Should().Be(ClientError.NoError);
        resp.ErrorMessage.Should().BeNull();
        resp.Exception.Should().Be(ex);
    }

    [Fact]
    public void GetCalendarResponse_NotFound_InitializesCorrectly()
    {
        var notFound = GetCalendarResponse.NotFound("/missing/calendar/");

        notFound.IsSuccess.Should().BeFalse();
        notFound.Href.Should().Be("/missing/calendar/");
        notFound.Calendar.Should().BeNull();
        notFound.ErrorCode.Should().Be(ClientError.NotFound);
        notFound.ErrorMessage.Should().Be("Resource not found.");
    }

    [Fact]
    public void ClientResponseCollection_TracksOverallResultAndEnumerates()
    {
        var collection = new ClientResponseCollection<GetCalendarResponse>();
        collection.OverallResult.Should().Be(ClientResult.Success); // default enum value

        var okResp = new GetCalendarResponse { Result = ClientResult.Success };
        var errResp = new GetCalendarResponse { Result = ClientResult.Error };

        collection.Add(okResp);
        collection.OverallResult.Should().Be(ClientResult.Success);

        collection.Add(errResp);
        collection.OverallResult.Should().Be(ClientResult.Error);

        collection.Count.Should().Be(2);
        collection[0].Should().Be(okResp);
        collection[1].Should().Be(errResp);

        var list = collection.ToList();
        list.Should().HaveCount(2);

        IEnumerable nonGeneric = collection;
        nonGeneric.Cast<GetCalendarResponse>().Should().HaveCount(2);
    }

    [Fact]
    public void MultistatusItemExtensions_ToGetPrincipalNameResponse_Success()
    {
        var props = new Dictionary<XName, PropResponse>
        {
            [XNames.CurrentUserPrincipal] = new PropResponse(new XElement(XNames.CurrentUserPrincipal, "/principals/users/test/"), 200)
        };
        var item = new MultistatusItem("/root/", props, 200);
        var resp = item.ToGetPrincipalNameResponse();

        resp.IsSuccess.Should().BeTrue();
        resp.PrincipalName.Should().Be("/principals/users/test/");
        resp.ErrorCode.Should().Be(ClientError.NoError);
    }

    [Fact]
    public void MultistatusItemExtensions_ToGetPrincipalNameResponse_Failure()
    {
        var item = new MultistatusItem("/root/", [], 404);
        var resp = item.ToGetPrincipalNameResponse();

        resp.IsSuccess.Should().BeFalse();
        resp.PrincipalName.Should().BeNull();
        resp.ErrorCode.Should().Be(ClientError.NotFound);
    }

    [Fact]
    public void MultistatusItemExtensions_ToGetPrincipalResponse()
    {
        var props = new Dictionary<XName, PropResponse>
        {
            [XNames.DisplayName] = new PropResponse(new XElement(XNames.DisplayName, "User"), 200)
        };
        var item = new MultistatusItem("/principals/u/", props, 200);
        var resp = item.ToGetPrincipalResponse();

        resp.IsSuccess.Should().BeTrue();
        resp.Principal.Should().NotBeNull();
        resp.Principal!.DisplayName.Should().Be("User");

        var failItem = new MultistatusItem("/principals/u/", [], 500);
        var failResp = failItem.ToGetPrincipalResponse();
        failResp.IsSuccess.Should().BeFalse();
        failResp.Principal.Should().BeNull();
    }

    [Fact]
    public void MultistatusItemExtensions_ToGetCalendarResponse()
    {
        var props = new Dictionary<XName, PropResponse>
        {
            [XNames.DisplayName] = new PropResponse(new XElement(XNames.DisplayName, "Cal"), 200)
        };
        var item = new MultistatusItem("/calendars/c/", props, 200);
        var resp = item.ToGetCalendarResponse();

        resp.IsSuccess.Should().BeTrue();
        resp.Calendar.Should().NotBeNull();
        resp.Calendar!.DisplayName.Should().Be("Cal");

        var failItem = new MultistatusItem("/calendars/c/", [], 404);
        var failResp = failItem.ToGetCalendarResponse();
        failResp.IsSuccess.Should().BeFalse();
        failResp.Calendar.Should().BeNull();
    }

    [Fact]
    public void MultistatusItemExtensions_ToGetEventResponse()
    {
        var props = new Dictionary<XName, PropResponse>
        {
            [XNames.GetEtag] = new PropResponse(new XElement(XNames.GetEtag, "\"etag\""), 200)
        };
        var item = new MultistatusItem("/events/e.ics", props, 200);
        var resp = item.ToGetEventResponse();

        resp.IsSuccess.Should().BeTrue();
        resp.Event.Should().NotBeNull();
        resp.Event!.Etag.Should().Be("\"etag\"");

        var failItem = new MultistatusItem("/events/e.ics", [], 500);
        var failResp = failItem.ToGetEventResponse();
        failResp.IsSuccess.Should().BeFalse();
        failResp.Event.Should().BeNull();
    }
}
