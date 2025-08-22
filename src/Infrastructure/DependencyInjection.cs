using ImageTagging.Domain;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
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
        services.AddHttpClient<DamServices.DamIntegrationService>();
        services.AddScoped<IDamIntegrationService>(provider =>
        {
            var httpClientFactory = provider.GetRequiredService<IHttpClientFactory>();
            var httpClient = httpClientFactory.CreateClient();
            var logger = provider.GetRequiredService<ILogger<DamServices.DamIntegrationService>>();
            return new DamServices.DamIntegrationService(httpClient, logger, settings.DamApiBaseUrl, settings.DamUsername, settings.DamPassword);
        });

        // Register configuration service
        services.AddSingleton<IConfigurationService, ConfigurationService>();

        return services;
    }
}