namespace Awin.Affiliate.Domain;

/// <summary>
/// Strongly typed identifier for an Awin advertiser (also called "programme" or "merchant").
/// The numeric identifier matches Awin's <c>awinmid</c> URL parameter and the
/// <c>advertiserId</c> field returned by the Publisher API.
/// </summary>
public readonly record struct AwinAdvertiserIdentity
{
    /// <summary>
    /// Creates a new <see cref="AwinAdvertiserIdentity"/>.
    /// </summary>
    /// <param name="value">Numeric advertiser id as a string (e.g. <c>"12345"</c>).</param>
    /// <exception cref="ArgumentException">Thrown when the value is null, empty, or non-numeric.</exception>
    public AwinAdvertiserIdentity(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Awin advertiser id is required.", nameof(value));
        }

        var trimmed = value.Trim();
        if (!IsNumeric(trimmed))
        {
            throw new ArgumentException(
                $"Awin advertiser id '{value}' must be numeric.",
                nameof(value));
        }

        Value = trimmed;
    }

    /// <summary>
    /// Numeric advertiser id (kept as a string to preserve any leading zeros and avoid
    /// 32-bit overflow on extremely large publishers).
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Attempts to parse a string into a valid <see cref="AwinAdvertiserIdentity"/>.
    /// </summary>
    public static bool TryParse(string? value, out AwinAdvertiserIdentity identity)
    {
        if (string.IsNullOrWhiteSpace(value) || !IsNumeric(value.Trim()))
        {
            identity = default;
            return false;
        }

        identity = new AwinAdvertiserIdentity(value);
        return true;
    }

    /// <inheritdoc/>
    public override string ToString() => Value ?? string.Empty;

    private static bool IsNumeric(string value)
    {
        for (var i = 0; i < value.Length; i++)
        {
            if (!char.IsDigit(value[i]))
            {
                return false;
            }
        }

        return value.Length > 0;
    }
}
