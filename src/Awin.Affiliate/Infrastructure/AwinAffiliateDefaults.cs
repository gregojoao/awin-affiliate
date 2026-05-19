namespace Awin.Affiliate.Infrastructure;

/// <summary>Shared constants for outbound Awin requests.</summary>
internal static class AwinAffiliateDefaults
{
    /// <summary>User-Agent header sent on every outbound request.</summary>
    public const string UserAgent = "Awin.Affiliate/1.0";

    /// <summary>Default Awin time zone used for report date ranges. Awin's accounts default to Europe/London.</summary>
    public const string DefaultTimezone = "Europe/London";

    /// <summary>Default Awin <c>dateType</c> filter — group by transaction date (not click date).</summary>
    public const string DefaultDateType = "transaction";
}
