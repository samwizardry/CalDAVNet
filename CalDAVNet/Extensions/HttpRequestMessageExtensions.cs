using System.Net.Http.Headers;

namespace CalDAVNet;

public static class HttpRequestMessageExtensions
{
    public static HttpRequestMessage WithBasicAuthorization(this HttpRequestMessage request, string token)
    {
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", token);
        return request;
    }

    public static HttpRequestMessage WithPrefer(this HttpRequestMessage request, string prefer)
    {
        request.Headers.Add("Prefer", prefer);
        return request;
    }

    public static HttpRequestMessage WithDepth(this HttpRequestMessage request, int depth)
    {
        request.Headers.Add("Depth", depth.ToString());
        return request;
    }

    /// <summary>
    /// Adds If-Match header with value of <paramref name="version"/> to the <paramref name="request"/>.
    /// </summary>
    /// <param name="request"><see cref="HttpRequestMessage"/>.</param>
    /// <param name="version">etag or ctag.</param>
    /// <returns></returns>
    public static HttpRequestMessage WithVersion(this HttpRequestMessage request, string version)
    {
        var cleanVersion = version.Trim('\"');
        request.Headers.Add("If-Match", $"\"{cleanVersion}\"");
        return request;
    }
}
