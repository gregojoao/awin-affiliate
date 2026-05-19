using Awin.Affiliate.Infrastructure;
using FluentAssertions;

namespace Awin.Affiliate.Tests.Infrastructure;

public class AwinAffiliateExceptionTests
{
    [Fact]
    public void ApiException_StoresStatusCodeAndBody()
    {
        var ex = new AwinAffiliateApiException("boom", 500, "internal error");
        ex.StatusCode.Should().Be(500);
        ex.ResponseBody.Should().Be("internal error");
    }

    [Fact]
    public void AuthException_AlwaysCarriesStatusCode()
    {
        var ex = new AwinAffiliateAuthException("nope", 401, "{\"error\":\"unauthorized\"}");
        ex.StatusCode.Should().Be(401);
        ex.ResponseBody.Should().Contain("unauthorized");
    }

    [Fact]
    public void RateLimitException_OptionallyCarriesRetryAfter()
    {
        var ex = new AwinAffiliateRateLimitException("slow down", TimeSpan.FromSeconds(5), null);
        ex.RetryAfter.Should().Be(TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void NotFoundException_RemembersResourceId()
    {
        var ex = new AwinAffiliateNotFoundException("ORDER-1", "not found");
        ex.ResourceId.Should().Be("ORDER-1");
    }

    [Fact]
    public void UnsupportedException_RemembersCapability()
    {
        var ex = new AwinAffiliateUnsupportedException("creative-report", "not available");
        ex.Capability.Should().Be("creative-report");
    }

    [Fact]
    public void Hierarchy_RootsAtAwinAffiliateException()
    {
        new AwinAffiliateApiException("x").Should().BeAssignableTo<AwinAffiliateException>();
        new AwinAffiliateAuthException("x", 401, null).Should().BeAssignableTo<AwinAffiliateException>();
        new AwinAffiliateRateLimitException("x", null, null).Should().BeAssignableTo<AwinAffiliateException>();
        new AwinAffiliateNotFoundException("id", "x").Should().BeAssignableTo<AwinAffiliateException>();
        new AwinAffiliateUnsupportedException("cap", "x").Should().BeAssignableTo<AwinAffiliateException>();
    }
}
