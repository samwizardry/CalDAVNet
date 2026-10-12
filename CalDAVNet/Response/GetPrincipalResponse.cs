using System.Diagnostics.CodeAnalysis;

namespace CalDAVNet;

public sealed class GetPrincipalResponse : ClientResponse
{
    [MemberNotNullWhen(true, nameof(Principal))]
    public override bool IsSuccess => base.IsSuccess;

    public string Href { get; init; } = null!;

    public Principal? Principal { get; init; }

    internal GetPrincipalResponse() { }

    internal static GetPrincipalResponse NotFound(string href)
    {
        return new GetPrincipalResponse
        {
            Href = href,
            Result = ClientResult.Error,
            ErrorCode = ResponseMappings.StatusCodeToClientError(404),
            ErrorMessage = ResponseMappings.StatusCodeToErrorMessage(404)
        };
    }

    internal static GetPrincipalResponse FromException(string href, Exception ex)
    {
        var statusCode = ex is HttpRequestException httpEx ? (int?)httpEx.StatusCode ?? 500 : 500;
        return new GetPrincipalResponse
        {
            Href = href,
            Result = ClientResult.Error,
            ErrorCode = ResponseMappings.StatusCodeToClientError(statusCode),
            ErrorMessage = ResponseMappings.StatusCodeToErrorMessage(statusCode) ?? ex.Message,
            Exception = ex
        };
    }
}
