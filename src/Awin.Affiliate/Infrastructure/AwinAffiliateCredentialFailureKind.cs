namespace Awin.Affiliate.Infrastructure;

/// <summary>Stable classification for Awin credential/authentication failures.</summary>
public enum AwinAffiliateCredentialFailureKind
{
    /// <summary>The credential is malformed, unknown, or otherwise invalid.</summary>
    Invalid,

    /// <summary>The credential previously existed but is no longer valid.</summary>
    Expired,

    /// <summary>Awin rejected the request with HTTP 401.</summary>
    Unauthorized,

    /// <summary>Awin rejected the request with HTTP 403.</summary>
    Forbidden
}
