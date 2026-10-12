using System.Xml.Linq;

namespace CalDAVNet;

public class Event
{
    private string? _etag;
    private string? _calendarData;

    public string? Etag
    {
        get
        {
            if (_etag is null &&
                Properties.TryGetValue(XNames.GetEtag, out var prop)
                && prop.IsSuccessStatusCode)
            {
                _etag = prop.Prop.Value;
            }

            return _etag;
        }
    }

    public string? CalendarData
    {
        get
        {
            if (_calendarData is null &&
                Properties.TryGetValue(XNames.CalendarData, out var prop)
                && prop.IsSuccessStatusCode)
            {
                _calendarData = prop.Prop.Value;
            }

            return _calendarData;
        }
    }

    public string Href { get; } = null!;

    public IReadOnlyDictionary<XName, PropResponse> Properties { get; }

    public Ical.Net.Calendar? ICalCalendar { get; }

    /// <summary>
    /// Master / primary calendar event (typically without RecurrenceIdentifier, or the first event).
    /// </summary>
    public Ical.Net.CalendarComponents.CalendarEvent? MasterEvent { get; }

    /// <summary>
    /// All calendar event components within the iCalendar resource (including master event and recurrence exceptions).
    /// </summary>
    public IReadOnlyList<Ical.Net.CalendarComponents.CalendarEvent> ICalEvents { get; } = [];

    /// <summary>
    /// Recurrence exception instances (components with RecurrenceIdentifier).
    /// </summary>
    public IReadOnlyList<Ical.Net.CalendarComponents.CalendarEvent> RecurrenceExceptions { get; } = [];

    /// <summary>
    /// Whether this event has recurrence rules or recurrence exceptions.
    /// </summary>
    public bool IsRecurring => ICalEvents.Any(e => e.RecurrenceRule is not null || e.RecurrenceIdentifier is not null);

    /// <summary>
    /// Evaluates recurrence rules (RRULE) and exceptions (RECURRENCE-ID) to return all occurrences of this event.
    /// If <paramref name="startTime"/> is omitted, evaluation starts from the master event's DTSTART.
    /// If <paramref name="endTime"/> is specified, only occurrences with start time up to <paramref name="endTime"/> are returned.
    /// </summary>
    public IEnumerable<Ical.Net.DataTypes.Occurrence> GetOccurrences(DateTime? startTime = null, DateTime? endTime = null)
    {
        if (ICalCalendar is null)
            return [];

        var calStart = startTime.HasValue
            ? new Ical.Net.DataTypes.CalDateTime(startTime.Value)
            : MasterEvent?.Start;

        var occurrences = ICalCalendar.GetOccurrences(calStart);

        if (endTime.HasValue)
        {
            var endCal = new Ical.Net.DataTypes.CalDateTime(endTime.Value);
            return occurrences.Where(o => o.Period.StartTime.AsUtc <= endCal.AsUtc);
        }

        return occurrences;
    }

    public Event(MultistatusItem item)
    {
        Href = item.Href;
        Properties = item.Properties;

        if (!string.IsNullOrWhiteSpace(CalendarData))
        {
            ICalCalendar = Ical.Net.Calendar.Load(CalendarData);
            if (ICalCalendar is not null)
            {
                var events = ICalCalendar.Events.ToList();
                ICalEvents = events;
                MasterEvent = events.FirstOrDefault(e => e.RecurrenceIdentifier is null)
                    ?? events.FirstOrDefault();
                RecurrenceExceptions = events.Where(e => e.RecurrenceIdentifier is not null).ToList();
            }
        }
    }
}
