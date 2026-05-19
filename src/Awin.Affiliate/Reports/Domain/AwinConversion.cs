using Awin.Affiliate.Domain;

namespace Awin.Affiliate.Reports.Domain;

/// <summary>
/// A single Awin transaction (a.k.a. conversion). Field names follow Awin's wire format
/// closely so that the mapping logic stays obvious; the SDK only adds typed wrappers
/// around <see cref="Money"/> and <see cref="AwinTransactionStatus"/>.
/// </summary>
public sealed record AwinConversion
{
    /// <summary>Awin transaction id.</summary>
    public required string Id { get; init; }

    /// <summary>Numeric advertiser id.</summary>
    public required string AdvertiserId { get; init; }

    /// <summary>Advertiser display name. May be empty when Awin doesn't return it.</summary>
    public string AdvertiserName { get; init; } = string.Empty;

    /// <summary>Sale amount before commission.</summary>
    public Money SaleAmount { get; init; } = Money.Zero;

    /// <summary>Commission amount payable to the publisher.</summary>
    public Money CommissionAmount { get; init; } = Money.Zero;

    /// <summary>Commission status as returned by Awin (typed).</summary>
    public AwinTransactionStatus Status { get; init; } = AwinTransactionStatus.Unknown;

    /// <summary>Date of the transaction (sale or click, depending on the dateType filter used).</summary>
    public DateTimeOffset? TransactionDate { get; init; }

    /// <summary>Order reference supplied by the advertiser, when available.</summary>
    public string? OrderReference { get; init; }

    /// <summary>Click reference (clickref) recorded by Awin.</summary>
    public string? ClickRef { get; init; }

    /// <summary>Second click reference (clickref2), when set.</summary>
    public string? ClickRef2 { get; init; }
}
