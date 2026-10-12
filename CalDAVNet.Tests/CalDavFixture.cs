using System.Net.Http.Headers;

namespace CalDAVNet.Tests;

public class CalDavFixture
{
    public string? BaseAddress { get; }
    public string? Username { get; }
    public string? Password { get; }
    public bool IsConfigured { get; }

    public CalDavFixture()
    {
        DotNetEnv.Env.Load();
        BaseAddress = Environment.GetEnvironmentVariable("CALDAVNET_BASEADDRESS");
        Username = Environment.GetEnvironmentVariable("CALDAVNET_USERNAME");
        Password = Environment.GetEnvironmentVariable("CALDAVNET_PASSWORD");

        IsConfigured = !string.IsNullOrWhiteSpace(BaseAddress)
            && !string.IsNullOrWhiteSpace(Username)
            && !string.IsNullOrWhiteSpace(Password);
    }

    public CalDAVClient CreateClient()
    {
        if (!IsConfigured)
        {
            throw new InvalidOperationException("CalDAV credentials are not configured in .env");
        }

        var handler = new ThrottlingRetryHandler(new HttpClientHandler());
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri(BaseAddress!)
        };

        var authBytes = System.Text.Encoding.UTF8.GetBytes($"{Username}:{Password}");
        httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Basic", Convert.ToBase64String(authBytes));
        httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("CalDAVNet.Tests/1.0");

        return new CalDAVClient(httpClient);
    }
}

public class ThrottlingRetryHandler : DelegatingHandler
{
    private static readonly SemaphoreSlim Semaphore = new(1, 1);
    private static DateTime _lastRequestTime = DateTime.MinValue;

    public ThrottlingRetryHandler(HttpMessageHandler innerHandler) : base(innerHandler) { }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        await Semaphore.WaitAsync(cancellationToken);
        try
        {
            byte[]? contentBytes = null;
            MediaTypeHeaderValue? contentType = null;
            if (request.Content != null)
            {
                contentBytes = await request.Content.ReadAsByteArrayAsync(cancellationToken);
                contentType = request.Content.Headers.ContentType;
            }

            for (int attempt = 0; attempt < 5; attempt++)
            {
                var elapsed = DateTime.UtcNow - _lastRequestTime;
                if (elapsed < TimeSpan.FromMilliseconds(1500))
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(1500) - elapsed, cancellationToken);
                }

                HttpRequestMessage reqToSend = request;
                if (attempt > 0)
                {
                    reqToSend = new HttpRequestMessage(request.Method, request.RequestUri);
                    foreach (var h in request.Headers)
                    {
                        reqToSend.Headers.TryAddWithoutValidation(h.Key, h.Value);
                    }
                    if (contentBytes != null)
                    {
                        reqToSend.Content = new ByteArrayContent(contentBytes);
                        if (contentType != null)
                        {
                            reqToSend.Content.Headers.ContentType = contentType;
                        }
                    }
                }

                var response = await base.SendAsync(reqToSend, cancellationToken);
                _lastRequestTime = DateTime.UtcNow;

                if ((int)response.StatusCode == 429)
                {
                    await Task.Delay(TimeSpan.FromSeconds(3 + attempt * 2), cancellationToken);
                    continue;
                }

                return response;
            }

            return await base.SendAsync(request, cancellationToken);
        }
        finally
        {
            Semaphore.Release();
        }
    }
}
