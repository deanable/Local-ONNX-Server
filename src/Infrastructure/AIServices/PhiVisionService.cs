using ImageTagging.Domain;
using Microsoft.ML.OnnxRuntimeGenAI;
using System.Drawing;
using System.Runtime.CompilerServices;

namespace ImageTagging.Infrastructure.AIServices;

/// <summary>
/// AI service implementation for Phi-3.5 vision model
/// Based on the Microsoft AI Dev Gallery DescribeImage sample
/// </summary>
public class PhiVisionService : IImageProcessingService, IAIModelService
{
    private Model? _model;
    private MultiModalProcessor? _processor;
    private TokenizerStream? _tokenizerStream;
    private bool _isInitialized;
    private readonly ILogger<PhiVisionService> _logger;

    public PhiVisionService(ILogger<PhiVisionService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Initialize the Phi-3.5 vision model
    /// </summary>
    public async Task<bool> InitializeModelAsync(string modelPath, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!File.Exists(modelPath))
            {
                _logger.LogError("Model file not found: {ModelPath}", modelPath);
                return false;
            }

            await Task.Run(() =>
            {
                _model = new Model(modelPath);
                _processor = new MultiModalProcessor(_model);
                _tokenizerStream = _processor.CreateStream();
                _isInitialized = true;
            }, cancellationToken);

            _logger.LogInformation("Phi-3.5 vision model initialized successfully from {ModelPath}", modelPath);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize Phi-3.5 vision model");
            await UnloadModelAsync();
            return false;
        }
    }

    /// <summary>
    /// Check if the model is loaded
    /// </summary>
    public Task<bool> IsModelLoadedAsync() => Task.FromResult(_isInitialized);

    /// <summary>
    /// Get model information
    /// </summary>
    public Task<ModelInfo> GetModelInfoAsync()
    {
        return Task.FromResult(new ModelInfo
        {
            Name = "Phi-3.5 Vision",
            Version = "3.5",
            Description = "Microsoft Phi-3.5 multimodal vision model",
            IsLoaded = _isInitialized,
            LoadedDate = _isInitialized ? DateTime.UtcNow : null,
            MemoryUsage = 0 // Could be calculated from model metadata
        });
    }

    /// <summary>
    /// Unload the model and clean up resources
    /// </summary>
    public Task UnloadModelAsync()
    {
        _tokenizerStream?.Dispose();
        _processor?.Dispose();
        _model?.Dispose();

        _tokenizerStream = null;
        _processor = null;
        _model = null;
        _isInitialized = false;

        _logger.LogInformation("Phi-3.5 vision model unloaded");
        return Task.CompletedTask;
    }

    /// <summary>
    /// Process an image from file path
    /// </summary>
    public async Task<Image> ProcessImageAsync(string imagePath, CancellationToken cancellationToken = default)
    {
        if (!_isInitialized || _model == null || _processor == null || _tokenizerStream == null)
        {
            throw new InvalidOperationException("Model not initialized");
        }

        var image = new Image
        {
            FileName = Path.GetFileName(imagePath),
            FilePath = imagePath,
            ContentType = GetContentType(imagePath),
            FileSize = new FileInfo(imagePath).Length,
            AnalysisStatus = AnalysisStatus.Processing
        };

        try
        {
            // Get image dimensions
            using (var img = System.Drawing.Image.FromFile(imagePath))
            {
                image.Width = img.Width;
                image.Height = img.Height;
            }

            // Generate description
            var description = await GenerateDescriptionAsync(imagePath, cancellationToken);
            image.Description = description;

            // Extract tags from description
            var tags = await ExtractTagsFromDescriptionAsync(description, cancellationToken);
            foreach (var tag in tags)
            {
                image.Tags.Add(tag);
            }

            image.AnalysisStatus = AnalysisStatus.Completed;
            image.AnalyzedDate = DateTime.UtcNow;
            image.AnalyzedBy = "Phi-3.5 Vision";

            _logger.LogInformation("Successfully processed image: {FileName}", image.FileName);
        }
        catch (Exception ex)
        {
            image.AnalysisStatus = AnalysisStatus.Failed;
            _logger.LogError(ex, "Failed to process image: {FileName}", image.FileName);
            throw;
        }

        return image;
    }

    /// <summary>
    /// Process an image from stream
    /// </summary>
    public async Task<Image> ProcessImageAsync(Stream imageStream, string fileName, CancellationToken cancellationToken = default)
    {
        // Save stream to temporary file for processing
        var tempPath = Path.Combine(Path.GetTempPath(), $"temp_{Guid.NewGuid()}_{fileName}");

        try
        {
            using (var fileStream = File.Create(tempPath))
            {
                await imageStream.CopyToAsync(fileStream, cancellationToken);
            }

            return await ProcessImageAsync(tempPath, cancellationToken);
        }
        finally
        {
            // Clean up temporary file
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }
    }

    /// <summary>
    /// Generate streaming description for an image
    /// </summary>
    public async IAsyncEnumerable<string> GenerateDescriptionStreamingAsync(
        string imagePath,
        string question = "What is this image?",
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (!_isInitialized || _model == null || _processor == null || _tokenizerStream == null)
        {
            throw new InvalidOperationException("Model not initialized");
        }

        var images = Images.Load([imagePath]);
        var prompt = $"<|user|>\n<|image_1|>\n{question}<|end|>\n<|assistant|>\n";
        string[] stopTokens = ["</s>", "<|user|>", "<|end|>", "<|assistant|>"];

        var inputTensors = _processor.ProcessImages(prompt, images);

        using GeneratorParams generatorParams = new(_model);
        generatorParams.SetSearchOption("max_length", 4096);
        generatorParams.SetInputs(inputTensors);

        cancellationToken.ThrowIfCancellationRequested();

        using var generator = new Generator(_model, generatorParams);

        while (!generator.IsDone())
        {
            cancellationToken.ThrowIfCancellationRequested();

            await Task.Delay(0, cancellationToken).ConfigureAwait(false);

            // This step takes a long time, theoretically, most cancellation get hung here
            generator.GenerateNextToken();
            cancellationToken.ThrowIfCancellationRequested();

            var part = _tokenizerStream.Decode(generator.GetSequence(0)[^1]);

            if (stopTokens.Contains(part))
            {
                break;
            }

            cancellationToken.ThrowIfCancellationRequested();
            yield return part;
        }
    }

    /// <summary>
    /// Generate complete description for an image
    /// </summary>
    private async Task<string> GenerateDescriptionAsync(string imagePath, CancellationToken cancellationToken = default)
    {
        var descriptionBuilder = new System.Text.StringBuilder();

        await foreach (var part in GenerateDescriptionStreamingAsync(imagePath, cancellationToken: cancellationToken))
        {
            descriptionBuilder.Append(part);
        }

        return descriptionBuilder.ToString().Trim();
    }

    /// <summary>
    /// Extract tags from description using simple keyword extraction
    /// In a real implementation, this could use more sophisticated NLP
    /// </summary>
    public async Task<IEnumerable<Tag>> ExtractTagsFromDescriptionAsync(string description, CancellationToken cancellationToken = default)
    {
        var tags = new List<Tag>();

        // Simple keyword extraction - split by common delimiters and filter
        var words = description
            .Split([' ', ',', '.', ';', ':', '\n'], StringSplitOptions.RemoveEmptyEntries)
            .Select(w => w.Trim().ToLower())
            .Where(w => w.Length > 3) // Filter out very short words
            .Distinct()
            .Take(20); // Limit number of tags

        foreach (var word in words)
        {
            tags.Add(new Tag
            {
                Name = word,
                Category = "Auto-Extracted",
                Confidence = 0.8, // Default confidence
                Source = "AI",
                CreatedBy = "Phi-3.5 Vision"
            });
        }

        return await Task.FromResult(tags);
    }

    /// <summary>
    /// Get content type from file extension
    /// </summary>
    private string GetContentType(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLower();
        return extension switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".bmp" => "image/bmp",
            ".webp" => "image/webp",
            _ => "application/octet-stream"
        };
    }
}