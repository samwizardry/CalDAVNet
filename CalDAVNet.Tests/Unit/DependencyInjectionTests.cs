using System.Net.Http.Headers;
using System.Text;
using CalDAVNet;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace CalDAVNet.Tests.Unit;

public class DependencyInjectionTests
{
    private class TestScopedCredentialsProvider : ICalDAVCredentialsProvider
    {
        public string CurrentUser { get; set; } = "default-user";
        public string CurrentPass { get; set; } = "default-pass";

        public CalDAVCredentials? GetCredentials()
            => new CalDAVCredentials(CurrentUser, CurrentPass);
    }

    [Fact]
    public void CalDAVOptions_PropertyHelpers_WorkAsExpected()
    {
        var options = new CalDAVOptions();
        options.SetBaseAddress("https://caldav.example.com/dav/");
        options.SetCredentials("myuser", "mypass");
        options.Timeout = TimeSpan.FromSeconds(30);
        options.UserAgent = "CustomAgent/1.0";

        options.BaseAddress.Should().Be(new Uri("https://caldav.example.com/dav/"));
        options.BaseAddressString.Should().Be("https://caldav.example.com/dav/");
        options.Username.Should().Be("myuser");
        options.Password.Should().Be("mypass");
        options.Timeout.Should().Be(TimeSpan.FromSeconds(30));
        options.UserAgent.Should().Be("CustomAgent/1.0");

        options.BaseAddressString = "https://other.example.com/";
        options.BaseAddress.Should().Be(new Uri("https://other.example.com/"));

        options.BaseAddressString = null;
        options.BaseAddress.Should().BeNull();
    }

    [Fact]
    public void AddCalDAVClient_RegistersClientAndResolvesServices()
    {
        var services = new ServiceCollection();

        services.AddCalDAVClient(options =>
        {
            options.BaseAddress = new Uri("https://caldav.yandex.ru/");
            options.Username = "user@yandex.ru";
            options.Password = "secret-pass";
            options.Timeout = TimeSpan.FromSeconds(45);
            options.UserAgent = "TestUserAgent/1.0";
        });

        var serviceProvider = services.BuildServiceProvider();

        var clientInterface = serviceProvider.GetService<ICalDAVClient>();
        var clientConcrete = serviceProvider.GetService<CalDAVClient>();
        var clientFactory = serviceProvider.GetService<ICalDAVClientFactory>();

        clientInterface.Should().NotBeNull();
        clientConcrete.Should().NotBeNull();
        clientFactory.Should().NotBeNull();
    }

    [Fact]
    public void AddCalDAVClient_AppliesHttpClientOptionsCorrectly()
    {
        var services = new ServiceCollection();
        var customHeaderApplied = false;

        services.AddCalDAVClient(options =>
        {
            options.BaseAddress = new Uri("https://caldav.yandex.ru/");
            options.Username = "testuser";
            options.Password = "testpass";
            options.Timeout = TimeSpan.FromSeconds(50);
            options.UserAgent = "CustomCalDAVNet/2.0";
            options.ConfigureHttpClient = client =>
            {
                client.DefaultRequestHeaders.Add("X-Custom-Header", "Value123");
                customHeaderApplied = true;
            };
        });

        var serviceProvider = services.BuildServiceProvider();
        var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
        var httpClient = httpClientFactory.CreateClient(CalDAVServiceCollectionExtensions.DefaultClientName);

        httpClient.BaseAddress.Should().Be(new Uri("https://caldav.yandex.ru/"));
        httpClient.Timeout.Should().Be(TimeSpan.FromSeconds(50));
        httpClient.DefaultRequestHeaders.UserAgent.ToString().Should().Contain("CustomCalDAVNet/2.0");

        var expectedAuth = Convert.ToBase64String(Encoding.UTF8.GetBytes("testuser:testpass"));
        httpClient.DefaultRequestHeaders.Authorization.Should().NotBeNull();
        httpClient.DefaultRequestHeaders.Authorization!.Scheme.Should().Be("Basic");
        httpClient.DefaultRequestHeaders.Authorization.Parameter.Should().Be(expectedAuth);

        customHeaderApplied.Should().BeTrue();
        httpClient.DefaultRequestHeaders.GetValues("X-Custom-Header").Should().Contain("Value123");
    }

    [Fact]
    public void AddCalDAVClient_WithServiceProvider_ConfiguresOptions()
    {
        var services = new ServiceCollection();
        services.AddSingleton(new Uri("https://sp-configured.example.com/"));

        services.AddCalDAVClient((sp, options) =>
        {
            options.BaseAddress = sp.GetRequiredService<Uri>();
            options.Username = "sp-user";
            options.Password = "sp-pass";
        });

        var serviceProvider = services.BuildServiceProvider();
        var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
        var httpClient = httpClientFactory.CreateClient(CalDAVServiceCollectionExtensions.DefaultClientName);

        httpClient.BaseAddress.Should().Be(new Uri("https://sp-configured.example.com/"));
        var expectedAuth = Convert.ToBase64String(Encoding.UTF8.GetBytes("sp-user:sp-pass"));
        httpClient.DefaultRequestHeaders.Authorization!.Parameter.Should().Be(expectedAuth);
    }

