using Awin.Affiliate.Application;
using FluentAssertions;

namespace Awin.Affiliate.Tests.Application;

public class AwinAffiliateOptionsTests
{
    [Fact]
    public void Defaults_AreSet()
    {
        var options = new AwinAffiliateOptions();
        options.Endpoint.Should().Be(AwinAffiliateOptions.DefaultEndpoint);
        options.TrackingEndpoint.Should().Be(AwinAffiliateOptions.DefaultTrackingEndpoint);
        options.Timeout.Should().Be(AwinAffiliateOptions.DefaultTimeout);
        options.IsConfigured.Should().BeFalse();
    }

    [Fact]
    public void Validate_Throws_WhenPublisherIdMissing()
    {
        var options = new AwinAffiliateOptions();
        var act = () => options.Validate();
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*PublisherId*");
    }

    [Fact]
    public void Validate_Succeeds_WithMinimumConfig()
    {
        var options = new AwinAffiliateOptions { PublisherId = "12345" };
        options.Validate();
        options.IsConfigured.Should().BeTrue();
    }

    [Fact]
    public void Validate_Rejects_ZeroTimeout()
    {
        var options = new AwinAffiliateOptions
        {
            PublisherId = "12345",
            Timeout = TimeSpan.Zero
        };
        var act = () => options.Validate();
        act.Should().Throw<InvalidOperationException>().WithMessage("*Timeout*");
    }

    [Fact]
    public void Validate_RejectsPlainHttpEndpoints()
    {
        var options = new AwinAffiliateOptions
        {
            PublisherId = "12345",
            Endpoint = new Uri("http://api.awin.test"),
            TrackingEndpoint = new Uri("https://www.awin1.com/cread.php")
        };

        var act = () => options.Validate();

        act.Should().Throw<InvalidOperationException>().WithMessage("*HTTPS*");
    }

    [Fact]
    public void Validate_RejectsPlainHttpTrackingEndpoint()
    {
        var options = new AwinAffiliateOptions
        {
            PublisherId = "12345",
            Endpoint = new Uri("https://api.awin.com"),
            TrackingEndpoint = new Uri("http://www.awin1.com/cread.php")
        };

        var act = () => options.Validate();

        act.Should().Throw<InvalidOperationException>().WithMessage("*HTTPS*");
    }

    [Theory]
    [InlineData("COLE_PUBLISHER_ID")]
    [InlineData("cole_seu_id")]
    public void IsConfigured_RejectsPlaceholderValues(string placeholder)
    {
        var options = new AwinAffiliateOptions { PublisherId = placeholder };
        options.IsConfigured.Should().BeFalse();
    }
}
