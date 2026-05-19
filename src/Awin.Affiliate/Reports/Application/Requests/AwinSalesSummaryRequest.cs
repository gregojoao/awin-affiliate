namespace Awin.Affiliate.Reports.Application.Requests;

/// <summary>Input for <c>GetSalesSummaryAsync</c>.</summary>
public sealed record AwinSalesSummaryRequest
{
    /// <summary>Period start (inclusive).</summary>
    public required DateOnly PeriodStart { get; init; }

    /// <summary>Period end (inclusive).</summary>
    public required DateOnly PeriodEnd { get; init; }

    /// <summary>Optional advertiser id filter.</summary>
    public IReadOnlyList<string> AdvertiserIds { get; init; } = Array.Empty<string>();

    /// <summary>
    /// How many advertisers to include in <c>TopAdvertisers</c>. Defaults to 5; set to 0
    /// to skip the per-advertiser breakdown.
    /// </summary>
    public int TopAdvertiserCount { get; init; } = 5;
}
