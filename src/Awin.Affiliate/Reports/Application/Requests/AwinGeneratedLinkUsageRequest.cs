namespace Awin.Affiliate.Reports.Application.Requests;

/// <summary>Input for <c>GetGeneratedLinkUsageAsync</c>.</summary>
public sealed record AwinGeneratedLinkUsageRequest
{
    /// <summary>Period start (inclusive).</summary>
    public required DateOnly PeriodStart { get; init; }

    /// <summary>Period end (inclusive).</summary>
    public required DateOnly PeriodEnd { get; init; }

    /// <summary>Tracking link or creative id to scope the report to.</summary>
    public required string LinkId { get; init; }
}
