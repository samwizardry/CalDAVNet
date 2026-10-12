using System.Net.Http.Headers;
using System.Text;
using CalDAVNet;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection;

public static class CalDAVServiceCollectionExtensions
{
    /// <summary>
    /// Default client configuration name used by <see cref="IHttpClientFactory"/>.
    /// </summary>
    public const string DefaultClientName = "CalDAVClient";

    /// <summary>
    /// Adds and configures default <see cref="ICalDAVClient"/> and <see cref="CalDAVClient"/> in the service collection.
    /// </summary>
    public static IHttpClientBuilder AddCalDAVClient(
        this IServiceCollection services,
        Action<CalDAVOptions>? configureOptions = null)
    {
        if (configureOptions != null)
        {
            services.Configure(configureOptions);
        }

        return services.AddCalDAVClientCore();
    }

    /// <summary>
    /// Adds and configures default <see cref="ICalDAVClient"/> and <see cref="CalDAVClient"/> in the service collection with access to <see cref="IServiceProvider"/>.
    /// </summary>
    public static IHttpClientBuilder AddCalDAVClient(
        this IServiceCollection services,
        Action<IServiceProvider, CalDAVOptions> configureOptions)
    {
        services.AddOptions<CalDAVOptions>().Configure<IServiceProvider>((opts, sp) => configureOptions(sp, opts));

        return services.AddCalDAVClientCore();
    }

    /// <summary>
    /// Adds and configures a named <see cref="ICalDAVClient"/> in the service collection, resolvable via <see cref="ICalDAVClientFactory"/>.
    /// </summary>
    public static IHttpClientBuilder AddCalDAVClient(
        this IServiceCollection services,
        string name,
        Action<CalDAVOptions>? configureOptions = null)
    {
        if (configureOptions != null)
        {
            services.Configure(name, configureOptions);
        }

        services.TryAddSingleton<ICalDAVClientFactory, CalDAVClientFactory>();

        return services.AddHttpClient(name, (serviceProvider, httpClient) =>
        {
            var optionsMonitor = serviceProvider.GetService<IOptionsMonitor<CalDAVOptions>>();
            var options = optionsMonitor?.Get(name) ?? new CalDAVOptions();
            ApplyOptions(httpClient, options);
        });
    }

    /// <summary>
    /// Adds and configures a named <see cref="ICalDAVClient"/> in the service collection with access to <see cref="IServiceProvider"/>.
    /// </summary>
    public static IHttpClientBuilder AddCalDAVClient(
        this IServiceCollection services,
        string name,
        Action<IServiceProvider, CalDAVOptions> configureOptions)
    {
        services.AddOptions<CalDAVOptions>(name).Configure<IServiceProvider>((opts, sp) => configureOptions(sp, opts));

        services.TryAddSingleton<ICalDAVClientFactory, CalDAVClientFactory>();

        return services.AddHttpClient(name, (serviceProvider, httpClient) =>
        {
            var optionsMonitor = serviceProvider.GetService<IOptionsMonitor<CalDAVOptions>>();
            var options = optionsMonitor?.Get(name) ?? new CalDAVOptions();
            ApplyOptions(httpClient, options);
        });
    }

    /// <summary>
    /// Registers a scoped <see cref="ICalDAVCredentialsProvider"/> to dynamically supply user credentials for the current HTTP request or scope.
    /// </summary>
    public static IServiceCollection AddCalDAVCredentialsProvider<TProvider>(this IServiceCollection services)
        where TProvider : class, ICalDAVCredentialsProvider
    {
        services.AddScoped<ICalDAVCredentialsProvider, TProvider>();
        return services;
    }

    private static IHttpClientBuilder AddCalDAVClientCore(this IServiceCollection services)
    {
        services.TryAddSingleton<ICalDAVClientFactory, CalDAVClientFactory>();

        var builder = services.AddHttpClient(DefaultClientName, (serviceProvider, httpClient) =>
        {
            var options = serviceProvider.GetService<IOptions<CalDAVOptions>>()?.Value ?? new CalDAVOptions();

            var credentialsProvider = serviceProvider.GetService<ICalDAVCredentialsProvider>();
            if (credentialsProvider != null)
            {
                var creds = credentialsProvider.GetCredentials();
                if (creds != null)
                {
                    options = new CalDAVOptions
                    {
                        BaseAddress = options.BaseAddress,
                        Timeout = options.Timeout,
                        UserAgent = options.UserAgent,
                        ConfigureHttpClient = options.ConfigureHttpClient,
                        Username = creds.Username,
                        Password = creds.Password
                    };
                }
            }

            ApplyOptions(httpClient, options);
        });

        services.TryAddTransient<ICalDAVClient>(sp =>
        {
            var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
            var httpClient = httpClientFactory.CreateClient(DefaultClientName);
            return new CalDAVClient(httpClient);
        });

        services.TryAddTransient(sp => (CalDAVClient)sp.GetRequiredService<ICalDAVClient>());

        return builder;
    }

    internal static void ApplyOptions(HttpClient httpClient, CalDAVOptions options)
    {
        if (options.BaseAddress != null)
        {
            httpClient.BaseAddress = options.BaseAddress;
        }

        if (!string.IsNullOrEmpty(options.Username) && !string.IsNullOrEmpty(options.Password))
        {
            var authBytes = Encoding.UTF8.GetBytes($"{options.Username}:{options.Password}");
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(authBytes));
        }

        if (options.Timeout.HasValue)
        {
            httpClient.Timeout = options.Timeout.Value;
        }

        if (!string.IsNullOrWhiteSpace(options.UserAgent))
        {
            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(options.UserAgent);
        }

        options.ConfigureHttpClient?.Invoke(httpClient);
    }
}
