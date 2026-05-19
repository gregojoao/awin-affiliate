namespace Awin.Affiliate.Infrastructure;

/// <summary>Minimal HTTP transport abstraction used by the Awin Reports surface. Hand-rolled
/// so callers can substitute a fake in tests without bringing in a heavier mocking
/// framework.</summary>
internal interface IAwinHttpTransport
{
    /// <summary>Issues an authenticated GET against the supplied path and returns the JSON body.</summary>
    Task<AwinHttpResponse> GetJsonAsync(
        string pathAndQuery,
        CancellationToken cancellationToken);
}

/// <summary>Container for the body returned by <see cref="IAwinHttpTransport"/>.</summary>
internal sealed record AwinHttpResponse(int StatusCode, string Body);
