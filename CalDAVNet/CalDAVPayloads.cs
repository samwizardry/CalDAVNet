using System.Xml.Linq;

namespace CalDAVNet;

public static class CalDAVPayloads
{
    private static XElement PropfindTemplate()
    {
        var propfind = new XElement(XNames.Propfind,
            new XAttribute(XNamespace.Xmlns + Constants.DAV.Prefix, Constants.DAV.Namespace),
            new XAttribute(XNamespace.Xmlns + Constants.Server.Prefix, Constants.Server.Namespace),
            new XAttribute(XNamespace.Xmlns + Constants.Cal.Prefix, Constants.Cal.Namespace),
            new XAttribute(XNamespace.Xmlns + Constants.Apple.Prefix, Constants.Apple.Namespace));

        return propfind;
    }

    public static XDocument Propfind(IEnumerable<XName> propfindNames)
    {
        var propfind = PropfindTemplate();

        if (propfindNames.Any())
        {
            foreach (var name in propfindNames)
            {
                propfind.Add(new XElement(name));
            }
        }

        return new XDocument(new XDeclaration("1.0", "UTF-8", null), propfind);
    }

    public static XDocument Propfind(IEnumerable<XName> propfindNames, IEnumerable<XName> propNames)
    {
        var propfind = PropfindTemplate();

        if (propfindNames.Any())
        {
            foreach (var name in propfindNames)
            {
                propfind.Add(new XElement(name));
            }
        }

        if (propNames.Any())
        {
            var prop = new XElement(XNames.Prop);

            foreach (var name in propNames)
            {
                prop.Add(new XElement(name));
            }

            propfind.Add(prop);
        }

        return new XDocument(new XDeclaration("1.0", "UTF-8", null), propfind);
    }

    public static XDocument PropPropfind(IEnumerable<XName> propNames)
    {
        var propfind = PropfindTemplate();

        if (propNames.Any())
        {
            var prop = new XElement(XNames.Prop);

            foreach (var name in propNames)
            {
                prop.Add(new XElement(name));
            }

            propfind.Add(prop);
        }

        return new XDocument(new XDeclaration("1.0", "UTF-8", null), propfind);
    }

    public static XDocument AllPropPropfind()
    {
        var propfind = new XElement(XNames.Propfind,
            new XAttribute(XNamespace.Xmlns + Constants.DAV.Prefix, Constants.DAV.Namespace),
            new XElement(XNames.AllProp));

        return new XDocument(new XDeclaration("1.0", "UTF-8", null), propfind);
    }

    public static XDocument CurrentUserPrincipalPropfind()
    {
        var propfind = new XElement(XNames.Propfind,
            new XAttribute(XNamespace.Xmlns + Constants.DAV.Prefix, Constants.DAV.Namespace),
            new XElement(XNames.CurrentUserPrincipal));

        return new XDocument(new XDeclaration("1.0", "UTF-8", null), propfind);
    }

    public static XDocument SyncCollection(string? syncToken = null)
    {
        var syncCollection = new XElement(XNames.SyncCollection,
            new XAttribute(XNamespace.Xmlns + Constants.DAV.Prefix, Constants.DAV.Namespace),
            new XElement(XNames.SyncToken, syncToken),
            new XElement(XNames.SyncLevel, 1),
            new XElement(XNames.Prop, new XElement(XNames.GetEtag)));

        return new XDocument(new XDeclaration("1.0", "UTF-8", null), syncCollection);
    }

    public static XDocument CalendarQuery(XElement filter)
    {
        var calendarQuery = new XElement(XNames.CalendarQuery,
            new XAttribute(XNamespace.Xmlns + Constants.DAV.Prefix, Constants.DAV.Namespace),
            new XAttribute(XNamespace.Xmlns + Constants.Cal.Prefix, Constants.Cal.Namespace));

        var prop = new XElement(XNames.Prop,
            new XElement(XNames.GetEtag),
            new XElement(XNames.CalendarData));

        calendarQuery.Add(prop);
        calendarQuery.Add(filter);

        return new XDocument(new XDeclaration("1.0", "UTF-8", null), calendarQuery);
    }

