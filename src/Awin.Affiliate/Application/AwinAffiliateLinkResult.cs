namespace Awin.Affiliate.Application;

/// <summary>Result of <see cref="IAwinAffiliateClient.GenerateAffiliateLinkAsync"/>.</summary>
public sealed record AwinAffiliateLinkResult
{
    /// <summary>Affiliate URL ready to be shared with end users.</summary>
    public required Uri AffiliateUrl { get; init; }

    /// <summary>Original destination URL (after redirect resolution, when requested).</summary>
    public required Uri OriginUrl { get; init; }

    /// <summary>Advertiser id used to build the link.</summary>
    public required string AdvertiserId { get; init; }

    /// <summary>How the affiliate URL was produced (see <see cref="AwinAffiliateLinkSource"/>).</summary>
    public required AwinAffiliateLinkSource Source { get; init; }
}
