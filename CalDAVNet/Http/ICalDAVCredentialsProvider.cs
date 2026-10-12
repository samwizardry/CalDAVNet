namespace CalDAVNet;

/// <summary>
/// Credentials used for CalDAV Basic Authentication.
/// </summary>
/// <param name="Username">CalDAV account username/login.</param>
/// <param name="Password">CalDAV account password or application-specific token.</param>
public record CalDAVCredentials(string Username, string Password);

/// <summary>
/// Provides CalDAV user credentials dynamically for the current scope or execution context (e.g. from current user's HTTP session or claims).
/// </summary>
public interface ICalDAVCredentialsProvider
{
    /// <summary>
    /// Gets the CalDAV credentials for the current user/context, or null if unauthenticated.
    /// </summary>
    CalDAVCredentials? GetCredentials();
}
