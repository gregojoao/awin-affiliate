using Awin.Affiliate.Infrastructure;

namespace Awin.Affiliate.Reports.Configuration;

/// <summary>
/// Options for <see cref="Awin.Affiliate.Reports.Application.AwinAffiliateReportsClient"/>.
/// Mirrors the bot's original <c>AwinReporterOptions</c> with the SDK-friendly shape: no
/// project-specific concerns, no DB-backed secrets loader.
/// </summary>
public sealed class AwinAffiliateReportsOptions
{
    /// <summary>Default Awin Publisher API endpoint.</summary>
    public static readonly Uri DefaultEndpoint = new("https://api.awin.com");

    /// <summary>Default HTTP timeout.</summary>
    public static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(30);

    /// <summary>Awin Publisher API endpoint. Override only for testing or alternate regions.</summary>
    public Uri Endpoint { get; set; } = DefaultEndpoint;

    /// <summary>Numeric publisher id (your awinaffid). Required.</summary>
    public string PublisherId { get; set; } = string.Empty;

    /// <summary>OAuth2 long-lived access token (Toolbox &gt; API credentials). Required.</summary>
    public string AccessToken { get; set; } = string.Empty;

    /// <summary>Awin <c>dateType</c> filter — <c>transaction</c> (default) or <c>click</c>.</summary>
    public string DateType { get; set; } = AwinAffiliateDefaults.DefaultDateType;

    /// <summary>Time zone applied to Awin date filters. Defaults to <c>Europe/London</c>.</summary>
    public string Timezone { get; set; } = AwinAffiliateDefaults.DefaultTimezone;

    /// <summary>Optional default advertiser ids applied to list calls that do not specify one.</summary>
    public IReadOnlyList<string> DefaultAdvertiserIds { get; set; } = Array.Empty<string>();

    /// <summary>HTTP timeout.</summary>
    public TimeSpan Timeout { get; set; } = DefaultTimeout;

    /// <summary>How many retries the transport performs on 5xx/transient errors.</summary>
    public int MaxRetries { get; set; } = 1;

    /// <summary>
    /// True when both publisher id and access token are non-empty, non-placeholder values.
    /// </summary>
    public bool IsConfigured =>
        IsRealValue(PublisherId) && IsRealValue(AccessToken);

    /// <summary>Throws when required options are missing or invalid.</summary>
    public void Validate()
    {
        if (!IsRealValue(PublisherId))
        {
            throw new InvalidOperationException("Awin Reports PublisherId is required.");
        }

        if (!IsRealValue(AccessToken))
        {
            throw new InvalidOperationException("Awin Reports AccessToken is required.");
        }

        if (Endpoint is null ||
            Endpoint.Scheme != Uri.UriSchemeHttps)
        {
            throw new InvalidOperationException("Awin Reports Endpoint must be an absolute HTTPS URL.");
        }

        if (Timeout <= TimeSpan.Zero)
        {
            throw new InvalidOperationException("Awin Reports Timeout must be greater than zero.");
        }

        if (string.IsNullOrWhiteSpace(Timezone))
        {
            throw new InvalidOperationException("Awin Reports Timezone is required.");
        }
    }

    internal static bool IsRealValue(string? value)
        => !string.IsNullOrWhiteSpace(value) &&
           !value!.StartsWith("COLE_", StringComparison.OrdinalIgnoreCase);
}
