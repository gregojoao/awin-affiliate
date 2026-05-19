using Awin.Affiliate.Reports.Application;
using Awin.Affiliate.Reports.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Awin.Affiliate.Reports.DependencyInjection;

/// <summary>DI extensions for registering the Awin reports client.</summary>
public static class AwinAffiliateReportsServiceCollectionExtensions
{
    /// <summary>Default configuration section bound by <see cref="AddAwinAffiliateReports(IServiceCollection, IConfiguration, string)"/>.</summary>
    public const string DefaultConfigurationSectionName = "Awin:Reports";

    /// <summary>Registers <see cref="IAwinAffiliateReportsClient"/> with options configured in code.</summary>
    public static IServiceCollection AddAwinAffiliateReports(
        this IServiceCollection services,
        Action<AwinAffiliateReportsOptions> configureOptions)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configureOptions);

        services.AddOptions<AwinAffiliateReportsOptions>()
            .Configure(configureOptions)
            .Validate(AreOptionsValid, "Awin Reports options are invalid.")
            .ValidateOnStart();

        return services.AddCore();
    }

    /// <summary>Registers <see cref="IAwinAffiliateReportsClient"/> by binding to a configuration section.</summary>
    public static IServiceCollection AddAwinAffiliateReports(
        this IServiceCollection services,
        IConfiguration configuration,
        string sectionName = DefaultConfigurationSectionName)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddOptions<AwinAffiliateReportsOptions>()
            .Bind(configuration.GetSection(sectionName))
            .Validate(AreOptionsValid, $"Configuration section '{sectionName}' contains invalid Awin Reports options.")
            .ValidateOnStart();

        return services.AddCore();
    }

    private static IServiceCollection AddCore(this IServiceCollection services)
    {
        services.AddHttpClient<IAwinAffiliateReportsClient, AwinAffiliateReportsClient>();
        return services;
    }

    private static bool AreOptionsValid(AwinAffiliateReportsOptions options)
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
