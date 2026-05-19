namespace Awin.Affiliate.Application;

/// <summary>
/// Options for <see cref="AwinAffiliateClient"/> (link generation surface). Reports
/// have their own <c>AwinAffiliateReportsOptions</c> in the <c>Awin.Affiliate.Reports</c> namespace.
/// </summary>
public sealed class AwinAffiliateOptions
{
    /// <summary>Default Awin Publisher API endpoint.</summary>
    public static readonly Uri DefaultEndpoint = new("https://api.awin.com");

    /// <summary>Default cread.php tracking endpoint used to build deep links client-side.</summary>
    public static readonly Uri DefaultTrackingEndpoint = new("https://www.awin1.com/cread.php");

    /// <summary>Default HTTP timeout.</summary>
    public static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Awin Publisher API endpoint. Only used by
    /// <see cref="IAwinAffiliateClient.ResolveAwinUrlAsync"/> redirect resolution when callers pass
    /// a short URL that must be followed.
    /// </summary>
    public Uri Endpoint { get; set; } = DefaultEndpoint;

    /// <summary>cread.php tracking endpoint used to construct deep links.</summary>
    public Uri TrackingEndpoint { get; set; } = DefaultTrackingEndpoint;

    /// <summary>
    /// Publisher id (your awinaffid). Required: without it, generated links cannot attribute
    /// clicks to your account.
    /// </summary>
    public string PublisherId { get; set; } = string.Empty;

    /// <summary>
    /// OAuth2 long-lived access token. Only needed when the SDK is configured to call the
    /// <c>/links/convert</c> endpoint (currently kept as a placeholder hook — most callers
    /// build cread.php links locally and never hit the API for link generation).
    /// </summary>
    public string AccessToken { get; set; } = string.Empty;

    /// <summary>HTTP timeout for any outbound request issued by the link client.</summary>
    public TimeSpan Timeout { get; set; } = DefaultTimeout;

    /// <summary>
    /// Validates required options. Throws on missing publisher id or invalid endpoint.
    /// AccessToken is optional for the link client.
    /// </summary>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(PublisherId))
        {
            throw new InvalidOperationException("Awin affiliate PublisherId is required.");
        }

        if (TrackingEndpoint is null ||
            (TrackingEndpoint.Scheme != Uri.UriSchemeHttp && TrackingEndpoint.Scheme != Uri.UriSchemeHttps))
        {
            throw new InvalidOperationException(
                "Awin affiliate TrackingEndpoint must be an absolute HTTP/HTTPS URL.");
        }

        if (Endpoint is null ||
            (Endpoint.Scheme != Uri.UriSchemeHttp && Endpoint.Scheme != Uri.UriSchemeHttps))
        {
            throw new InvalidOperationException(
                "Awin affiliate Endpoint must be an absolute HTTP/HTTPS URL.");
        }

        if (Timeout <= TimeSpan.Zero)
        {
            throw new InvalidOperationException("Awin affiliate Timeout must be greater than zero.");
        }
    }

    /// <summary>
    /// True when the publisher id is a non-empty, non-placeholder value. Mirrors the
    /// <c>COLE_*</c> placeholder guard used by the original bot configuration.
    /// </summary>
    public bool IsConfigured => IsRealValue(PublisherId);

    internal static bool IsRealValue(string? value)
        => !string.IsNullOrWhiteSpace(value) &&
           !value!.StartsWith("COLE_", StringComparison.OrdinalIgnoreCase);
}
