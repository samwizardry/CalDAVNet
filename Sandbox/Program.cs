using System.Net.Http.Headers;
using System.Text;

using CalDAVNet;

using Ical.Net.CalendarComponents;
using Ical.Net.DataTypes;
using Ical.Net.Serialization;

using Microsoft.Extensions.DependencyInjection;

namespace Sandbox;

class Program
{
    public static readonly CalendarSerializer CalendarSerializer = new CalendarSerializer();

    static async Task Main(string[] args)
    {
        DotNetEnv.Env.Load();

        var baseAddress = Environment.GetEnvironmentVariable("CALDAVNET_BASEADDRESS")!;
        var username = Environment.GetEnvironmentVariable("CALDAVNET_USERNAME")!;
        var password = Environment.GetEnvironmentVariable("CALDAVNET_PASSWORD")!;

        var services = new ServiceCollection();

        services.AddHttpClient("CalDAVClient");

        var provider = services.BuildServiceProvider();

        using var scope = provider.CreateScope();
        var httpClientFactory = scope.ServiceProvider.GetRequiredService<IHttpClientFactory>();

        var httpClient = httpClientFactory.CreateClient("CalDAVClient");
        httpClient.BaseAddress = new Uri(baseAddress);
        httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}")));

        var client = new CalDAVClient(httpClient);

        await Process(client);

        #region Filter Test

        //FilterBuilder filterBuilder = new FilterBuilder();

        //filterBuilder.AddCompFilter(Constants.CompFilter.VEVENT)
        //    .AddTimeRange(DateTime.UtcNow, DateTime.UtcNow.AddMonths(1))
        //    .AddTextMatch(Constants.PropFilter.SUMMARY, "Meeting", false, "i;unicode-casemap");

        //Console.WriteLine(filterBuilder.ToXElement.ToString());

        #endregion

        #region All day event test

        //DateTime start = new DateTime(2025, 9, 3, 0, 0, 0, DateTimeKind.Local);
        //DateTime end = new DateTime(2025, 9, 5, 0, 0, 0, DateTimeKind.Local);


        //Console.WriteLine(start);
        //Console.WriteLine(DateOnly.FromDateTime(start));
        //Console.WriteLine(DateOnly.FromDateTime(start.ToUniversalTime()));
        //Console.WriteLine(new CalDateTime(DateOnly.FromDateTime(start)));
        //Console.WriteLine(new CalDateTime(DateOnly.FromDateTime(start.ToUniversalTime())));

        //CalendarSerializer serializer = new CalendarSerializer();
        //Ical.Net.Calendar cal = new();

        //var calEvent = new CalendarEvent
        //{
        //    Summary = "Ev",
        //    Start = new CalDateTime(start.ToUniversalTime()),
        //    End = new CalDateTime(end.ToUniversalTime())
        //};
        //Console.WriteLine(serializer.SerializeToString(calEvent));

        //cal.Events.Add(calEvent);
        //Console.WriteLine(serializer.SerializeToString(cal));

        //cal.AddTimeZone(TimeZoneInfo.Local);
        //cal.AddTimeZone(TimeZoneInfo.FindSystemTimeZoneById("Tokyo Standard Time"));
        //Console.WriteLine(serializer.SerializeToString(cal));

