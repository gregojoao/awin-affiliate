namespace Awin.Affiliate.Infrastructure;

/// <summary>Raised when Awin returns HTTP 401 or 403 — the access token is missing,
/// expired, or lacks permission for the requested resource.</summary>
public sealed class AwinAffiliateAuthException : AwinAffiliateException
{
    /// <summary>Stable platform identifier for integrations that aggregate credential failures.</summary>
    public string Platform => "awin";

    /// <summary>Stable credential failure classification.</summary>
    public AwinAffiliateCredentialFailureKind Kind { get; }

    /// <summary>Provider-specific error code, when Awin includes one in the response body.</summary>
    public string? ProviderErrorCode { get; }

    /// <summary>Provider-specific error message, when Awin includes one in the response body.</summary>
    public string? ProviderMessage { get; }

    /// <summary>Always true for this exception type.</summary>
    public bool IsCredentialError => true;

    /// <summary>Credential failures are deterministic until credentials or permissions change.</summary>
    public bool IsRetryable => false;

    /// <summary>HTTP status code returned by Awin (401 or 403).</summary>
    public int StatusCode { get; }

    /// <summary>Raw response body returned by Awin (truncated).</summary>
    public string? ResponseBody { get; }

    /// <summary>Creates a new <see cref="AwinAffiliateAuthException"/>.</summary>
    public AwinAffiliateAuthException(string message, int statusCode, string? responseBody)
        : this(message, statusCode, responseBody, KindFromStatusCode(statusCode))
    {
    }

    /// <summary>Creates a new <see cref="AwinAffiliateAuthException"/> with credential failure details.</summary>
    public AwinAffiliateAuthException(
        string message,
        int statusCode,
        string? responseBody,
        AwinAffiliateCredentialFailureKind kind,
        string? providerErrorCode = null,
        string? providerMessage = null)
        : base(message)
    {
        StatusCode = statusCode;
        ResponseBody = responseBody;
        Kind = kind;
        ProviderErrorCode = providerErrorCode;
        ProviderMessage = providerMessage;
    }

    private static AwinAffiliateCredentialFailureKind KindFromStatusCode(int statusCode)
        => statusCode switch
        {
            401 => AwinAffiliateCredentialFailureKind.Unauthorized,
            403 => AwinAffiliateCredentialFailureKind.Forbidden,
            _ => AwinAffiliateCredentialFailureKind.Invalid
        };
}
