namespace Awin.Affiliate.Application;

/// <summary>
/// Input for <see cref="IAwinAffiliateClient.GenerateAffiliateLinkAsync"/>. Awin requires
/// both the destination URL and the advertiser id (a.k.a. <c>awinmid</c>) — without the
/// advertiser id, generated links do not track.
/// </summary>
public sealed record AwinAffiliateLinkRequest
{
    /// <summary>Destination URL the affiliate link should send users to.</summary>
    public required Uri OriginUrl { get; init; }

    /// <summary>
    /// Numeric Awin advertiser id (<c>awinmid</c>). The SDK refuses to generate a link without
    /// this — Awin would silently swallow the click otherwise.
    /// </summary>
    public required string AdvertiserId { get; init; }

    /// <summary>
    /// Optional sub-id values appended to the link as <c>clickref</c> and <c>clickref2</c>.
    /// Awin only honours up to two values; additional entries are silently ignored.
    /// </summary>
    public IReadOnlyList<string> SubIds { get; init; } = Array.Empty<string>();

    /// <summary>
    /// When true, the SDK follows redirects on <see cref="OriginUrl"/> before building the link.
    /// Useful when the input is a short URL that hides the real destination.
    /// </summary>
    public bool ResolveShortUrls { get; init; } = false;
}
