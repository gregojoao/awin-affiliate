using Awin.Affiliate.Domain;
using FluentAssertions;

namespace Awin.Affiliate.Tests.Domain;

public class AwinTransactionStatusTests
{
    [Theory]
    [InlineData("approved", AwinTransactionStatus.Approved)]
    [InlineData("APPROVED", AwinTransactionStatus.Approved)]
    [InlineData("pending", AwinTransactionStatus.Pending)]
    [InlineData("declined", AwinTransactionStatus.Declined)]
    [InlineData("anything-else", AwinTransactionStatus.Unknown)]
    [InlineData("", AwinTransactionStatus.Unknown)]
    [InlineData(null, AwinTransactionStatus.Unknown)]
    public void ParseStatus_MapsWireValues(string? wire, AwinTransactionStatus expected)
    {
        AwinTransactionStatusExtensions.ParseStatus(wire).Should().Be(expected);
    }

    [Theory]
    [InlineData(AwinTransactionStatus.Approved, "approved")]
    [InlineData(AwinTransactionStatus.Pending, "pending")]
    [InlineData(AwinTransactionStatus.Declined, "declined")]
    [InlineData(AwinTransactionStatus.Unknown, "")]
    public void ToWireString_RoundTrips(AwinTransactionStatus status, string expected)
    {
        status.ToWireString().Should().Be(expected);
    }
}
