using Awin.Affiliate.Infrastructure;
using Awin.Affiliate.Reports.Application;
using Awin.Affiliate.Reports.Application.Requests;
using Awin.Affiliate.Reports.Configuration;
using FluentAssertions;

namespace Awin.Affiliate.Tests.Reports;

public class AwinAffiliateReportsClientTests
{
    private static AwinAffiliateReportsOptions ValidOptions() => new()
    {
        PublisherId = "987654",
        AccessToken = "test-token"
    };

    [Fact]
    public async Task GetConversionAsync_Throws_WhenOrderRefNotFound()
    {
        var transport = new FakeAwinHttpTransport
        {
            Responder = _ => new AwinHttpResponse(200, "[]")
        };
        var client = new AwinAffiliateReportsClient(transport, ValidOptions());

        var act = async () => await client.GetConversionAsync("ORDER-NOPE");
        var ex = await act.Should().ThrowAsync<AwinAffiliateNotFoundException>();
        ex.Which.ResourceId.Should().Be("ORDER-NOPE");
    }

    [Fact]
    public async Task GetConversionAsync_ReturnsMatch_ByOrderRef()
    {
        var transport = new FakeAwinHttpTransport
        {
            Responder = _ => new AwinHttpResponse(200, """
                [
                  { "id":"1","advertiserId":"100","orderRef":"OTHER","commissionStatus":"approved","commissionAmount":{"amount":1,"currency":"BRL"},"saleAmount":{"amount":10,"currency":"BRL"} },
                  { "id":"2","advertiserId":"100","orderRef":"MATCH","commissionStatus":"approved","commissionAmount":{"amount":2,"currency":"BRL"},"saleAmount":{"amount":20,"currency":"BRL"} }
                ]
                """)
        };

        var client = new AwinAffiliateReportsClient(transport, ValidOptions());
        var match = await client.GetConversionAsync("MATCH");

        match.Id.Should().Be("2");
        match.OrderReference.Should().Be("MATCH");
    }

    [Fact]
    public async Task ListAdvertisersAsync_MapsProgrammesJson()
    {
        var transport = new FakeAwinHttpTransport
        {
            Responder = path => path.Contains("/programmes/")
                ? new AwinHttpResponse(200, """
                    [
                      { "id":"100","name":"Kabum","relationship":"joined","currencyCode":"BRL","primarySector":"Electronics","country":"BR" },
                      { "id":"200","name":"Submarino","relationship":"pending" }
                    ]
                    """)
                : new AwinHttpResponse(200, "[]")
        };
        var client = new AwinAffiliateReportsClient(transport, ValidOptions());

        var advertisers = await client.ListAdvertisersAsync();

        advertisers.Should().HaveCount(2);
        advertisers[0].Id.Should().Be("100");
        advertisers[0].Name.Should().Be("Kabum");
        advertisers[0].Relationship.Should().Be("joined");
        advertisers[0].CurrencyCode.Should().Be("BRL");
        advertisers[0].CountryCode.Should().Be("BR");
        advertisers[1].Relationship.Should().Be("pending");
    }

    [Fact]
    public async Task ValidateCredentialsAsync_CallsProgrammesEndpoint()
    {
        var transport = new FakeAwinHttpTransport
        {
            Responder = path => path.Contains("/programmes/")
                ? new AwinHttpResponse(200, "[]")
                : throw new InvalidOperationException("unexpected endpoint")
        };
        var client = new AwinAffiliateReportsClient(transport, ValidOptions());

        await client.ValidateCredentialsAsync();

        transport.RequestedPaths.Should().ContainSingle();
        transport.RequestedPaths[0].Should().Be("/publishers/987654/programmes/");
    }

    [Theory]
    [InlineData(401, AwinAffiliateCredentialFailureKind.Unauthorized)]
    [InlineData(403, AwinAffiliateCredentialFailureKind.Forbidden)]
    public async Task ValidateCredentialsAsync_PropagatesAuthException(int statusCode, AwinAffiliateCredentialFailureKind kind)
    {
        var transport = new FakeAwinHttpTransport
        {
            Responder = _ => throw new AwinAffiliateAuthException("nope", statusCode, null, kind)
        };
        var client = new AwinAffiliateReportsClient(transport, ValidOptions());

        var act = async () => await client.ValidateCredentialsAsync();

        var ex = await act.Should().ThrowAsync<AwinAffiliateAuthException>();
        ex.Which.Kind.Should().Be(kind);
        ex.Which.StatusCode.Should().Be(statusCode);
    }

    [Fact]
    public async Task GetClickStatsAsync_AggregatesAcrossRows()
    {
        var transport = new FakeAwinHttpTransport
        {
            Responder = _ => new AwinHttpResponse(200, """
                [
                  { "clicks": 50, "impressions": 1000 },
                  { "clicks": 25, "impressions": 500 }
                ]
                """)
        };
        var client = new AwinAffiliateReportsClient(transport, ValidOptions());
        var stats = await client.GetClickStatsAsync(new AwinClickStatsRequest
        {
            PeriodStart = new DateOnly(2026, 5, 1),
            PeriodEnd = new DateOnly(2026, 5, 7)
        });

        stats.Clicks.Should().Be(75);
        stats.Impressions.Should().Be(1500);
        stats.Supported.Should().BeTrue();
    }

    [Fact]
    public async Task GetClickStatsAsync_ReturnsUnsupported_OnAuthFailure()
    {
        var transport = new FakeAwinHttpTransport
        {
            Responder = _ => throw new AwinAffiliateAuthException("nope", 403, null)
        };
        var client = new AwinAffiliateReportsClient(transport, ValidOptions());

        var stats = await client.GetClickStatsAsync(new AwinClickStatsRequest
        {
            PeriodStart = new DateOnly(2026, 5, 1),
            PeriodEnd = new DateOnly(2026, 5, 7)
        });

        stats.Supported.Should().BeFalse();
        stats.UnsupportedReason.Should().Contain("403");
    }

    [Fact]
    public async Task GetGeneratedLinkUsageAsync_RequiresLinkId()
    {
        var client = new AwinAffiliateReportsClient(new FakeAwinHttpTransport(), ValidOptions());
        var act = async () => await client.GetGeneratedLinkUsageAsync(new AwinGeneratedLinkUsageRequest
        {
            LinkId = "",
            PeriodStart = new DateOnly(2026, 5, 1),
            PeriodEnd = new DateOnly(2026, 5, 7)
        });

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*LinkId*");
    }

    [Fact]
    public async Task Constructor_Throws_OnInvalidOptions()
    {
        var act = () => new AwinAffiliateReportsClient(new FakeAwinHttpTransport(), new AwinAffiliateReportsOptions());
        act.Should().Throw<InvalidOperationException>();
        await Task.CompletedTask;
    }
}
