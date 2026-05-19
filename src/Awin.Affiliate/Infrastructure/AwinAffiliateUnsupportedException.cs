namespace Awin.Affiliate.Infrastructure;

/// <summary>Raised when the caller asks for data that the configured publisher does not
/// have access to (e.g. the creative report endpoint is not enabled on the account).</summary>
public sealed class AwinAffiliateUnsupportedException : AwinAffiliateException
{
    /// <summary>The endpoint or capability that is not supported.</summary>
    public string Capability { get; }

    /// <summary>Creates a new <see cref="AwinAffiliateUnsupportedException"/>.</summary>
    public AwinAffiliateUnsupportedException(string capability, string message)
        : base(message)
    {
        Capability = capability ?? string.Empty;
    }
}
