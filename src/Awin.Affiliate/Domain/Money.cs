namespace Awin.Affiliate.Domain;

/// <summary>
/// Value object representing a monetary amount with an ISO-4217 currency code.
/// Awin returns commission and sale values as objects of the form
/// <c>{ amount, currency }</c>; this type mirrors that shape and is safe to pass
/// across SDK boundaries.
/// </summary>
public readonly record struct Money
{
    /// <summary>A zero-value <see cref="Money"/> in an unspecified currency.</summary>
    public static readonly Money Zero = new(0m, string.Empty);

    /// <summary>
    /// Creates a new <see cref="Money"/> value.
    /// </summary>
    /// <param name="amount">Monetary amount.</param>
    /// <param name="currency">ISO-4217 currency code (e.g. <c>BRL</c>, <c>EUR</c>, <c>USD</c>).
    /// Trimmed and normalized to upper-case. May be empty for the <see cref="Zero"/> sentinel.</param>
    public Money(decimal amount, string? currency)
    {
        Amount = amount;
        Currency = (currency ?? string.Empty).Trim().ToUpperInvariant();
    }

    /// <summary>Monetary amount.</summary>
    public decimal Amount { get; }

    /// <summary>ISO-4217 currency code (upper-case, no whitespace). Empty when unspecified.</summary>
    public string Currency { get; }

    /// <summary>
    /// Adds two <see cref="Money"/> values. When one operand has an empty currency, the other
    /// operand's currency is used. Otherwise both currencies must match.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when both operands have non-empty
    /// currencies that do not match.</exception>
    public static Money operator +(Money left, Money right)
    {
        var currency = ResolveCurrency(left.Currency, right.Currency);
        return new Money(left.Amount + right.Amount, currency);
    }

    /// <summary>Returns a new <see cref="Money"/> with the same currency and a different amount.</summary>
    public Money WithAmount(decimal amount) => new(amount, Currency);

    /// <inheritdoc/>
    public override string ToString()
        => string.IsNullOrEmpty(Currency)
            ? Amount.ToString(System.Globalization.CultureInfo.InvariantCulture)
            : $"{Amount.ToString(System.Globalization.CultureInfo.InvariantCulture)} {Currency}";

    private static string ResolveCurrency(string left, string right)
    {
        if (string.IsNullOrEmpty(left)) return right;
        if (string.IsNullOrEmpty(right)) return left;
        if (!string.Equals(left, right, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Cannot combine Money values with mismatched currencies '{left}' and '{right}'.");
        }

        return left;
    }
}
