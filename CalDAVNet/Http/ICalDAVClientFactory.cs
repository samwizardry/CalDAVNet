namespace CalDAVNet;

/// <summary>
/// A factory abstraction for creating <see cref="ICalDAVClient"/> instances with dynamic, per-user, or named configurations.
/// </summary>
public interface ICalDAVClientFactory
{
    /// <summary>
    /// Creates an <see cref="ICalDAVClient"/> instance using the default client configuration.
    /// </summary>
    /// <returns>A new <see cref="ICalDAVClient"/> instance.</returns>
    ICalDAVClient CreateClient();

    /// <summary>
    /// Creates an <see cref="ICalDAVClient"/> instance using the specified client configuration name.
    /// </summary>
    /// <param name="name">The logical name of the client configuration.</param>
    /// <returns>A new <see cref="ICalDAVClient"/> instance.</returns>
    ICalDAVClient CreateClient(string name);

    /// <summary>
    /// Creates an <see cref="ICalDAVClient"/> instance with specific user credentials,
    /// inheriting the default client configuration (base address, timeout, handlers).
    /// </summary>
    /// <param name="username">User-specific login.</param>
    /// <param name="password">User-specific password or application token.</param>
    /// <returns>A new <see cref="ICalDAVClient"/> instance.</returns>
    ICalDAVClient CreateClient(string username, string password);

    /// <summary>
    /// Creates an <see cref="ICalDAVClient"/> instance with specific user credentials and base address.
    /// </summary>
    /// <param name="baseAddress">CalDAV server base address.</param>
    /// <param name="username">User-specific login.</param>
    /// <param name="password">User-specific password or application token.</param>
    /// <returns>A new <see cref="ICalDAVClient"/> instance.</returns>
    ICalDAVClient CreateClient(Uri baseAddress, string username, string password);

    /// <summary>
    /// Creates an <see cref="ICalDAVClient"/> instance with specific user credentials and base address string.
    /// </summary>
    /// <param name="baseAddressUrl">CalDAV server base address URL.</param>
    /// <param name="username">User-specific login.</param>
    /// <param name="password">User-specific password or application token.</param>
    /// <returns>A new <see cref="ICalDAVClient"/> instance.</returns>
    ICalDAVClient CreateClient(string baseAddressUrl, string username, string password);

    /// <summary>
    /// Creates an <see cref="ICalDAVClient"/> instance with dynamic options,
    /// inheriting handlers and policies from the specified named client configuration (or default if null).
    /// </summary>
    /// <param name="options">Dynamic options for the client.</param>
    /// <param name="name">Optional logical name of the underlying HttpClient configuration.</param>
    /// <returns>A new <see cref="ICalDAVClient"/> instance.</returns>
    ICalDAVClient CreateClient(CalDAVOptions options, string? name = null);
}
