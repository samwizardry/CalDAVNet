using System.Collections;
using System.Xml.Linq;
using FluentAssertions;

namespace CalDAVNet.Tests.Unit;

public class ModelTests
{
    [Fact]
    public void Calendar_ParsesAllMetadataProperties()
    {
        var props = new Dictionary<XName, PropResponse>
        {
            [XNames.DisplayName] = new PropResponse(new XElement(XNames.DisplayName, "My Calendar"), 200),
            [XNames.CalendarDescription] = new PropResponse(new XElement(XNames.CalendarDescription, "Calendar Desc"), 200),
            [XNames.CalendarColor] = new PropResponse(new XElement(XNames.CalendarColor, "#112233"), 200),
            [XNames.GetCtag] = new PropResponse(new XElement(XNames.GetCtag, "ctag-42"), 200),
            [XNames.SyncToken] = new PropResponse(new XElement(XNames.SyncToken, "token-100"), 200)
        };

        var item = new MultistatusItem("/cal/href/", props, 200);
        var calendar = new Calendar(item);

        calendar.Href.Should().Be("/cal/href/");
        calendar.DisplayName.Should().Be("My Calendar");
        calendar.Description.Should().Be("Calendar Desc");
        calendar.Color.Should().Be("#112233");
        calendar.Ctag.Should().Be("ctag-42");
        calendar.SyncToken.Should().Be("token-100");
    }

    [Fact]
    public void Calendar_ParsesSupportedCalendarComponentSet()
    {
        var compSetElement = new XElement(XNames.SupportedCalendarComponentSet,
            new XElement(XNames.Comp, new XAttribute("name", Constants.Comp.VEVENT)),
            new XElement(XNames.Comp, new XAttribute("name", Constants.Comp.VTODO)),
            new XElement(XNames.Comp, new XAttribute("name", Constants.Comp.VJOURNAL)),
            new XElement(XNames.Comp, new XAttribute("name", Constants.Comp.VFREEBUSY)),
            new XElement(XNames.Comp, new XAttribute("name", Constants.Comp.VALARM)),
            new XElement(XNames.Comp, new XAttribute("name", "UNKNOWN")));

        var props = new Dictionary<XName, PropResponse>
        {
            [XNames.SupportedCalendarComponentSet] = new PropResponse(compSetElement, 200)
        };

        var calendar = new Calendar(new MultistatusItem("/cal/", props));

        calendar.SupportedCalendarComponentSet.Should().HaveFlag(CalendarComponent.VEVENT);
        calendar.SupportedCalendarComponentSet.Should().HaveFlag(CalendarComponent.VTODO);
        calendar.SupportedCalendarComponentSet.Should().HaveFlag(CalendarComponent.VJOURNAL);
        calendar.SupportedCalendarComponentSet.Should().HaveFlag(CalendarComponent.VFREEBUSY);
        calendar.SupportedCalendarComponentSet.Should().HaveFlag(CalendarComponent.VALARM);

        calendar.IsComponentSupported(CalendarComponent.VEVENT).Should().BeTrue();
        calendar.IsComponentSupported(CalendarComponent.VTODO).Should().BeTrue();
    }

    [Fact]
    public void Calendar_ParsesCurrentUserPrivilegeSet()
    {
        var privSetElement = new XElement(XNames.CurrentUserPrivilegeSet,
            new XElement(XNames.Privilege, new XElement(XNames.Read)),
            new XElement(XNames.Privilege, new XElement(XNames.ReadFreeBusy)),
            new XElement(XNames.Privilege, new XElement(XNames.Write)),
            new XElement(XNames.Privilege, new XElement(XNames.WriteContent)),
            new XElement(XNames.Privilege, new XElement(XNames.WriteProperties)),
            new XElement(XNames.Privilege, new XElement(XNames.Bind)),
            new XElement(XNames.Privilege, new XElement(XNames.Unbind)),
            new XElement(XNames.Privilege, new XElement(XNames.ReadAcl)),
            new XElement(XNames.Privilege, new XElement(XNames.WriteAcl)),
            new XElement(XNames.Privilege, new XElement(XNames.All)));

        var props = new Dictionary<XName, PropResponse>
        {
            [XNames.CurrentUserPrivilegeSet] = new PropResponse(privSetElement, 200)
        };

        var calendar = new Calendar(new MultistatusItem("/cal/", props));

        calendar.CurrentUserPrivilegeSet.Should().HaveFlag(CurrentUserPrivilegeSet.Read);
        calendar.CurrentUserPrivilegeSet.Should().HaveFlag(CurrentUserPrivilegeSet.ReadFreeBusy);
        calendar.CurrentUserPrivilegeSet.Should().HaveFlag(CurrentUserPrivilegeSet.Write);
        calendar.CurrentUserPrivilegeSet.Should().HaveFlag(CurrentUserPrivilegeSet.WriteContent);
        calendar.CurrentUserPrivilegeSet.Should().HaveFlag(CurrentUserPrivilegeSet.WriteProperties);
        calendar.CurrentUserPrivilegeSet.Should().HaveFlag(CurrentUserPrivilegeSet.Bind);
        calendar.CurrentUserPrivilegeSet.Should().HaveFlag(CurrentUserPrivilegeSet.Unbind);
        calendar.CurrentUserPrivilegeSet.Should().HaveFlag(CurrentUserPrivilegeSet.ReadAcl);
        calendar.CurrentUserPrivilegeSet.Should().HaveFlag(CurrentUserPrivilegeSet.WriteAcl);
        calendar.CurrentUserPrivilegeSet.Should().HaveFlag(CurrentUserPrivilegeSet.All);
    }

