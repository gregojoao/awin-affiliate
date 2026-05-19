using System.Net;
using Awin.Affiliate.Infrastructure;
using Awin.Affiliate.Reports.Application;
using Awin.Affiliate.Reports.Application.Requests;
using Awin.Affiliate.Reports.Configuration;
using Awin.Affiliate.Tests.Infrastructure;
using FluentAssertions;

namespace Awin.Affiliate.Tests.Reports;

public class ErrorMappingTests
{
    private static AwinAffiliateReportsOptions OptionsWith(HttpClient httpClient)
    {
        return new AwinAffiliateReportsOptions
        {
            PublisherId = "987654",
            AccessToken = "test-token",
            Endpoint = httpClient.BaseAddress ?? new Uri("https://api.awin.com"),
            MaxRetries = 0
        };
    }

    private static AwinAffiliateReportsClient BuildClient(FakeHttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.awin.com") };
        var options = OptionsWith(httpClient);
        return new AwinAffiliateReportsClient(httpClient, options);
    }

    [Fact]
    public async Task ListConversions_Surfaces_AuthException_On401()
    {
        var handler = FakeHttpMessageHandler.Json("{\"error\":\"bad token\"}", HttpStatusCode.Unauthorized);
        var client = BuildClient(handler);

        var act = async () => await client.ListConversionsAsync(new ListAwinConversionsRequest
        {
            PeriodStart = new DateOnly(2026, 5, 1),
            PeriodEnd = new DateOnly(2026, 5, 7)
        });

        var ex = await act.Should().ThrowAsync<AwinAffiliateAuthException>();
        ex.Which.StatusCode.Should().Be(401);
    }

    [Fact]
    public async Task ListConversions_Surfaces_RateLimit_On429()
    {
        var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests)
        {
            Content = new StringContent("limited", System.Text.Encoding.UTF8, "application/json")
        };
        var handler = new FakeHttpMessageHandler(response);
        var client = BuildClient(handler);

        var act = async () => await client.ListConversionsAsync(new ListAwinConversionsRequest
        {
            PeriodStart = new DateOnly(2026, 5, 1),
            PeriodEnd = new DateOnly(2026, 5, 7)
        });

        await act.Should().ThrowAsync<AwinAffiliateRateLimitException>();
    }

    [Fact]
    public async Task ListConversions_Surfaces_ApiException_On400()
    {
        var handler = FakeHttpMessageHandler.Json("{\"message\":\"bad request\"}", HttpStatusCode.BadRequest);
        var client = BuildClient(handler);

        var act = async () => await client.ListConversionsAsync(new ListAwinConversionsRequest
        {
            PeriodStart = new DateOnly(2026, 5, 1),
            PeriodEnd = new DateOnly(2026, 5, 7)
        });

        var ex = await act.Should().ThrowAsync<AwinAffiliateApiException>();
        ex.Which.StatusCode.Should().Be(400);
        ex.Which.ResponseBody.Should().Contain("bad request");
    }

    [Fact]
    public async Task ListConversions_Surfaces_ApiException_On500_WithoutRetry()
    {
        var handler = FakeHttpMessageHandler.Json("server error", HttpStatusCode.InternalServerError);
        var client = BuildClient(handler);

        var act = async () => await client.ListConversionsAsync(new ListAwinConversionsRequest
        {
            PeriodStart = new DateOnly(2026, 5, 1),
            PeriodEnd = new DateOnly(2026, 5, 7)
        });

        var ex = await act.Should().ThrowAsync<AwinAffiliateApiException>();
        ex.Which.StatusCode.Should().Be(500);
        handler.Requests.Should().HaveCount(1);
    }
}
