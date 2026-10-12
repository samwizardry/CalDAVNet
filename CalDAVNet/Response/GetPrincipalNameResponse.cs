using System.Diagnostics.CodeAnalysis;

namespace CalDAVNet;

public sealed class GetPrincipalNameResponse : ClientResponse
{
    [MemberNotNullWhen(true, nameof(PrincipalName))]
    public override bool IsSuccess => base.IsSuccess;

    public string Href { get; init; } = null!;

    public string? PrincipalName { get; init; }

    internal GetPrincipalNameResponse() { }

    internal static GetPrincipalNameResponse NotFound(string href)
    {
        return new GetPrincipalNameResponse
        {
            Href = href,
            Result = ClientResult.Error,
            ErrorCode = ResponseMappings.StatusCodeToClientError(404),
            ErrorMessage = ResponseMappings.StatusCodeToErrorMessage(404)
        };
    }

    internal static GetPrincipalNameResponse FromException(string href, Exception ex)
    {
        var statusCode = ex is HttpRequestException httpEx ? (int?)httpEx.StatusCode ?? 500 : 500;
        return new GetPrincipalNameResponse
        {
            Href = href,
            Result = ClientResult.Error,
            ErrorCode = ResponseMappings.StatusCodeToClientError(statusCode),
            ErrorMessage = ResponseMappings.StatusCodeToErrorMessage(statusCode) ?? ex.Message,
            Exception = ex
        };
    }
}
