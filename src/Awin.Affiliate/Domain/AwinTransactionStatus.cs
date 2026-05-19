namespace Awin.Affiliate.Domain;

/// <summary>
/// Strongly-typed counterpart to Awin's <c>commissionStatus</c> string. Awin's API
/// uses lower-case literals; <see cref="AwinTransactionStatusExtensions"/> handles parsing
/// and serialization back to the wire form.
/// </summary>
public enum AwinTransactionStatus
{
    /// <summary>Awin reported a status the SDK does not recognise.</summary>
    Unknown = 0,

    /// <summary>Approved by the advertiser; commission is payable. Wire value: <c>approved</c>.</summary>
    Approved,

    /// <summary>Awaiting advertiser validation. Wire value: <c>pending</c>.</summary>
    Pending,

    /// <summary>Rejected by the advertiser; no commission will be paid. Wire value: <c>declined</c>.</summary>
    Declined
}

/// <summary>Extensions for converting <see cref="AwinTransactionStatus"/> to/from wire strings.</summary>
public static class AwinTransactionStatusExtensions
{
    /// <summary>
    /// Parses Awin's <c>commissionStatus</c> wire value. Unrecognised inputs return
    /// <see cref="AwinTransactionStatus.Unknown"/>.
    /// </summary>
    public static AwinTransactionStatus ParseStatus(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return AwinTransactionStatus.Unknown;
        }

        return value.Trim().ToLowerInvariant() switch
        {
            "approved" => AwinTransactionStatus.Approved,
            "pending" => AwinTransactionStatus.Pending,
            "declined" => AwinTransactionStatus.Declined,
            _ => AwinTransactionStatus.Unknown
        };
    }

    /// <summary>Returns the wire representation of an <see cref="AwinTransactionStatus"/>.</summary>
    public static string ToWireString(this AwinTransactionStatus status)
        => status switch
        {
            AwinTransactionStatus.Approved => "approved",
            AwinTransactionStatus.Pending => "pending",
            AwinTransactionStatus.Declined => "declined",
            _ => string.Empty
        };
}
