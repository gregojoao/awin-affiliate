using Awin.Affiliate.Domain;

namespace Awin.Affiliate.Reports.Domain;

/// <summary>
/// Aggregated sales metrics over the requested period. Built from <c>/reports/aggregated/</c>
/// when supported, or computed from <c>/transactions/</c> otherwise.
/// </summary>
public sealed record AwinSalesSummary
{
    /// <summary>Period start (inclusive).</summary>
    public required DateOnly PeriodStart { get; init; }

    /// <summary>Period end (inclusive).</summary>
    public required DateOnly PeriodEnd { get; init; }

    /// <summary>Total number of conversions in the period.</summary>
    public int Conversions { get; init; }

    /// <summary>Total clicks recorded for the period, when the report endpoint returns them.</summary>
    public long? Clicks { get; init; }

    /// <summary>Gross revenue (sum of sale amounts).</summary>
    public Money GrossRevenue { get; init; } = Money.Zero;

    /// <summary>Total commission payable to the publisher.</summary>
    public Money Commission { get; init; } = Money.Zero;

    /// <summary>
    /// Average commission rate expressed as a percentage (commission / gross * 100).
    /// Zero when gross revenue is zero.
    /// </summary>
    public decimal AvgCommissionRate { get; init; }

    /// <summary>Conversion rate (conversions / clicks). Null when clicks are unavailable.</summary>
    public decimal? ConversionRate { get; init; }

    /// <summary>Conversion counts grouped by status.</summary>
    public IReadOnlyDictionary<AwinTransactionStatus, int> ByStatus { get; init; }
        = new Dictionary<AwinTransactionStatus, int>();

    /// <summary>Top advertisers (by commission) found inside the period.</summary>
    public IReadOnlyList<AwinSalesSummaryAdvertiser> TopAdvertisers { get; init; }
        = Array.Empty<AwinSalesSummaryAdvertiser>();

    /// <summary>
    /// True when the underlying data source was available. False when the SDK had to fall
    /// back to a degraded computation (e.g. no access to <c>/reports/aggregated/</c>).
    /// </summary>
    public bool Supported { get; init; } = true;

    /// <summary>Reason the summary is unsupported, when <see cref="Supported"/> is false.</summary>
    public string? UnsupportedReason { get; init; }
}

/// <summary>One row of the <c>TopAdvertisers</c> list inside <see cref="AwinSalesSummary"/>.</summary>
public sealed record AwinSalesSummaryAdvertiser
{
    /// <summary>Advertiser id (matches Awin's <c>advertiserId</c>).</summary>
    public required string AdvertiserId { get; init; }

    /// <summary>Advertiser display name.</summary>
    public string AdvertiserName { get; init; } = string.Empty;

    /// <summary>Number of conversions attributed to this advertiser.</summary>
    public int Conversions { get; init; }

    /// <summary>Total commission earned through this advertiser.</summary>
    public Money Commission { get; init; } = Money.Zero;
}
