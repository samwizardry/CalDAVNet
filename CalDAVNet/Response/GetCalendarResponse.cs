using System.Diagnostics.CodeAnalysis;

namespace CalDAVNet;

public sealed class GetCalendarResponse : ClientResponse
{
    [MemberNotNullWhen(true, nameof(Calendar))]
    public override bool IsSuccess => base.IsSuccess;

    public string Href { get; init; } = null!;

    public Calendar? Calendar { get; init; }

    internal GetCalendarResponse() { }

    internal static GetCalendarResponse NotFound(string href)
    {
        return new GetCalendarResponse
        {
            Href = href,
            Result = ClientResult.Error,
            ErrorCode = ResponseMappings.StatusCodeToClientError(404),
            ErrorMessage = ResponseMappings.StatusCodeToErrorMessage(404)
        };
    }

    internal static GetCalendarResponse FromException(string href, Exception ex)
    {
        var statusCode = ex is HttpRequestException httpEx ? (int?)httpEx.StatusCode ?? 500 : 500;
        return new GetCalendarResponse
        {
            Href = href,
            Result = ClientResult.Error,
            ErrorCode = ResponseMappings.StatusCodeToClientError(statusCode),
            ErrorMessage = ResponseMappings.StatusCodeToErrorMessage(statusCode) ?? ex.Message,
            Exception = ex
        };
    }
}
