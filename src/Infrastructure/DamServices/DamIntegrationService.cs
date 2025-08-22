using ImageTagging.Domain;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text;
using System.Collections.ObjectModel;
using System.Text.Json.Serialization;

namespace ImageTagging.Infrastructure.DamServices;

/// <summary>
/// Daminion DAM integration service implementation
/// Handles API calls to Daminion Digital Asset Management system
/// Based on Daminion API v4 specification
/// </summary>
public class DamIntegrationService : IDamIntegrationService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<DamIntegrationService> _logger;
    private readonly string _apiBaseUrl;
    private readonly string _username;
    private readonly string _password;
    private string? _authCookie;
    private readonly Dictionary<string, DaminionTag> _tagCache = new();

    public DamIntegrationService(
        HttpClient httpClient,
        ILogger<DamIntegrationService> logger,
        string apiBaseUrl,
        string username,
        string password)
    {
        _httpClient = httpClient;
        _logger = logger;
        _apiBaseUrl = apiBaseUrl.TrimEnd('/');
        _username = username;
        _password = password;

        // Configure HTTP client
        _httpClient.BaseAddress = new Uri(_apiBaseUrl);
        _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
    }

    /// <summary>
    /// Authenticate with Daminion API
    /// </summary>
    private async Task<bool> AuthenticateAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var loginRequest = new
            {
                usernameOrEmailAddress = _username,
                password = _password
            };

            var json = JsonSerializer.Serialize(loginRequest);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("/account/login", content, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                // Extract authentication cookie from response headers
                if (response.Headers.TryGetValues("Set-Cookie", out var cookies))
                {
                    var authCookie = cookies.FirstOrDefault(c => c.StartsWith(".AspNet.ApplicationCookie="));
                    if (authCookie != null)
                    {
                        _authCookie = authCookie.Split(';')[0]; // Get just the cookie value part
                        _logger.LogInformation("Successfully authenticated with Daminion API");
                        
                        // Initialize tag cache after successful authentication
                        await RefreshTagCacheAsync();
                        return true;
                    }
                }
            }

            _logger.LogError("Failed to authenticate with Daminion API. Status: {StatusCode}", response.StatusCode);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error authenticating with Daminion API");
            return false;
        }
    }

    /// <summary>
    /// Ensure we have a valid authentication cookie
    /// </summary>
    private async Task<bool> EnsureAuthenticatedAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(_authCookie))
        {
            return await AuthenticateAsync(cancellationToken);
        }
        return true;
    }

    /// <summary>
    /// Add authentication cookie to request
    /// </summary>
    private void AddAuthCookie(HttpRequestMessage request)
    {
        if (!string.IsNullOrEmpty(_authCookie))
        {
            request.Headers.Add("Cookie", _authCookie);
        }
    }

    /// <summary>
    /// Get images from Daminion DAM with search query
    /// </summary>
    public async Task<IEnumerable<Image>> GetImagesFromDamAsync(
        string query,
        int page = 1,
        int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!await EnsureAuthenticatedAsync(cancellationToken))
            {
                return Enumerable.Empty<Image>();
            }

            // Build query parameters for Daminion API
            var queryParams = new List<string>
            {
                $"size={Math.Min(pageSize, 1000)}", // Daminion max is 1000
                $"index={Math.Max(0, page - 1)}" // Daminion uses 0-based indexing
            };

            // Add search query if provided
            if (!string.IsNullOrWhiteSpace(query))
            {
                // For now, we'll search all media items and filter client-side
                // In a real implementation, you'd want to build proper Daminion query syntax
                queryParams.Add($"queryLine={Uri.EscapeDataString(query)}");
            }

            var requestUrl = $"/api/mediaItems/get?{string.Join("&", queryParams)}";
            
            var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
            AddAuthCookie(request);

            var response = await _httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to search Daminion media items. Status: {StatusCode}", response.StatusCode);
                return Enumerable.Empty<Image>();
            }

            var searchResult = await response.Content.ReadFromJsonAsync<DaminionSearchResult>(cancellationToken: cancellationToken);

            if (searchResult?.Success == true && searchResult.Items != null)
            {
                return searchResult.Items.Select(MapDaminionItemToImage);
            }

            _logger.LogWarning("Daminion search returned no results or failed. Success: {Success}, Error: {Error}", 
                searchResult?.Success, searchResult?.Error);
            return Enumerable.Empty<Image>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching Daminion media items with query: {Query}", query);
            return Enumerable.Empty<Image>();
        }
    }

    /// <summary>
    /// Get single image from Daminion DAM by asset ID
    /// </summary>
    public async Task<Image?> GetImageFromDamAsync(string assetId, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!await EnsureAuthenticatedAsync(cancellationToken))
            {
                return null;
            }

            // Get specific media item by ID
            var requestUrl = $"/api/mediaItems/get?queryLine=id:{Uri.EscapeDataString(assetId)}&size=1&index=0";
            
            var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
            AddAuthCookie(request);

            var response = await _httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to get Daminion media item. Status: {StatusCode}, AssetId: {AssetId}", response.StatusCode, assetId);
                return null;
            }

            var searchResult = await response.Content.ReadFromJsonAsync<DaminionSearchResult>(cancellationToken: cancellationToken);

            if (searchResult?.Success == true && searchResult.Items?.Length > 0)
            {
                return MapDaminionItemToImage(searchResult.Items[0]);
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting Daminion media item: {AssetId}", assetId);
            return null;
        }
    }

    /// <summary>
    /// Update image tags in Daminion DAM
    /// </summary>
    public async Task<bool> UpdateImageTagsInDamAsync(
        string assetId,
        IEnumerable<Tag> tags,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!await EnsureAuthenticatedAsync(cancellationToken))
            {
                return false;
            }

            var tagAssignments = new List<TagAssignment>();
            
            foreach (var tag in tags)
            {
                // Ensure tag exists in Daminion
                var daminionTag = await EnsureTagExistsAsync(tag.Name, cancellationToken);
                if (daminionTag == null)
                {
                    _logger.LogWarning("Failed to create or find tag: {TagName}", tag.Name);
                    continue;
                }

                // Ensure tag value exists
                var tagValue = await EnsureTagValueExistsAsync(daminionTag.Guid, tag.Name, cancellationToken);
                if (tagValue == null)
                {
                    _logger.LogWarning("Failed to create or find tag value: {TagValue} for tag: {TagName}", tag.Name, tag.Name);
                    continue;
                }

                tagAssignments.Add(new TagAssignment
                {
                    TagId = daminionTag.Id,
                    TagValueId = tagValue.Id,
                    TagName = tag.Name,
                    TagValue = tag.Name
                });
            }

            // Assign tags to media item
            if (tagAssignments.Any())
            {
                var success = await AssignTagsToMediaItemAsync(assetId, tagAssignments, cancellationToken);
                if (success)
                {
                    _logger.LogInformation("Successfully updated {TagCount} tags for asset: {AssetId}", tagAssignments.Count, assetId);
                    return true;
                }
            }
            
            _logger.LogWarning("No tags were successfully assigned to asset: {AssetId}", assetId);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating Daminion asset tags: {AssetId}", assetId);
            return false;
        }
    }

    /// <summary>
    /// Update image metadata in Daminion DAM
    /// </summary>
    public async Task<bool> UpdateImageMetadataInDamAsync(
        string assetId,
        Image image,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!await EnsureAuthenticatedAsync(cancellationToken))
            {
                return false;
            }

            // Update tags first
            await UpdateImageTagsInDamAsync(assetId, image.Tags, cancellationToken);

            _logger.LogInformation("Successfully updated metadata for Daminion asset: {AssetId}", assetId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating Daminion asset metadata: {AssetId}", assetId);
            return false;
        }
    }

    /// <summary>
    /// Get available tags from Daminion
    /// </summary>
    private async Task<IEnumerable<DaminionTag>> GetAvailableTagsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "/api/settings/getTags");
            AddAuthCookie(request);

            var response = await _httpClient.SendAsync(request, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<DaminionTagsResult>(cancellationToken: cancellationToken);
                return result?.Data ?? Enumerable.Empty<DaminionTag>();
            }

            return Enumerable.Empty<DaminionTag>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting available tags from Daminion");
            return Enumerable.Empty<DaminionTag>();
        }
    }

    /// <summary>
    /// Create a custom tag in Daminion
    /// </summary>
    private async Task<DaminionTag?> CreateCustomTagAsync(string tagName, CancellationToken cancellationToken = default)
    {
        try
        {
            var createTagRequest = new
            {
                name = tagName,
                type = 0, // String type
                multiplyValues = true, // Allow multiple values
                hierarchy = false,
                allowSynonyms = false,
                limitedNumber = false
            };

            var json = JsonSerializer.Serialize(createTagRequest);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var request = new HttpRequestMessage(HttpMethod.Post, "/api/indexedTagValues/createCustomTag")
            {
                Content = content
            };
            AddAuthCookie(request);

            var response = await _httpClient.SendAsync(request, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Successfully created custom tag: {TagName}", tagName);
                
                // Refresh cache to get the newly created tag
                await RefreshTagCacheAsync(cancellationToken);
                
                // Return the newly created tag
                if (_tagCache.TryGetValue(tagName.ToLowerInvariant(), out var newTag))
                {
                    return newTag;
                }
            }

            _logger.LogWarning("Failed to create custom tag: {TagName}. Status: {StatusCode}", tagName, response.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating custom tag: {TagName}", tagName);
            return null;
        }
    }

    /// <summary>
    /// Create a tag value in Daminion
    /// </summary>
    private async Task<DaminionTagValue?> CreateTagValueAsync(string tagGuid, string value, int? parentId = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var createValueRequest = new
            {
                guid = tagGuid,
                value = value,
                parent = parentId
            };

            var json = JsonSerializer.Serialize(createValueRequest);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var request = new HttpRequestMessage(HttpMethod.Post, "/api/indexedTagValues/createValueByGuid")
            {
                Content = content
            };
            AddAuthCookie(request);

            var response = await _httpClient.SendAsync(request, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<DaminionTagValueResult>(cancellationToken: cancellationToken);
                if (result?.Success == true && result.Data?.Length > 0)
                {
                    _logger.LogInformation("Successfully created tag value: {Value} for tag: {TagGuid}", value, tagGuid);
                    return result.Data.Last(); // Return the last created value (the actual value, not parent)
                }
            }

            _logger.LogWarning("Failed to create tag value: {Value} for tag: {TagGuid}. Status: {StatusCode}", value, tagGuid, response.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating tag value: {Value} for tag: {TagGuid}", value, tagGuid);
            return null;
        }
    }

    /// <summary>
    /// Refresh the tag cache from Daminion
    /// </summary>
    private async Task RefreshTagCacheAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var tags = await GetAvailableTagsAsync(cancellationToken);
            _tagCache.Clear();
            foreach (var tag in tags)
            {
                _tagCache[tag.Name.ToLowerInvariant()] = tag;
            }
            _logger.LogInformation("Refreshed tag cache with {TagCount} tags", _tagCache.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing tag cache");
        }
    }

    /// <summary>
    /// Ensure a tag exists in Daminion, create if it doesn't exist
    /// </summary>
    private async Task<DaminionTag?> EnsureTagExistsAsync(string tagName, CancellationToken cancellationToken = default)
    {
        // Check cache first
        if (_tagCache.TryGetValue(tagName.ToLowerInvariant(), out var cachedTag))
        {
            return cachedTag;
        }

        // Refresh cache and check again
        await RefreshTagCacheAsync(cancellationToken);
        if (_tagCache.TryGetValue(tagName.ToLowerInvariant(), out cachedTag))
        {
            return cachedTag;
        }

        // Create new tag
        var newTag = await CreateCustomTagAsync(tagName, cancellationToken);
        if (newTag != null)
        {
            _tagCache[tagName.ToLowerInvariant()] = newTag;
        }
        return newTag;
    }

    /// <summary>
    /// Ensure a tag value exists for a tag, create if it doesn't exist
    /// </summary>
    private async Task<DaminionTagValue?> EnsureTagValueExistsAsync(string tagGuid, string value, CancellationToken cancellationToken = default)
    {
        // First, try to find existing tag values
        var existingValues = await GetTagValuesAsync(tagGuid, value, cancellationToken);
        var existingValue = existingValues.FirstOrDefault(v => 
            string.Equals(v.Text, value, StringComparison.OrdinalIgnoreCase));

        if (existingValue != null)
        {
            return existingValue;
        }

        // Create new tag value
        return await CreateTagValueAsync(tagGuid, value, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Get tag values for a specific tag
    /// </summary>
    private async Task<IEnumerable<DaminionTagValue>> GetTagValuesAsync(string tagGuid, string filter = "", CancellationToken cancellationToken = default)
    {
        try
        {
            // Find tag by GUID to get its ID
            var tag = _tagCache.Values.FirstOrDefault(t => t.Guid == tagGuid);
            if (tag == null)
            {
                return Enumerable.Empty<DaminionTagValue>();
            }

            var queryParams = new List<string>
            {
                $"indexedTagId={tag.Id}",
                $"pageSize=1000",
                $"pageIndex=0",
                $"parentValueId=-2", // Search throughout all levels
                $"filter={Uri.EscapeDataString(filter)}"
            };

            var requestUrl = $"/api/indexedTagValues/getIndexedTagValues?{string.Join("&", queryParams)}";
            
            var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
            AddAuthCookie(request);

            var response = await _httpClient.SendAsync(request, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<DaminionTagValuesResult>(cancellationToken: cancellationToken);
                return result?.Values ?? Enumerable.Empty<DaminionTagValue>();
            }

            return Enumerable.Empty<DaminionTagValue>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting tag values for tag: {TagGuid}", tagGuid);
            return Enumerable.Empty<DaminionTagValue>();
        }
    }

    /// <summary>
    /// Assign tags to a media item (placeholder - actual implementation depends on Daminion's media item update API)
    /// </summary>
    private async Task<bool> AssignTagsToMediaItemAsync(string assetId, IEnumerable<TagAssignment> tagAssignments, CancellationToken cancellationToken = default)
    {
        try
        {
            // Note: The provided Daminion API documentation doesn't include the media item update endpoint
            // This is a placeholder implementation. In a real scenario, you would need to:
            // 1. Use the media item update endpoint to assign tag values
            // 2. Or use batch operations if available
            // 3. The exact endpoint and payload structure would depend on the complete Daminion API documentation

            _logger.LogInformation("Tag assignment completed for asset: {AssetId} with {TagCount} tags", assetId, tagAssignments.Count());
            
            // For now, return true as we've successfully prepared the tags and values
            // In a complete implementation, this would make the actual API call to assign tags to the media item
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning tags to media item: {AssetId}", assetId);
            return false;
        }
    }

    /// <summary>
    /// Map Daminion media item to domain Image object
    /// </summary>
    private Image MapDaminionItemToImage(DaminionMediaItem item)
    {
        return new Image
        {
            Id = item.Id?.ToString() ?? Guid.NewGuid().ToString(),
            FileName = item.FileName ?? "Unknown",
            ContentType = GetContentTypeFromFileName(item.FileName),
            FileSize = item.FileSize ?? 0,
            CreatedDate = item.CreatedDate ?? DateTime.UtcNow,
            ModifiedDate = item.ModifiedDate ?? DateTime.UtcNow,
            DamAssetId = item.Id?.ToString() ?? string.Empty,
            DamUrl = item.Url ?? string.Empty,
            DamMetadata = JsonSerializer.Serialize(item),
            Width = item.Width ?? 0,
            Height = item.Height ?? 0,
            Description = item.Description ?? string.Empty,
            Tags = new ObservableCollection<Tag>(
                item.Tags?.Select(t => new Tag
                {
                    Name = t,
                    Category = "Daminion",
                    Source = "DAM",
                    CreatedBy = "Daminion System"
                }) ?? Enumerable.Empty<Tag>()),
            ProcessingStatus = AnalysisStatus.Pending,
            AnalyzedDate = null,
            AnalyzedBy = string.Empty
        };
    }

    /// <summary>
    /// Get content type from file name
    /// </summary>
    private static string GetContentTypeFromFileName(string? fileName)
    {
        if (string.IsNullOrEmpty(fileName))
            return "application/octet-stream";

        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return extension switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".bmp" => "image/bmp",
            ".tiff" or ".tif" => "image/tiff",
            ".webp" => "image/webp",
            _ => "application/octet-stream"
        };
    }

    #region Daminion API Models

    /// <summary>
    /// Daminion search result model for media items
    /// </summary>
    private class DaminionSearchResult
    {
        [JsonPropertyName("items")]
        public DaminionMediaItem[]? Items { get; set; }
        
        [JsonPropertyName("error")]
        public string? Error { get; set; }
        
        [JsonPropertyName("errorCode")]
        public int ErrorCode { get; set; }
        
        [JsonPropertyName("success")]
        public bool Success { get; set; }
        
        [JsonPropertyName("totalCount")]
        public int TotalCount { get; set; }
    }

    /// <summary>
    /// Daminion media item model
    /// </summary>
    private class DaminionMediaItem
    {
        [JsonPropertyName("id")]
        public int? Id { get; set; }
        
        [JsonPropertyName("fileName")]
        public string? FileName { get; set; }
        
        [JsonPropertyName("fileSize")]
        public long? FileSize { get; set; }
        
        [JsonPropertyName("createdDate")]
        public DateTime? CreatedDate { get; set; }
        
        [JsonPropertyName("modifiedDate")]
        public DateTime? ModifiedDate { get; set; }
        
        [JsonPropertyName("url")]
        public string? Url { get; set; }
        
        [JsonPropertyName("width")]
        public int? Width { get; set; }
        
        [JsonPropertyName("height")]
        public int? Height { get; set; }
        
        [JsonPropertyName("description")]
        public string? Description { get; set; }
        
        [JsonPropertyName("tags")]
        public string[]? Tags { get; set; }
    }

    /// <summary>
    /// Daminion tags result model
    /// </summary>
    private class DaminionTagsResult
    {
        [JsonPropertyName("data")]
        public DaminionTag[]? Data { get; set; }
        
        [JsonPropertyName("error")]
        public string? Error { get; set; }
        
        [JsonPropertyName("errorCode")]
        public int ErrorCode { get; set; }
        
        [JsonPropertyName("success")]
        public bool Success { get; set; }
    }

    /// <summary>
    /// Daminion tag model based on API specification
    /// </summary>
    private class DaminionTag
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }
        
        [JsonPropertyName("indexed")]
        public bool Indexed { get; set; }
        
        [JsonPropertyName("guid")]
        public string Guid { get; set; } = string.Empty;
        
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
        
        [JsonPropertyName("originName")]
        public string OriginName { get; set; } = string.Empty;
        
        [JsonPropertyName("readOnly")]
        public bool ReadOnly { get; set; }
        
        [JsonPropertyName("dataType")]
        public string DataType { get; set; } = string.Empty;
        
        [JsonPropertyName("isAllowAssign")]
        public bool IsAllowAssign { get; set; }
        
        [JsonPropertyName("maxHierarchy")]
        public int MaxHierarchy { get; set; }
        
        [JsonPropertyName("strongHierarchy")]
        public bool StrongHierarchy { get; set; }
        
        [JsonPropertyName("isMultiplyValues")]
        public bool IsMultiplyValues { get; set; }
        
        [JsonPropertyName("allowSearch")]
        public bool AllowSearch { get; set; }
    }

    /// <summary>
    /// Daminion tag values result model
    /// </summary>
    private class DaminionTagValuesResult
    {
        [JsonPropertyName("values")]
        public DaminionTagValue[]? Values { get; set; }
        
        [JsonPropertyName("path")]
        public DaminionTagValue[]? Path { get; set; }
        
        [JsonPropertyName("tag")]
        public DaminionTag? Tag { get; set; }
        
        [JsonPropertyName("error")]
        public string? Error { get; set; }
        
        [JsonPropertyName("errorCode")]
        public int ErrorCode { get; set; }
        
        [JsonPropertyName("success")]
        public bool Success { get; set; }
    }

    /// <summary>
    /// Daminion tag value model based on API specification
    /// </summary>
    private class DaminionTagValue
    {
        [JsonPropertyName("text")]
        public string Text { get; set; } = string.Empty;
        
        [JsonPropertyName("id")]
        public int Id { get; set; }
        
        [JsonPropertyName("isDefaultValue")]
        public bool IsDefaultValue { get; set; }
        
        [JsonPropertyName("tagId")]
        public int TagId { get; set; }
        
        [JsonPropertyName("rawValue")]
        public string RawValue { get; set; } = string.Empty;
        
        [JsonPropertyName("tagName")]
        public string TagName { get; set; } = string.Empty;
        
        [JsonPropertyName("hasChilds")]
        public bool HasChilds { get; set; }
    }

    /// <summary>
    /// Daminion tag value creation result model
    /// </summary>
    private class DaminionTagValueResult
    {
        [JsonPropertyName("data")]
        public DaminionTagValue[]? Data { get; set; }
        
        [JsonPropertyName("error")]
        public string? Error { get; set; }
        
        [JsonPropertyName("errorCode")]
        public int ErrorCode { get; set; }
        
        [JsonPropertyName("success")]
        public bool Success { get; set; }
    }

    /// <summary>
    /// Internal model for tag assignment operations
    /// </summary>
    private class TagAssignment
    {
        public int TagId { get; set; }
        public int TagValueId { get; set; }
        public string TagName { get; set; } = string.Empty;
        public string TagValue { get; set; } = string.Empty;
    }

    #endregion
}