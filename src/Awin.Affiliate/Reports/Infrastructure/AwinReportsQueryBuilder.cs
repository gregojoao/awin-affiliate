using System.Globalization;
using System.Text;

namespace Awin.Affiliate.Reports.Infrastructure;

/// <summary>Builds the query strings expected by Awin's Publisher API report endpoints.</summary>
internal static class AwinReportsQueryBuilder
{
    /// <summary>Formats a <see cref="DateOnly"/> for Awin start dates (<c>yyyy-MM-ddT00:00:00</c>).</summary>
    public static string FormatStart(DateOnly date)
        => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) + "T00:00:00";

    /// <summary>Formats a <see cref="DateOnly"/> for Awin end dates (<c>yyyy-MM-ddT23:59:59</c>).</summary>
    public static string FormatEnd(DateOnly date)
        => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) + "T23:59:59";

    /// <summary>
    /// Builds the <c>/publishers/{id}/transactions/</c> path including query parameters.
    /// </summary>
    public static string BuildTransactionsPath(
        string publisherId,
        DateOnly startDate,
        DateOnly endDate,
        string dateType,
        string timezone,
        IReadOnlyList<string>? advertiserIds = null,
        int? offset = null,
        int? limit = null)
    {
        var qs = new QueryStringBuilder();
        qs.Add("startDate", FormatStart(startDate));
        qs.Add("endDate", FormatEnd(endDate));
        qs.Add("dateType", dateType);
        qs.Add("timezone", timezone);

        if (advertiserIds is { Count: > 0 })
        {
            qs.Add("advertiserIds", string.Join(',', advertiserIds));
        }
        if (offset.HasValue)
        {
            qs.Add("offset", offset.Value.ToString(CultureInfo.InvariantCulture));
        }
        if (limit.HasValue)
        {
            qs.Add("limit", limit.Value.ToString(CultureInfo.InvariantCulture));
        }

        return $"/publishers/{Uri.EscapeDataString(publisherId)}/transactions/{qs.ToQueryString()}";
    }

    /// <summary>Builds the <c>/publishers/{id}/programmes/</c> path.</summary>
    public static string BuildProgrammesPath(string publisherId)
        => $"/publishers/{Uri.EscapeDataString(publisherId)}/programmes/";

    /// <summary>
    /// Builds the <c>/publishers/{id}/reports/aggregated/</c> path used by
    /// <c>GetSalesSummaryAsync</c>.
    /// </summary>
    public static string BuildAggregatedReportPath(
        string publisherId,
        DateOnly startDate,
        DateOnly endDate,
        string dateType,
        string timezone,
        IReadOnlyList<string>? advertiserIds = null)
    {
        var qs = new QueryStringBuilder();
        qs.Add("startDate", FormatStart(startDate));
        qs.Add("endDate", FormatEnd(endDate));
        qs.Add("dateType", dateType);
        qs.Add("timezone", timezone);
        if (advertiserIds is { Count: > 0 })
        {
            qs.Add("advertiserIds", string.Join(',', advertiserIds));
        }

        return $"/publishers/{Uri.EscapeDataString(publisherId)}/reports/aggregated/{qs.ToQueryString()}";
    }

    /// <summary>
    /// Builds the <c>/publishers/{id}/reports/creative/</c> path used by click stats
    /// and generated-link usage.
    /// </summary>
    public static string BuildCreativeReportPath(
        string publisherId,
        DateOnly startDate,
        DateOnly endDate,
        string dateType,
        string timezone,
        string? linkId = null,
        IReadOnlyList<string>? advertiserIds = null)
    {
        var qs = new QueryStringBuilder();
        qs.Add("startDate", FormatStart(startDate));
        qs.Add("endDate", FormatEnd(endDate));
        qs.Add("dateType", dateType);
        qs.Add("timezone", timezone);
        if (!string.IsNullOrWhiteSpace(linkId))
        {
            qs.Add("creativeId", linkId);
        }
        if (advertiserIds is { Count: > 0 })
        {
            qs.Add("advertiserIds", string.Join(',', advertiserIds));
        }

        return $"/publishers/{Uri.EscapeDataString(publisherId)}/reports/creative/{qs.ToQueryString()}";
    }

    private sealed class QueryStringBuilder
    {
        private readonly List<KeyValuePair<string, string>> _entries = new();

        public void Add(string key, string value)
        {
            _entries.Add(new KeyValuePair<string, string>(key, value ?? string.Empty));
        }

        public string ToQueryString()
        {
            if (_entries.Count == 0) return string.Empty;

            var sb = new StringBuilder("?");
            for (var i = 0; i < _entries.Count; i++)
            {
                if (i > 0) sb.Append('&');
                sb.Append(Uri.EscapeDataString(_entries[i].Key));
                sb.Append('=');
                sb.Append(Uri.EscapeDataString(_entries[i].Value));
            }

            return sb.ToString();
        }
    }
}
