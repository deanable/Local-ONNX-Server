namespace ImageTagging.Domain;

/// <summary>
/// Application settings configuration
/// </summary>
public class AppSettings
{
    /// <summary>
    /// Path to the AI model file (ONNX format)
    /// </summary>
    public string AIModelPath { get; set; } = string.Empty;

    /// <summary>
    /// Base URL for the DAM API
    /// </summary>
    public string DamApiBaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// Username for DAM authentication
    /// </summary>
    public string DamUsername { get; set; } = string.Empty;

    /// <summary>
    /// Password for DAM authentication
    /// </summary>
    public string DamPassword { get; set; } = string.Empty;

    /// <summary>
    /// API key for DAM (legacy - use username/password for Daminion)
    /// </summary>
    public string DamApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Number of images to process in a batch
    /// </summary>
    public int BatchSize { get; set; } = 10;

    /// <summary>
    /// Maximum number of concurrent processing operations
    /// </summary>
    public int MaxConcurrentProcessing { get; set; } = 3;

    /// <summary>
    /// Default question to ask the AI model
    /// </summary>
    public string DefaultQuestion { get; set; } = "What is this image?";

    /// <summary>
    /// Whether to automatically save results to DAM
    /// </summary>
    public bool AutoSaveToDam { get; set; } = false;

    /// <summary>
    /// Output format for AI analysis results
    /// </summary>
    public string OutputFormat { get; set; } = "JSON";

    /// <summary>
    /// Logging level
    /// </summary>
    public string LogLevel { get; set; } = "Information";
}