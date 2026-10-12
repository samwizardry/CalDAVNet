using System.Xml.Linq;
using FluentAssertions;

namespace CalDAVNet.Tests.Unit;

public class CalDAVPayloadsTests
{
    [Fact]
    public void AllProp_CreatesValidXml()
    {
        var doc = CalDAVPayloads.AllProp();

        doc.Declaration.Should().NotBeNull();
        doc.Declaration!.Encoding.Should().Be("UTF-8");
        doc.Root.Should().NotBeNull();
        doc.Root!.Name.Should().Be(XNames.Propfind);
        doc.Root.Element(XNames.AllProp).Should().NotBeNull();
    }

    [Fact]
    public void CurrentUserPrincipal_CreatesValidXml()
    {
        var doc = CalDAVPayloads.CurrentUserPrincipal();

        doc.Root.Should().NotBeNull();
        doc.Root!.Name.Should().Be(XNames.Propfind);
        doc.Root.Element(XNames.CurrentUserPrincipal).Should().NotBeNull();
    }

    [Fact]
    public void Propfind_AddsPropElementWithSpecifiedChildren()
    {
        var doc = CalDAVPayloads.Propfind([XNames.DisplayName, XNames.GetCtag]);

        doc.Root.Should().NotBeNull();
        var prop = doc.Root!.Element(XNames.Prop);
        prop.Should().NotBeNull();
        prop!.Element(XNames.DisplayName).Should().NotBeNull();
        prop.Element(XNames.GetCtag).Should().NotBeNull();
    }

    [Fact]
    public void Propfind_Params_AddsPropElementWithSpecifiedChildren()
    {
        var doc = CalDAVPayloads.Propfind(XNames.DisplayName, XNames.GetCtag);

        doc.Root.Should().NotBeNull();
        var prop = doc.Root!.Element(XNames.Prop);
        prop.Should().NotBeNull();
        prop!.Element(XNames.DisplayName).Should().NotBeNull();
        prop.Element(XNames.GetCtag).Should().NotBeNull();
    }

    [Fact]
    public void Propfind_WithAllProp_ReturnsAllPropXml()
    {
        var doc = CalDAVPayloads.Propfind([XNames.AllProp]);

        doc.Root.Should().NotBeNull();
        doc.Root!.Element(XNames.AllProp).Should().NotBeNull();
    }

    [Fact]
    public void Propfind_EmptyList_DoesNotAddPropElement()
    {
        var doc = CalDAVPayloads.Propfind([]);

        doc.Root.Should().NotBeNull();
        doc.Root!.Element(XNames.Prop).Should().BeNull();
    }

    [Fact]
    public void CustomPropfind_WithRootElementsAndPropNames_AddsBoth()
    {
        var doc = CalDAVPayloads.CustomPropfind([XNames.ResourceType], [XNames.DisplayName, XNames.Comment]);

        doc.Root.Should().NotBeNull();
        doc.Root!.Element(XNames.ResourceType).Should().NotBeNull();
        var prop = doc.Root.Element(XNames.Prop);
        prop.Should().NotBeNull();
        prop!.Element(XNames.DisplayName).Should().NotBeNull();
        prop.Element(XNames.Comment).Should().NotBeNull();
    }

    [Fact]
    public void CustomPropfind_WithRootElementsOnly_AddsDirectChildren()
    {
        var doc = CalDAVPayloads.CustomPropfind([XNames.ResourceType, XNames.GetCtag]);

        doc.Root.Should().NotBeNull();
        doc.Root!.Elements(XNames.ResourceType).Should().ContainSingle();
        doc.Root.Elements(XNames.GetCtag).Should().ContainSingle();
    }

    [Fact]
    public void SyncCollection_WithSyncToken_CreatesValidXml()
    {
        var doc = CalDAVPayloads.SyncCollection("sync-token-123");

        doc.Root.Should().NotBeNull();
        doc.Root!.Name.Should().Be(XNames.SyncCollection);
        doc.Root.Element(XNames.SyncToken)!.Value.Should().Be("sync-token-123");
        doc.Root.Element(XNames.SyncLevel)!.Value.Should().Be("1");
        doc.Root.Element(XNames.Prop)!.Element(XNames.GetEtag).Should().NotBeNull();
    }

