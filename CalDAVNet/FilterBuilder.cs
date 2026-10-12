using System.Xml.Linq;

namespace CalDAVNet;

public class FilterBuilder
{
    private readonly XElement _filter = new XElement(XNames.Filter);
    private readonly XElement _vcalendar = new XElement(XNames.CompFilter, new XAttribute("name", Constants.Comp.VCALENDAR));

    public FilterBuilder()
    {
        _filter.Add(_vcalendar);
    }

    public XElement ToXElement => _filter;

    public FilterBuilder AddCompFilter(XElement compFilter)
    {
        _vcalendar.Add(compFilter);
        return this;
    }

    public FilterBuilder AddCompFilter(CompFilterBuilder compFilterBuilder)
    {
        return AddCompFilter(compFilterBuilder.ToXElement);
    }
}

public class CompFilterBuilder
{
    private readonly XElement _compFilter = null!;

    public CompFilterBuilder(string compName)
    {
        _compFilter = new XElement(XNames.CompFilter, new XAttribute("name", compName));
    }

    public XElement ToXElement => _compFilter;

    public CompFilterBuilder AddTimeRange(DateTime start, DateTime end)
    {
        var timeRange = new XElement(XNames.TimeRange,
            new XAttribute("start", start.ToString("yyyyMMdd'T'HHmmss'Z'")),
            new XAttribute("end", end.ToString("yyyyMMdd'T'HHmmss'Z'")));

        _compFilter.Add(timeRange);

        return this;
    }

    /// <summary>
    /// Adds a text-match condition to a property filter within a calendar component filter.
    /// This is used to match text content of a specific property (e.g., SUMMARY, DESCRIPTION).
    /// </summary>
    /// <param name="propName">The name of the property to filter on (e.g., <c>SUMMARY</c>).</param>
    /// <param name="value">The text value to match within the property.</param>
    /// <param name="negate">Whether the match should be negated (i.e., select entries that do <b>not</b> match).</param>
    /// <param name="collation">
    /// The collation to use for comparison (e.g., <c>i;unicode-casemap</c>). Optional; if null, the server default is used.
    /// </param>
    /// <returns></returns>
    public CompFilterBuilder AddTextMatch(string propName, string value,
        bool negate = false, string? collation = null)
    {
        var textMatch = new XElement(XNames.TextMatch, value);

        if (negate)
            textMatch.SetAttributeValue("negate", "yes");

        if (!string.IsNullOrEmpty(collation))
            textMatch.SetAttributeValue("collation", collation);

        var propFilter = new XElement(XNames.PropFilter, new XAttribute("name", propName), textMatch);
        _compFilter.Add(propFilter);

        return this;
    }
}
