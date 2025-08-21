# Image Tagging Application

A Windows Forms application for AI-powered image tagging with DAM integration, built using clean architecture principles.

## 🏗️ Architecture

This solution follows Clean Architecture principles with the following layers:

- **Domain**: Core business entities and interfaces
- **Application**: Use cases and application services
- **Infrastructure**: External services (AI, DAM API, configuration)
- **Presentation**: WinForms UI

## 🚀 Features

- **AI-Powered Image Analysis**: Uses Phi-3.5 vision model for image description and tagging
- **DAM Integration**: Connects to Digital Asset Management systems via REST API
- **Batch Processing**: Process multiple images concurrently
- **Streaming Analysis**: Real-time feedback during AI processing
- **Clean Architecture**: Maintainable and testable codebase
- **WinForms UI**: Familiar Windows desktop interface

## 📋 Prerequisites

- **Windows 10/11**
- **Visual Studio 2022** with .NET 8.0 support
- **Phi-3.5 Vision Model** (ONNX format) running locally
- **DAM System** with REST API (optional)

## 🛠️ Setup

### 1. Clone the Repository

```bash
git clone <repository-url>
cd ImageTaggingSolution
```

### 2. Configure AI Model

1. Download the Phi-3.5 vision model (ONNX format)
2. Place it in a local directory (e.g., `C:\Models\`)
3. Update the model path in application settings

### 3. Configure DAM Integration (Optional)

1. Set up your DAM system API endpoint
2. Configure API base URL and authentication key in settings
3. Ensure DAM API supports the required endpoints

### 4. Build and Run

```bash
# Open ImageTaggingSolution.sln in Visual Studio
# Build the solution
# Run the ImageTagging.Presentation project
```

## ⚙️ Configuration

Application settings are stored in `appsettings.json`:

```json
{
  "AIModelPath": "C:\\Models\\phi-3.5-vision.onnx",
  "DamApiBaseUrl": "http://localhost:8080",
  "DamApiKey": "your-api-key",
  "BatchSize": 10,
  "MaxConcurrentProcessing": 3,
  "DefaultQuestion": "What is this image?",
  "AutoSaveToDam": false,
  "OutputFormat": "JSON",
  "LogLevel": "Information"
}
```

## 🔧 API Integration

### DAM API Requirements

The application expects the following DAM API endpoints:

- `GET /api/assets/search?q={query}&page={page}&size={size}` - Search assets
- `GET /api/assets/{assetId}` - Get single asset
- `PUT /api/assets/{assetId}/tags` - Update asset tags
- `PUT /api/assets/{assetId}/metadata` - Update asset metadata

### AI Model Requirements

- Phi-3.5 vision model in ONNX format
- Model should support multimodal inputs (text + images)
- Compatible with Microsoft.ML.OnnxRuntimeGenAI

## 🖥️ Usage

### Basic Image Analysis

1. **Load Image**: Click "Load from File" or search DAM
2. **Analyze**: Click "Analyze Image" to start AI processing
3. **View Results**: Review generated description and tags
4. **Save to DAM**: Optionally update DAM with AI results

### Batch Processing

1. **Search DAM**: Enter search query to find multiple images
2. **Select Images**: Choose images for batch processing
3. **Process Batch**: Analyze multiple images concurrently
4. **Review Results**: Check processing status and results

## 🧪 Testing

```bash
# Run tests from Visual Studio Test Explorer
# or use dotnet CLI
dotnet test
```

## 📦 Dependencies

### Core Dependencies

- **Microsoft.ML.OnnxRuntimeGenAI**: AI model inference
- **Microsoft.Extensions.DependencyInjection**: DI container
- **Microsoft.Extensions.Logging**: Logging framework
- **System.Windows.Forms**: UI framework

### Development Dependencies

- **Microsoft.NET.Test.Sdk**: Testing framework
- **xunit**: Unit testing
- **Moq**: Mocking framework

## 🏃‍♂️ Running the Application

1. **Start the application** from Visual Studio or built executable
2. **Configure settings** via Settings menu if needed
3. **Load an image** from file system or DAM
4. **Click "Analyze Image"** to start AI processing
5. **View results** in the description and tags panels

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add tests for new functionality
5. Submit a pull request

## 📄 License

This project is licensed under the MIT License - see the LICENSE file for details.

## 🆘 Troubleshooting

### Common Issues

**AI Model Not Loading**
- Verify model path in settings
- Ensure model file is not corrupted
- Check file permissions

**DAM Connection Failed**
- Verify API endpoint URL
- Check API key configuration
- Ensure DAM system is running

**Performance Issues**
- Reduce batch size in settings
- Lower max concurrent processing limit
- Ensure sufficient system memory

### Logs

Application logs are written to the console and can be found in:
- Visual Studio Output window
- Windows Event Viewer (if configured)

## 📞 Support

For support and questions:
- Create an issue on GitHub
- Check the troubleshooting section
- Review the architecture documentation

---

Built with ❤️ using Clean Architecture principles and the latest .NET technologies.