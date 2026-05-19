namespace Awin.Affiliate.Infrastructure;

/// <summary>Raised when Awin returns HTTP 401 or 403 — the access token is missing,
/// expired, or lacks permission for the requested resource.</summary>
public sealed class AwinAffiliateAuthException : AwinAffiliateException
{
    /// <summary>HTTP status code returned by Awin (401 or 403).</summary>
    public int StatusCode { get; }

    /// <summary>Raw response body returned by Awin (truncated).</summary>
    public string? ResponseBody { get; }

    /// <summary>Creates a new <see cref="AwinAffiliateAuthException"/>.</summary>
    public AwinAffiliateAuthException(string message, int statusCode, string? responseBody)
        : base(message)
    {
        StatusCode = statusCode;
        ResponseBody = responseBody;
    }
}
