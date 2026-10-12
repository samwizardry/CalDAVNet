namespace CalDAVNet;

/// <summary>
/// Configuration options for CalDAV client.
/// </summary>
public class CalDAVOptions
{
    private Uri? _baseAddress;

    /// <summary>
    /// Base address of the CalDAV server (e.g. https://caldav.yandex.ru/).
    /// </summary>
    public Uri? BaseAddress
    {
        get => _baseAddress;
        set => _baseAddress = value;
    }

    /// <summary>
    /// Gets or sets BaseAddress using a string URI.
    /// </summary>
    public string? BaseAddressString
    {
        get => _baseAddress?.ToString();
        set => _baseAddress = !string.IsNullOrWhiteSpace(value) ? new Uri(value) : null;
    }

    /// <summary>
    /// Username for CalDAV authentication.
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    /// Password or application token for CalDAV authentication.
    /// </summary>
    public string? Password { get; set; }

    /// <summary>
    /// Request timeout (default is 100 seconds).
    /// </summary>
    public TimeSpan? Timeout { get; set; }

    /// <summary>
    /// User-Agent header value (default is "CalDAVNet").
    /// </summary>
    public string? UserAgent { get; set; } = "CalDAVNet";

    /// <summary>
    /// Optional callback for additional configuration of the underlying <see cref="HttpClient"/>.
    /// </summary>
    public Action<HttpClient>? ConfigureHttpClient { get; set; }

    /// <summary>
    /// Sets username and password credentials.
    /// </summary>
    public CalDAVOptions SetCredentials(string username, string password)
    {
        Username = username;
        Password = password;
        return this;
    }

    /// <summary>
    /// Sets BaseAddress from a URI string.
    /// </summary>
    public CalDAVOptions SetBaseAddress(string url)
    {
        BaseAddress = new Uri(url);
        return this;
    }
}
