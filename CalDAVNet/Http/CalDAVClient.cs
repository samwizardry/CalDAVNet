using System.Net;
using System.Text;
using System.Xml.Linq;

namespace CalDAVNet;

public class CalDAVClient : ICalDAVClient
{
    private readonly HttpClient _httpClient;

    public CalDAVClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<MultistatusResponse> SendForMultistatusAsync(HttpRequestMessage request, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);

        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        return new MultistatusResponse(content);
    }

    public async Task SendAsync(HttpRequestMessage request, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);

        response.EnsureSuccessStatusCode();
    }

    public async Task<GetPrincipalNameResponse> GetPrincipalNameAsync(CancellationToken cancellationToken = default)
    {
        var request = new HttpRequestMessage(ICalDAVClient.Propfind, "")
        {
            Content = CalDAVPayloads.CurrentUserPrincipal().ToStringContent()
        }
        .WithDepth(0)
        .WithPrefer("return-minimal");

        try
        {
            var response = await SendForMultistatusAsync(request, cancellationToken).ConfigureAwait(false);
            var item = response.FirstOrDefault();
            return item is not null ? item.ToGetPrincipalNameResponse() : GetPrincipalNameResponse.NotFound("");
        }
        catch (HttpRequestException ex)
        {
            return GetPrincipalNameResponse.FromException("", ex);
        }
    }

    public async Task<GetPrincipalResponse> GetPrincipalAsync(string upn, CancellationToken cancellationToken = default)
    {
        var body = CalDAVPayloads.AllProp();

        var request = new HttpRequestMessage(ICalDAVClient.Propfind, upn)
        {
            Content = body.ToStringContent()
        }
        .WithDepth(0)
        .WithPrefer("return-minimal");

        try
        {
            var response = await SendForMultistatusAsync(request, cancellationToken).ConfigureAwait(false);
            var item = response.FirstOrDefault();
            return item is not null ? item.ToGetPrincipalResponse() : GetPrincipalResponse.NotFound(upn);
        }
        catch (HttpRequestException ex)
        {
            return GetPrincipalResponse.FromException(upn, ex);
        }
    }

    public async Task<ClientResponseCollection<GetCalendarResponse>> GetCalendarsAsync(string href, XDocument body, CancellationToken cancellationToken = default)
    {
        var request = new HttpRequestMessage(ICalDAVClient.Propfind, href)
        {
            Content = body.ToStringContent()
        }
        .WithDepth(1)
        .WithPrefer("return-minimal");

        try
        {
            var response = await SendForMultistatusAsync(request, cancellationToken).ConfigureAwait(false);

            ClientResponseCollection<GetCalendarResponse> result = [];

            foreach (var item in response)
            {
                if (!item.IsCalendar)
                {
                    continue;
                }

                result.Add(item.ToGetCalendarResponse());
            }

            return result;
        }
        catch (HttpRequestException ex)
        {
            ClientResponseCollection<GetCalendarResponse> result = [];
            result.Add(GetCalendarResponse.FromException(href, ex));
            return result;
        }
    }

    public async Task<GetCalendarResponse> GetCalendarAsync(string href, XDocument body, CancellationToken cancellationToken = default)
    {
        var request = new HttpRequestMessage(ICalDAVClient.Propfind, href)
        {
            Content = body.ToStringContent()
        }
        .WithDepth(0)
        .WithPrefer("return-minimal");

        try
        {
            var response = await SendForMultistatusAsync(request, cancellationToken).ConfigureAwait(false);
            var item = response.FirstOrDefault(x => x.IsCalendar);
            return item is not null ? item.ToGetCalendarResponse() : GetCalendarResponse.NotFound(href);
        }
        catch (HttpRequestException ex)
        {
            return GetCalendarResponse.FromException(href, ex);
        }
    }

    public Task CreateCalendarAsync(string href, XDocument body, CancellationToken cancellationToken = default)
    {
        var request = new HttpRequestMessage(ICalDAVClient.Mkcalendar, href)
        {
            Content = body.ToStringContent()
        }
        .WithPrefer("return=representation");

        return SendAsync(request, cancellationToken);
    }

    public Task UpdateCalendarAsync(string href, string ctag, XDocument body, CancellationToken cancellationToken = default)
    {
        var request = new HttpRequestMessage(ICalDAVClient.Proppatch, href)
        {
            Content = body.ToStringContent()
        }
        .WithVersion(ctag)
        .WithPrefer("return=representation");

        return SendAsync(request, cancellationToken);
    }

    public async Task<ClientResponseCollection<GetEventResponse>> GetEventsAsync(string href, XDocument body, CancellationToken cancellationToken = default)
    {
        var request = new HttpRequestMessage(ICalDAVClient.Report, href)
        {
            Content = body.ToStringContent()
        }
        .WithDepth(0)
        .WithPrefer("return-minimal");

        try
        {
            var response = await SendForMultistatusAsync(request, cancellationToken).ConfigureAwait(false);

            ClientResponseCollection<GetEventResponse> result = [];

            foreach (var item in response)
            {
                result.Add(item.ToGetEventResponse());
            }

            return result;
        }
        catch (HttpRequestException ex)
        {
            ClientResponseCollection<GetEventResponse> result = [];
            result.Add(GetEventResponse.FromException(href, ex));
            return result;
        }
    }

    public async Task<GetEventResponse> GetEventAsync(string href, XDocument body,
        CancellationToken cancellationToken = default)
    {
        var request = new HttpRequestMessage(ICalDAVClient.Report, href)
        {
            Content = body.ToStringContent()
        }
        .WithDepth(0)
        .WithPrefer("return-minimal");

        try
        {
            var response = await SendForMultistatusAsync(request, cancellationToken).ConfigureAwait(false);
            var item = response.FirstOrDefault();
            return item is not null ? item.ToGetEventResponse() : GetEventResponse.NotFound(href);
        }
        catch (HttpRequestException ex)
        {
            return GetEventResponse.FromException(href, ex);
        }
    }

    public async Task<GetEventResponse> GetEventAsync(string calendarHref, string eventHref,
        CancellationToken cancellationToken = default)
    {
        var response = await GetEventAsync(calendarHref, CalDAVPayloads.CalendarMultiget(eventHref), cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccess && response.Href == calendarHref)
        {
            return new GetEventResponse
            {
                Href = eventHref,
                Result = response.Result,
                ErrorCode = response.ErrorCode,
                ErrorMessage = response.ErrorMessage,
                Exception = response.Exception
            };
        }

        return response;
    }

    public Task CreateEventAsync(string href, string body, CancellationToken cancellationToken = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Put, href)
        {
            Content = new StringContent(body, Encoding.UTF8, "text/calendar")
        }
        .WithPrefer("return=representation");

        return SendAsync(request, cancellationToken);
    }

    public Task UpdateEventAsync(string href, string etag, string body, CancellationToken cancellationToken = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Put, href)
        {
            Content = new StringContent(body, Encoding.UTF8, "text/calendar")
        }
        .WithVersion(etag)
        .WithPrefer("return=representation");

        return SendAsync(request, cancellationToken);
    }

    public Task DeleteAsync(string href, string etag, CancellationToken cancellationToken = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Delete, href)
            .WithVersion(etag)
            .WithPrefer("return-minimal");

        return SendAsync(request, cancellationToken);
    }

    public async Task<ChangeCollection> SyncCalendarItemsAsync(string href, string? syncToken, CancellationToken cancellationToken = default)
    {
        var request = new HttpRequestMessage(ICalDAVClient.Report, href)
        {
            Content = CalDAVPayloads.SyncCollection(syncToken).ToStringContent()
        }
        .WithDepth(1)
        .WithPrefer("return-minimal");

        var response = await SendForMultistatusAsync(request, cancellationToken).ConfigureAwait(false);

        ChangeCollection result = new ChangeCollection()
        {
            SyncToken = response.SyncToken
        };

        foreach (var item in response)
        {
            result.Add(new ItemChange(item));
        }

        return result;
    }
}
