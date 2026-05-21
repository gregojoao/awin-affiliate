using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Awin.Affiliate.Infrastructure;

/// <summary>
/// Default <see cref="IAwinHttpTransport"/> implementation. Sends Bearer-authenticated
/// requests against the Awin Publisher API, maps non-success responses to the SDK's
/// exception hierarchy, and retries once on transient 5xx and timeout errors.
/// </summary>
internal sealed class AwinAffiliateHttpTransport : IAwinHttpTransport
{
    private readonly HttpClient _httpClient;
    private readonly Uri _endpoint;
    private readonly string _accessToken;
    private readonly TimeSpan _timeout;
    private readonly int _maxRetries;

    public AwinAffiliateHttpTransport(
        HttpClient httpClient,
        Uri endpoint,
        string accessToken,
        TimeSpan timeout,
        int maxRetries = 1)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
        _accessToken = accessToken ?? throw new ArgumentNullException(nameof(accessToken));
        _timeout = timeout > TimeSpan.Zero ? timeout : TimeSpan.FromSeconds(30);
        _maxRetries = Math.Max(0, maxRetries);
    }

    public async Task<AwinHttpResponse> GetJsonAsync(string pathAndQuery, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pathAndQuery);

        Exception? lastTransientFailure = null;

        for (var attempt = 0; attempt <= _maxRetries; attempt++)
        {
            if (attempt > 0)
            {
                await Task.Delay(BackoffFor(attempt), cancellationToken);
            }

            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(_timeout);

            try
            {
                var requestUri = new Uri(_endpoint, pathAndQuery);
                using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
                AwinAffiliateAuthenticator.Apply(request, _accessToken);

                using var response = await _httpClient.SendAsync(request, timeoutCts.Token);
                var body = await response.Content.ReadAsStringAsync(timeoutCts.Token);

                if (response.IsSuccessStatusCode)
                {
                    return new AwinHttpResponse((int)response.StatusCode, body);
                }

                ThrowMappedException(response, body);

                // Below this point: 5xx (and other unmapped statuses) are retryable.
                if (IsTransient(response.StatusCode) && attempt < _maxRetries)
                {
                    lastTransientFailure = new AwinAffiliateApiException(
                        $"Awin API HTTP {(int)response.StatusCode}: {Truncate(body, 500)}",
                        (int)response.StatusCode,
                        body);
                    continue;
                }

                throw new AwinAffiliateApiException(
                    $"Awin API HTTP {(int)response.StatusCode}: {Truncate(body, 500)}",
                    (int)response.StatusCode,
                    body);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                lastTransientFailure = new AwinAffiliateApiException(
                    $"Awin API request timed out after {_timeout.TotalMilliseconds:0}ms.");
                if (attempt < _maxRetries) continue;
                throw lastTransientFailure;
            }
            catch (HttpRequestException ex)
            {
                lastTransientFailure = new AwinAffiliateApiException(
                    $"Awin API HTTP transport error: {ex.Message}", ex);
                if (attempt < _maxRetries) continue;
                throw lastTransientFailure;
            }
        }

        // Defensive — loop should always either return or throw.
        throw lastTransientFailure ?? new AwinAffiliateApiException("Awin API request failed.");
    }

    private static void ThrowMappedException(HttpResponseMessage response, string body)
    {
        var statusCode = (int)response.StatusCode;
        switch (response.StatusCode)
        {
            case HttpStatusCode.Unauthorized:
            {
                var providerError = ExtractProviderError(body);
                throw new AwinAffiliateAuthException(
                    $"Awin API authentication failed (HTTP {statusCode}). " +
                    "Verify the AccessToken (Toolbox > API credentials).",
                    statusCode,
                    SanitizeAuthResponseBody(body),
                    AwinAffiliateCredentialFailureKind.Unauthorized,
                    providerError.Code,
                    providerError.Message);
            }

            case HttpStatusCode.Forbidden:
            {
                var providerError = ExtractProviderError(body);
                throw new AwinAffiliateAuthException(
                    $"Awin API authorization failed (HTTP {statusCode}). " +
                    "Verify the AccessToken permissions for this publisher.",
                    statusCode,
                    SanitizeAuthResponseBody(body),
                    AwinAffiliateCredentialFailureKind.Forbidden,
                    providerError.Code,
                    providerError.Message);
            }

            case HttpStatusCode.TooManyRequests:
                throw new AwinAffiliateRateLimitException(
                    $"Awin API rate limit exceeded (HTTP 429).",
                    response.Headers.RetryAfter?.Delta,
                    body);

            case HttpStatusCode.NotFound:
                // 404 is bubbled up as a non-retried AwinAffiliateApiException so callers
                // can distinguish "empty result" from "missing resource".
                break;
        }
    }

    private static bool IsTransient(HttpStatusCode statusCode)
        => (int)statusCode >= 500 && (int)statusCode <= 599;

    private static TimeSpan BackoffFor(int attempt)
        => TimeSpan.FromMilliseconds(200 * Math.Pow(2, attempt - 1));

    private static string Truncate(string value, int max)
        => string.IsNullOrEmpty(value) || value.Length <= max ? value : value[..max];

    private static ProviderError ExtractProviderError(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return ProviderError.Empty;
        }

        try
        {
            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.ValueKind != JsonValueKind.Object)
            {
                return ProviderError.Empty;
            }

            var code = ReadString(doc.RootElement, "errorCode")
                ?? ReadString(doc.RootElement, "code")
                ?? ReadString(doc.RootElement, "error");

            var message = ReadString(doc.RootElement, "message")
                ?? ReadString(doc.RootElement, "error_description")
                ?? ReadString(doc.RootElement, "errorMessage")
                ?? ReadString(doc.RootElement, "detail")
                ?? ReadString(doc.RootElement, "title");

            return new ProviderError(
                SanitizeProviderValue(code),
                SanitizeProviderValue(message));
        }
        catch (JsonException)
        {
            return ProviderError.Empty;
        }
    }

    private static string? ReadString(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var value))
        {
            return null;
        }

        return value.ValueKind switch
        {
            JsonValueKind.String => value.GetString(),
            JsonValueKind.Number => value.GetRawText(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            _ => null
        };
    }

    private static string? SanitizeProviderValue(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return RedactSensitiveText(Truncate(value.Trim(), 500));
    }

    private static string? SanitizeAuthResponseBody(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return body;
        }

        try
        {
            using var doc = JsonDocument.Parse(body);
            var sanitized = SanitizeJsonElement(doc.RootElement);
            return Truncate(JsonSerializer.Serialize(sanitized), 1000);
        }
        catch (JsonException)
        {
            return RedactSensitiveText(Truncate(body, 1000));
        }
    }

    private static object? SanitizeJsonElement(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.Object => SanitizeJsonObject(element),
            JsonValueKind.Array => element.EnumerateArray().Select(SanitizeJsonElement).ToArray(),
            JsonValueKind.String => RedactSensitiveText(element.GetString() ?? string.Empty),
            JsonValueKind.Number => element.GetRawText(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            _ => element.GetRawText()
        };
    }

    private static Dictionary<string, object?> SanitizeJsonObject(JsonElement element)
    {
        var sanitized = new Dictionary<string, object?>(StringComparer.Ordinal);
        foreach (var property in element.EnumerateObject())
        {
            sanitized[property.Name] = IsSensitiveProperty(property.Name)
                ? "[REDACTED]"
                : SanitizeJsonElement(property.Value);
        }

        return sanitized;
    }

    private static bool IsSensitiveProperty(string name)
        => name.Contains("token", StringComparison.OrdinalIgnoreCase) ||
           name.Contains("authorization", StringComparison.OrdinalIgnoreCase) ||
           name.Contains("apiKey", StringComparison.OrdinalIgnoreCase) ||
           name.Contains("secret", StringComparison.OrdinalIgnoreCase) ||
           name.Contains("password", StringComparison.OrdinalIgnoreCase);

    private static string RedactSensitiveText(string value)
    {
        var redacted = Regex.Replace(
            value,
            @"(?i)\bBearer\s+[A-Za-z0-9._~+/=-]+",
            "Bearer [REDACTED]");

        redacted = Regex.Replace(
            redacted,
            @"(?i)(access[_-]?token|api[_-]?key|authorization|secret|password)[""'\s:=]+[A-Za-z0-9._~+/=-]+",
            "$1=[REDACTED]");

        return redacted;
    }

    private sealed record ProviderError(string? Code, string? Message)
    {
        public static ProviderError Empty { get; } = new(null, null);
    }
}
