namespace Awin.Affiliate.Infrastructure;

/// <summary>Raised when the Awin API returns a non-success response or a malformed body.</summary>
public sealed class AwinAffiliateApiException : AwinAffiliateException
{
    /// <summary>Optional HTTP status code, when the failure originated from an HTTP response.</summary>
    public int? StatusCode { get; }

    /// <summary>Optional raw response body returned by the API (truncated to a sensible length).</summary>
    public string? ResponseBody { get; }

    /// <summary>Creates a new <see cref="AwinAffiliateApiException"/>.</summary>
    public AwinAffiliateApiException(string message)
        : base(message)
    {
    }

    /// <summary>Creates a new <see cref="AwinAffiliateApiException"/> wrapping an inner exception.</summary>
    public AwinAffiliateApiException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>Creates a new <see cref="AwinAffiliateApiException"/> with full HTTP context.</summary>
    public AwinAffiliateApiException(string message, int statusCode, string? responseBody)
        : base(message)
    {
        StatusCode = statusCode;
        ResponseBody = responseBody;
    }

    /// <summary>Creates a new <see cref="AwinAffiliateApiException"/> with HTTP context and inner exception.</summary>
    public AwinAffiliateApiException(string message, int statusCode, string? responseBody, Exception innerException)
        : base(message, innerException)
    {
        StatusCode = statusCode;
        ResponseBody = responseBody;
    }
}
