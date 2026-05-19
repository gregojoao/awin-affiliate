using Awin.Affiliate.Infrastructure;

namespace Awin.Affiliate.Tests.Reports;

internal sealed class FakeAwinHttpTransport : IAwinHttpTransport
{
    public List<string> RequestedPaths { get; } = new();
    public Func<string, AwinHttpResponse>? Responder { get; set; }

    public Task<AwinHttpResponse> GetJsonAsync(string pathAndQuery, CancellationToken cancellationToken)
    {
        RequestedPaths.Add(pathAndQuery);
        if (Responder is null)
        {
            return Task.FromResult(new AwinHttpResponse(200, "[]"));
        }

        return Task.FromResult(Responder(pathAndQuery));
    }
}
