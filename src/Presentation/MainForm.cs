using ImageTagging.Application;
using ImageTagging.Domain;
using Microsoft.Extensions.Logging;

namespace ImageTagging.Presentation;

public partial class MainForm : Form
{
    private readonly ImageProcessingService _imageProcessingService;
    private readonly IDamIntegrationService _damIntegrationService;
    private readonly IAIModelService _aiModelService;
    private readonly IConfigurationService _configurationService;
    private readonly ILogger<MainForm> _logger;

    private ImageTagging.Domain.Image? _currentImage;
    private CancellationTokenSource? _cancellationTokenSource;

    public MainForm(
        ImageProcessingService imageProcessingService,
        IDamIntegrationService damIntegrationService,
        IAIModelService aiModelService,
        IConfigurationService configurationService,
        ILogger<MainForm> logger)
    {
        _imageProcessingService = imageProcessingService;
        _damIntegrationService = damIntegrationService;
        _aiModelService = aiModelService;
        _configurationService = configurationService;
        _logger = logger;

        InitializeComponent();
        SetupUI();
    }

    private void SetupUI()
    {
        // Form properties
        this.Text = "AI Image Tagging Application";
        this.Size = new Size(1200, 800);
        this.StartPosition = FormStartPosition.CenterScreen;

        // Menu strip
        var menuStrip = new MenuStrip();
        var fileMenu = new ToolStripMenuItem("File");
        var settingsMenu = new ToolStripMenuItem("Settings");
        var helpMenu = new ToolStripMenuItem("Help");

        settingsMenu.Click += SettingsMenu_Click;

        fileMenu.DropDownItems.Add("Exit", null, (s, e) => System.Windows.Forms.Application.Exit());
        settingsMenu.DropDownItems.Add("Preferences", null, SettingsMenu_Click);

        menuStrip.Items.Add(fileMenu);
        menuStrip.Items.Add(settingsMenu);
        menuStrip.Items.Add(helpMenu);

        this.MainMenuStrip = menuStrip;
        this.Controls.Add(menuStrip);

        // Main panel
        var mainPanel = new TableLayoutPanel
        {
            RowCount = 2,
            ColumnCount = 2,
            Dock = DockStyle.Fill,
            Padding = new Padding(10)
        };

        mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 60));
        mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 40));
        mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

        // Image display area
        var imageGroupBox = new GroupBox
        {
            Text = "Image Preview",
            Dock = DockStyle.Fill,
            Padding = new Padding(5)
        };

        var imagePanel = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true
        };

        var pictureBox = new PictureBox
        {
            SizeMode = PictureBoxSizeMode.Zoom,
            Dock = DockStyle.Fill,
            BorderStyle = BorderStyle.FixedSingle
        };

        imagePanel.Controls.Add(pictureBox);
        imageGroupBox.Controls.Add(imagePanel);

        // DAM integration panel
        var damGroupBox = new GroupBox
        {
            Text = "DAM Integration",
            Dock = DockStyle.Fill,
            Padding = new Padding(5)
        };

        var damPanel = new TableLayoutPanel
        {
            RowCount = 4,
            ColumnCount = 1,
            Dock = DockStyle.Fill
        };

        damPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        damPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        damPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        damPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var searchTextBox = new TextBox
        {
            PlaceholderText = "Enter search query...",
            Dock = DockStyle.Fill
        };

        var searchButton = new Button
        {
            Text = "Search DAM",
            Dock = DockStyle.Fill
        };

        var loadFromFileButton = new Button
        {
            Text = "Load from File",
            Dock = DockStyle.Fill
        };

        var damListView = new ListView
        {
            View = View.Details,
            FullRowSelect = true,
            Dock = DockStyle.Fill
        };

        damListView.Columns.Add("File Name", 150);
        damListView.Columns.Add("Asset ID", 100);
        damListView.Columns.Add("Status", 80);

        searchButton.Click += async (s, e) => await SearchDamAsync(searchTextBox.Text);
        loadFromFileButton.Click += LoadFromFileButton_Click;

        damPanel.Controls.Add(searchTextBox, 0, 0);
        damPanel.Controls.Add(searchButton, 0, 1);
        damPanel.Controls.Add(loadFromFileButton, 0, 2);
        damPanel.Controls.Add(damListView, 0, 3);

        damGroupBox.Controls.Add(damPanel);

        // Analysis panel
        var analysisGroupBox = new GroupBox
        {
            Text = "AI Analysis",
            Dock = DockStyle.Fill,
            Padding = new Padding(5)
        };

        var analysisPanel = new TableLayoutPanel
        {
            RowCount = 4,
            ColumnCount = 1,
            Dock = DockStyle.Fill
        };

        analysisPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        analysisPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        analysisPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        analysisPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var analyzeButton = new Button
        {
            Text = "Analyze Image",
            Dock = DockStyle.Fill,
            Enabled = false
        };

        var cancelButton = new Button
        {
            Text = "Cancel",
            Dock = DockStyle.Fill,
            Enabled = false
        };

        var progressBar = new ProgressBar
        {
            Dock = DockStyle.Fill,
            Visible = false
        };

        var descriptionTextBox = new RichTextBox
        {
            Dock = DockStyle.Fill,
            ReadOnly = true
        };

        analyzeButton.Click += AnalyzeButton_Click;
        cancelButton.Click += CancelButton_Click;

        analysisPanel.Controls.Add(analyzeButton, 0, 0);
        analysisPanel.Controls.Add(cancelButton, 0, 1);
        analysisPanel.Controls.Add(progressBar, 0, 2);
        analysisPanel.Controls.Add(descriptionTextBox, 0, 3);

        analysisGroupBox.Controls.Add(analysisPanel);

        // Results panel
        var resultsGroupBox = new GroupBox
        {
            Text = "Tags & Results",
            Dock = DockStyle.Fill,
            Padding = new Padding(5)
        };

        var tagsListView = new ListView
        {
            View = View.Details,
            FullRowSelect = true,
            Dock = DockStyle.Fill
        };

        tagsListView.Columns.Add("Tag", 150);
        tagsListView.Columns.Add("Category", 100);
        tagsListView.Columns.Add("Confidence", 80);
        tagsListView.Columns.Add("Source", 80);

        resultsGroupBox.Controls.Add(tagsListView);

        // Add controls to main panel
        mainPanel.Controls.Add(imageGroupBox, 0, 0);
        mainPanel.Controls.Add(damGroupBox, 1, 0);
        mainPanel.Controls.Add(analysisGroupBox, 0, 1);
        mainPanel.Controls.Add(resultsGroupBox, 1, 1);

        this.Controls.Add(mainPanel);

        // Store references for later use
        this.Tag = new UIComponents
        {
            PictureBox = pictureBox,
            SearchTextBox = searchTextBox,
            DamListView = damListView,
            AnalyzeButton = analyzeButton,
            CancelButton = cancelButton,
            ProgressBar = progressBar,
            DescriptionTextBox = descriptionTextBox,
            TagsListView = tagsListView
        };
    }

    private async void SettingsMenu_Click(object? sender, EventArgs e)
    {
        using var settingsForm = new SettingsForm(_configurationService);
        if (settingsForm.ShowDialog() == DialogResult.OK)
        {
            // Reload settings and potentially reinitialize services
            var settings = await _configurationService.GetSettingsAsync();
            if (!string.IsNullOrEmpty(settings.AIModelPath))
            {
                var modelInitialized = await _aiModelService.InitializeModelAsync(settings.AIModelPath);
                if (!modelInitialized)
                {
                    MessageBox.Show("Failed to reinitialize AI model with new settings.");
                }
            }
        }
    }

    private void LoadFromFileButton_Click(object? sender, EventArgs e)
    {
        using var openFileDialog = new OpenFileDialog
        {
            Filter = "Image files (*.jpg, *.jpeg, *.png, *.gif, *.bmp)|*.jpg;*.jpeg;*.png;*.gif;*.bmp",
            Title = "Select an image to analyze"
        };

        if (openFileDialog.ShowDialog() == DialogResult.OK)
        {
            LoadImageFromPath(openFileDialog.FileName);
        }
    }

    private async Task SearchDamAsync(string query)
    {
        try
        {
            var components = (UIComponents)this.Tag;
            components.DamListView.Items.Clear();

            var images = await _damIntegrationService.GetImagesFromDamAsync(query);

            foreach (var image in images)
            {
                var item = new ListViewItem(image.FileName);
                item.SubItems.Add(image.DamAssetId);
                item.SubItems.Add(image.ProcessingStatus);
                item.Tag = image;
                components.DamListView.Items.Add(item);
            }

            components.DamListView.SelectedIndexChanged += DamListView_SelectedIndexChanged;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching DAM");
            MessageBox.Show($"Error searching DAM: {ex.Message}");
        }
    }

    private void DamListView_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (sender is ListView listView && listView.SelectedItems.Count > 0)
        {
            var selectedItem = listView.SelectedItems[0];
            if (selectedItem.Tag is ImageTagging.Domain.Image image)
            {
                _currentImage = image;
                LoadImageFromDam(image);
                UpdateAnalyzeButton(true);
            }
        }
    }

    private void LoadImageFromPath(string imagePath)
    {
        var components = (UIComponents)this.Tag;

        try
        {
            _currentImage = new ImageTagging.Domain.Image
            {
                FileName = Path.GetFileName(imagePath),
                FilePath = imagePath,
                ContentType = GetContentType(imagePath),
                FileSize = new FileInfo(imagePath).Length
            };

            using (var img = System.Drawing.Image.FromFile(imagePath))
            {
                _currentImage.Width = img.Width;
                _currentImage.Height = img.Height;
            }

            components.PictureBox.Image = System.Drawing.Image.FromFile(imagePath);
            UpdateAnalyzeButton(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading image from path");
            MessageBox.Show($"Error loading image: {ex.Message}");
        }
    }

    private async void LoadImageFromDam(ImageTagging.Domain.Image image)
    {
        var components = (UIComponents)this.Tag;

        try
        {
            // Note: In a real implementation, you might need to download the image
            // from DAM or use a direct URL. This is a simplified example.
            if (!string.IsNullOrEmpty(image.FilePath) && File.Exists(image.FilePath))
            {
                components.PictureBox.Image = System.Drawing.Image.FromFile(image.FilePath);
            }
            else
            {
                // Placeholder for DAM image loading
                components.DescriptionTextBox.Text = $"Selected: {image.FileName} (ID: {image.DamAssetId})";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading image from DAM");
            MessageBox.Show($"Error loading image from DAM: {ex.Message}");
        }
    }

    private async void AnalyzeButton_Click(object? sender, EventArgs e)
    {
        if (_currentImage == null) return;

        var components = (UIComponents)this.Tag;
        _cancellationTokenSource = new CancellationTokenSource();

        try
        {
            UpdateUIForAnalysis(true);
            components.DescriptionTextBox.Clear();
            components.TagsListView.Items.Clear();

            var result = await _imageProcessingService.ProcessImageFromPathAsync(_currentImage.FilePath, _cancellationTokenSource.Token);

            if (result.IsSuccess && result.Image != null)
            {
                components.DescriptionTextBox.Text = result.Image.Description;

                foreach (var tag in result.Image.Tags)
                {
                    var item = new ListViewItem(tag.Name);
                    item.SubItems.Add(tag.Category);
                    item.SubItems.Add(tag.Confidence.ToString("P2"));
                    item.SubItems.Add(tag.Source);
                    components.TagsListView.Items.Add(item);
                }

                _currentImage = result.Image;
                MessageBox.Show("Image analysis completed successfully!");
            }
            else
            {
                MessageBox.Show($"Analysis failed: {result.ErrorMessage}");
            }
        }
        catch (OperationCanceledException)
        {
            components.DescriptionTextBox.Text = "Analysis cancelled.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during image analysis");
            MessageBox.Show($"Error during analysis: {ex.Message}");
        }
        finally
        {
            UpdateUIForAnalysis(false);
        }
    }

    private void CancelButton_Click(object? sender, EventArgs e)
    {
        _cancellationTokenSource?.Cancel();
    }

    private void UpdateAnalyzeButton(bool enabled)
    {
        var components = (UIComponents)this.Tag;
        components.AnalyzeButton.Enabled = enabled;
    }

    private void UpdateUIForAnalysis(bool isAnalyzing)
    {
        var components = (UIComponents)this.Tag;

        components.AnalyzeButton.Enabled = !isAnalyzing;
        components.CancelButton.Enabled = isAnalyzing;
        components.ProgressBar.Visible = isAnalyzing;
        components.ProgressBar.Style = ProgressBarStyle.Marquee;
    }

    private string GetContentType(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLower();
        return extension switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".bmp" => "image/bmp",
            _ => "application/octet-stream"
        };
    }

    private class UIComponents
    {
        public required PictureBox PictureBox { get; set; }
        public required TextBox SearchTextBox { get; set; }
        public required ListView DamListView { get; set; }
        public required Button AnalyzeButton { get; set; }
        public required Button CancelButton { get; set; }
        public required ProgressBar ProgressBar { get; set; }
        public required RichTextBox DescriptionTextBox { get; set; }
        public required ListView TagsListView { get; set; }
    }
}