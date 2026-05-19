using Awin.Affiliate.Domain;
using FluentAssertions;

namespace Awin.Affiliate.Tests.Domain;

public class AwinAdvertiserIdentityTests
{
    [Fact]
    public void Constructor_AcceptsNumericValue()
    {
        var id = new AwinAdvertiserIdentity("12345");
        id.Value.Should().Be("12345");
        id.ToString().Should().Be("12345");
    }

    [Fact]
    public void Constructor_TrimsWhitespace()
    {
        new AwinAdvertiserIdentity("  42  ").Value.Should().Be("42");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Constructor_RejectsEmptyValue(string? value)
    {
        var act = () => new AwinAdvertiserIdentity(value!);
        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("12-34")]
    [InlineData("12.34")]
    public void Constructor_RejectsNonNumeric(string value)
    {
        var act = () => new AwinAdvertiserIdentity(value);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void TryParse_ReturnsTrue_ForValidNumeric()
    {
        AwinAdvertiserIdentity.TryParse("987", out var id).Should().BeTrue();
        id.Value.Should().Be("987");
    }

    [Fact]
    public void TryParse_ReturnsFalse_ForInvalid()
    {
        AwinAdvertiserIdentity.TryParse("not-a-number", out _).Should().BeFalse();
    }
}
