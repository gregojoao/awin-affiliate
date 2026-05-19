namespace Awin.Affiliate.Reports.Domain;

/// <summary>Click and impression statistics for the configured period. Returned by
/// <c>GetClickStatsAsync</c>.</summary>
public sealed record AwinClickStats
{
    /// <summary>Period start (inclusive).</summary>
    public required DateOnly PeriodStart { get; init; }

    /// <summary>Period end (inclusive).</summary>
    public required DateOnly PeriodEnd { get; init; }

    /// <summary>Total clicks.</summary>
    public long Clicks { get; init; }

    /// <summary>Total impressions, when available.</summary>
    public long? Impressions { get; init; }

    /// <summary>True when the underlying data source was available.</summary>
    public bool Supported { get; init; } = true;

    /// <summary>Reason the result is unsupported, when <see cref="Supported"/> is false.</summary>
    public string? UnsupportedReason { get; init; }
}
