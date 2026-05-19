using Awin.Affiliate.Domain;
using Awin.Affiliate.Reports.Application;
using Awin.Affiliate.Reports.Application.Requests;
using Awin.Affiliate.Reports.Configuration;
using Awin.Affiliate.Reports.Domain;
using FluentAssertions;

namespace Awin.Affiliate.Tests.Reports;

public class ListConversionsTests
{
    private static AwinAffiliateReportsOptions ValidOptions() => new()
    {
        PublisherId = "987654",
        AccessToken = "test-token"
    };

    private static AwinAffiliateReportsClient ClientWith(FakeAwinHttpTransport transport)
        => new(transport, ValidOptions());

    [Fact]
    public async Task BuildsExpectedQueryString()
    {
        var transport = new FakeAwinHttpTransport();
        var client = ClientWith(transport);

        await client.ListConversionsAsync(new ListAwinConversionsRequest
        {
            PeriodStart = new DateOnly(2026, 5, 1),
            PeriodEnd = new DateOnly(2026, 5, 7),
            AdvertiserIds = new[] { "111", "222" }
        });

        var path = transport.RequestedPaths.Single();
        path.Should().StartWith("/publishers/987654/transactions/");
        path.Should().Contain("startDate=" + Uri.EscapeDataString("2026-05-01T00:00:00"));
        path.Should().Contain("endDate=" + Uri.EscapeDataString("2026-05-07T23:59:59"));
        path.Should().Contain("dateType=transaction");
        path.Should().Contain("timezone=" + Uri.EscapeDataString("Europe/London"));
        path.Should().Contain("advertiserIds=" + Uri.EscapeDataString("111,222"));
    }

    [Fact]
    public async Task ParsesTransactionsAndPreservesCurrency()
    {
        var transport = new FakeAwinHttpTransport
        {
            Responder = _ => new Awin.Affiliate.Infrastructure.AwinHttpResponse(200, """
                [
                  {
                    "id": "tx-1",
                    "advertiserId": "12345",
                    "advertiserName": "Kabum",
                    "commissionAmount": { "amount": 12.50, "currency": "BRL" },
                    "saleAmount": { "amount": 250.00, "currency": "BRL" },
                    "commissionStatus": "approved",
                    "transactionDate": "2026-05-04T13:24:00Z",
                    "orderRef": "ORDER-42",
                    "clickRef": "telegram"
                  }
                ]
                """)
        };

        var client = ClientWith(transport);
        var page = await client.ListConversionsAsync(new ListAwinConversionsRequest
        {
            PeriodStart = new DateOnly(2026, 5, 1),
            PeriodEnd = new DateOnly(2026, 5, 7)
        });

        page.Items.Should().HaveCount(1);
        var tx = page.Items[0];
        tx.Id.Should().Be("tx-1");
        tx.AdvertiserId.Should().Be("12345");
        tx.AdvertiserName.Should().Be("Kabum");
        tx.SaleAmount.Should().Be(new Money(250m, "BRL"));
        tx.CommissionAmount.Should().Be(new Money(12.5m, "BRL"));
        tx.Status.Should().Be(AwinTransactionStatus.Approved);
        tx.TransactionDate.Should().NotBeNull();
        tx.OrderReference.Should().Be("ORDER-42");
        tx.ClickRef.Should().Be("telegram");
    }

    [Fact]
    public async Task FiltersByStatus()
    {
        var transport = new FakeAwinHttpTransport
        {
            Responder = _ => new Awin.Affiliate.Infrastructure.AwinHttpResponse(200, """
                [
                  { "id": "1", "advertiserId": "1", "commissionStatus": "approved" },
                  { "id": "2", "advertiserId": "1", "commissionStatus": "declined" },
                  { "id": "3", "advertiserId": "1", "commissionStatus": "approved" }
                ]
                """)
        };

        var client = ClientWith(transport);
        var page = await client.ListConversionsAsync(new ListAwinConversionsRequest
        {
            PeriodStart = new DateOnly(2026, 5, 1),
            PeriodEnd = new DateOnly(2026, 5, 7),
            Status = AwinConversionStatusFilter.Approved
        });

        page.Items.Select(i => i.Id).Should().BeEquivalentTo(new[] { "1", "3" });
    }

    [Fact]
    public async Task PassesPaginationParameters()
    {
        var transport = new FakeAwinHttpTransport();
        var client = ClientWith(transport);

        await client.ListConversionsAsync(new ListAwinConversionsRequest
        {
            PeriodStart = new DateOnly(2026, 5, 1),
            PeriodEnd = new DateOnly(2026, 5, 7),
            Offset = 100,
            Limit = 250
        });

        var path = transport.RequestedPaths.Single();
        path.Should().Contain("offset=100");
        path.Should().Contain("limit=250");
    }

    [Fact]
    public async Task Throws_WhenWindowExceeds31Days()
    {
        var client = ClientWith(new FakeAwinHttpTransport());
        var act = async () => await client.ListConversionsAsync(new ListAwinConversionsRequest
        {
            PeriodStart = new DateOnly(2026, 1, 1),
            PeriodEnd = new DateOnly(2026, 3, 1)
        });

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*31-day*");
    }

    [Fact]
    public async Task FallsBackToDefaultAdvertisers()
    {
        var transport = new FakeAwinHttpTransport();
        var options = ValidOptions();
        options.DefaultAdvertiserIds = new[] { "999" };
        var client = new AwinAffiliateReportsClient(transport, options);

        await client.ListConversionsAsync(new ListAwinConversionsRequest
        {
            PeriodStart = new DateOnly(2026, 5, 1),
            PeriodEnd = new DateOnly(2026, 5, 7)
        });

        transport.RequestedPaths.Single().Should().Contain("advertiserIds=999");
    }

    [Fact]
    public async Task ReturnsEmptyPage_WhenAwinReturnsEmptyArray()
    {
        var transport = new FakeAwinHttpTransport
        {
            Responder = _ => new Awin.Affiliate.Infrastructure.AwinHttpResponse(200, "[]")
        };
        var client = ClientWith(transport);

        var page = await client.ListConversionsAsync(new ListAwinConversionsRequest
        {
            PeriodStart = new DateOnly(2026, 5, 1),
            PeriodEnd = new DateOnly(2026, 5, 7)
        });

        page.Items.Should().BeEmpty();
        page.Count.Should().Be(0);
    }
}
