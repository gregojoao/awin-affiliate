using Awin.Affiliate.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Awin.Affiliate.Application;

/// <summary>
/// Default implementation of <see cref="IAwinAffiliateClient"/>. Builds cread.php
/// deep links client-side and follows redirects for short URLs through the supplied
/// <see cref="HttpClient"/>.
/// </summary>
public sealed class AwinAffiliateClient : IAwinAffiliateClient
{
    private readonly HttpClient _httpClient;
    private readonly AwinAffiliateOptions _options;

    /// <summary>DI constructor used by <c>AddAwinAffiliate</c>.</summary>
    [ActivatorUtilitiesConstructor]
    public AwinAffiliateClient(HttpClient httpClient, IOptions<AwinAffiliateOptions> options)
        : this(httpClient, options?.Value ?? throw new ArgumentNullException(nameof(options)))
    {
    }

    /// <summary>Direct constructor for callers that build options in code.</summary>
    public AwinAffiliateClient(HttpClient httpClient, AwinAffiliateOptions options)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <inheritdoc/>
    public async Task<AwinAffiliateLinkResult> GenerateAffiliateLinkAsync(
        AwinAffiliateLinkRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        _options.Validate();
        EnsureValidOriginUrl(request.OriginUrl);
        EnsureValidAdvertiserId(request.AdvertiserId);

        using var timeoutCts = CreateTimeoutTokenSource(cancellationToken);
        var token = timeoutCts.Token;

        var destination = request.ResolveShortUrls
            ? await ResolveAwinUrlCoreAsync(request.OriginUrl, token)
            : request.OriginUrl;

        var affiliateUrl = AwinAffiliateLinkBuilder.Build(
            _options.TrackingEndpoint,
            _options.PublisherId,
            request.AdvertiserId,
            destination,
            request.SubIds);

        return new AwinAffiliateLinkResult
        {
            AffiliateUrl = affiliateUrl,
            OriginUrl = destination,
            AdvertiserId = request.AdvertiserId.Trim(),
            Source = AwinAffiliateLinkSource.TrackingDeepLink
        };
    }

    /// <inheritdoc/>
    public async Task<Uri> ResolveAwinUrlAsync(Uri shortUrl, CancellationToken cancellationToken = default)
    {
        EnsureValidOriginUrl(shortUrl);
        EnsureTrustedAwinResolveUrl(shortUrl);

        using var timeoutCts = CreateTimeoutTokenSource(cancellationToken);
        return await ResolveAwinUrlCoreAsync(shortUrl, timeoutCts.Token);
    }

    private async Task<Uri> ResolveAwinUrlCoreAsync(Uri shortUrl, CancellationToken cancellationToken)
    {
        EnsureTrustedAwinResolveUrl(shortUrl);

        using var request = new HttpRequestMessage(HttpMethod.Get, shortUrl);
        request.Headers.UserAgent.ParseAdd(AwinAffiliateDefaults.UserAgent);

        try
        {
            using var response = await _httpClient.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

            if (response.RequestMessage?.RequestUri is { } finalUrl &&
                IsValidHttpUri(finalUrl))
            {
                return finalUrl;
            }

            return shortUrl;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new AwinAffiliateApiException(
                $"Awin URL resolve timed out after {_options.Timeout.TotalMilliseconds:0}ms.");
        }
        catch
        {
            return shortUrl;
        }
    }

    private CancellationTokenSource CreateTimeoutTokenSource(CancellationToken cancellationToken)
    {
        var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(_options.Timeout);
        return cts;
    }

    private static void EnsureValidOriginUrl(Uri originUrl)
    {
        if (originUrl is null || !IsValidHttpUri(originUrl))
        {
            throw new ArgumentException("A valid HTTP/HTTPS origin URL is required.", nameof(originUrl));
        }
    }

    private static void EnsureValidAdvertiserId(string advertiserId)
    {
        if (string.IsNullOrWhiteSpace(advertiserId))
        {
            throw new ArgumentException(
                "Awin advertiserId (awinmid) is required — without it, generated links cannot attribute clicks.",
                nameof(advertiserId));
        }
    }

    private static bool IsValidHttpUri(Uri uri)
        => uri.IsAbsoluteUri &&
           (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);

    private static void EnsureTrustedAwinResolveUrl(Uri shortUrl)
    {
        if (!IsTrustedAwinResolveHost(shortUrl))
        {
            throw new ArgumentException(
                "Awin URL resolving is only allowed for awin.com and awin1.com hosts.",
                nameof(shortUrl));
        }
    }

    private static bool IsTrustedAwinResolveHost(Uri uri)
    {
        var host = uri.IdnHost;
        return host.Equals("awin.com", StringComparison.OrdinalIgnoreCase) ||
               host.EndsWith(".awin.com", StringComparison.OrdinalIgnoreCase) ||
               host.Equals("awin1.com", StringComparison.OrdinalIgnoreCase) ||
               host.EndsWith(".awin1.com", StringComparison.OrdinalIgnoreCase);
    }
}
