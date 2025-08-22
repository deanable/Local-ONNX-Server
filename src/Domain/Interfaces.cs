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