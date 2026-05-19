namespace Awin.Affiliate.Reports.Application.Requests;

/// <summary>Input for <c>GetClickStatsAsync</c>.</summary>
public sealed record AwinClickStatsRequest
{
    /// <summary>Period start (inclusive).</summary>
    public required DateOnly PeriodStart { get; init; }

    /// <summary>Period end (inclusive).</summary>
    public required DateOnly PeriodEnd { get; init; }

    /// <summary>Optional advertiser id filter.</summary>
    public IReadOnlyList<string> AdvertiserIds { get; init; } = Array.Empty<string>();
}
