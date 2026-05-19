namespace Awin.Affiliate.Infrastructure;

/// <summary>Raised when a point-lookup (e.g. <c>GetConversionAsync</c>) cannot find the
/// requested resource. Maps to HTTP 404 or an empty list result on list endpoints.</summary>
public sealed class AwinAffiliateNotFoundException : AwinAffiliateException
{
    /// <summary>Identifier of the resource that was being looked up (e.g. an order reference).</summary>
    public string ResourceId { get; }

    /// <summary>Creates a new <see cref="AwinAffiliateNotFoundException"/>.</summary>
    public AwinAffiliateNotFoundException(string resourceId, string message)
        : base(message)
    {
        ResourceId = resourceId ?? string.Empty;
    }
}
