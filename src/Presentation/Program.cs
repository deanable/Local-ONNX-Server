using ImageTagging.Application;
using ImageTagging.Domain;
using ImageTagging.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ImageTagging.Presentation;

internal static class Program
{
    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    static async Task Main()
    {
        // To customize application configuration such as set high DPI settings or default font,
        // see https://aka.ms/applicationconfiguration.
        ApplicationConfiguration.Initialize();

        // Set up dependency injection
        var serviceProvider = ConfigureServices();

        // Load application settings
        var configurationService = serviceProvider.GetRequiredService<IConfigurationService>();
        var settings = await configurationService.GetSettingsAsync();

        // Initialize AI model if path is configured
        if (!string.IsNullOrEmpty(settings.AIModelPath))
        {
            var aiModelService = serviceProvider.GetRequiredService<IAIModelService>();
            var modelInitialized = await aiModelService.InitializeModelAsync(settings.AIModelPath);

            if (!modelInitialized)
            {
                MessageBox.Show(
                    "Failed to initialize AI model. Please check the model path in settings.",
                    "Initialization Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        // Run the application
        System.Windows.Forms.Application.Run(serviceProvider.GetRequiredService<MainForm>());
    }

    private static ServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();

        // Add logging
        services.AddLogging(configure =>
        {
            configure.AddConsole();
            configure.SetMinimumLevel(LogLevel.Information);
        });

        // Load default settings for DI configuration
        var defaultSettings = new AppSettings
        {
            AIModelPath = string.Empty,
            DamApiBaseUrl = "http://localhost:8080",
            DamApiKey = string.Empty,
            BatchSize = 10,
            MaxConcurrentProcessing = 3,
            DefaultQuestion = "What is this image?",
            AutoSaveToDam = false,
            OutputFormat = "JSON",
            LogLevel = "Information"
        };

        // Add infrastructure services
        services.AddInfrastructure(defaultSettings);

        // Add application services
        services.AddTransient<ImageProcessingService>();

        // Add presentation layer services
        services.AddTransient<MainForm>();
        services.AddTransient<SettingsForm>();

        return services.BuildServiceProvider();
    }
}