    private static XElement CalendarMultigetTemplate()
    {
        var multiget = new XElement(XNames.CalendarMultiget,
            new XAttribute(XNamespace.Xmlns + Constants.DAV.Prefix, Constants.DAV.Namespace),
            new XAttribute(XNamespace.Xmlns + Constants.Cal.Prefix, Constants.Cal.Namespace));

        var prop = new XElement(XNames.Prop,
            new XElement(XNames.GetEtag),
            new XElement(XNames.CalendarData));

        multiget.Add(prop);

        return multiget;
    }

    public static XDocument CalendarMultiget(string href)
    {
        var multiget = CalendarMultigetTemplate();
        multiget.Add(new XElement(XNames.Href, href));
        return new XDocument(new XDeclaration("1.0", "UTF-8", null), multiget);
    }

    public static XDocument CalendarMultiget(IEnumerable<string> hrefs)
    {
        var multiget = CalendarMultigetTemplate();

        foreach (var href in hrefs)
        {
            multiget.Add(new XElement(XNames.Href, href));
        }

        return new XDocument(new XDeclaration("1.0", "UTF-8", null), multiget);
    }

    public static XDocument Mkcalendar(string displayName, string supportedComponent, string? description = null, string? color = null)
    {
        var supportedCalendarComponentSet = new XElement(XNames.SupportedCalendarComponentSet,
            new XElement(XNames.Comp, new XAttribute("name", supportedComponent)));

        var prop = new XElement(XNames.Prop,
            new XElement(XNames.DisplayName, displayName),
            supportedCalendarComponentSet);

        if (!string.IsNullOrEmpty(description))
            prop.Add(new XElement(XNames.CalendarDescription, description));

        if (!string.IsNullOrEmpty(color))
            prop.Add(new XElement(XNames.CalendarColor, color));

        var mkcalendar = new XElement(XNames.Mkcalendar,
            new XAttribute(XNamespace.Xmlns + Constants.DAV.Prefix, Constants.DAV.Namespace),
            new XAttribute(XNamespace.Xmlns + Constants.Cal.Prefix, Constants.Cal.Namespace),
            new XElement(XNames.Set, prop));

        return new XDocument(new XDeclaration("1.0", "UTF-8", null), mkcalendar);
    }
}

[Obsolete("Use CalDAVPayloads instead.")]
public static class BodyHelper
{
    public static XDocument Propfind(IEnumerable<XName> propfindNames)
        => CalDAVPayloads.Propfind(propfindNames);

    public static XDocument Propfind(IEnumerable<XName> propfindNames, IEnumerable<XName> propNames)
        => CalDAVPayloads.Propfind(propfindNames, propNames);

    public static XDocument PropPropfind(IEnumerable<XName> propNames)
        => CalDAVPayloads.PropPropfind(propNames);

    public static XDocument AllPropPropfind()
        => CalDAVPayloads.AllPropPropfind();

    public static XDocument CurrentUserPrincipalPropfind()
        => CalDAVPayloads.CurrentUserPrincipalPropfind();

    public static XDocument SyncCollection(string? syncToken = null)
        => CalDAVPayloads.SyncCollection(syncToken);

    public static XDocument CalendarQuery(XElement filter)
        => CalDAVPayloads.CalendarQuery(filter);

    public static XDocument CalendarMultiget(string href)
        => CalDAVPayloads.CalendarMultiget(href);

    public static XDocument CalendarMultiget(IEnumerable<string> hrefs)
        => CalDAVPayloads.CalendarMultiget(hrefs);

    public static XDocument Mkcalendar(string displayName, string supportedComponent, string? description = null, string? color = null)
        => CalDAVPayloads.Mkcalendar(displayName, supportedComponent, description, color);
}
