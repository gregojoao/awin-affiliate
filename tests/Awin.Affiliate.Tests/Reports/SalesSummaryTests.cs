using Awin.Affiliate.Domain;
using Awin.Affiliate.Infrastructure;
using Awin.Affiliate.Reports.Application;
using Awin.Affiliate.Reports.Application.Requests;
using Awin.Affiliate.Reports.Configuration;
using FluentAssertions;

namespace Awin.Affiliate.Tests.Reports;

public class SalesSummaryTests
{
    private static AwinAffiliateReportsOptions ValidOptions() => new()
    {
        PublisherId = "987654",
        AccessToken = "test-token"
    };

    [Fact]
    public async Task AggregatesAcrossStatusAndAdvertisers()
    {
        var transport = new FakeAwinHttpTransport
        {
            Responder = _ => new AwinHttpResponse(200, """
                [
                  { "id":"1","advertiserId":"100","advertiserName":"Kabum",
                    "commissionAmount":{"amount":10,"currency":"BRL"},
                    "saleAmount":{"amount":100,"currency":"BRL"},
                    "commissionStatus":"approved" },
                  { "id":"2","advertiserId":"100","advertiserName":"Kabum",
                    "commissionAmount":{"amount":5,"currency":"BRL"},
                    "saleAmount":{"amount":50,"currency":"BRL"},
                    "commissionStatus":"pending" },
                  { "id":"3","advertiserId":"200","advertiserName":"Submarino",
                    "commissionAmount":{"amount":20,"currency":"BRL"},
                    "saleAmount":{"amount":200,"currency":"BRL"},
                    "commissionStatus":"approved" }
                ]
                """)
        };

        var client = new AwinAffiliateReportsClient(transport, ValidOptions());

        var summary = await client.GetSalesSummaryAsync(new AwinSalesSummaryRequest
        {
            PeriodStart = new DateOnly(2026, 5, 1),
            PeriodEnd = new DateOnly(2026, 5, 7)
        });

        summary.Conversions.Should().Be(3);
        summary.GrossRevenue.Should().Be(new Money(350m, "BRL"));
        summary.Commission.Should().Be(new Money(35m, "BRL"));
        summary.AvgCommissionRate.Should().Be(10m); // 35 / 350 * 100
        summary.ByStatus[AwinTransactionStatus.Approved].Should().Be(2);
        summary.ByStatus[AwinTransactionStatus.Pending].Should().Be(1);
        summary.TopAdvertisers.Should().HaveCount(2);
        summary.TopAdvertisers[0].AdvertiserId.Should().Be("200"); // highest commission
        summary.TopAdvertisers[0].Commission.Should().Be(new Money(20m, "BRL"));
        summary.Supported.Should().BeTrue();
    }

    [Fact]
    public async Task ZeroTransactions_ReturnsEmptySummary_WithoutException()
    {
        var transport = new FakeAwinHttpTransport
        {
            Responder = _ => new AwinHttpResponse(200, "[]")
        };

        var client = new AwinAffiliateReportsClient(transport, ValidOptions());
        var summary = await client.GetSalesSummaryAsync(new AwinSalesSummaryRequest
        {
            PeriodStart = new DateOnly(2026, 5, 1),
            PeriodEnd = new DateOnly(2026, 5, 7)
        });

        summary.Conversions.Should().Be(0);
        summary.GrossRevenue.Amount.Should().Be(0m);
        summary.Commission.Amount.Should().Be(0m);
        summary.AvgCommissionRate.Should().Be(0m);
        summary.ByStatus.Should().BeEmpty();
        summary.TopAdvertisers.Should().BeEmpty();
    }

    [Fact]
    public async Task TopAdvertiserCountZero_SkipsBreakdown()
    {
        var transport = new FakeAwinHttpTransport
        {
            Responder = _ => new AwinHttpResponse(200, """
                [ { "id":"1","advertiserId":"100","commissionAmount":{"amount":1,"currency":"BRL"},"saleAmount":{"amount":10,"currency":"BRL"},"commissionStatus":"approved" } ]
                """)
        };

        var client = new AwinAffiliateReportsClient(transport, ValidOptions());
        var summary = await client.GetSalesSummaryAsync(new AwinSalesSummaryRequest
        {
            PeriodStart = new DateOnly(2026, 5, 1),
            PeriodEnd = new DateOnly(2026, 5, 7),
            TopAdvertiserCount = 0
        });

        summary.Conversions.Should().Be(1);
        summary.TopAdvertisers.Should().BeEmpty();
    }
}
