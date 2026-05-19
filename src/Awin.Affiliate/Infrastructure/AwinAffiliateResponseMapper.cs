using System.Globalization;
using System.Text.Json;

namespace Awin.Affiliate.Infrastructure;

/// <summary>JSON helpers shared by the link client and reports surface.</summary>
internal static class AwinAffiliateResponseMapper
{
    /// <summary>Parses a JSON body or throws <see cref="AwinAffiliateApiException"/>.</summary>
    public static JsonDocument ParseJson(string body, string contextLabel)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return JsonDocument.Parse("{}");
        }

        try
        {
            return JsonDocument.Parse(body);
        }
        catch (JsonException ex)
        {
            throw new AwinAffiliateApiException(
                $"Awin API ({contextLabel}) returned non-JSON response: {Truncate(body, 500)}",
                ex);
        }
    }

    /// <summary>Reads a string property, normalising number/bool to their textual form.</summary>
    public static string? ReadString(JsonElement element, string propertyName)
    {
        if (element.ValueKind != JsonValueKind.Object ||
            !element.TryGetProperty(propertyName, out var prop))
        {
            return null;
        }

        return prop.ValueKind switch
        {
            JsonValueKind.String => prop.GetString(),
            JsonValueKind.Number => prop.GetRawText(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            _ => null
        };
    }

    /// <summary>Reads a decimal property, parsing numeric strings if necessary.</summary>
    public static decimal? ReadDecimal(JsonElement element, string propertyName)
    {
        if (element.ValueKind != JsonValueKind.Object ||
            !element.TryGetProperty(propertyName, out var prop))
        {
            return null;
        }

        return prop.ValueKind switch
        {
            JsonValueKind.Number when prop.TryGetDecimal(out var d) => d,
            JsonValueKind.String when decimal.TryParse(
                prop.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var ds) => ds,
            _ => null
        };
    }

    /// <summary>Reads a long property.</summary>
    public static long? ReadLong(JsonElement element, string propertyName)
    {
        if (element.ValueKind != JsonValueKind.Object ||
            !element.TryGetProperty(propertyName, out var prop))
        {
            return null;
        }

        return prop.ValueKind switch
        {
            JsonValueKind.Number when prop.TryGetInt64(out var n) => n,
            JsonValueKind.String when long.TryParse(
                prop.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var ns) => ns,
            _ => null
        };
    }

    /// <summary>Reads an int property.</summary>
    public static int? ReadInt(JsonElement element, string propertyName)
        => (int?)ReadLong(element, propertyName);

    /// <summary>Reads a <see cref="DateTimeOffset"/> property in ISO-8601 form.</summary>
    public static DateTimeOffset? ReadDateTimeOffset(JsonElement element, string propertyName)
    {
        var raw = ReadString(element, propertyName);
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        if (DateTimeOffset.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var dto))
        {
            return dto;
        }

        return null;
    }

    private static string Truncate(string value, int max)
        => string.IsNullOrEmpty(value) || value.Length <= max ? value : value[..max];
}
