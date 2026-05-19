namespace Awin.Affiliate.Domain;

/// <summary>
/// Strongly typed identifier for an Awin publisher account. Matches the <c>awinaffid</c>
/// tracking URL parameter and the <c>publisherId</c> path segment in the Publisher API.
/// </summary>
public readonly record struct AwinPublisherIdentity
{
    /// <summary>
    /// Creates a new <see cref="AwinPublisherIdentity"/>.
    /// </summary>
    /// <param name="value">Numeric publisher id as a string (e.g. <c>"987654"</c>).</param>
    /// <exception cref="ArgumentException">Thrown when the value is null, empty, or non-numeric.</exception>
    public AwinPublisherIdentity(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Awin publisher id is required.", nameof(value));
        }

        var trimmed = value.Trim();
        if (!IsNumeric(trimmed))
        {
            throw new ArgumentException(
                $"Awin publisher id '{value}' must be numeric.",
                nameof(value));
        }

        Value = trimmed;
    }

    /// <summary>Numeric publisher id.</summary>
    public string Value { get; }

    /// <summary>Attempts to parse a string into a valid <see cref="AwinPublisherIdentity"/>.</summary>
    public static bool TryParse(string? value, out AwinPublisherIdentity identity)
    {
        if (string.IsNullOrWhiteSpace(value) || !IsNumeric(value.Trim()))
        {
            identity = default;
            return false;
        }

        identity = new AwinPublisherIdentity(value);
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
