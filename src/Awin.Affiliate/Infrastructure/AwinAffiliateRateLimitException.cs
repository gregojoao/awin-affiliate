namespace Awin.Affiliate.Infrastructure;

/// <summary>Raised when Awin returns HTTP 429 (Too Many Requests). Awin documents a soft
/// limit of roughly 20 requests/second per publisher; back off and retry after the
/// <see cref="RetryAfter"/> duration when supplied.</summary>
public sealed class AwinAffiliateRateLimitException : AwinAffiliateException
{
    /// <summary>Server-suggested back-off duration parsed from the Retry-After header, when present.</summary>
    public TimeSpan? RetryAfter { get; }

    /// <summary>Raw response body returned by Awin (truncated).</summary>
    public string? ResponseBody { get; }

    /// <summary>Creates a new <see cref="AwinAffiliateRateLimitException"/>.</summary>
    public AwinAffiliateRateLimitException(string message, TimeSpan? retryAfter, string? responseBody)
        : base(message)
    {
        RetryAfter = retryAfter;
        ResponseBody = responseBody;
    }
}
