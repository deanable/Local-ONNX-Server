---
description: Repository Information Overview
alwaysApply: true
---

# Image Tagging Application Information

## Summary
A Windows Forms application for AI-powered image tagging with Digital Asset Management (DAM) integration, built using clean architecture principles. The application uses the Phi-3.5 vision model in ONNX format for image analysis and tagging.

## Structure
- **src/**: Contains the application source code organized in clean architecture layers
  - **Domain/**: Core business entities and interfaces
  - **Application/**: Use cases and application services
  - **Infrastructure/**: External services (AI, DAM API, configuration)
  - **Presentation/**: WinForms UI components
- **tests/**: Contains test projects (currently empty)

## Language & Runtime
**Language**: C#
**Version**: .NET 8.0
**Build System**: MSBuild
**Package Manager**: NuGet

## Dependencies
**Main Dependencies**:
- Microsoft.ML.OnnxRuntimeGenAI (0.4.0): AI model inference
- Microsoft.Extensions.DependencyInjection (8.0.0): DI container
- Microsoft.Extensions.Logging (8.0.0): Logging framework
- Newtonsoft.Json (13.0.3): JSON serialization
- System.Configuration.ConfigurationManager (8.0.0): Configuration management
- Microsoft.Extensions.Http (8.0.0): HTTP client factory

**Development Dependencies**:
- Microsoft.NET.Test.Sdk: Testing framework (referenced in README)
- xunit: Unit testing (referenced in README)
- Moq: Mocking framework (referenced in README)

## Build & Installation
```bash
# Open solution in Visual Studio
dotnet restore
dotnet build
# Run the application
dotnet run --project src/Presentation/ImageTagging.Presentation.csproj
```

## Configuration
**Configuration File**: appsettings.json
**Key Settings**:
- AIModelPath: Path to Phi-3.5 vision model in ONNX format
- DamApiBaseUrl: Base URL for DAM API integration
- DamApiKey: Authentication key for DAM API
- BatchSize: Number of images to process in a batch
- MaxConcurrentProcessing: Maximum concurrent image processing
- DefaultQuestion: Default prompt for AI model
- OutputFormat: Format for AI analysis results (JSON)

## Main Files & Resources
**Entry Point**: src/Presentation/Program.cs
**Main Form**: src/Presentation/MainForm.cs
**Settings Form**: src/Presentation/SettingsForm.cs
**Core Service**: src/Application/ImageProcessingService.cs
**AI Integration**: src/Infrastructure/AIServices/
**DAM Integration**: src/Infrastructure/DamServices/

## Testing
**Framework**: xunit (referenced in README)
**Test Location**: tests/ImageTagging.Tests/
**Run Command**:
```bash
dotnet test
```

## Prerequisites
- Windows 10/11
- Visual Studio 2022 with .NET 8.0 support
- Phi-3.5 Vision Model (ONNX format)
- DAM System with REST API (optional)