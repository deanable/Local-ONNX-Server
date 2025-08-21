using ImageTagging.Domain;
using System.Configuration;

namespace ImageTagging.Infrastructure;

/// <summary>
/// Configuration service implementation
/// </summary>
public class ConfigurationService : IConfigurationService
{
    private readonly string _configFilePath;

    public ConfigurationService(string configFilePath = "appsettings.json")
    {
        _configFilePath = configFilePath;
    }

    /// <summary>
    /// Get application settings
    /// </summary>
    public async Task<AppSettings> GetSettingsAsync()
    {
        try
        {
            if (!File.Exists(_configFilePath))
            {
                return GetDefaultSettings();
            }

            var json = await File.ReadAllTextAsync(_configFilePath);
            var settings = System.Text.Json.JsonSerializer.Deserialize<AppSettings>(json);

            return settings ?? GetDefaultSettings();
        }
        catch (Exception)
        {
            return GetDefaultSettings();
        }
    }

    /// <summary>
    /// Save application settings
    /// </summary>
    public async Task SaveSettingsAsync(AppSettings settings)
    {
        try
        {
            var json = System.Text.Json.JsonSerializer.Serialize(settings, new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true
            });

            await File.WriteAllTextAsync(_configFilePath, json);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to save application settings", ex);
        }
    }

    /// <summary>
    /// Get default application settings
    /// </summary>
    private AppSettings GetDefaultSettings()
    {
        return new AppSettings
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
    }
}