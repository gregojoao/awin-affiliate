using Awin.Affiliate.Reports.Application.Requests;
using Awin.Affiliate.Reports.Domain;

namespace Awin.Affiliate.Reports.Application;

/// <summary>
/// Read-only access to Awin Publisher reports. Each method maps to one Awin Publisher API
/// endpoint; see the README for the full mapping table.
/// </summary>
public interface IAwinAffiliateReportsClient
{
    /// <summary>
    /// Validates that the configured publisher id and access token can access Awin reports.
    /// Returns normally when credentials are accepted; authentication failures surface as
    /// <see cref="Awin.Affiliate.Infrastructure.AwinAffiliateAuthException"/>.
    /// </summary>
    Task ValidateCredentialsAsync(CancellationToken cancellationToken = default);

    /// <summary>Lists transactions (a.k.a. conversions) for the requested period.</summary>
    Task<AwinConversionPage> ListConversionsAsync(
        ListAwinConversionsRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns a single conversion by order reference. Throws
    /// <see cref="Awin.Affiliate.Infrastructure.AwinAffiliateNotFoundException"/> when no
    /// matching transaction is found.
    /// </summary>
    Task<AwinConversion> GetConversionAsync(
        string orderReference,
        CancellationToken cancellationToken = default);

    /// <summary>Computes a sales summary for the requested period.</summary>
    Task<AwinSalesSummary> GetSalesSummaryAsync(
        AwinSalesSummaryRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>Returns click/impression counts for the requested period.</summary>
    Task<AwinClickStats> GetClickStatsAsync(
        AwinClickStatsRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>Returns usage statistics for a specific generated link.</summary>
    Task<AwinGeneratedLinkUsage> GetGeneratedLinkUsageAsync(
        AwinGeneratedLinkUsageRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>Lists every advertiser/programme accessible to the publisher.</summary>
    Task<IReadOnlyList<AwinAdvertiser>> ListAdvertisersAsync(
        CancellationToken cancellationToken = default);
}
