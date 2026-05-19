namespace Awin.Affiliate.Infrastructure;

/// <summary>
/// Builds Awin tracking deep links from a publisher id, advertiser id, and destination URL.
/// Awin's tracking URL pattern is:
/// <code>
/// https://www.awin1.com/cread.php?awinmid={advertiserId}&amp;awinaffid={publisherId}&amp;ued={urlEncodedDestUrl}&amp;clickref={subId1}&amp;clickref2={subId2}
/// </code>
/// No HTTP call is required — the link is composed entirely on the client.
/// </summary>
internal static class AwinAffiliateLinkBuilder
{
    /// <summary>Builds the affiliate URL.</summary>
    public static Uri Build(
        Uri trackingEndpoint,
        string publisherId,
        string advertiserId,
        Uri destination,
        IReadOnlyList<string>? subIds)
    {
        ArgumentNullException.ThrowIfNull(trackingEndpoint);
        ArgumentNullException.ThrowIfNull(destination);

        var query = new List<KeyValuePair<string, string>>(5)
        {
            new("awinmid", advertiserId.Trim()),
            new("awinaffid", publisherId.Trim()),
            new("ued", destination.ToString())
        };

        var sanitized = SanitizeSubIds(subIds);
        if (sanitized.Count > 0)
        {
            query.Add(new KeyValuePair<string, string>("clickref", sanitized[0]));
        }
        if (sanitized.Count > 1)
        {
            query.Add(new KeyValuePair<string, string>("clickref2", sanitized[1]));
        }

        var builder = new UriBuilder(trackingEndpoint)
        {
            Query = string.Join('&', query.ConvertAll(kv =>
                $"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value)}"))
        };

        return builder.Uri;
    }

    private static List<string> SanitizeSubIds(IReadOnlyList<string>? subIds)
    {
        if (subIds is null || subIds.Count == 0)
        {
            return new List<string>();
        }

        var result = new List<string>(Math.Min(2, subIds.Count));
        for (var i = 0; i < subIds.Count && result.Count < 2; i++)
        {
            var raw = subIds[i];
            if (string.IsNullOrWhiteSpace(raw)) continue;
            result.Add(raw.Trim());
        }

        return result;
    }
}
