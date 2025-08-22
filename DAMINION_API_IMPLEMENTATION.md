# Daminion API Implementation

This document describes the implementation of the Daminion API v4 integration in the Image Tagging Application.

## Overview

The Daminion API integration allows the application to:
- Authenticate with Daminion DAM systems
- Search and retrieve media items
- Create and manage tags and tag values
- Assign AI-generated tags to media items

## Implementation Details

### Core Components

#### 1. DamIntegrationService
**Location**: `src/Infrastructure/DamServices/DamIntegrationService.cs`

The main service implementing the `IDamIntegrationService` interface with the following key features:

- **Authentication**: Cookie-based authentication using `/account/login` endpoint
- **Tag Management**: Create custom tags and tag values
- **Media Search**: Search media items with query support
- **Tag Assignment**: Assign AI-generated tags to media items
- **Caching**: Tag caching for improved performance

#### 2. API Models
Based on the Daminion API v4 specification:

- `DaminionTag`: Represents tags with full API properties
- `DaminionTagValue`: Represents tag values with hierarchy support
- `DaminionMediaItem`: Represents media items from search results
- `DaminionSearchResult`: Search response wrapper
- Various result models for API responses

### Key Features Implemented

#### Authentication
```csharp
// Authenticates using username/password and stores session cookie
private async Task<bool> AuthenticateAsync(CancellationToken cancellationToken = default)
```

#### Tag Management
```csharp
// Creates custom tags in Daminion
private async Task<DaminionTag?> CreateCustomTagAsync(string tagName, CancellationToken cancellationToken = default)

// Creates tag values for existing tags
private async Task<DaminionTagValue?> CreateTagValueAsync(string tagGuid, string value, int? parentId = null, CancellationToken cancellationToken = default)

// Ensures tags exist, creating them if necessary
private async Task<DaminionTag?> EnsureTagExistsAsync(string tagName, CancellationToken cancellationToken = default)
```

#### Media Item Operations
```csharp
// Search media items with query support
public async Task<IEnumerable<Image>> GetImagesFromDamAsync(string query, int page = 1, int pageSize = 50, CancellationToken cancellationToken = default)

// Get specific media item by ID
public async Task<Image?> GetImageFromDamAsync(string assetId, CancellationToken cancellationToken = default)

// Update tags for media items
public async Task<bool> UpdateImageTagsInDamAsync(string assetId, IEnumerable<Tag> tags, CancellationToken cancellationToken = default)
```

### API Endpoints Implemented

Based on the provided Daminion API v4 documentation:

1. **Authentication**
   - `POST /account/login` - User authentication

2. **Tag Management**
   - `GET /api/settings/getTags` - Get available tags
   - `POST /api/indexedTagValues/createCustomTag` - Create custom tags
   - `POST /api/indexedTagValues/createValueByGuid` - Create tag values
   - `GET /api/indexedTagValues/getIndexedTagValues` - Get tag values

3. **Media Items**
   - `GET /api/mediaItems/get` - Search media items
   - `GET /api/mediaItems/getSort` - Get sort tags (referenced but not fully implemented)

### Configuration

#### AppSettings
Updated to include Daminion authentication:

```csharp
public class AppSettings
{
    public string DamApiBaseUrl { get; set; } = string.Empty;
    public string DamUsername { get; set; } = string.Empty;
    public string DamPassword { get; set; } = string.Empty;
    // ... other settings
}
```

#### Dependency Injection
```csharp
services.AddScoped<IDamIntegrationService>(provider =>
{
    var httpClientFactory = provider.GetRequiredService<IHttpClientFactory>();
    var httpClient = httpClientFactory.CreateClient();
    var logger = provider.GetRequiredService<ILogger<DamIntegrationService>>();
    return new DamIntegrationService(httpClient, logger, settings.DamApiBaseUrl, settings.DamUsername, settings.DamPassword);
});
```

### User Interface

#### Settings Form
Updated to include Daminion credentials:
- DAM API URL field
- Username field
- Password field (masked input)

### Usage Example

```csharp
// Search for images
var images = await damService.GetImagesFromDamAsync("landscape", page: 1, pageSize: 20);

// Create tags from AI analysis
var aiTags = new List<Tag>
{
    new Tag { Name = "landscape", Category = "AI", Confidence = 0.95 },
    new Tag { Name = "mountain", Category = "AI", Confidence = 0.87 }
};

// Assign tags to media item
await damService.UpdateImageTagsInDamAsync("12345", aiTags);
```

### Error Handling

The implementation includes comprehensive error handling:
- Network connectivity issues
- Authentication failures
- API response errors
- Invalid data handling
- Logging for debugging

### Performance Optimizations

1. **Tag Caching**: Tags are cached to reduce API calls
2. **Batch Operations**: Support for batch tag creation
3. **Async/Await**: Non-blocking operations throughout
4. **Connection Reuse**: HttpClient reuse for better performance

### Testing

#### Unit Tests
- API model serialization tests
- Service interface compliance tests
- Error handling tests

#### Integration Tests
- Authentication flow tests
- Tag creation and assignment tests
- Media item search tests

**Note**: Integration tests require a running Daminion instance with valid credentials.

### Limitations and Future Enhancements

#### Current Limitations
1. **Media Item Update**: The provided API documentation doesn't include the complete media item update endpoint for tag assignment. The current implementation prepares tags but uses a placeholder for the actual assignment.

2. **Batch Operations**: While the infrastructure supports batch operations, the specific Daminion batch endpoints aren't fully documented in the provided specification.

3. **Advanced Search**: The current search implementation uses basic query strings. Advanced Daminion query syntax could be implemented for more sophisticated searches.

#### Future Enhancements
1. **Complete Media Item Update**: Implement full media item update once the complete API specification is available
2. **Hierarchical Tags**: Enhanced support for hierarchical tag structures
3. **Metadata Sync**: Bidirectional metadata synchronization
4. **Bulk Operations**: Batch processing for large media collections
5. **Real-time Updates**: WebSocket or polling for real-time DAM updates

### Security Considerations

1. **Credential Storage**: Passwords are stored in configuration files - consider encryption for production
2. **HTTPS**: Always use HTTPS for API communications
3. **Session Management**: Proper cookie handling and session timeout
4. **Input Validation**: All user inputs are validated before API calls

### Troubleshooting

#### Common Issues
1. **Authentication Failures**: Check username/password and API URL
2. **Network Errors**: Verify Daminion server accessibility
3. **Tag Creation Failures**: Ensure unique tag names and proper permissions
4. **Search Issues**: Verify query syntax and user permissions

#### Logging
The implementation includes comprehensive logging at various levels:
- Information: Successful operations
- Warning: Non-critical issues
- Error: Failures with full exception details

### API Compliance

This implementation follows the Daminion API v4 specification as provided, including:
- Proper HTTP methods and endpoints
- Correct request/response formats
- Authentication flow
- Error handling patterns
- JSON serialization with proper property names

The implementation is designed to be extensible and maintainable, following clean architecture principles and SOLID design patterns.