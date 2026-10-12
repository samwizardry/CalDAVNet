using System.Xml.Linq;

namespace CalDAVNet;

public interface ICalDAVClient
{
    public static readonly HttpMethod Propfind = new HttpMethod("PROPFIND");
    public static readonly HttpMethod Proppatch = new HttpMethod("PROPPATCH");
    public static readonly HttpMethod Report = new HttpMethod("REPORT");
    public static readonly HttpMethod Mkcalendar = new HttpMethod("MKCALENDAR");

    /// <summary>
    /// Sends an HTTP request.
    /// </summary>
    /// <exception cref="HttpRequestException"></exception>
    Task<MultistatusResponse> SendForMultistatusAsync(HttpRequestMessage request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends an HTTP request.
    /// </summary>
    /// <exception cref="HttpRequestException"></exception>
    Task SendAsync(HttpRequestMessage request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves current user principal name.
    /// </summary>
    /// <exception cref="HttpRequestException"></exception>
    Task<GetPrincipalNameResponse> GetPrincipalNameAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves current principal.
    /// </summary>
    /// <param name="upn">User principal name.</param>
    /// <exception cref="HttpRequestException"></exception>
    Task<GetPrincipalResponse> GetPrincipalAsync(string upn, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a collection of calendars.
    /// </summary>
    /// <param name="href">Principal calendar home set.</param>
    /// <param name="body"><see cref="CalDAVPayloads.Propfind(IEnumerable{XName})"/> body.</param>
    /// <exception cref="HttpRequestException"></exception>
    Task<ClientResponseCollection<GetCalendarResponse>> GetCalendarsAsync(string href, XDocument body, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a calendar.
    /// </summary>
    /// <param name="href">Calendar href.</param>
    /// <param name="body"><see cref="CalDAVPayloads.Propfind(IEnumerable{XName})"/> body.</param>
    /// <exception cref="HttpRequestException"></exception>
    Task<GetCalendarResponse> GetCalendarAsync(string href, XDocument body, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a calendar.
    /// </summary>
    /// <param name="href">Target calendar href to create.</param>
    /// <param name="body"><see cref="CalDAVPayloads.Mkcalendar(string, string, string?, string?)"/> body.</param>
    Task CreateCalendarAsync(string href, XDocument body, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a calendar.
    /// </summary>
    /// <param name="href">Calendar href.</param>
    /// <param name="ctag">Calendar version.</param>
    /// <param name="body"><see cref="CalDAVPayloads.PropertyUpdate(string?, string?, string?)"/> body.</param>
    Task UpdateCalendarAsync(string href, string ctag, XDocument body, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a collection of events.
    /// </summary>
    /// <param name="href">Calendar href.</param>
    /// <param name="body"><see cref="CalDAVPayloads.CalendarMultiget(IEnumerable{string})"/> or <see cref="CalDAVPayloads.CalendarQuery(XElement)"/> body.</param>
    Task<ClientResponseCollection<GetEventResponse>> GetEventsAsync(string href, XDocument body, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves an event.
    /// </summary>
    /// <param name="href">Calendar href.</param>
    /// <param name="body"><see cref="CalDAVPayloads.CalendarMultiget(string)"/> body.</param>
    Task<GetEventResponse> GetEventAsync(string href, XDocument body, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves an event.
    /// </summary>
    /// <param name="calendarHref">Calendar href.</param>
    /// <param name="eventHref">Event href.</param>
    Task<GetEventResponse> GetEventAsync(string calendarHref, string eventHref, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates calendar event.
    /// </summary>
    /// <param name="href">Event href.</param>
    /// <param name="body">Event body (calendar-data).</param>
    Task CreateEventAsync(string href, string body, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates calendar event.
    /// </summary>
    /// <param name="href">Event href.</param>
    /// <param name="etag">Event version.</param>
    /// <param name="body">Event body (calendar-data).</param>
    Task UpdateEventAsync(string href, string etag, string body, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes resource on a caldav server (e.g. calendar or event).
    /// </summary>
    /// <param name="href">Resource href.</param>
    /// <param name="etag">Resource version.</param>
    Task DeleteAsync(string href, string etag, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves calendar changes.
    /// </summary>
    /// <param name="href">Calendar href.</param>
    /// <param name="syncToken">Last sync token.</param>
    Task<ChangeCollection> SyncCalendarItemsAsync(string href, string? syncToken, CancellationToken cancellationToken = default);
}
