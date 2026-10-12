using System.Collections;
using System.Xml.Linq;
using FluentAssertions;

namespace CalDAVNet.Tests.Unit;

public class MultistatusResponseTests
{
    private const string SampleMultistatusXml = """
        <?xml version="1.0" encoding="utf-8"?>
        <D:multistatus xmlns:D="DAV:" xmlns:C="urn:ietf:params:xml:ns:caldav" xmlns:CS="http://calendarserver.org/ns/">
            <D:sync-token>sync-token-999</D:sync-token>
            <D:response>
                <D:href>/calendars/user/events/</D:href>
                <D:propstat>
                    <D:prop>
                        <D:resourcetype>
                            <D:collection/>
                            <C:calendar/>
                        </D:resourcetype>
                        <D:displayname>Personal</D:displayname>
                        <CS:getctag>ctag-123</CS:getctag>
                    </D:prop>
                    <D:status>HTTP/1.1 200 OK</D:status>
                </D:propstat>
                <D:status>HTTP/1.1 200 OK</D:status>
            </D:response>
            <D:response>
                <D:href>/calendars/user/events/e1.ics</D:href>
                <D:propstat>
                    <D:prop>
                        <D:getetag>"etag-456"</D:getetag>
                    </D:prop>
                    <D:status>HTTP/1.1 200 OK</D:status>
                </D:propstat>
                <D:status>HTTP/1.1 200 OK</D:status>
            </D:response>
        </D:multistatus>
        """;

    [Fact]
    public void ParsesEntriesAndSyncTokenCorrectly()
    {
        var response = new MultistatusResponse(SampleMultistatusXml);

        response.SyncToken.Should().Be("sync-token-999");
        response.Count().Should().Be(2);

        var first = response.First();
        first.Href.Should().Be("/calendars/user/events/");
        first.StatusCode.Should().Be(200);
        first.IsSuccessStatusCode.Should().BeTrue();
        first.IsCalendar.Should().BeTrue();
        first.Properties.Should().ContainKey(XNames.DisplayName);
        first.Properties[XNames.DisplayName].Prop.Value.Should().Be("Personal");
        first.Properties[XNames.DisplayName].IsSuccessStatusCode.Should().BeTrue();

        var second = response.Skip(1).First();
        second.Href.Should().Be("/calendars/user/events/e1.ics");
        second.IsCalendar.Should().BeFalse();
        second.Properties.Should().ContainKey(XNames.GetEtag);
    }

    [Fact]
    public void NonGenericEnumerator_WorksCorrectly()
    {
        var response = new MultistatusResponse(SampleMultistatusXml);
        IEnumerable nonGeneric = response;
        var count = 0;
        foreach (var item in nonGeneric)
        {
            item.Should().BeOfType<MultistatusItem>();
            count++;
        }
        count.Should().Be(2);
    }

    [Fact]
    public void MultistatusResponse_WithoutSyncToken_SyncTokenIsNull()
    {
        var xml = """
            <D:multistatus xmlns:D="DAV:">
                <D:response>
                    <D:href>/test/</D:href>
                    <D:status>HTTP/1.1 200 OK</D:status>
                </D:response>
            </D:multistatus>
            """;

        var response = new MultistatusResponse(xml);
        response.SyncToken.Should().BeNull();
        response.Count().Should().Be(1);
    }

    [Fact]
    public void MultistatusItem_NonSuccessStatusCode_IsSuccessStatusCodeIsFalse()
    {
        var item = new MultistatusItem("/item/", [], 404);
        item.StatusCode.Should().Be(404);
        item.IsSuccessStatusCode.Should().BeFalse();
        item.IsCalendar.Should().BeFalse();
    }

    [Fact]
    public void PropResponse_TracksStatusCorrectly()
    {
        var propEl = new XElement("test", "value");
        var successProp = new PropResponse(propEl, 200);
        successProp.IsSuccessStatusCode.Should().BeTrue();

        var failedProp = new PropResponse(propEl, 403);
        failedProp.IsSuccessStatusCode.Should().BeFalse();
    }
}
