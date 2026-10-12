using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace CalDAVNet;

/// <summary>
/// Default implementation of <see cref="ICalDAVClientFactory"/> using <see cref="IHttpClientFactory"/>.
/// </summary>
public class CalDAVClientFactory : ICalDAVClientFactory
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IOptionsMonitor<CalDAVOptions>? _optionsMonitor;

    public CalDAVClientFactory(
        IHttpClientFactory httpClientFactory,
        IOptionsMonitor<CalDAVOptions>? optionsMonitor = null)
    {
        _httpClientFactory = httpClientFactory;
        _optionsMonitor = optionsMonitor;
    }

    /// <inheritdoc />
    public ICalDAVClient CreateClient()
    {
        return CreateClient(CalDAVServiceCollectionExtensions.DefaultClientName);
    }

    /// <inheritdoc />
    public ICalDAVClient CreateClient(string name)
    {
        var httpClient = _httpClientFactory.CreateClient(name);
        return new CalDAVClient(httpClient);
    }

    /// <inheritdoc />
    public ICalDAVClient CreateClient(string username, string password)
    {
        var options = GetBaseOptions(CalDAVServiceCollectionExtensions.DefaultClientName);
        options.Username = username;
        options.Password = password;
        return CreateClient(options, CalDAVServiceCollectionExtensions.DefaultClientName);
    }

    /// <inheritdoc />
    public ICalDAVClient CreateClient(Uri baseAddress, string username, string password)
    {
        var options = GetBaseOptions(CalDAVServiceCollectionExtensions.DefaultClientName);
        options.BaseAddress = baseAddress;
        options.Username = username;
        options.Password = password;
        return CreateClient(options, CalDAVServiceCollectionExtensions.DefaultClientName);
    }

    /// <inheritdoc />
    public ICalDAVClient CreateClient(string baseAddressUrl, string username, string password)
    {
        return CreateClient(new Uri(baseAddressUrl), username, password);
    }

    /// <inheritdoc />
    public ICalDAVClient CreateClient(CalDAVOptions options, string? name = null)
    {
        var clientName = string.IsNullOrEmpty(name) ? CalDAVServiceCollectionExtensions.DefaultClientName : name;
        var httpClient = _httpClientFactory.CreateClient(clientName);

        CalDAVServiceCollectionExtensions.ApplyOptions(httpClient, options);

        return new CalDAVClient(httpClient);
    }

    private CalDAVOptions GetBaseOptions(string name)
    {
        var current = _optionsMonitor?.Get(name) ?? new CalDAVOptions();
        return new CalDAVOptions
        {
            BaseAddress = current.BaseAddress,
            Timeout = current.Timeout,
            UserAgent = current.UserAgent,
            ConfigureHttpClient = current.ConfigureHttpClient
        };
    }
}
