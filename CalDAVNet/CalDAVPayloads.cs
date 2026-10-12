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

    /// <summary>
    /// Builds a standard PROPFIND payload requesting specific properties wrapped in &lt;D:prop&gt;.
    /// </summary>
    public static XDocument Propfind(IEnumerable<XName> propNames)
    {
        var names = propNames as IList<XName> ?? propNames.ToList();
        if (names.Count == 1 && names[0] == XNames.AllProp)
        {
            return AllProp();
        }

        var propfind = PropfindTemplate();

        if (names.Count > 0)
        {
            var prop = new XElement(XNames.Prop);

            foreach (var name in names)
            {
                prop.Add(new XElement(name));
            }

            propfind.Add(prop);
        }

        return new XDocument(new XDeclaration("1.0", "UTF-8", null), propfind);
    }

    /// <summary>
    /// Builds a standard PROPFIND payload requesting specific properties wrapped in &lt;D:prop&gt;.
    /// </summary>
    public static XDocument Propfind(params XName[] propNames)
        => Propfind((IEnumerable<XName>)propNames);

    /// <summary>
    /// Builds a PROPFIND payload with custom root elements directly under &lt;D:propfind&gt;,
    /// and optional properties wrapped in &lt;D:prop&gt;.
    /// </summary>
    public static XDocument CustomPropfind(IEnumerable<XName> rootElements, IEnumerable<XName>? propNames = null)
    {
        var propfind = PropfindTemplate();

        foreach (var name in rootElements)
        {
            propfind.Add(new XElement(name));
        }

        if (propNames != null && propNames.Any())
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

    /// <summary>
    /// Builds a PROPFIND payload requesting all properties (&lt;D:allprop/&gt;).
    /// </summary>
    public static XDocument AllProp()
    {
        var propfind = new XElement(XNames.Propfind,
            new XAttribute(XNamespace.Xmlns + Constants.DAV.Prefix, Constants.DAV.Namespace),
            new XElement(XNames.AllProp));

        return new XDocument(new XDeclaration("1.0", "UTF-8", null), propfind);
    }

    /// <summary>
    /// Builds a PROPFIND payload requesting the current user principal (&lt;D:current-user-principal/&gt;).
    /// </summary>
    public static XDocument CurrentUserPrincipal()
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
