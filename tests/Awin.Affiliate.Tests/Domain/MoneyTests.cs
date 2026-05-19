using Awin.Affiliate.Domain;
using FluentAssertions;

namespace Awin.Affiliate.Tests.Domain;

public class MoneyTests
{
    [Fact]
    public void Constructor_NormalisesCurrencyToUpper()
    {
        var money = new Money(10m, "brl");
        money.Amount.Should().Be(10m);
        money.Currency.Should().Be("BRL");
    }

    [Fact]
    public void Zero_HasEmptyCurrency()
    {
        Money.Zero.Amount.Should().Be(0m);
        Money.Zero.Currency.Should().BeEmpty();
    }

    [Fact]
    public void Addition_SumsAmounts_WhenCurrenciesMatch()
    {
        (new Money(5m, "BRL") + new Money(3m, "BRL")).Should().Be(new Money(8m, "BRL"));
    }

    [Fact]
    public void Addition_UsesNonEmptyCurrency_WhenOneOperandIsZero()
    {
        (Money.Zero + new Money(7m, "BRL")).Currency.Should().Be("BRL");
        (new Money(7m, "BRL") + Money.Zero).Currency.Should().Be("BRL");
    }

    [Fact]
    public void Addition_Throws_OnMismatchedCurrencies()
    {
        var act = () => _ = new Money(1m, "BRL") + new Money(1m, "USD");
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void WithAmount_KeepsCurrency()
    {
        new Money(1m, "BRL").WithAmount(99m).Should().Be(new Money(99m, "BRL"));
    }

    [Fact]
    public void ToString_IncludesCurrency_WhenPresent()
    {
        new Money(12.34m, "BRL").ToString().Should().Be("12.34 BRL");
        Money.Zero.ToString().Should().Be("0");
    }
}
