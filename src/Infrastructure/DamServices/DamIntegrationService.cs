using ImageTagging.Domain;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using System.Text.Json;

namespace ImageTagging.Infrastructure.DamServices;

/// <summary>
/// DAM integration service implementation
/// Handles API calls to Digital Asset Management system
/// </summary>
public class DamIntegrationService : IDamIntegrationService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<DamIntegrationService> _logger;
    private readonly string _apiBaseUrl;
    private readonly string _apiKey;

    public DamIntegrationService(
        HttpClient httpClient,
        ILogger<DamIntegrationService> logger,
        string apiBaseUrl,
        string apiKey)
    {
        _httpClient = httpClient;
        _logger = logger;
        _apiBaseUrl = apiBaseUrl;
        _apiKey = apiKey;

        // Configure HTTP client
        _httpClient.BaseAddress = new Uri(apiBaseUrl);
        _httpClient.DefaultRequestHeaders.Add("X-API-Key", apiKey);
        _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
    }

    /// <summary>
    /// Get images from DAM with search query
    /// </summary>
    public async Task<IEnumerable<Image>> GetImagesFromDamAsync(
        string query,
        int page = 1,
        int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var requestUrl = $"/api/assets/search?q={Uri.EscapeDataString(query)}&page={page}&size={pageSize}";
            var response = await _httpClient.GetAsync(requestUrl, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to search DAM assets. Status: {StatusCode}", response.StatusCode);
                return Enumerable.Empty<Image>();
            }

            var searchResult = await response.Content.ReadFromJsonAsync<DamSearchResult>(cancellationToken: cancellationToken);

            return searchResult?.Assets?.Select(MapDamAssetToImage) ?? Enumerable.Empty<Image>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching DAM assets with query: {Query}", query);
            return Enumerable.Empty<Image>();
        }
    }

    /// <summary>
    /// Get single image from DAM by asset ID
    /// </summary>
    public async Task<Image?> GetImageFromDamAsync(string assetId, CancellationToken cancellationToken = default)
    {
        try
        {
            var requestUrl = $"/api/assets/{assetId}";
            var response = await _httpClient.GetAsync(requestUrl, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to get DAM asset. Status: {StatusCode}, AssetId: {AssetId}", response.StatusCode, assetId);
                return null;
            }

            var asset = await response.Content.ReadFromJsonAsync<DamAsset>(cancellationToken: cancellationToken);

            return asset != null ? MapDamAssetToImage(asset) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting DAM asset: {AssetId}", assetId);
            return null;
        }
    }

    /// <summary>
    /// Update image tags in DAM
    /// </summary>
    public async Task<bool> UpdateImageTagsInDamAsync(
        string assetId,
        IEnumerable<Tag> tags,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var requestUrl = $"/api/assets/{assetId}/tags";
            var tagNames = tags.Select(t => t.Name).ToArray();

            var updateRequest = new
            {
                tags = tagNames,
                updatedBy = "ImageTaggingApp",
                updatedDate = DateTime.UtcNow
            };

            var response = await _httpClient.PutAsJsonAsync(requestUrl, updateRequest, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to update DAM asset tags. Status: {StatusCode}, AssetId: {AssetId}", response.StatusCode, assetId);
                return false;
            }

            _logger.LogInformation("Successfully updated tags for DAM asset: {AssetId}", assetId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating DAM asset tags: {AssetId}", assetId);
            return false;
        }
    }

    /// <summary>
    /// Update image metadata in DAM
    /// </summary>
    public async Task<bool> UpdateImageMetadataInDamAsync(
        string assetId,
        Image image,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var requestUrl = $"/api/assets/{assetId}/metadata";

            var metadataUpdate = new
            {
                description = image.Description,
                tags = image.Tags.Select(t => t.Name).ToArray(),
                analyzedBy = image.AnalyzedBy,
                analyzedDate = image.AnalyzedDate,
                analysisStatus = image.ProcessingStatus,
                width = image.Width,
                height = image.Height,
                fileSize = image.FileSize
            };

            var response = await _httpClient.PutAsJsonAsync(requestUrl, metadataUpdate, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to update DAM asset metadata. Status: {StatusCode}, AssetId: {AssetId}", response.StatusCode, assetId);
                return false;
            }

            _logger.LogInformation("Successfully updated metadata for DAM asset: {AssetId}", assetId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating DAM asset metadata: {AssetId}", assetId);
            return false;
        }
    }

    /// <summary>
    /// Map DAM asset to domain Image object
    /// </summary>
    private Image MapDamAssetToImage(DamAsset asset)
    {
        return new Image
        {
            Id = asset.Id,
            FileName = asset.FileName,
            ContentType = asset.ContentType,
            FileSize = asset.FileSize,
            CreatedDate = asset.CreatedDate,
            ModifiedDate = asset.ModifiedDate,
            DamAssetId = asset.Id,
            DamUrl = asset.Url,
            DamMetadata = JsonSerializer.Serialize(asset.Metadata),
            Width = asset.Width ?? 0,
            Height = asset.Height ?? 0,
            Description = asset.Metadata?.Description ?? string.Empty,
            Tags = new System.Collections.ObjectModel.ObservableCollection<Tag>(
                asset.Tags?.Select(t => new Tag
                {
                    Name = t,
                    Category = "DAM",
                    Source = "DAM",
                    CreatedBy = "DAM System"
                }) ?? Enumerable.Empty<Tag>()),
            ProcessingStatus = string.IsNullOrEmpty(asset.Metadata?.AnalysisStatus) ? AnalysisStatus.Pending : asset.Metadata.AnalysisStatus,
            AnalyzedDate = asset.Metadata?.AnalyzedDate,
            AnalyzedBy = asset.Metadata?.AnalyzedBy ?? string.Empty
        };
    }

    /// <summary>
    /// DAM search result model
    /// </summary>
    private class DamSearchResult
    {
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public DamAsset[]? Assets { get; set; }
    }

    /// <summary>
    /// DAM asset model
    /// </summary>
    private class DamAsset
    {
        public string Id { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime ModifiedDate { get; set; }
        public string Url { get; set; } = string.Empty;
        public int? Width { get; set; }
        public int? Height { get; set; }
        public string[]? Tags { get; set; }
        public DamAssetMetadata? Metadata { get; set; }
    }

    /// <summary>
    /// DAM asset metadata model
    /// </summary>
    private class DamAssetMetadata
    {
        public string? Description { get; set; }
        public string? AnalysisStatus { get; set; }
        public DateTime? AnalyzedDate { get; set; }
        public string? AnalyzedBy { get; set; }
    }
}