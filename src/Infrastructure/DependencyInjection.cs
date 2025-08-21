using ImageTagging.Domain;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ImageTagging.Infrastructure;

/// <summary>
/// Dependency injection configuration for infrastructure layer
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Add infrastructure services to the DI container
    /// </summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, AppSettings settings)
    {
        // Register AI services
        services.AddSingleton<IImageProcessingService, AIServices.PhiVisionService>();
        services.AddSingleton<IAIModelService, AIServices.PhiVisionService>();

        // Register DAM services
        services.AddHttpClient<IDamIntegrationService, DamServices.DamIntegrationService>((provider, client) =>
        {
            var logger = provider.GetRequiredService<ILogger<DamServices.DamIntegrationService>>();
            return new DamServices.DamIntegrationService(client, logger, settings.DamApiBaseUrl, settings.DamApiKey);
        });

        // Register configuration service
        services.AddSingleton<IConfigurationService, ConfigurationService>();

        // Register batch processing service
        services.AddTransient<IBatchProcessingService, BatchProcessingService>();

        return services;
    }
}