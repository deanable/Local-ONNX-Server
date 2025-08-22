using System.Collections.ObjectModel;

namespace ImageTagging.Domain;

/// <summary>
/// Represents an image with its metadata and tags
/// </summary>
public class Image
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime ModifiedDate { get; set; } = DateTime.UtcNow;

    // Image dimensions
    public int Width { get; set; }
    public int Height { get; set; }

    // DAM specific properties
    public string DamAssetId { get; set; } = string.Empty;
    public string DamUrl { get; set; } = string.Empty;
    public string DamMetadata { get; set; } = string.Empty;

    // AI Analysis results
    public string Description { get; set; } = string.Empty;
    public ObservableCollection<Tag> Tags { get; set; } = new();
    public string ProcessingStatus { get; set; } = AnalysisStatus.Pending;
    public DateTime? AnalyzedDate { get; set; }
    public string AnalyzedBy { get; set; } = string.Empty;

    // Navigation properties
    public string BatchId { get; set; } = string.Empty;
    public ImageBatch? Batch { get; set; }
}

/// <summary>
/// Represents a tag/label for an image
/// </summary>
public class Tag
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public double Confidence { get; set; }
    public string Source { get; set; } = "AI"; // AI, Manual, DAM
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public string CreatedBy { get; set; } = string.Empty;
}

/// <summary>
/// Represents a batch of images for processing
/// </summary>
public class ImageBatch
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public string CreatedBy { get; set; } = string.Empty;
    public string Status { get; set; } = BatchStatus.Created;
    public int TotalImages { get; set; }
    public int ProcessedImages { get; set; }
    public int FailedImages { get; set; }

    public ObservableCollection<Image> Images { get; set; } = new();
}

/// <summary>
/// Analysis status constants
/// </summary>
public static class AnalysisStatus
{
    public const string Pending = "Pending";
    public const string Processing = "Processing";
    public const string Completed = "Completed";
    public const string Failed = "Failed";
    public const string Cancelled = "Cancelled";
}

/// <summary>
/// Batch status constants
/// </summary>
public static class BatchStatus
{
    public const string Created = "Created";
    public const string Processing = "Processing";
    public const string Completed = "Completed";
    public const string Failed = "Failed";
    public const string Cancelled = "Cancelled";
}