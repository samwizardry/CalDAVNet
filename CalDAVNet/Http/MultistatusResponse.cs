using System.Collections;
using System.Xml.Linq;

namespace CalDAVNet;

public class MultistatusResponse : IEnumerable<MultistatusItem>
{
    private readonly List<MultistatusItem> _entries = null!;

    public string? SyncToken { get; }

    internal MultistatusResponse(string content)
    {
        var root = XElement.Parse(content);

        _entries = root.Elements(XNames.Response).Select(ParseResponse).ToList();

        if (root.Element(XNames.SyncToken) is XElement element)
        {
            SyncToken = element.Value;
        }
    }

    private static MultistatusItem ParseResponse(XElement response)
    {
        Dictionary<XName, PropResponse> properties = [];

        foreach (var propstat in response.Elements(XNames.Propstat))
        {
            int statusCode = propstat.GetStatusCodeOrDefault(0);

            foreach (var prop in propstat.Elements(XNames.Prop))
            {
                foreach (var p in prop.Elements())
                {
                    if (!properties.TryGetValue(p.Name, out var existing) || !existing.IsSuccessStatusCode)
                    {
                        properties[p.Name] = new PropResponse(p, statusCode);
                    }
                }
            }
        }

        string href = response.Element(XNames.Href)?.Value ?? string.Empty;

        return new MultistatusItem(href,
            properties,
            response.GetStatusCodeOrDefault(200));
    }

    public IEnumerator<MultistatusItem> GetEnumerator()
    {
        return _entries.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
