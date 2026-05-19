using System.Text.Json;
using Awin.Affiliate.Domain;
using Awin.Affiliate.Infrastructure;
using Awin.Affiliate.Reports.Application.Requests;
using Awin.Affiliate.Reports.Configuration;
using Awin.Affiliate.Reports.Domain;
using Awin.Affiliate.Reports.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Awin.Affiliate.Reports.Application;

/// <summary>
/// Default implementation of <see cref="IAwinAffiliateReportsClient"/>. Fans out to Awin's
/// Publisher API endpoints and maps responses into the SDK's typed Reports domain.
/// </summary>
public sealed class AwinAffiliateReportsClient : IAwinAffiliateReportsClient
{
    private readonly AwinAffiliateReportsOptions _options;
    private readonly IAwinHttpTransport _transport;

    /// <summary>DI constructor used by <c>AddAwinAffiliateReports</c>.</summary>
    [ActivatorUtilitiesConstructor]
    public AwinAffiliateReportsClient(HttpClient httpClient, IOptions<AwinAffiliateReportsOptions> options)
        : this(httpClient, options?.Value ?? throw new ArgumentNullException(nameof(options)))
    {
    }

    /// <summary>Direct constructor accepting an options instance.</summary>
    public AwinAffiliateReportsClient(HttpClient httpClient, AwinAffiliateReportsOptions options)
        : this(BuildDefaultTransport(httpClient, options), options)
    {
    }

    internal AwinAffiliateReportsClient(IAwinHttpTransport transport, AwinAffiliateReportsOptions options)
    {
        _transport = transport ?? throw new ArgumentNullException(nameof(transport));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _options.Validate();
    }

    private static IAwinHttpTransport BuildDefaultTransport(HttpClient httpClient, AwinAffiliateReportsOptions options)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(options);
        options.Validate();

