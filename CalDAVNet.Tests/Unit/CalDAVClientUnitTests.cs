using System.Net;
using System.Text;
using System.Xml.Linq;
using FluentAssertions;

namespace CalDAVNet.Tests.Unit;

public class CalDAVClientUnitTests
{
    private class MockHttpMessageHandler : HttpMessageHandler
    {
        public Func<HttpRequestMessage, HttpResponseMessage> HandlerFunc { get; set; } = null!;
        public HttpRequestMessage? LastRequest { get; private set; }
        public string? LastRequestBody { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            if (request.Content != null)
            {
                LastRequestBody = await request.Content.ReadAsStringAsync(cancellationToken);
            }
            return HandlerFunc(request);
        }
    }

    private static (CalDAVClient client, MockHttpMessageHandler handler) CreateMockClient(
        Func<HttpRequestMessage, HttpResponseMessage> handlerFunc)
    {
        var handler = new MockHttpMessageHandler { HandlerFunc = handlerFunc };
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://caldav.example.com/")
        };
        var client = new CalDAVClient(httpClient);
        return (client, handler);
    }

    private const string SimpleMultistatusResponseXml = """
        <?xml version="1.0" encoding="utf-8"?>
        <D:multistatus xmlns:D="DAV:">
            <D:response>
                <D:href>/resource/</D:href>
                <D:status>HTTP/1.1 200 OK</D:status>
            </D:response>
        </D:multistatus>
        """;

    [Fact]
    public async Task SendForMultistatusAsync_ReturnsParsedMultistatusResponse()
    {
        var (client, handler) = CreateMockClient(req => new HttpResponseMessage(HttpStatusCode.MultiStatus)
        {
            Content = new StringContent(SimpleMultistatusResponseXml, Encoding.UTF8, "application/xml")
        });

        var req = new HttpRequestMessage(HttpMethod.Get, "/test");
        var result = await client.SendForMultistatusAsync(req);

        result.Should().NotBeNull();
        result.Single().Href.Should().Be("/resource/");
    }

    [Fact]
    public async Task SendAsync_EnsuresSuccessStatusCode()
    {
        var (client, _) = CreateMockClient(req => new HttpResponseMessage(HttpStatusCode.NoContent));
        var req = new HttpRequestMessage(HttpMethod.Post, "/test");

        var act = async () => await client.SendAsync(req);
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task SendAsync_ThrowsWhenNonSuccess()
    {
        var (client, _) = CreateMockClient(req => new HttpResponseMessage(HttpStatusCode.InternalServerError));
        var req = new HttpRequestMessage(HttpMethod.Post, "/test");

        var act = async () => await client.SendAsync(req);
        await act.Should().ThrowAsync<HttpRequestException>();
    }

    [Fact]
    public async Task GetPrincipalNameAsync_SendsPropfindWithDepthZero()
    {
        var xml = """
            <D:multistatus xmlns:D="DAV:">
                <D:response>
                    <D:href>/</D:href>
                    <D:propstat>
                        <D:prop>
                            <D:current-user-principal><D:href>/users/alice/</D:href></D:current-user-principal>
                        </D:prop>
                        <D:status>HTTP/1.1 200 OK</D:status>
                    </D:propstat>
                    <D:status>HTTP/1.1 200 OK</D:status>
                </D:response>
            </D:multistatus>
            """;

        var (client, handler) = CreateMockClient(req => new HttpResponseMessage(HttpStatusCode.MultiStatus)
        {
            Content = new StringContent(xml, Encoding.UTF8, "application/xml")
        });

        var result = await client.GetPrincipalNameAsync();

        handler.LastRequest!.Method.Method.Should().Be("PROPFIND");
        handler.LastRequest.Headers.GetValues("Depth").Should().Contain("0");
        result.IsSuccess.Should().BeTrue();
        result.PrincipalName.Should().Be("/users/alice/");
    }

    [Fact]
    public async Task GetPrincipalAsync_SendsPropfindWithAllProp()
    {
        var xml = """
            <D:multistatus xmlns:D="DAV:" xmlns:C="urn:ietf:params:xml:ns:caldav">
                <D:response>
                    <D:href>/users/alice/</D:href>
                    <D:propstat>
                        <D:prop>
                            <D:displayname>Alice</D:displayname>
                            <C:calendar-home-set><D:href>/calendars/alice/</D:href></C:calendar-home-set>
                        </D:prop>
                        <D:status>HTTP/1.1 200 OK</D:status>
                    </D:propstat>
                    <D:status>HTTP/1.1 200 OK</D:status>
                </D:response>
            </D:multistatus>
            """;

        var (client, handler) = CreateMockClient(req => new HttpResponseMessage(HttpStatusCode.MultiStatus)
        {
            Content = new StringContent(xml, Encoding.UTF8, "application/xml")
        });

        var result = await client.GetPrincipalAsync("/users/alice/");

        handler.LastRequest!.Method.Method.Should().Be("PROPFIND");
        handler.LastRequestBody.Should().Contain("allprop");
        result.IsSuccess.Should().BeTrue();
        result.Principal!.DisplayName.Should().Be("Alice");
        result.Principal.CalendarHomeSet.Should().Be("/calendars/alice/");
    }

    [Fact]
    public async Task GetCalendarsAsync_FiltersOnlyCalendarResources()
    {
        var xml = """
            <D:multistatus xmlns:D="DAV:" xmlns:C="urn:ietf:params:xml:ns:caldav">
                <D:response>
                    <D:href>/calendars/alice/cal1/</D:href>
                    <D:propstat>
                        <D:prop>
                            <D:resourcetype><D:collection/><C:calendar/></D:resourcetype>
                            <D:displayname>Calendar 1</D:displayname>
                        </D:prop>
                        <D:status>HTTP/1.1 200 OK</D:status>
                    </D:propstat>
                    <D:status>HTTP/1.1 200 OK</D:status>
                </D:response>
                <D:response>
                    <D:href>/calendars/alice/non-cal/</D:href>
                    <D:propstat>
                        <D:prop>
                            <D:resourcetype><D:collection/></D:resourcetype>
                        </D:prop>
                        <D:status>HTTP/1.1 200 OK</D:status>
                    </D:propstat>
                    <D:status>HTTP/1.1 200 OK</D:status>
                </D:response>
            </D:multistatus>
            """;

        var (client, handler) = CreateMockClient(req => new HttpResponseMessage(HttpStatusCode.MultiStatus)
        {
            Content = new StringContent(xml, Encoding.UTF8, "application/xml")
        });

        var calendars = await client.GetCalendarsAsync("/calendars/alice/", CalDAVPayloads.AllPropPropfind());

        handler.LastRequest!.Headers.GetValues("Depth").Should().Contain("1");
        calendars.Count.Should().Be(1);
        calendars[0].Calendar!.DisplayName.Should().Be("Calendar 1");
    }

    [Fact]
    public async Task GetCalendarAsync_ReturnsSingleCalendar()
    {
        var xml = """
            <D:multistatus xmlns:D="DAV:" xmlns:C="urn:ietf:params:xml:ns:caldav">
                <D:response>
                    <D:href>/calendars/alice/cal1/</D:href>
                    <D:propstat>
                        <D:prop>
                            <D:resourcetype><D:collection/><C:calendar/></D:resourcetype>
                            <D:displayname>Single Cal</D:displayname>
                        </D:prop>
                        <D:status>HTTP/1.1 200 OK</D:status>
                    </D:propstat>
                    <D:status>HTTP/1.1 200 OK</D:status>
                </D:response>
            </D:multistatus>
            """;

        var (client, handler) = CreateMockClient(req => new HttpResponseMessage(HttpStatusCode.MultiStatus)
        {
            Content = new StringContent(xml, Encoding.UTF8, "application/xml")
        });

        var result = await client.GetCalendarAsync("/calendars/alice/cal1/", CalDAVPayloads.AllPropPropfind());

        result.IsSuccess.Should().BeTrue();
        result.Calendar!.DisplayName.Should().Be("Single Cal");
    }

    [Fact]
    public async Task GetCalendarAsync_When404_ReturnsNotFoundResponse()
    {
        var (client, _) = CreateMockClient(req => new HttpResponseMessage(HttpStatusCode.NotFound));

        var result = await client.GetCalendarAsync("/missing/", CalDAVPayloads.AllPropPropfind());

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(ClientError.NotFound);
        result.Href.Should().Be("/missing/");
    }

    [Fact]
    public async Task CreateCalendarAsync_SendsMkcalendar()
    {
        var (client, handler) = CreateMockClient(req => new HttpResponseMessage(HttpStatusCode.Created));

        var body = CalDAVPayloads.Mkcalendar("New Cal", Constants.Comp.VEVENT);
        await client.CreateCalendarAsync("/home/my-cal/", body);

        handler.LastRequest!.Method.Method.Should().Be("MKCALENDAR");
        handler.LastRequest.RequestUri!.ToString().Should().EndWith("/home/my-cal/");
    }

    [Fact]
    public async Task UpdateCalendarAsync_SendsProppatchWithVersion()
    {
        var (client, handler) = CreateMockClient(req => new HttpResponseMessage(HttpStatusCode.MultiStatus));

        var body = new PropertyUpdateBuilder().AddDisplayName("Updated").Build();
        await client.UpdateCalendarAsync("/cal/", "ctag-99", body);

        handler.LastRequest!.Method.Method.Should().Be("PROPPATCH");
        handler.LastRequest.Headers.GetValues("If-Match").Should().Contain("\"ctag-99\"");
    }

    [Fact]
    public async Task GetEventsAsync_SendsReportWithDepthZero()
    {
        var xml = """
            <D:multistatus xmlns:D="DAV:">
                <D:response>
                    <D:href>/cal/e1.ics</D:href>
                    <D:status>HTTP/1.1 200 OK</D:status>
                </D:response>
            </D:multistatus>
            """;

        var (client, handler) = CreateMockClient(req => new HttpResponseMessage(HttpStatusCode.MultiStatus)
        {
            Content = new StringContent(xml, Encoding.UTF8, "application/xml")
        });

        var results = await client.GetEventsAsync("/cal/", CalDAVPayloads.CalendarMultiget("/cal/e1.ics"));

        handler.LastRequest!.Method.Method.Should().Be("REPORT");
        handler.LastRequest.Headers.GetValues("Depth").Should().Contain("0");
        results.Count.Should().Be(1);
    }

    [Fact]
    public async Task GetEventAsync_OverloadWithEventHref_CallsMultiget()
    {
        var xml = """
            <D:multistatus xmlns:D="DAV:">
                <D:response>
                    <D:href>/cal/e1.ics</D:href>
                    <D:status>HTTP/1.1 200 OK</D:status>
                </D:response>
            </D:multistatus>
            """;

        var (client, handler) = CreateMockClient(req => new HttpResponseMessage(HttpStatusCode.MultiStatus)
        {
            Content = new StringContent(xml, Encoding.UTF8, "application/xml")
        });

        var result = await client.GetEventAsync("/cal/", "/cal/e1.ics");

        handler.LastRequest!.Method.Method.Should().Be("REPORT");
        handler.LastRequestBody.Should().Contain("calendar-multiget");
        handler.LastRequestBody.Should().Contain("/cal/e1.ics");
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task GetEventAsync_WhenServerReturnsRecurringEventWithException_ThrowsInvalidOperationException()
    {
        var recurringXml = """
            <D:multistatus xmlns:D="DAV:" xmlns:C="urn:ietf:params:xml:ns:caldav">
                <D:response>
                    <D:href>/cal/recurring.ics</D:href>
                    <D:propstat>
                        <D:prop>
                            <D:getetag>"etag-1"</D:getetag>
                            <C:calendar-data>BEGIN:VCALENDAR
            VERSION:2.0
            BEGIN:VEVENT
            UID:rec-1
            DTSTART:20261012T100000Z
            DTEND:20261012T110000Z
            RRULE:FREQ=DAILY;COUNT=5
            SUMMARY:Daily Sync
            END:VEVENT
            BEGIN:VEVENT
            UID:rec-1
            RECURRENCE-ID:20261013T100000Z
            DTSTART:20261013T140000Z
            DTEND:20261013T150000Z
            SUMMARY:Daily Sync (Moved)
            END:VEVENT
            END:VCALENDAR</C:calendar-data>
                        </D:prop>
                        <D:status>HTTP/1.1 200 OK</D:status>
                    </D:propstat>
                    <D:status>HTTP/1.1 200 OK</D:status>
                </D:response>
            </D:multistatus>
            """;

        var (client, _) = CreateMockClient(req => new HttpResponseMessage(HttpStatusCode.MultiStatus)
        {
            Content = new StringContent(recurringXml, Encoding.UTF8, "application/xml")
        });

        var response = await client.GetEventAsync("/cal/", "/cal/recurring.ics");

        response.IsSuccess.Should().BeTrue();
        response.Event.Should().NotBeNull();
        response.Event!.MasterEvent!.Summary.Should().Be("Daily Sync");
        response.Event.ICalEvents.Should().HaveCount(2);
        response.Event.RecurrenceExceptions.Single().Summary.Should().Be("Daily Sync (Moved)");
    }

    [Fact]
    public async Task GetEventAsync_WhenHttp404_ReturnsNotFoundResponse()
    {
        var (client, _) = CreateMockClient(req => new HttpResponseMessage(HttpStatusCode.NotFound));

        var response = await client.GetEventAsync("/cal/", "/cal/missing.ics");

        response.IsSuccess.Should().BeFalse();
        response.ErrorCode.Should().Be(ClientError.NotFound);
        response.Href.Should().Be("/cal/missing.ics");
        response.Event.Should().BeNull();
    }

    [Fact]
    public async Task GetEventAsync_WhenEmptyMultistatus_ReturnsNotFoundResponse()
    {
        var emptyXml = """<D:multistatus xmlns:D="DAV:" />""";
        var (client, _) = CreateMockClient(req => new HttpResponseMessage(HttpStatusCode.MultiStatus)
        {
            Content = new StringContent(emptyXml, Encoding.UTF8, "application/xml")
        });

        var response = await client.GetEventAsync("/cal/", "/cal/missing.ics");

        response.IsSuccess.Should().BeFalse();
        response.ErrorCode.Should().Be(ClientError.NotFound);
        response.Href.Should().Be("/cal/missing.ics");
    }

    [Fact]
    public async Task GetEventAsync_WhenHttp500_ReturnsFailureResponse()
    {
        var (client, _) = CreateMockClient(req => new HttpResponseMessage(HttpStatusCode.InternalServerError));

        var response = await client.GetEventAsync("/cal/", "/cal/error.ics");

        response.IsSuccess.Should().BeFalse();
        response.ErrorCode.Should().Be(ClientError.Failure);
        response.Exception.Should().NotBeNull();
    }

    [Fact]
    public async Task GetCalendarAsync_WhenEmptyMultistatus_ReturnsNotFoundResponse()
    {
        var emptyXml = """<D:multistatus xmlns:D="DAV:" />""";
        var (client, _) = CreateMockClient(req => new HttpResponseMessage(HttpStatusCode.MultiStatus)
        {
            Content = new StringContent(emptyXml, Encoding.UTF8, "application/xml")
        });

        var response = await client.GetCalendarAsync("/cal/", CalDAVPayloads.AllPropPropfind());

        response.IsSuccess.Should().BeFalse();
        response.ErrorCode.Should().Be(ClientError.NotFound);
        response.Href.Should().Be("/cal/");
    }

    [Fact]
    public async Task GetCalendarAsync_WhenHttp500_ReturnsFailureResponse()
    {
        var (client, _) = CreateMockClient(req => new HttpResponseMessage(HttpStatusCode.InternalServerError));

        var response = await client.GetCalendarAsync("/cal/", CalDAVPayloads.AllPropPropfind());

        response.IsSuccess.Should().BeFalse();
        response.ErrorCode.Should().Be(ClientError.Failure);
        response.Exception.Should().NotBeNull();
    }

    [Fact]
    public async Task GetPrincipalNameAsync_WhenHttp404_ReturnsNotFoundResponse()
    {
        var (client, _) = CreateMockClient(req => new HttpResponseMessage(HttpStatusCode.NotFound));

        var response = await client.GetPrincipalNameAsync();

        response.IsSuccess.Should().BeFalse();
        response.ErrorCode.Should().Be(ClientError.NotFound);
    }

    [Fact]
    public async Task GetPrincipalAsync_WhenHttp404_ReturnsNotFoundResponse()
    {
        var (client, _) = CreateMockClient(req => new HttpResponseMessage(HttpStatusCode.NotFound));

        var response = await client.GetPrincipalAsync("/missing/user/");

        response.IsSuccess.Should().BeFalse();
        response.ErrorCode.Should().Be(ClientError.NotFound);
        response.Href.Should().Be("/missing/user/");
    }

    [Fact]
    public async Task GetEventsAsync_WhenHttpError_ReturnsCollectionWithErrorResponse()
    {
        var (client, _) = CreateMockClient(req => new HttpResponseMessage(HttpStatusCode.NotFound));

        var result = await client.GetEventsAsync("/cal/", CalDAVPayloads.AllPropPropfind());

        result.OverallResult.Should().Be(ClientResult.Error);
        result.Count.Should().Be(1);
        result[0].ErrorCode.Should().Be(ClientError.NotFound);
    }

    [Fact]
    public async Task GetCalendarsAsync_WhenHttpError_ReturnsCollectionWithErrorResponse()
    {
        var (client, _) = CreateMockClient(req => new HttpResponseMessage(HttpStatusCode.InternalServerError));

        var result = await client.GetCalendarsAsync("/cal/", CalDAVPayloads.AllPropPropfind());

        result.OverallResult.Should().Be(ClientResult.Error);
        result.Count.Should().Be(1);
        result[0].ErrorCode.Should().Be(ClientError.Failure);
    }

    [Fact]
    public async Task CreateEventAsync_SendsPutWithTextCalendar()
    {
        var (client, handler) = CreateMockClient(req => new HttpResponseMessage(HttpStatusCode.Created));

        await client.CreateEventAsync("/cal/e.ics", "BEGIN:VCALENDAR...END:VCALENDAR");

        handler.LastRequest!.Method.Should().Be(HttpMethod.Put);
        handler.LastRequest.Content!.Headers.ContentType!.MediaType.Should().Be("text/calendar");
        handler.LastRequestBody.Should().Be("BEGIN:VCALENDAR...END:VCALENDAR");
    }

    [Fact]
    public async Task UpdateEventAsync_SendsPutWithIfMatchVersion()
    {
        var (client, handler) = CreateMockClient(req => new HttpResponseMessage(HttpStatusCode.NoContent));

        await client.UpdateEventAsync("/cal/e.ics", "etag-v1", "BEGIN:VCALENDAR...END:VCALENDAR");

        handler.LastRequest!.Method.Should().Be(HttpMethod.Put);
        handler.LastRequest.Headers.GetValues("If-Match").Should().Contain("\"etag-v1\"");
    }

    [Fact]
    public async Task DeleteAsync_SendsDeleteWithIfMatchVersion()
    {
        var (client, handler) = CreateMockClient(req => new HttpResponseMessage(HttpStatusCode.NoContent));

        await client.DeleteAsync("/cal/e.ics", "etag-del");

        handler.LastRequest!.Method.Should().Be(HttpMethod.Delete);
        handler.LastRequest.Headers.GetValues("If-Match").Should().Contain("\"etag-del\"");
    }

    [Fact]
    public async Task SyncCalendarItemsAsync_SendsReportWithDepthOneAndParsesChanges()
    {
        var xml = """
            <D:multistatus xmlns:D="DAV:">
                <D:sync-token>sync-token-next</D:sync-token>
                <D:response>
                    <D:href>/cal/item1.ics</D:href>
                    <D:propstat>
                        <D:prop><D:getetag>"etag-sync"</D:getetag></D:prop>
                        <D:status>HTTP/1.1 200 OK</D:status>
                    </D:propstat>
                    <D:status>HTTP/1.1 200 OK</D:status>
                </D:response>
            </D:multistatus>
            """;

        var (client, handler) = CreateMockClient(req => new HttpResponseMessage(HttpStatusCode.MultiStatus)
        {
            Content = new StringContent(xml, Encoding.UTF8, "application/xml")
        });

        var changes = await client.SyncCalendarItemsAsync("/cal/", "sync-token-prev");

        handler.LastRequest!.Method.Method.Should().Be("REPORT");
        handler.LastRequest.Headers.GetValues("Depth").Should().Contain("1");
        handler.LastRequestBody.Should().Contain("sync-collection");
        handler.LastRequestBody.Should().Contain("sync-token-prev");

        changes.SyncToken.Should().Be("sync-token-next");
        changes.Count.Should().Be(1);
        changes[0].Href.Should().Be("/cal/item1.ics");
        changes[0].Etag.Should().Be("\"etag-sync\"");
    }
}