    [Fact]
    public void AddCalDAVClient_UsesPreconfiguredOptionsIfNoActionPassed()
    {
        var services = new ServiceCollection();

        services.Configure<CalDAVOptions>(options =>
        {
            options.BaseAddress = new Uri("https://preconfigured.example.com/");
            options.Username = "pre-user";
            options.Password = "pre-pass";
        });

        services.AddCalDAVClient();

        var serviceProvider = services.BuildServiceProvider();
        var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
        var httpClient = httpClientFactory.CreateClient(CalDAVServiceCollectionExtensions.DefaultClientName);

        httpClient.BaseAddress.Should().Be(new Uri("https://preconfigured.example.com/"));
        var expectedAuth = Convert.ToBase64String(Encoding.UTF8.GetBytes("pre-user:pre-pass"));
        httpClient.DefaultRequestHeaders.Authorization!.Parameter.Should().Be(expectedAuth);
    }

    [Fact]
    public void AddCalDAVClient_NamedClient_CreatesConfiguredClientViaFactory()
    {
        var services = new ServiceCollection();

        services.AddCalDAVClient("YandexWork", options =>
        {
            options.BaseAddress = new Uri("https://caldav.yandex.ru/work/");
            options.Username = "work@company.ru";
            options.Password = "work-pass";
        });

        services.AddCalDAVClient("Personal", (sp, options) =>
        {
            options.BaseAddress = new Uri("https://caldav.personal.ru/");
            options.Username = "personal@mail.ru";
            options.Password = "personal-pass";
        });

        var serviceProvider = services.BuildServiceProvider();
        var clientFactory = serviceProvider.GetRequiredService<ICalDAVClientFactory>();

        var workClient = clientFactory.CreateClient("YandexWork");
        workClient.Should().NotBeNull();

        var personalClient = clientFactory.CreateClient("Personal");
        personalClient.Should().NotBeNull();
    }

    [Fact]
    public void CalDAVClientFactory_MultiUser_CreatesDistinctClientsWithRespectiveCredentials()
    {
        var services = new ServiceCollection();
        services.AddCalDAVClient(options =>
        {
            options.SetBaseAddress("https://caldav.yandex.ru/");
            options.Timeout = TimeSpan.FromSeconds(30);
            options.UserAgent = "MultiUserApp/1.0";
        });

        var serviceProvider = services.BuildServiceProvider();
        var factory = serviceProvider.GetRequiredService<ICalDAVClientFactory>();

        // User Alice
        var aliceClient = factory.CreateClient("alice@yandex.ru", "alice-token");
        aliceClient.Should().NotBeNull();

        // User Bob
        var bobClient = factory.CreateClient("bob@yandex.ru", "bob-token");
        bobClient.Should().NotBeNull();

        // Check each user's HttpClient in factory
        var aliceOptions = new CalDAVOptions { Username = "alice@yandex.ru", Password = "alice-token" };
        var aliceHttp = new HttpClient();
        CalDAVServiceCollectionExtensions.ApplyOptions(aliceHttp, aliceOptions);

        var bobOptions = new CalDAVOptions { Username = "bob@yandex.ru", Password = "bob-token" };
        var bobHttp = new HttpClient();
        CalDAVServiceCollectionExtensions.ApplyOptions(bobHttp, bobOptions);

        aliceHttp.DefaultRequestHeaders.Authorization!.Parameter.Should()
            .Be(Convert.ToBase64String(Encoding.UTF8.GetBytes("alice@yandex.ru:alice-token")));
        bobHttp.DefaultRequestHeaders.Authorization!.Parameter.Should()
            .Be(Convert.ToBase64String(Encoding.UTF8.GetBytes("bob@yandex.ru:bob-token")));
    }

    [Fact]
    public void CalDAVClientFactory_WithCustomServerAndCredentials_CreatesClient()
    {
        var services = new ServiceCollection();
        services.AddCalDAVClient();

        var serviceProvider = services.BuildServiceProvider();
        var factory = serviceProvider.GetRequiredService<ICalDAVClientFactory>();

        var client = factory.CreateClient("https://nextcloud.company.com/remote.php/dav/", "corp-user", "corp-token");
        client.Should().NotBeNull();
    }

    [Fact]
    public void AddCalDAVCredentialsProvider_DynamicallyProvidesCredentialsPerScope()
    {
        var services = new ServiceCollection();
        var testProvider = new TestScopedCredentialsProvider();

        services.AddSingleton(testProvider);
        services.AddScoped<ICalDAVCredentialsProvider>(sp => sp.GetRequiredService<TestScopedCredentialsProvider>());

        services.AddCalDAVClient(options =>
        {
            options.SetBaseAddress("https://caldav.company.com/");
        });

        var rootProvider = services.BuildServiceProvider();

        // Scope 1: User Alice
        testProvider.CurrentUser = "alice";
        testProvider.CurrentPass = "alice-pass";
        using (var scope1 = rootProvider.CreateScope())
        {
            var client1 = scope1.ServiceProvider.GetRequiredService<ICalDAVClient>();
            client1.Should().NotBeNull();
        }

        // Scope 2: User Bob
        testProvider.CurrentUser = "bob";
        testProvider.CurrentPass = "bob-pass";
        using (var scope2 = rootProvider.CreateScope())
        {
            var client2 = scope2.ServiceProvider.GetRequiredService<ICalDAVClient>();
            client2.Should().NotBeNull();
        }
    }
}