        #endregion
    }

    static async Task Process(CalDAVClient client)
    {
        // TEST: Getting UPN
        var upnResponse = await client.GetPrincipalNameAsync();

        if (!upnResponse.IsSuccess)
        {
            throw new Exception(upnResponse.ErrorMessage);
        }

        Console.WriteLine($"User principal name {upnResponse.PrincipalName}");

        // TEST: Getting principal data
        var principalResponse = await client.GetPrincipalAsync(upnResponse.PrincipalName);

        if (!principalResponse.IsSuccess)
        {
            throw new Exception(principalResponse.ErrorMessage);
        }

        var principal = principalResponse.Principal;
        ArgumentNullException.ThrowIfNull(principal.CalendarHomeSet);

        Console.WriteLine();
        Console.WriteLine($"Principal {principal.DisplayName ?? "[NO DISPLAY NAME]"}");
        Console.WriteLine($"Calendar home set {principal.CalendarHomeSet ?? "[NO CALENDAR HOME SET]"}");

        // TEST: Getting calendars
        var calendarResponseCollection = await client.GetCalendarsAsync(principal.CalendarHomeSet!,
            BodyHelper.Propfind(
                [],
                //[XNames.ResourceType, XNames.GetCtag, XNames.SyncToken, XNames.SupportedCalendarComponentSet, XNames.DisplayName, XNames.CurrentUserPrivilegeSet],
                [XNames.AllProp]));

        Console.WriteLine();
        if (calendarResponseCollection.Count > 0)
        {
            Console.WriteLine("Calendars list:");

            foreach (var response in calendarResponseCollection)
            {
                if (response.IsSuccess)
                {
                    var calendar = response.Calendar;
                    Console.WriteLine($"{calendar.DisplayName} - {calendar.Href}. Privileges: {calendar.CurrentUserPrivilegeSet}");
                }
                else
                {
                    Console.WriteLine(response.ErrorMessage);
                }
            }
        }
        else
        {
            Console.WriteLine("Calendars list is empty");
        }

        // TEST: Getting default calendar
        var defaultCalendar = calendarResponseCollection.FirstOrDefault()?.Calendar;
        ArgumentNullException.ThrowIfNull(defaultCalendar);

        Console.WriteLine();
        Console.WriteLine($"Estimated default calendar {defaultCalendar.Href}");

        // TEST: Getting single calendar by it's href
        try
        {
            Console.WriteLine();
            Console.WriteLine($"Loading single calendar {defaultCalendar.Href}");

            var defaultCalendarResponse = await client.GetCalendarAsync(defaultCalendar.Href,
                BodyHelper.Propfind([XNames.ResourceType, XNames.GetCtag, XNames.SyncToken, XNames.DisplayName]));

            if (!defaultCalendarResponse.IsSuccess)
            {
                throw new Exception(defaultCalendarResponse.ErrorMessage);
            }

            defaultCalendar = defaultCalendarResponse.Calendar;

            /*
             * Or
             * 
             * defaultCalendar = await client.GetCalendarAsync(calendars.FirstOrDefault()!.Href,
                BuildBodyHelper.BuildPropfindBody([XNames.AllProp], []));
             * Or
             * 
             * defaultCalendar = await client.GetCalendarAsync(calendars.FirstOrDefault()!.Href,
                BuildBodyHelper.BuildAllPropPropfindBody());
             */

            ArgumentNullException.ThrowIfNull(defaultCalendar);

            Console.WriteLine($"Calendar {defaultCalendar.DisplayName} loaded successfully");
        }
        catch
        {
            Console.WriteLine($"Calendar not found");
            throw;
        }

        // TEST: Sync
        //Console.WriteLine("\n-----------------------------------------");
        //Console.WriteLine("Sync test\n");
        //await ProcessSyncTest(client, defaultCalendar);

        //foreach (var e in syncItemCollection)
        //{
        //    Console.WriteLine($"{e.Href} - {e.Etag}");
        //}

        //Console.WriteLine("\n-----------------------------------------");
        //Console.WriteLine("Processing mailbox logic\n");
        //await ProcessMailboxLogic(client, principal.CalendarHomeSet!);

        //Console.WriteLine("\n-----------------------------------------");
        //Console.WriteLine("Processing calendar logic\n");
        //await ProcessCalendarLogic(client, defaultCalendar);

        //Console.WriteLine("\n-----------------------------------------");
        //Console.WriteLine("Processing event logic\n");
        //await ProcessEventLogic(client, defaultCalendar);

        //await Test(client, defaultCalendar);
    }

    static async Task ProcessMailboxLogic(CalDAVClient client, string calendarHomeSet)
    {
        string calendarName = "calendarToUpdate";
        var body = BodyHelper.Mkcalendar(calendarName, Constants.Comp.VEVENT, null, "#ff0000");
        await client.CreateCalendarAsync(calendarHomeSet, body);

        var responseCollection = await client.GetCalendarsAsync(calendarHomeSet, BodyHelper.Propfind(
            [XNames.ResourceType, XNames.SupportedCalendarComponentSet, XNames.GetCtag, XNames.DisplayName, XNames.Comment]));

        Calendar calendar = null!;

        foreach (var response in responseCollection)
        {
            if (response.IsSuccess)
            {
                if (string.Equals(response.Calendar.DisplayName, calendarName, StringComparison.OrdinalIgnoreCase))
                {
                    calendar = response.Calendar;
                    break;
                }
            }
            else
            {
                Console.WriteLine(response.ErrorMessage);
            }
        }

        // Updating
        await client.UpdateCalendarAsync(calendar.Href, calendar.Ctag!,
            new PropertyUpdateBuilder()
                .AddDisplayName("UpdatedFromNewBuilder")
                .RemoveColor()
                .Build());

        // Deleting
        await client.DeleteAsync(calendar.Href, calendar.Ctag!);
    }

    static async Task ProcessCalendarLogic(CalDAVClient client, Calendar calendar)
    {
        // TEST: Getting filtered events
        var filter = new FilterBuilder()
            .AddCompFilter(new CompFilterBuilder(Constants.Comp.VEVENT)
                .AddTimeRange(DateTime.UtcNow.AddMonths(-12), DateTime.UtcNow.AddMonths(1))
                .ToXElement);

        var responseCollection = await client.GetEventsAsync(calendar.Href, BodyHelper.CalendarQuery(filter.ToXElement));

        if (responseCollection.Count > 0)
        {
            Console.WriteLine("Event collection (loaded with calendar query body):");

            foreach (var response in responseCollection)
            {
                if (response.IsSuccess)
                {
                    Console.WriteLine(response.Event.Href);
                }
                else
                {
                    Console.WriteLine(response.ErrorMessage);
                }
            }

            Console.WriteLine();

            responseCollection = await client.GetEventsAsync(calendar.Href, BodyHelper.CalendarMultiget(responseCollection.Select(x => x.Event!.Href)));

            if (responseCollection.Count > 0)
            {
                Console.WriteLine("Event collection (loaded with multiget body):");

                foreach (var response in responseCollection)
                {
                    if (response.IsSuccess)
                    {
                        var @event = response.Event;
                        if (@event.ICalCalendar is not null)
                        {
                            Console.Write($"Events count: {@event.ICalCalendar.Events.Count}");

                            if (@event.ICalEvent is not null)
                            {
                                Console.Write($" Summary: {@event.ICalEvent.Summary}");
                            }

                            Console.WriteLine();
                        }
                    }
                }
            }
            else
            {
                Console.WriteLine("Calendar has no events");
            }
        }
        else
        {
            Console.WriteLine("Calendar is empty");
        }
    }

    static async Task ProcessEventLogic(CalDAVClient client, Calendar calendar)
    {
        // TEST: Getting events with calendar query body
        var date = new CalDateTime(DateTime.UtcNow.AddHours(1));
        var calendarEvent = new CalendarEvent
        {
            // If Name property is used, it MUST be RFC 5545 compliant
            Summary = "Spider-Man", // Should always be present
            Description = "Hobbit goes here", // optional
            Start = date,
            End = date.AddMinutes(45),
            Uid = Guid.NewGuid().ToString()
        };

        Ical.Net.Calendar cal = new();
        cal.Events.Add(calendarEvent);

        Console.WriteLine("Creating event...");

        await client.CreateEventAsync(calendar.Href, CalendarSerializer.SerializeToString(cal)!);

        Console.WriteLine($"Event {calendarEvent.Summary} created");

        var eventResponse = await client.GetEventAsync(calendar.Href, calendarEvent.Uid);
        if (!eventResponse.IsSuccess || eventResponse.Event.ICalEvent is null)
        {
            throw new Exception("Failed to retrieve event.");
        }

        var @event = eventResponse.Event;
        Console.WriteLine($"{@event.ICalEvent.Summary} loaded");

        Console.WriteLine("Updating event...");

        @event.ICalEvent.Summary = "New summary";
        await client.UpdateEventAsync(@event.Href, @event.Etag!, CalendarSerializer.SerializeToString(@event.ICalCalendar)!);

        Console.WriteLine("Deleting event...");

        await client.DeleteAsync(@event.Href, @event.Etag!);

        Console.WriteLine("Event deleted");
    }

    static async Task Test(CalDAVClient client, Calendar calendar)
    {
        string[] eventHrefs = [
            "/calendars/i.tagiev%40adamcode.ru/events-27560559/141zhi0zyi72c5zy3h9m3pwquyandex.ru.ics",
            "/calendars/i.tagiev%40adamcode.ru/events-27560559/141zhi0zyi72arl6o1o2lb4lyandex.ru.ics",
            "/calendars/i.tagiev%40adamcode.ru/events-27560559/040000008200E00074C5B7101A82E00800000000701880A06550DB01000000000000000010000000065B0A1B3DCBEE4AB8A848F4A08A8CAD.ics",
            "/calendars/i.tagiev%40adamcode.ru/events-27560559/141zhiaxs872csmumc652r9e0yandex.ru.ics",
            "/calendars/i.tagiev%40adamcode.ru/events-27560559/6ad964bf-6b40-4724-b230-3c961d98e023.ics",
            "/calendars/i.tagiev%40adamcode.ru/events-27560559/37dab9ae-8a2c-4e7b-93c3-9efc40c15371.ics",
            "/calendars/i.tagiev%40adamcode.ru/events-27560559/040000008200E00074C5B7101A82E00800000000705F66EE9450DB0100000000000000001000000070CE13D763679E42BF1183B9AF12EC8F.ics",
            "Wrong id"];

        var responseCollection = await client.GetEventsAsync(calendar.Href, BodyHelper.CalendarMultiget(eventHrefs));

        string wrongCalendarId = "my-wrong-calendar-id";

        var calendarResponse = await client.GetCalendarAsync(wrongCalendarId, BodyHelper.Propfind([XNames.ResourceType, XNames.GetCtag, XNames.SyncToken, XNames.DisplayName]));
    }

    static async Task ProcessSyncTest(CalDAVClient client, Calendar calendar)
    {
        // sync-token:1 1768221209000
        // sync-token:1 1768221327000
        // sync-token:1 1768221327000

        var changes = await client.SyncCalendarItemsAsync(calendar.Href, "sync-token:1 1768221209000");

        Console.WriteLine(changes.SyncToken);

        foreach (var change in changes)
        {
            Console.WriteLine($"""
                Status code: {change.StatusCode}
                Href: {change.Href}
                Etag: {change.Etag}

                """);
        }

        var eventResponses = await client.GetEventsAsync(calendar.Href, BodyHelper.CalendarMultiget(changes.Select(x => x.Href)));

        if (eventResponses.Count > 0)
        {
            foreach (var response in eventResponses)
            {
                if (response.IsSuccess)
                {
                    var @event = response.Event;
                    if (@event.ICalCalendar is not null)
                    {
                        Console.Write($"Events count: {@event.ICalCalendar.Events.Count}");

                        if (@event.ICalEvent is not null)
                        {
                            Console.Write($" Summary: {@event.ICalEvent.Summary}");
                        }

                        Console.WriteLine();
                    }
                }
            }
        }
        else
        {
            Console.WriteLine("Calendar has no events");
        }
    }
}
