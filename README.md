# CalDAVNet

[![NuGet Version](https://img.shields.io/nuget/v/Semisoft.LdapHelper.svg)](https://www.nuget.org/packages/Semisoft.LdapHelper/)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![.NET 10](https://img.shields.io/badge/.NET-10.0-purple.svg)](https://dotnet.microsoft.com/)

.NET client library for communicating with CalDAV servers compliant with CalDAV (RFC 4791), WebDAV (RFC 4918), and iCalendar (RFC 5545).

---

## Features

- **Calendars & Events**: Discover calendar collections, query events, and perform CRUD operations on `.ics` resources.
- **Recurring Events**: Support for master events (`MasterEvent`), modified occurrences (`RecurrenceExceptions`), and date-range expansion (`GetOccurrences`).
- **WebDAV Sync**: Incremental collection synchronization using WebDAV sync tokens (`SyncCalendarItemsAsync`).
- **ASP.NET Core Integration**:
  - `AddCalDAVClient` registration backed by `IHttpClientFactory` connection pooling.
  - Multi-user authentication support via `ICalDAVClientFactory` and scoped `ICalDAVCredentialsProvider`.
- **Safe Error Handling**: Structured response objects with status codes and failure details instead of throwing unhandled exceptions.

---

## Building and Testing

### Prerequisites
- [.NET 10 SDK](https://dotnet.microsoft.com/)

### Build
```bash
dotnet build
```

### Run Tests
Unit tests run completely offline and verify XML serialization, model parsing, error handling, and DI registration:
```bash
dotnet test
```

To run integration tests against a live CalDAV server, create a `.env` file in the `CalDAVNet.Tests` directory:
```env
CALDAVNET_BASEADDRESS=https://caldav.host.ru/
CALDAVNET_USERNAME=your_username@host.ru
CALDAVNET_PASSWORD=your_app_password
```

---

## Usage Examples

### 1. Standalone Client (Without DI)

```csharp
using System.Net.Http.Headers;
using System.Text;
using CalDAVNet;
using CalDAVNet.Http;

var httpClient = new HttpClient
{
    BaseAddress = new Uri("https://caldav.host.ru/")
};

var auth = Convert.ToBase64String(Encoding.UTF8.GetBytes("login:password"));
httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", auth);

ICalDAVClient client = new CalDAVClient(httpClient);

// Retrieve current principal and calendars
var principalName = await client.GetPrincipalNameAsync();
var principal = await client.GetPrincipalAsync(principalName.PrincipalName!);

var calendars = await client.GetCalendarsAsync(
    principal.Principal!.CalendarHomeSet!, 
    CalDAVPayloads.AllProp());

foreach (var cal in calendars)
{
    Console.WriteLine($"Calendar: {cal.Calendar?.DisplayName} ({cal.Calendar?.Href})");
}
```

---

### 2. ASP.NET Core Integration (Dependency Injection)

#### Registration in `Program.cs`:
```csharp
builder.Services.AddCalDAVClient(options =>
{
    options.SetBaseAddress("https://caldav.host.ru/");
    options.SetCredentials("my_user@host.ru", "app_password");
});
```

#### Injecting into a service:
```csharp
public class MyCalendarService(ICalDAVClient calDavClient)
{
    public async Task<GetCalendarResponse> GetCalendarAsync(string href)
    {
        return await calDavClient.GetCalendarAsync(href, CalDAVPayloads.AllProp());
    }
}
```

---

### 3. Multi-User Applications

When users in your application authenticate with their own separate CalDAV credentials:

```csharp
// Register server base address in Program.cs:
builder.Services.AddCalDAVClient(options =>
{
    options.SetBaseAddress("https://caldav.host.ru/");
});

// Inject ICalDAVClientFactory into your service:
public class UserService(ICalDAVClientFactory clientFactory)
{
    public async Task SyncUserAsync(string userLogin, string userPassword)
    {
        // Creates an isolated client for this specific user
        // while reusing pooled connections from IHttpClientFactory:
        var client = clientFactory.CreateClient(userLogin, userPassword);

        var events = await client.GetEventsAsync("/calendars/user/events/", CalDAVPayloads.AllProp());
        // ...
    }
}
```

---

### 4. Working with Recurring Events

```csharp
var response = await client.GetEventAsync("/calendars/user/events/", "meeting.ics");

if (response.IsSuccess && response.Event != null)
{
    var ev = response.Event;

    // Master recurring event (without RECURRENCE-ID)
    Console.WriteLine($"Master summary: {ev.MasterEvent?.Summary}");

    // Modified occurrences / exceptions (with RECURRENCE-ID)
    foreach (var exception in ev.RecurrenceExceptions)
    {
        Console.WriteLine($"Modified occurrence: {exception.Summary} at {exception.Start}");
    }

    // Expand occurrences within a specified date/time range:
    var occurrences = ev.GetOccurrences(DateTime.UtcNow, DateTime.UtcNow.AddDays(7));
    foreach (var occ in occurrences)
    {
        Console.WriteLine($"Occurrence: {occ.Period.StartTime}");
    }
}
```

---

## License

This project is licensed under the [MIT](LICENSE) License.