    [Fact]
    public void Calendar_WhenPropertiesNonSuccess_ReturnsNull()
    {
        var props = new Dictionary<XName, PropResponse>
        {
            [XNames.DisplayName] = new PropResponse(new XElement(XNames.DisplayName, "Fail"), 403),
            [XNames.CalendarDescription] = new PropResponse(new XElement(XNames.CalendarDescription, "Fail"), 403),
            [XNames.CalendarColor] = new PropResponse(new XElement(XNames.CalendarColor, "Fail"), 403),
            [XNames.GetCtag] = new PropResponse(new XElement(XNames.GetCtag, "Fail"), 403),
            [XNames.SyncToken] = new PropResponse(new XElement(XNames.SyncToken, "Fail"), 403),
            [XNames.SupportedCalendarComponentSet] = new PropResponse(new XElement(XNames.SupportedCalendarComponentSet), 403),
            [XNames.CurrentUserPrivilegeSet] = new PropResponse(new XElement(XNames.CurrentUserPrivilegeSet), 403)
        };

        var calendar = new Calendar(new MultistatusItem("/cal/", props));
        calendar.DisplayName.Should().BeNull();
        calendar.Description.Should().BeNull();
        calendar.Color.Should().BeNull();
        calendar.Ctag.Should().BeNull();
        calendar.SyncToken.Should().BeNull();
        calendar.SupportedCalendarComponentSet.Should().Be(CalendarComponent.None);
        calendar.CurrentUserPrivilegeSet.Should().Be(CurrentUserPrivilegeSet.None);
    }

    [Fact]
    public void Event_ParsesICalDataAndEtag()
    {
        var ics = """
            BEGIN:VCALENDAR
            VERSION:2.0
            PRODID:-//Example Corp.//EN
            BEGIN:VEVENT
            UID:test-uid-123
            DTSTART:20261012T100000Z
            DTEND:20261012T110000Z
            SUMMARY:Unit Test Event
            DESCRIPTION:Test Event Description
            END:VEVENT
            END:VCALENDAR
            """;

        var props = new Dictionary<XName, PropResponse>
        {
            [XNames.GetEtag] = new PropResponse(new XElement(XNames.GetEtag, "\"etag-1\""), 200),
            [XNames.CalendarData] = new PropResponse(new XElement(XNames.CalendarData, ics), 200)
        };

        var @event = new Event(new MultistatusItem("/cal/event.ics", props));

        @event.Href.Should().Be("/cal/event.ics");
        @event.Etag.Should().Be("\"etag-1\"");
        @event.CalendarData.Should().Be(ics);
        @event.ICalCalendar.Should().NotBeNull();
        @event.MasterEvent.Should().NotBeNull();
        @event.MasterEvent!.Summary.Should().Be("Unit Test Event");
        @event.MasterEvent.Description.Should().Be("Test Event Description");
        @event.MasterEvent.Uid.Should().Be("test-uid-123");
    }

    [Fact]
    public void Event_WithoutCalendarData_ICalPropertiesAreNull()
    {
        var props = new Dictionary<XName, PropResponse>();
        var @event = new Event(new MultistatusItem("/cal/e.ics", props));

        @event.CalendarData.Should().BeNull();
        @event.ICalCalendar.Should().BeNull();
        @event.MasterEvent.Should().BeNull();
    }

