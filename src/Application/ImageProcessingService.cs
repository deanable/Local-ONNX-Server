using ImageTagging.Domain;

namespace ImageTagging.Application;

/// <summary>
/// Application service for image processing operations
/// </summary>
public class ImageProcessingService
{
    private readonly IImageProcessingService _imageProcessingService;
    private readonly IDamIntegrationService _damIntegrationService;
    private readonly IAIModelService _aiModelService;

    public ImageProcessingService(
        IImageProcessingService imageProcessingService,
        IDamIntegrationService damIntegrationService,
        IAIModelService aiModelService)
    {
        _imageProcessingService = imageProcessingService;
        _damIntegrationService = damIntegrationService;
        _aiModelService = aiModelService;
    }

    /// <summary>
    /// Process a single image from file path
    /// </summary>
    public async Task<ImageProcessingResult> ProcessImageFromPathAsync(string imagePath, CancellationToken cancellationToken = default)
    {
        try
        {
            var image = await _imageProcessingService.ProcessImageAsync(imagePath, cancellationToken);
            return ImageProcessingResult.Success(image);
        }
        catch (Exception ex)
        {
            return ImageProcessingResult.Failure(ex.Message);
        }
    }

    /// <summary>
    /// Process a single image from DAM
    /// </summary>
    public async Task<ImageProcessingResult> ProcessImageFromDamAsync(string assetId, CancellationToken cancellationToken = default)
    {
        try
        {
            var image = await _damIntegrationService.GetImageFromDamAsync(assetId, cancellationToken);
            if (image == null)
            {
                return ImageProcessingResult.Failure($"Image with asset ID {assetId} not found in DAM");
            }

            // Process the image with AI
            var processedImage = await _imageProcessingService.ProcessImageAsync(image.FilePath, cancellationToken);

            // Update the original image with AI results
            image.Description = processedImage.Description;
            image.Tags = processedImage.Tags;
            image.AnalysisStatus = processedImage.AnalysisStatus;
            image.AnalyzedDate = processedImage.AnalyzedDate;

            return ImageProcessingResult.Success(image);
        }
        catch (Exception ex)
        {
            return ImageProcessingResult.Failure(ex.Message);
        }
    }

    /// <summary>
    /// Process multiple images from DAM
    /// </summary>
    public async Task<BatchProcessingResult> ProcessImagesFromDamAsync(
        string query,
        int page = 1,
        int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var images = await _damIntegrationService.GetImagesFromDamAsync(query, page, pageSize, cancellationToken);
            var processedImages = new List<Image>();
            var errors = new List<string>();

            foreach (var image in images)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                try
                {
                    var result = await ProcessImageFromDamAsync(image.DamAssetId, cancellationToken);
                    if (result.IsSuccess)
                    {
                        processedImages.Add(result.Image!);
                    }
                    else
                    {
                        errors.Add($"Failed to process {image.FileName}: {result.ErrorMessage}");
                    }
                }
                catch (Exception ex)
                {
                    errors.Add($"Failed to process {image.FileName}: {ex.Message}");
                }
            }

            return BatchProcessingResult.Create(processedImages, errors);
        }
        catch (Exception ex)
        {
            return BatchProcessingResult.Failure(ex.Message);
        }
    }

    /// <summary>
    /// Update image tags in DAM
    /// </summary>
    public async Task<bool> UpdateImageTagsInDamAsync(string assetId, IEnumerable<Tag> tags, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _damIntegrationService.UpdateImageTagsInDamAsync(assetId, tags, cancellationToken);
        }
        catch (Exception)
        {
            return false;
        }
    }
}

/// <summary>
/// Result of image processing operations
/// </summary>
public class ImageProcessingResult
{
    public bool IsSuccess { get; }
    public Image? Image { get; }
    public string ErrorMessage { get; }

    private ImageProcessingResult(bool isSuccess, Image? image, string errorMessage)
    {
        IsSuccess = isSuccess;
        Image = image;
        ErrorMessage = errorMessage;
    }

    public static ImageProcessingResult Success(Image image) =>
        new(true, image, string.Empty);

    public static ImageProcessingResult Failure(string errorMessage) =>
        new(false, null, errorMessage);
}

/// <summary>
/// Result of batch processing operations
/// </summary>
public class BatchProcessingResult
{
    public bool IsSuccess { get; }
    public IReadOnlyList<Image> ProcessedImages { get; }
    public IReadOnlyList<string> Errors { get; }
    public string ErrorMessage { get; }

    private BatchProcessingResult(bool isSuccess, IReadOnlyList<Image> processedImages, IReadOnlyList<string> errors, string errorMessage)
    {
        IsSuccess = isSuccess;
        ProcessedImages = processedImages;
        Errors = errors;
        ErrorMessage = errorMessage;
    }

    public static BatchProcessingResult Create(IReadOnlyList<Image> processedImages, IReadOnlyList<string> errors) =>
        new(true, processedImages, errors, string.Empty);

    public static BatchProcessingResult Failure(string errorMessage) =>
        new(false, new List<Image>(), new List<string>(), errorMessage);
}