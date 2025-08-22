namespace ImageTagging.Domain;

/// <summary>
/// Interface for configuration service
/// </summary>
public interface IConfigurationService
{
    /// <summary>
    /// Get application settings
    /// </summary>
    Task<AppSettings> GetSettingsAsync();

    /// <summary>
    /// Save application settings
    /// </summary>
    Task<bool> SaveSettingsAsync(AppSettings settings);
}