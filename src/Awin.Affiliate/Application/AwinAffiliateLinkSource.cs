namespace Awin.Affiliate.Application;

/// <summary>Source of the affiliate URL returned by <see cref="IAwinAffiliateClient"/>.</summary>
public enum AwinAffiliateLinkSource
{
    /// <summary>
    /// Built client-side from the cread.php tracking endpoint with the publisher id, advertiser
    /// id, and destination URL. This is the default — no HTTP call is made.
    /// </summary>
    TrackingDeepLink = 0,

    /// <summary>
    /// Returned by the Awin link-conversion API (when available). Reserved for a future
    /// integration; the SDK does not call this endpoint today.
    /// </summary>
    ApiConverted = 1
}
