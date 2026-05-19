using Awin.Affiliate.Application;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Awin.Affiliate.Infrastructure;

/// <summary>
/// DI extensions for registering <see cref="IAwinAffiliateClient"/> (link generation).
/// Reports clients are registered via <c>AddAwinAffiliateReports</c> in the
/// <c>Awin.Affiliate.Reports.DependencyInjection</c> namespace.
/// </summary>
public static class AwinAffiliateServiceCollectionExtensions
{
    /// <summary>Default configuration section bound by <see cref="AddAwinAffiliate(IServiceCollection, IConfiguration, string)"/>.</summary>
    public const string DefaultConfigurationSectionName = "Awin:Affiliate";

    /// <summary>Registers the SDK with options configured in code.</summary>
    public static IServiceCollection AddAwinAffiliate(
        this IServiceCollection services,
        Action<AwinAffiliateOptions> configureOptions)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configureOptions);

        services.AddOptions<AwinAffiliateOptions>()
            .Configure(configureOptions)
            .Validate(AreOptionsValid, "Awin affiliate options are invalid.")
            .ValidateOnStart();

        return services.AddAwinAffiliateCore();
    }

    /// <summary>Registers the SDK by binding options to a configuration section.</summary>
    public static IServiceCollection AddAwinAffiliate(
        this IServiceCollection services,
        IConfiguration configuration,
        string sectionName = DefaultConfigurationSectionName)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var section = configuration.GetSection(sectionName);
        services.AddOptions<AwinAffiliateOptions>()
            .Bind(section)
            .Validate(AreOptionsValid, $"Configuration section '{sectionName}' contains invalid Awin affiliate options.")
            .ValidateOnStart();

        return services.AddAwinAffiliateCore();
    }

    private static IServiceCollection AddAwinAffiliateCore(this IServiceCollection services)
    {
        services.AddHttpClient<IAwinAffiliateClient, AwinAffiliateClient>();
        return services;
    }

    private static bool AreOptionsValid(AwinAffiliateOptions options)
    {
        try
        {
            options.Validate();
            return true;
        }
        catch
        {
            return false;
        }
    }
}
