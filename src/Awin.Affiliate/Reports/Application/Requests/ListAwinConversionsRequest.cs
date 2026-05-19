using Awin.Affiliate.Reports.Domain;

namespace Awin.Affiliate.Reports.Application.Requests;

/// <summary>
/// Input for <c>ListConversionsAsync</c>. Awin enforces a maximum 31-day window per call —
/// callers that need a longer range should issue multiple requests.
/// </summary>
public sealed record ListAwinConversionsRequest
{
    /// <summary>Period start (inclusive).</summary>
    public required DateOnly PeriodStart { get; init; }

    /// <summary>Period end (inclusive).</summary>
    public required DateOnly PeriodEnd { get; init; }

    /// <summary>
    /// Optional status filter. Defaults to <see cref="AwinConversionStatusFilter.All"/>,
    /// matching Awin's default behaviour.
    /// </summary>
    public AwinConversionStatusFilter Status { get; init; } = AwinConversionStatusFilter.All;

    /// <summary>
    /// Optional advertiser ids to filter on. When empty, the SDK falls back to
    /// <c>AwinAffiliateReportsOptions.DefaultAdvertiserIds</c>; when both are empty, all
    /// advertisers are included.
    /// </summary>
    public IReadOnlyList<string> AdvertiserIds { get; init; } = Array.Empty<string>();

    /// <summary>Pagination offset (number of transactions to skip).</summary>
    public int Offset { get; init; }

    /// <summary>Pagination limit (default 1000, max 1000 per Awin docs).</summary>
    public int Limit { get; init; } = 1000;
}