    [Fact]
    public void Event_WithRecurringEventExceptions_ParsesMasterEventAndRecurrenceExceptionsSuccessfully()
    {
        // In RFC 5545, recurring events with modified exception instances contain multiple VEVENT blocks in one .ics
        var recurringIcsWithException = """
            BEGIN:VCALENDAR
            VERSION:2.0
            PRODID:-//Example Corp.//EN
            BEGIN:VEVENT
            UID:recurring-standup-1
            DTSTART:20261012T100000Z
            DTEND:20261012T110000Z
            RRULE:FREQ=DAILY;COUNT=5
            SUMMARY:Daily Standup
            END:VEVENT
            BEGIN:VEVENT
            UID:recurring-standup-1
            RECURRENCE-ID:20261013T100000Z
            DTSTART:20261013T140000Z
            DTEND:20261013T150000Z
            SUMMARY:Daily Standup (Rescheduled)
            END:VEVENT
            END:VCALENDAR
            """;

        var props = new Dictionary<XName, PropResponse>
        {
            [XNames.CalendarData] = new PropResponse(new XElement(XNames.CalendarData, recurringIcsWithException), 200)
        };

        var item = new MultistatusItem("/cal/recurring.ics", props);
        var @event = new Event(item);

        @event.IsRecurring.Should().BeTrue();
        @event.MasterEvent.Should().NotBeNull();
        @event.MasterEvent!.Summary.Should().Be("Daily Standup");
        @event.ICalEvents.Should().HaveCount(2);

        var exceptions = @event.RecurrenceExceptions.ToList();
        exceptions.Should().ContainSingle();
        exceptions[0].Summary.Should().Be("Daily Standup (Rescheduled)");
        exceptions[0].RecurrenceIdentifier.Should().NotBeNull();

        // All occurrences (COUNT=5)
        var allOccurrences = @event.GetOccurrences().ToList();
        allOccurrences.Should().HaveCount(5);

        // Filtered occurrences within 3-day window
        var limitedOccurrences = @event.GetOccurrences(
            new DateTime(2026, 10, 12, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 10, 14, 23, 59, 59, DateTimeKind.Utc)).ToList();
        limitedOccurrences.Should().HaveCount(3);
        ((Ical.Net.CalendarComponents.CalendarEvent)limitedOccurrences[1].Source).Summary.Should().Be("Daily Standup (Rescheduled)");
    }

    [Fact]
    public void Principal_ParsesDisplayNameAndCalendarHomeSet()
    {
        var props = new Dictionary<XName, PropResponse>
        {
            [XNames.DisplayName] = new PropResponse(new XElement(XNames.DisplayName, "User One"), 200),
            [XNames.CalendarHomeSet] = new PropResponse(new XElement(XNames.CalendarHomeSet, "/home/cal/"), 200)
        };

        var principal = new Principal(new MultistatusItem("/principals/user1/", props));

        principal.Href.Should().Be("/principals/user1/");
        principal.DisplayName.Should().Be("User One");
        principal.CalendarHomeSet.Should().Be("/home/cal/");
        principal.Properties.Should().BeEquivalentTo(props);
    }

    [Fact]
    public void Principal_WhenPropertiesFailed_ReturnsNull()
    {
        var props = new Dictionary<XName, PropResponse>
        {
            [XNames.DisplayName] = new PropResponse(new XElement(XNames.DisplayName, "User One"), 404),
            [XNames.CalendarHomeSet] = new PropResponse(new XElement(XNames.CalendarHomeSet, "/home/cal/"), 500)
        };

        var principal = new Principal(new MultistatusItem("/principals/user1/", props));
        principal.DisplayName.Should().BeNull();
        principal.CalendarHomeSet.Should().BeNull();
    }

    [Fact]
    public void ItemChange_TracksIsDeletedAndStatusCode()
    {
        var notFoundItem = new MultistatusItem("/e1.ics", [], 404);
        var change404 = new ItemChange(notFoundItem);
        change404.IsDeleted.Should().BeTrue();
        change404.IsSuccessStatusCode.Should().BeFalse();

        var goneItem = new MultistatusItem("/e2.ics", [], 410);
        var change410 = new ItemChange(goneItem);
        change410.IsDeleted.Should().BeTrue();

        var successProps = new Dictionary<XName, PropResponse>
        {
            [XNames.GetEtag] = new PropResponse(new XElement(XNames.GetEtag, "\"etag-change\""), 200)
        };
        var okItem = new MultistatusItem("/e3.ics", successProps, 200);
        var change200 = new ItemChange(okItem);
        change200.IsDeleted.Should().BeFalse();
        change200.IsSuccessStatusCode.Should().BeTrue();
        change200.Etag.Should().Be("\"etag-change\"");
    }

    [Fact]
    public void ChangeCollection_IndexCountAndEnumeration()
    {
        var collection = new ChangeCollection { SyncToken = "token-xyz" };
        collection.SyncToken.Should().Be("token-xyz");

        var c1 = new ItemChange(new MultistatusItem("/1.ics", []));
        var c2 = new ItemChange(new MultistatusItem("/2.ics", []));

        collection.Add(c1);
        collection.Add(c2);

        collection.Count.Should().Be(2);
        collection[0].Href.Should().Be("/1.ics");
        collection[1].Href.Should().Be("/2.ics");

        var enumerated = collection.ToList();
        enumerated.Should().HaveCount(2);

        IEnumerable nonGeneric = collection;
        nonGeneric.Cast<ItemChange>().Should().HaveCount(2);
    }
}
