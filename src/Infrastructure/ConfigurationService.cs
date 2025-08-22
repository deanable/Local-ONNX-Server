using ImageTagging.Domain;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace ImageTagging.Infrastructure;

/// <summary>
/// Configuration service for managing application settings
/// </summary>
public class ConfigurationService : IConfigurationService
{
    private readonly ILogger<ConfigurationService> _logger;
    private readonly string _configFilePath;
    private AppSettings? _cachedSettings;

    public ConfigurationService(ILogger<ConfigurationService> logger)
    {
        _logger = logger;
        _configFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
    }

    /// <summary>
    /// Get application settings
    /// </summary>
    public async Task<AppSettings> GetSettingsAsync()
    {
        if (_cachedSettings != null)
        {
            return _cachedSettings;
        }

        try
        {
            if (File.Exists(_configFilePath))
            {
                var json = await File.ReadAllTextAsync(_configFilePath);
                _cachedSettings = JsonSerializer.Deserialize<AppSettings>(json) ?? CreateDefaultSettings();
            }
            else
            {
                _cachedSettings = CreateDefaultSettings();
                await SaveSettingsAsync(_cachedSettings);
            }

            return _cachedSettings;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading settings from {ConfigFilePath}", _configFilePath);
            return CreateDefaultSettings();
        }
    }

    /// <summary>
    /// Save application settings
    /// </summary>
    public async Task<bool> SaveSettingsAsync(AppSettings settings)
    {
        try
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };

            var json = JsonSerializer.Serialize(settings, options);
            await File.WriteAllTextAsync(_configFilePath, json);

            _cachedSettings = settings;
            _logger.LogInformation("Settings saved successfully to {ConfigFilePath}", _configFilePath);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving settings to {ConfigFilePath}", _configFilePath);
            return false;
        }
    }

    /// <summary>
    /// Create default settings
    /// </summary>
    private static AppSettings CreateDefaultSettings()
    {
        return new AppSettings
        {
            AIModelPath = string.Empty,
            DamApiBaseUrl = "https://test.daminion.net",
            DamUsername = "admin",
            DamPassword = "admin",
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