namespace Awin.Affiliate.Reports.Domain;

/// <summary>
/// A programme/advertiser the publisher has a relationship with. Returned by
/// <c>ListAdvertisersAsync</c> against <c>/publishers/{publisherId}/programmes/</c>.
/// </summary>
public sealed record AwinAdvertiser
{
    /// <summary>Numeric advertiser id.</summary>
    public required string Id { get; init; }

    /// <summary>Advertiser display name.</summary>
    public required string Name { get; init; }

    /// <summary>Awin relationship status (<c>joined</c>, <c>pending</c>, etc.).</summary>
    public string Relationship { get; init; } = string.Empty;

    /// <summary>Default currency code.</summary>
    public string? CurrencyCode { get; init; }

    /// <summary>Primary sector / category supplied by Awin.</summary>
    public string? PrimarySector { get; init; }

    /// <summary>ISO country code where the programme operates.</summary>
    public string? CountryCode { get; init; }
}
