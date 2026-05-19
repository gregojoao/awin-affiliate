namespace Awin.Affiliate.Reports.Domain;

/// <summary>A page of conversions returned by <c>ListConversionsAsync</c>.</summary>
public sealed record AwinConversionPage
{
    /// <summary>Empty page sentinel.</summary>
    public static readonly AwinConversionPage Empty = new()
    {
        Items = Array.Empty<AwinConversion>(),
        Offset = 0,
        Count = 0
    };

    /// <summary>Items returned in this page.</summary>
    public required IReadOnlyList<AwinConversion> Items { get; init; }

    /// <summary>Offset of the first item in this page (used for pagination).</summary>
    public required int Offset { get; init; }

    /// <summary>Number of items in this page (≤ requested <c>limit</c>).</summary>
    public required int Count { get; init; }

    /// <summary>Total number of items across all pages, if Awin returns it. Null otherwise.</summary>
    public int? TotalCount { get; init; }
}
