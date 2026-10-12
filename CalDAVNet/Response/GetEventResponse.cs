using System.Diagnostics.CodeAnalysis;

namespace CalDAVNet;

public sealed class GetEventResponse : ClientResponse
{
    [MemberNotNullWhen(true, nameof(Event))]
    public override bool IsSuccess => base.IsSuccess;

    public string Href { get; init; } = null!;

    public Event? Event { get; init; }

    internal GetEventResponse() { }

    internal static GetEventResponse NotFound(string href)
    {
        return new GetEventResponse
        {
            Href = href,
            Result = ClientResult.Error,
            ErrorCode = ResponseMappings.StatusCodeToClientError(404),
            ErrorMessage = ResponseMappings.StatusCodeToErrorMessage(404)
        };
    }

    internal static GetEventResponse FromException(string href, Exception ex)
    {
        var statusCode = ex is HttpRequestException httpEx ? (int?)httpEx.StatusCode ?? 500 : 500;
        return new GetEventResponse
        {
            Href = href,
            Result = ClientResult.Error,
            ErrorCode = ResponseMappings.StatusCodeToClientError(statusCode),
            ErrorMessage = ResponseMappings.StatusCodeToErrorMessage(statusCode) ?? ex.Message,
            Exception = ex
        };
    }
}
