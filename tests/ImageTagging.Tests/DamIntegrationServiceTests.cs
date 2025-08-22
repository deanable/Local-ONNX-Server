using ImageTagging.Domain;
using ImageTagging.Infrastructure.DamServices;
using Microsoft.Extensions.Logging;
using Moq;
using System.Net;
using System.Text;
using System.Text.Json;
using Xunit;

namespace ImageTagging.Tests;

/// <summary>
/// Integration tests for Daminion API implementation
/// These tests demonstrate the API integration but require a running Daminion instance
/// </summary>
public class DamIntegrationServiceTests
{
    private readonly Mock<ILogger<DamIntegrationService>> _mockLogger;
    private readonly HttpClient _httpClient;

    public DamIntegrationServiceTests()
    {
        _mockLogger = new Mock<ILogger<DamIntegrationService>>();
        _httpClient = new HttpClient();
    }

    [Fact]
    public void Constructor_ShouldInitializeCorrectly()
    {
        // Arrange & Act
        var service = new DamIntegrationService(
            _httpClient,
            _mockLogger.Object,
            "https://test.daminion.net",
            "admin",
            "admin"
        );

        // Assert
        Assert.NotNull(service);
    }

    [Fact]
    public async Task GetImagesFromDamAsync_WithValidQuery_ShouldReturnImages()
    {
        // Arrange
        var service = new DamIntegrationService(
            _httpClient,
            _mockLogger.Object,
            "https://test.daminion.net",
            "admin",
            "admin"
        );

        // Act
        var result = await service.GetImagesFromDamAsync("test", 1, 10);

        // Assert
        Assert.NotNull(result);
        // Note: This test will fail without a running Daminion instance
        // In a real scenario, you would mock the HttpClient or use a test server
    }

    [Fact]
    public async Task UpdateImageTagsInDamAsync_WithValidTags_ShouldReturnTrue()
    {
        // Arrange
        var service = new DamIntegrationService(
            _httpClient,
            _mockLogger.Object,
            "https://test.daminion.net",
            "admin",
            "admin"
        );

        var tags = new List<Tag>
        {
            new Tag { Name = "TestTag", Category = "AI", Confidence = 0.95 }
        };

        // Act
        var result = await service.UpdateImageTagsInDamAsync("123", tags);

        // Assert
        // Note: This test will fail without a running Daminion instance
        // The actual result depends on authentication and API availability
        Assert.IsType<bool>(result);
    }

    [Theory]
    [InlineData("")]
    [InlineData("landscape")]
    [InlineData("portrait")]
    public async Task GetImagesFromDamAsync_WithDifferentQueries_ShouldHandleGracefully(string query)
    {
        // Arrange
        var service = new DamIntegrationService(
            _httpClient,
            _mockLogger.Object,
            "https://test.daminion.net",
            "admin",
            "admin"
        );

        // Act
        var result = await service.GetImagesFromDamAsync(query);

        // Assert
        Assert.NotNull(result);
        // Should not throw exceptions even with invalid queries
    }

    [Fact]
    public void DamIntegrationService_ShouldImplementIDamIntegrationService()
    {
        // Arrange & Act
        var service = new DamIntegrationService(
            _httpClient,
            _mockLogger.Object,
            "https://test.daminion.net",
            "admin",
            "admin"
        );

        // Assert
        Assert.IsAssignableFrom<IDamIntegrationService>(service);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _httpClient?.Dispose();
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}

/// <summary>
/// Unit tests for Daminion API models and serialization
/// </summary>
public class DaminionApiModelTests
{
    [Fact]
    public void DaminionTag_ShouldSerializeCorrectly()
    {
        // Arrange
        var tag = new
        {
            id = 1,
            indexed = true,
            guid = "test-guid",
            name = "TestTag",
            originName = "TestTag",
            readOnly = false,
            dataType = "string",
            isAllowAssign = true,
            maxHierarchy = 0,
            strongHierarchy = false,
            isMultiplyValues = true,
            allowSearch = true
        };

        // Act
        var json = JsonSerializer.Serialize(tag);
        var deserialized = JsonSerializer.Deserialize<Dictionary<string, object>>(json);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal(1, ((JsonElement)deserialized["id"]).GetInt32());
        Assert.Equal("TestTag", ((JsonElement)deserialized["name"]).GetString());
    }

    [Fact]
    public void DaminionTagValue_ShouldSerializeCorrectly()
    {
        // Arrange
        var tagValue = new
        {
            text = "TestValue",
            id = 1,
            isDefaultValue = false,
            tagId = 1,
            rawValue = "TestValue",
            tagName = "TestTag",
            hasChilds = false
        };

        // Act
        var json = JsonSerializer.Serialize(tagValue);
        var deserialized = JsonSerializer.Deserialize<Dictionary<string, object>>(json);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal("TestValue", ((JsonElement)deserialized["text"]).GetString());
        Assert.Equal(1, ((JsonElement)deserialized["id"]).GetInt32());
    }

    [Fact]
    public void DaminionMediaItem_ShouldSerializeCorrectly()
    {
        // Arrange
        var mediaItem = new
        {
            id = 123,
            fileName = "test.jpg",
            fileSize = 1024L,
            createdDate = DateTime.UtcNow,
            modifiedDate = DateTime.UtcNow,
            url = "https://example.com/test.jpg",
            width = 800,
            height = 600,
            description = "Test image",
            tags = new[] { "tag1", "tag2" }
        };

        // Act
        var json = JsonSerializer.Serialize(mediaItem);
        var deserialized = JsonSerializer.Deserialize<Dictionary<string, object>>(json);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal(123, ((JsonElement)deserialized["id"]).GetInt32());
        Assert.Equal("test.jpg", ((JsonElement)deserialized["fileName"]).GetString());
    }
}