    [Fact]
    public void SyncCollection_WithoutSyncToken_CreatesEmptySyncTokenElement()
    {
        var doc = CalDAVPayloads.SyncCollection(null);

        doc.Root.Should().NotBeNull();
        doc.Root!.Element(XNames.SyncToken)!.Value.Should().BeEmpty();
    }

    [Fact]
    public void CalendarQuery_ContainsPropAndFilter()
    {
        var filter = new XElement(XNames.Filter, "filter-content");
        var doc = CalDAVPayloads.CalendarQuery(filter);

        doc.Root.Should().NotBeNull();
        doc.Root!.Name.Should().Be(XNames.CalendarQuery);
        var prop = doc.Root.Element(XNames.Prop);
        prop.Should().NotBeNull();
        prop!.Element(XNames.GetEtag).Should().NotBeNull();
        prop.Element(XNames.CalendarData).Should().NotBeNull();
        doc.Root.Element(XNames.Filter)!.Value.Should().Be("filter-content");
    }

    [Fact]
    public void CalendarMultiget_SingleHref_CreatesValidXml()
    {
        var doc = CalDAVPayloads.CalendarMultiget("/calendar/event1.ics");

        doc.Root.Should().NotBeNull();
        doc.Root!.Name.Should().Be(XNames.CalendarMultiget);
        var prop = doc.Root.Element(XNames.Prop);
        prop.Should().NotBeNull();
        prop!.Element(XNames.GetEtag).Should().NotBeNull();
        prop.Element(XNames.CalendarData).Should().NotBeNull();
        var href = doc.Root.Element(XNames.Href);
        href.Should().NotBeNull();
        href!.Value.Should().Be("/calendar/event1.ics");
    }

    [Fact]
    public void CalendarMultiget_MultipleHrefs_CreatesValidXml()
    {
        string[] hrefs = ["/calendar/e1.ics", "/calendar/e2.ics", "/calendar/e3.ics"];
        var doc = CalDAVPayloads.CalendarMultiget(hrefs);

        doc.Root.Should().NotBeNull();
        var hrefElements = doc.Root!.Elements(XNames.Href).Select(x => x.Value).ToList();
        hrefElements.Should().BeEquivalentTo(hrefs);
    }

    [Fact]
    public void Mkcalendar_WithAllParameters_CreatesValidXml()
    {
        var doc = CalDAVPayloads.Mkcalendar("Work Calendar", Constants.Comp.VEVENT, "Work related", "#00ff00");

        doc.Root.Should().NotBeNull();
        doc.Root!.Name.Should().Be(XNames.Mkcalendar);
        var set = doc.Root.Element(XNames.Set);
        set.Should().NotBeNull();
        var prop = set!.Element(XNames.Prop);
        prop.Should().NotBeNull();
        prop!.Element(XNames.DisplayName)!.Value.Should().Be("Work Calendar");
        prop.Element(XNames.CalendarDescription)!.Value.Should().Be("Work related");
        prop.Element(XNames.CalendarColor)!.Value.Should().Be("#00ff00");

        var compSet = prop.Element(XNames.SupportedCalendarComponentSet);
        compSet.Should().NotBeNull();
        var comp = compSet!.Element(XNames.Comp);
        comp.Should().NotBeNull();
        comp!.Attribute("name")!.Value.Should().Be(Constants.Comp.VEVENT);
    }

    [Fact]
    public void Mkcalendar_WithoutOptionalParameters_ExcludesDescriptionAndColor()
    {
        var doc = CalDAVPayloads.Mkcalendar("Simple Calendar", Constants.Comp.VEVENT);

        var prop = doc.Root!.Element(XNames.Set)!.Element(XNames.Prop)!;
        prop.Element(XNames.DisplayName)!.Value.Should().Be("Simple Calendar");
        prop.Element(XNames.CalendarDescription).Should().BeNull();
        prop.Element(XNames.CalendarColor).Should().BeNull();
    }
}
