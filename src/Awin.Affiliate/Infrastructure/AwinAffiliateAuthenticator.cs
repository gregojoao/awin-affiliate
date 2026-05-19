using System.Net.Http.Headers;

namespace Awin.Affiliate.Infrastructure;

/// <summary>Applies Awin's Bearer-token authentication scheme to outbound requests.</summary>
internal static class AwinAffiliateAuthenticator
{
    /// <summary>
    /// Attaches <c>Authorization: Bearer {token}</c> to the supplied request. Awin's
    /// OAuth2 access tokens are long-lived (generated in Toolbox &gt; API credentials)
    /// and do not need refresh.
    /// </summary>
    public static void Apply(HttpRequestMessage request, string accessToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            throw new ArgumentException(
                "Awin AccessToken is required for Reports API calls.",
                nameof(accessToken));
        }

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken.Trim());
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Headers.UserAgent.ParseAdd(AwinAffiliateDefaults.UserAgent);
    }
}
