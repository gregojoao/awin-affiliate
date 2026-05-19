using System.Text.Json;
using Awin.Affiliate.Domain;
using Awin.Affiliate.Infrastructure;
using Awin.Affiliate.Reports.Domain;

namespace Awin.Affiliate.Reports.Infrastructure;

/// <summary>Maps Awin Publisher API JSON payloads into the SDK's typed Reports domain.</summary>
internal static class AwinReportsResponseMapper
{
    /// <summary>Parses an Awin transactions array.</summary>
    public static IReadOnlyList<AwinConversion> MapTransactions(JsonElement root)
    {
        if (root.ValueKind != JsonValueKind.Array)
        {
            return Array.Empty<AwinConversion>();
        }

        var items = new List<AwinConversion>(root.GetArrayLength());
        foreach (var element in root.EnumerateArray())
        {
            if (element.ValueKind != JsonValueKind.Object) continue;
            items.Add(MapConversion(element));
        }

        return items;
    }

    /// <summary>Maps a single transaction object.</summary>
    public static AwinConversion MapConversion(JsonElement element)
    {
        return new AwinConversion
        {
            Id = AwinAffiliateResponseMapper.ReadString(element, "id") ?? string.Empty,
            AdvertiserId = AwinAffiliateResponseMapper.ReadString(element, "advertiserId") ?? string.Empty,
            AdvertiserName = AwinAffiliateResponseMapper.ReadString(element, "advertiserName") ?? string.Empty,
            SaleAmount = ReadMoney(element, "saleAmount"),
            CommissionAmount = ReadMoney(element, "commissionAmount"),
            Status = AwinTransactionStatusExtensions.ParseStatus(
                AwinAffiliateResponseMapper.ReadString(element, "commissionStatus")),
            TransactionDate = AwinAffiliateResponseMapper.ReadDateTimeOffset(element, "transactionDate"),
            OrderReference = AwinAffiliateResponseMapper.ReadString(element, "orderRef"),
            ClickRef = AwinAffiliateResponseMapper.ReadString(element, "clickRef"),
            ClickRef2 = AwinAffiliateResponseMapper.ReadString(element, "clickRef2")
        };
    }

    /// <summary>Maps the array returned by <c>/programmes/</c>.</summary>
    public static IReadOnlyList<AwinAdvertiser> MapAdvertisers(JsonElement root)
    {
        if (root.ValueKind != JsonValueKind.Array)
        {
            return Array.Empty<AwinAdvertiser>();
        }

        var items = new List<AwinAdvertiser>(root.GetArrayLength());
        foreach (var element in root.EnumerateArray())
        {
            if (element.ValueKind != JsonValueKind.Object) continue;
            items.Add(new AwinAdvertiser
            {
                Id = AwinAffiliateResponseMapper.ReadString(element, "id") ?? string.Empty,
                Name = AwinAffiliateResponseMapper.ReadString(element, "name") ?? string.Empty,
                Relationship = AwinAffiliateResponseMapper.ReadString(element, "relationship") ?? string.Empty,
                CurrencyCode = AwinAffiliateResponseMapper.ReadString(element, "currencyCode"),
                PrimarySector = AwinAffiliateResponseMapper.ReadString(element, "primarySector"),
                CountryCode = AwinAffiliateResponseMapper.ReadString(element, "country")
                              ?? AwinAffiliateResponseMapper.ReadString(element, "countryCode")
            });
        }

        return items;
    }

    /// <summary>
    /// Reads a nested <c>{ amount, currency }</c> object. Returns <see cref="Money.Zero"/>
    /// when the property is missing.
    /// </summary>
    public static Money ReadMoney(JsonElement element, string propertyName)
    {
        if (element.ValueKind != JsonValueKind.Object ||
            !element.TryGetProperty(propertyName, out var moneyElement) ||
            moneyElement.ValueKind != JsonValueKind.Object)
        {
            return Money.Zero;
        }

        var amount = AwinAffiliateResponseMapper.ReadDecimal(moneyElement, "amount") ?? 0m;
        var currency = AwinAffiliateResponseMapper.ReadString(moneyElement, "currency") ?? string.Empty;
        return new Money(amount, currency);
    }
}
