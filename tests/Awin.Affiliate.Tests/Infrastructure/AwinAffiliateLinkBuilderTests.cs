using Awin.Affiliate.Infrastructure;
using FluentAssertions;

namespace Awin.Affiliate.Tests.Infrastructure;

public class AwinAffiliateLinkBuilderTests
{
    private static readonly Uri TrackingEndpoint = new("https://www.awin1.com/cread.php");

    [Fact]
    public void Build_OrdersParamsConsistently()
    {
        var uri = AwinAffiliateLinkBuilder.Build(
            TrackingEndpoint,
            "987",
            "12345",
            new Uri("https://shop.test/path?a=1&b=2"),
            new[] { "telegram" });

        uri.AbsoluteUri.Should().StartWith("https://www.awin1.com/cread.php?");
        uri.Query.Should().Contain("awinmid=12345");
        uri.Query.Should().Contain("awinaffid=987");
        uri.Query.Should().Contain("ued=" + Uri.EscapeDataString("https://shop.test/path?a=1&b=2"));
        uri.Query.Should().Contain("clickref=telegram");
    }

    [Fact]
    public void Build_OmitsClickrefs_WhenNoSubIds()
    {
        var uri = AwinAffiliateLinkBuilder.Build(
            TrackingEndpoint,
            "987",
            "12345",
            new Uri("https://shop.test/path"),
            null);

        uri.Query.Should().NotContain("clickref");
    }

    [Fact]
    public void Build_PreservesDestinationQueryParametersWhenEncoded()
    {
        var destination = new Uri("https://shop.test/p?utm_source=newsletter&utm_medium=email");

        var uri = AwinAffiliateLinkBuilder.Build(TrackingEndpoint, "987", "12345", destination, null);

        // The destination's '&' inside ued= must be encoded, so query parameters from the
        // destination cannot leak into the affiliate link as top-level params.
        var query = uri.Query;
        query.Should().Contain("ued=" + Uri.EscapeDataString(destination.ToString()));
        query.Split('&').Should().HaveCount(3); // awinmid, awinaffid, ued
    }
}