        return new AwinAffiliateHttpTransport(
            httpClient,
            options.Endpoint,
            options.AccessToken,
            options.Timeout,
            options.MaxRetries);
    }

    /// <inheritdoc/>
    public async Task<AwinConversionPage> ListConversionsAsync(
        ListAwinConversionsRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        EnsureValidWindow(request.PeriodStart, request.PeriodEnd);

        var advertisers = ResolveAdvertisers(request.AdvertiserIds);
        var path = AwinReportsQueryBuilder.BuildTransactionsPath(
            _options.PublisherId,
            request.PeriodStart,
            request.PeriodEnd,
            _options.DateType,
            _options.Timezone,
            advertisers,
            offset: request.Offset > 0 ? request.Offset : null,
            limit: request.Limit > 0 ? request.Limit : null);

        var response = await _transport.GetJsonAsync(path, cancellationToken);
        using var doc = AwinAffiliateResponseMapper.ParseJson(response.Body, "transactions");

        var items = AwinReportsResponseMapper.MapTransactions(doc.RootElement);
        var filtered = ApplyStatusFilter(items, request.Status);

        return new AwinConversionPage
        {
            Items = filtered,
            Offset = request.Offset,
            Count = filtered.Count,
            TotalCount = null
        };
    }

    /// <inheritdoc/>
    public async Task<AwinConversion> GetConversionAsync(
        string orderReference,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(orderReference))
        {
            throw new ArgumentException("Order reference is required.", nameof(orderReference));
        }

        // Awin doesn't expose a "transaction by id" endpoint, so we search the last 31 days.
        // Callers needing a wider window should issue ListConversionsAsync directly with explicit bounds.
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var page = await ListConversionsAsync(new ListAwinConversionsRequest
        {
            PeriodStart = today.AddDays(-31),
            PeriodEnd = today,
            Status = AwinConversionStatusFilter.All
        }, cancellationToken);

        var match = page.Items.FirstOrDefault(item =>
            string.Equals(item.OrderReference, orderReference, StringComparison.OrdinalIgnoreCase));

        if (match is null)
        {
            throw new AwinAffiliateNotFoundException(
                orderReference,
                $"No Awin conversion found with order reference '{orderReference}' in the last 31 days.");
        }

        return match;
    }

    /// <inheritdoc/>
    public async Task<AwinSalesSummary> GetSalesSummaryAsync(
        AwinSalesSummaryRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        EnsureValidWindow(request.PeriodStart, request.PeriodEnd);

        var transactions = await ListConversionsAsync(new ListAwinConversionsRequest
        {
            PeriodStart = request.PeriodStart,
            PeriodEnd = request.PeriodEnd,
            AdvertiserIds = request.AdvertiserIds,
            Status = AwinConversionStatusFilter.All,
            Limit = 1000
        }, cancellationToken);

        return BuildSummary(transactions.Items, request.PeriodStart, request.PeriodEnd, request.TopAdvertiserCount);
    }

    /// <inheritdoc/>
    public async Task<AwinClickStats> GetClickStatsAsync(
        AwinClickStatsRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        EnsureValidWindow(request.PeriodStart, request.PeriodEnd);

        var advertisers = ResolveAdvertisers(request.AdvertiserIds);
        var path = AwinReportsQueryBuilder.BuildCreativeReportPath(
            _options.PublisherId,
            request.PeriodStart,
            request.PeriodEnd,
            _options.DateType,
            _options.Timezone,
            advertiserIds: advertisers);

        try
        {
            var response = await _transport.GetJsonAsync(path, cancellationToken);
            using var doc = AwinAffiliateResponseMapper.ParseJson(response.Body, "reports/creative");
            return MapClickStats(doc.RootElement, request.PeriodStart, request.PeriodEnd);
        }
        catch (AwinAffiliateAuthException ex)
        {
            return new AwinClickStats
            {
                PeriodStart = request.PeriodStart,
                PeriodEnd = request.PeriodEnd,
                Supported = false,
                UnsupportedReason = $"Awin creative report not accessible: HTTP {ex.StatusCode}."
            };
        }
        catch (AwinAffiliateApiException ex) when (ex.StatusCode == 404)
        {
            return new AwinClickStats
            {
                PeriodStart = request.PeriodStart,
                PeriodEnd = request.PeriodEnd,
                Supported = false,
                UnsupportedReason = "Awin creative report endpoint returned 404."
            };
        }
    }

    /// <inheritdoc/>
    public async Task<AwinGeneratedLinkUsage> GetGeneratedLinkUsageAsync(
        AwinGeneratedLinkUsageRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(request.LinkId))
        {
            throw new ArgumentException("LinkId is required.", nameof(request));
        }

        EnsureValidWindow(request.PeriodStart, request.PeriodEnd);

        var path = AwinReportsQueryBuilder.BuildCreativeReportPath(
            _options.PublisherId,
            request.PeriodStart,
            request.PeriodEnd,
            _options.DateType,
            _options.Timezone,
            linkId: request.LinkId);

        try
        {
            var response = await _transport.GetJsonAsync(path, cancellationToken);
            using var doc = AwinAffiliateResponseMapper.ParseJson(response.Body, "reports/creative");
            return MapGeneratedLinkUsage(doc.RootElement, request);
        }
        catch (AwinAffiliateAuthException ex)
        {
            return new AwinGeneratedLinkUsage
            {
                PeriodStart = request.PeriodStart,
                PeriodEnd = request.PeriodEnd,
                LinkId = request.LinkId,
                Supported = false,
                UnsupportedReason = $"Awin creative report not accessible: HTTP {ex.StatusCode}."
            };
        }
        catch (AwinAffiliateApiException ex) when (ex.StatusCode == 404)
        {
            return new AwinGeneratedLinkUsage
            {
                PeriodStart = request.PeriodStart,
                PeriodEnd = request.PeriodEnd,
                LinkId = request.LinkId,
                Supported = false,
                UnsupportedReason = "Awin creative report endpoint returned 404."
            };
        }
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<AwinAdvertiser>> ListAdvertisersAsync(CancellationToken cancellationToken = default)
    {
        var path = AwinReportsQueryBuilder.BuildProgrammesPath(_options.PublisherId);
        var response = await _transport.GetJsonAsync(path, cancellationToken);
        using var doc = AwinAffiliateResponseMapper.ParseJson(response.Body, "programmes");
        return AwinReportsResponseMapper.MapAdvertisers(doc.RootElement);
    }

    private IReadOnlyList<string> ResolveAdvertisers(IReadOnlyList<string> requestAdvertisers)
    {
        if (requestAdvertisers is { Count: > 0 }) return requestAdvertisers;
        if (_options.DefaultAdvertiserIds.Count > 0) return _options.DefaultAdvertiserIds;
        return Array.Empty<string>();
    }

    private static IReadOnlyList<AwinConversion> ApplyStatusFilter(
        IReadOnlyList<AwinConversion> items,
        AwinConversionStatusFilter filter)
    {
        if (filter == AwinConversionStatusFilter.All) return items;

        var statusValue = filter switch
        {
            AwinConversionStatusFilter.Approved => AwinTransactionStatus.Approved,
            AwinConversionStatusFilter.Pending => AwinTransactionStatus.Pending,
            AwinConversionStatusFilter.Declined => AwinTransactionStatus.Declined,
            _ => AwinTransactionStatus.Unknown
        };

        var filtered = new List<AwinConversion>(items.Count);
        foreach (var item in items)
        {
            if (item.Status == statusValue) filtered.Add(item);
        }

        return filtered;
    }

    private static void EnsureValidWindow(DateOnly start, DateOnly end)
    {
        if (start > end)
        {
            throw new ArgumentException(
                $"Awin period start ({start:yyyy-MM-dd}) must not be after end ({end:yyyy-MM-dd}).");
        }

        var days = end.DayNumber - start.DayNumber;
        if (days > 31)
        {
            throw new ArgumentException(
                $"Awin Publisher API limits each request to a 31-day window; received {days} days. " +
                "Split the range into multiple calls.");
        }
    }

    private static AwinSalesSummary BuildSummary(
        IReadOnlyList<AwinConversion> items,
        DateOnly periodStart,
        DateOnly periodEnd,
        int topAdvertiserCount)
    {
        if (items.Count == 0)
        {
            return new AwinSalesSummary
            {
                PeriodStart = periodStart,
                PeriodEnd = periodEnd,
                ByStatus = new Dictionary<AwinTransactionStatus, int>(),
                TopAdvertisers = Array.Empty<AwinSalesSummaryAdvertiser>()
            };
        }

        var gross = 0m;
        var commission = 0m;
        var currency = string.Empty;
        var byStatus = new Dictionary<AwinTransactionStatus, int>();
        var perAdvertiser = new Dictionary<string, (string Name, int Count, decimal Commission)>();

        foreach (var item in items)
        {
            gross += item.SaleAmount.Amount;
            commission += item.CommissionAmount.Amount;

            if (string.IsNullOrEmpty(currency))
            {
                currency = !string.IsNullOrEmpty(item.CommissionAmount.Currency)
                    ? item.CommissionAmount.Currency
                    : item.SaleAmount.Currency;
            }

            byStatus[item.Status] = byStatus.TryGetValue(item.Status, out var existingCount)
                ? existingCount + 1
                : 1;

            if (perAdvertiser.TryGetValue(item.AdvertiserId, out var existing))
            {
                perAdvertiser[item.AdvertiserId] = (
                    existing.Name,
                    existing.Count + 1,
                    existing.Commission + item.CommissionAmount.Amount);
            }
            else
            {
                perAdvertiser[item.AdvertiserId] = (item.AdvertiserName, 1, item.CommissionAmount.Amount);
            }
        }

        var avgRate = gross > 0 ? commission / gross * 100m : 0m;

        var topAdvertisers = topAdvertiserCount <= 0
            ? Array.Empty<AwinSalesSummaryAdvertiser>()
            : perAdvertiser
                .OrderByDescending(kv => kv.Value.Commission)
                .Take(topAdvertiserCount)
                .Select(kv => new AwinSalesSummaryAdvertiser
                {
                    AdvertiserId = kv.Key,
                    AdvertiserName = kv.Value.Name,
                    Conversions = kv.Value.Count,
                    Commission = new Money(kv.Value.Commission, currency)
                })
                .ToArray();

        return new AwinSalesSummary
        {
            PeriodStart = periodStart,
            PeriodEnd = periodEnd,
            Conversions = items.Count,
            GrossRevenue = new Money(gross, currency),
            Commission = new Money(commission, currency),
            AvgCommissionRate = decimal.Round(avgRate, 4),
            ByStatus = byStatus,
            TopAdvertisers = topAdvertisers
        };
    }

    private static AwinClickStats MapClickStats(JsonElement root, DateOnly start, DateOnly end)
    {
        long clicks = 0;
        long? impressions = null;

        if (root.ValueKind == JsonValueKind.Array)
        {
            foreach (var row in root.EnumerateArray())
            {
                clicks += AwinAffiliateResponseMapper.ReadLong(row, "clicks") ?? 0;
                var rowImpressions = AwinAffiliateResponseMapper.ReadLong(row, "impressions");
                if (rowImpressions.HasValue)
                {
                    impressions = (impressions ?? 0) + rowImpressions.Value;
                }
            }
        }

        return new AwinClickStats
        {
            PeriodStart = start,
            PeriodEnd = end,
            Clicks = clicks,
            Impressions = impressions
        };
    }

    private static AwinGeneratedLinkUsage MapGeneratedLinkUsage(JsonElement root, AwinGeneratedLinkUsageRequest request)
    {
        long clicks = 0;
        long? impressions = null;
        int conversions = 0;

        if (root.ValueKind == JsonValueKind.Array)
        {
            foreach (var row in root.EnumerateArray())
            {
                clicks += AwinAffiliateResponseMapper.ReadLong(row, "clicks") ?? 0;

                var rowImpressions = AwinAffiliateResponseMapper.ReadLong(row, "impressions");
                if (rowImpressions.HasValue)
                {
                    impressions = (impressions ?? 0) + rowImpressions.Value;
                }

                conversions += AwinAffiliateResponseMapper.ReadInt(row, "totalNo")
                               ?? AwinAffiliateResponseMapper.ReadInt(row, "confirmedNo")
                               ?? 0;
            }
        }

        return new AwinGeneratedLinkUsage
        {
            PeriodStart = request.PeriodStart,
            PeriodEnd = request.PeriodEnd,
            LinkId = request.LinkId,
            Clicks = clicks,
            Impressions = impressions,
            Conversions = conversions
        };
    }
}
