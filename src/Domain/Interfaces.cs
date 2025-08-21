using System.Collections.ObjectModel;

namespace ImageTagging.Domain;

/// <summary>
/// Domain service for image processing operations
/// </summary>
public interface IImageProcessingService
{
    Task<Image> ProcessImageAsync(string imagePath, CancellationToken cancellationToken = default);
    Task<Image> ProcessImageAsync(Stream imageStream, string fileName, CancellationToken cancellationToken = default);
    Task<IEnumerable<Tag>> ExtractTagsFromDescriptionAsync(string description, CancellationToken cancellationToken = default);
    IAsyncEnumerable<string> GenerateDescriptionStreamingAsync(string imagePath, string question = "What is this image?", CancellationToken cancellationToken = default);
}

/// <summary>
/// Domain service for batch processing operations
/// </summary>
public interface IBatchProcessingService
{
    Task<ImageBatch> CreateBatchAsync(string name, string description, CancellationToken cancellationToken = default);
    Task<ImageBatch> ProcessBatchAsync(string batchId, CancellationToken cancellationToken = default);
    Task<ImageBatch?> GetBatchAsync(string batchId, CancellationToken cancellationToken = default);
    Task<IEnumerable<ImageBatch>> GetAllBatchesAsync(CancellationToken cancellationToken = default);
    Task CancelBatchProcessingAsync(string batchId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Domain service for DAM integration
/// </summary>
public interface IDamIntegrationService
{
    Task<IEnumerable<Image>> GetImagesFromDamAsync(string query, int page = 1, int pageSize = 50, CancellationToken cancellationToken = default);
    Task<Image?> GetImageFromDamAsync(string assetId, CancellationToken cancellationToken = default);
    Task<bool> UpdateImageTagsInDamAsync(string assetId, IEnumerable<Tag> tags, CancellationToken cancellationToken = default);
    Task<bool> UpdateImageMetadataInDamAsync(string assetId, Image image, CancellationToken cancellationToken = default);
}

/// <summary>
/// Domain service for AI model management
/// </summary>
public interface IAIModelService
{
    Task<bool> InitializeModelAsync(string modelPath, CancellationToken cancellationToken = default);
    Task<bool> IsModelLoadedAsync();
    Task<ModelInfo> GetModelInfoAsync();
    Task UnloadModelAsync();
}

/// <summary>
/// Domain service for configuration management
/// </summary>
public interface IConfigurationService
{
    Task<AppSettings> GetSettingsAsync();
    Task SaveSettingsAsync(AppSettings settings);
}

/// <summary>
/// Model information
/// </summary>
public class ModelInfo
{
    public string Name { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsLoaded { get; set; }
    public DateTime? LoadedDate { get; set; }
    public long MemoryUsage { get; set; }
}

/// <summary>
/// Application settings
/// </summary>
public class AppSettings
{
    public string AIModelPath { get; set; } = string.Empty;
    public string DamApiBaseUrl { get; set; } = string.Empty;
    public string DamApiKey { get; set; } = string.Empty;
    public int BatchSize { get; set; } = 10;
    public int MaxConcurrentProcessing { get; set; } = 3;
    public string DefaultQuestion { get; set; } = "What is this image?";
    public bool AutoSaveToDam { get; set; } = false;
    public string OutputFormat { get; set; } = "JSON";
    public string LogLevel { get; set; } = "Information";
}