namespace Awin.Affiliate.Reports.Domain;

/// <summary>
/// Usage report for a single generated affiliate link (clicks + impressions + conversions).
/// Backed by the Awin creative-report endpoint.
/// </summary>
public sealed record AwinGeneratedLinkUsage
{
    /// <summary>Period start (inclusive).</summary>
    public required DateOnly PeriodStart { get; init; }

    /// <summary>Period end (inclusive).</summary>
    public required DateOnly PeriodEnd { get; init; }

    /// <summary>Tracked link or creative identifier (matches Awin's report row).</summary>
    public string LinkId { get; init; } = string.Empty;

    /// <summary>Total clicks attributed to the link.</summary>
    public long Clicks { get; init; }

    /// <summary>Total impressions, when available.</summary>
    public long? Impressions { get; init; }

    /// <summary>Total conversions attributed to the link.</summary>
    public int Conversions { get; init; }

    /// <summary>True when the underlying data source was available.</summary>
    public bool Supported { get; init; } = true;

    /// <summary>Reason the result is unsupported, when <see cref="Supported"/> is false.</summary>
    public string? UnsupportedReason { get; init; }
}
