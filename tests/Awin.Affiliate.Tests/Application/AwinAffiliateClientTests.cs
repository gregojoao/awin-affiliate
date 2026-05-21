using System.Net;
using Awin.Affiliate.Application;
using Awin.Affiliate.Infrastructure;
using Awin.Affiliate.Tests.Infrastructure;
using FluentAssertions;

namespace Awin.Affiliate.Tests.Application;

public class AwinAffiliateClientTests
{
    private static AwinAffiliateOptions ValidOptions() => new() { PublisherId = "987654" };

    [Fact]
    public async Task GenerateAffiliateLink_BuildsCreadPhpUrl()
    {
        var handler = new FakeHttpMessageHandler((_, _) => throw new InvalidOperationException("network was called"));
        using var http = new HttpClient(handler);
        var client = new AwinAffiliateClient(http, ValidOptions());

        var result = await client.GenerateAffiliateLinkAsync(new AwinAffiliateLinkRequest
        {
            OriginUrl = new Uri("https://www.kabum.com.br/produto/123"),
            AdvertiserId = "12345"
        });

        result.AffiliateUrl.AbsoluteUri.Should().StartWith("https://www.awin1.com/cread.php");
        result.AffiliateUrl.Query.Should().Contain("awinmid=12345");
        result.AffiliateUrl.Query.Should().Contain("awinaffid=987654");
        result.AffiliateUrl.Query.Should().Contain(Uri.EscapeDataString("https://www.kabum.com.br/produto/123"));
        result.Source.Should().Be(AwinAffiliateLinkSource.TrackingDeepLink);
        result.AdvertiserId.Should().Be("12345");
        result.OriginUrl.Should().Be(new Uri("https://www.kabum.com.br/produto/123"));
        handler.Requests.Should().BeEmpty();
    }

    [Fact]
    public async Task GenerateAffiliateLink_AppendsSubIds_AsClickref()
    {
        var client = new AwinAffiliateClient(new HttpClient(), ValidOptions());

        var result = await client.GenerateAffiliateLinkAsync(new AwinAffiliateLinkRequest
        {
            OriginUrl = new Uri("https://www.kabum.com.br/produto/123"),
            AdvertiserId = "12345",
            SubIds = new[] { "telegram", "promo-summer" }
        });

        result.AffiliateUrl.Query.Should().Contain("clickref=telegram");
        result.AffiliateUrl.Query.Should().Contain("clickref2=promo-summer");
    }

    [Fact]
    public async Task GenerateAffiliateLink_IgnoresSubIdsBeyondSecond()
    {
        var client = new AwinAffiliateClient(new HttpClient(), ValidOptions());

        var result = await client.GenerateAffiliateLinkAsync(new AwinAffiliateLinkRequest
        {
            OriginUrl = new Uri("https://www.kabum.com.br/produto/123"),
            AdvertiserId = "12345",
            SubIds = new[] { "one", "two", "three", "four" }
        });

        result.AffiliateUrl.Query.Should().Contain("clickref=one");
        result.AffiliateUrl.Query.Should().Contain("clickref2=two");
        result.AffiliateUrl.Query.Should().NotContain("three");
        result.AffiliateUrl.Query.Should().NotContain("four");
    }

    [Fact]
    public async Task GenerateAffiliateLink_Throws_WhenAdvertiserIdEmpty()
    {
        var client = new AwinAffiliateClient(new HttpClient(), ValidOptions());

        var act = async () => await client.GenerateAffiliateLinkAsync(new AwinAffiliateLinkRequest
        {
            OriginUrl = new Uri("https://www.kabum.com.br/produto/123"),
            AdvertiserId = ""
        });

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*advertiserId*");
    }

    [Fact]
    public async Task GenerateAffiliateLink_Throws_WhenPublisherIdMissing()
    {
        var client = new AwinAffiliateClient(new HttpClient(), new AwinAffiliateOptions());

        var act = async () => await client.GenerateAffiliateLinkAsync(new AwinAffiliateLinkRequest
        {
            OriginUrl = new Uri("https://example.com"),
            AdvertiserId = "12345"
        });

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*PublisherId*");
    }

    [Fact]
    public async Task GenerateAffiliateLink_Throws_OnNonHttpScheme()
    {
        var client = new AwinAffiliateClient(new HttpClient(), ValidOptions());

        var act = async () => await client.GenerateAffiliateLinkAsync(new AwinAffiliateLinkRequest
        {
            OriginUrl = new Uri("ftp://example.com"),
            AdvertiserId = "12345"
        });

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*HTTP/HTTPS*");
    }

    [Fact]
    public async Task ResolveAwinUrlAsync_ReturnsRequestUri_OnSuccess()
    {
        // The fake handler echoes the request URI back via RequestMessage.RequestUri, so the
        // resolver should return the URI it was called with when no redirect mutates it.
        var handler = new FakeHttpMessageHandler(
            new HttpResponseMessage(HttpStatusCode.OK));
        using var http = new HttpClient(handler);
        var client = new AwinAffiliateClient(http, ValidOptions());

        var original = new Uri("https://s.click.test/abc");
        var resolved = await client.ResolveAwinUrlAsync(original);

        resolved.Should().Be(original);
        handler.Requests.Should().ContainSingle();
    }

    [Fact]
    public async Task ResolveAwinUrlAsync_ReturnsOriginalUrl_OnNetworkFailure()
    {
        var handler = new FakeHttpMessageHandler((_, _) => throw new HttpRequestException("boom"));
        using var http = new HttpClient(handler);
        var client = new AwinAffiliateClient(http, ValidOptions());

        var original = new Uri("https://s.click.test/abc");
        var resolved = await client.ResolveAwinUrlAsync(original);

        resolved.Should().Be(original);
    }
}
