namespace Awin.Affiliate.Application;

/// <summary>
/// High-level client for generating Awin affiliate (deep) links. Awin does not expose a
/// public "create link" API the way Shopee or AliExpress do — affiliate URLs are built
/// from the cread.php tracking endpoint. This client encapsulates that construction,
/// validates inputs, and optionally follows short URLs before building the link.
/// </summary>
public interface IAwinAffiliateClient
{
    /// <summary>Generates an Awin affiliate URL for the given destination/advertiser pair.</summary>
    Task<AwinAffiliateLinkResult> GenerateAffiliateLinkAsync(
        AwinAffiliateLinkRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Follows redirects for a short URL and returns the final destination. Returns the
    /// input URL unchanged when the redirect chain cannot be resolved (e.g. network error
    /// or non-HTTP scheme).
    /// </summary>
    Task<Uri> ResolveAwinUrlAsync(
        Uri shortUrl,
        CancellationToken cancellationToken = default);
}
