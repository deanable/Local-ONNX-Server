using ImageTagging.Domain;

namespace ImageTagging.Presentation;

public partial class SettingsForm : Form
{
    private readonly IConfigurationService _configurationService;
    private AppSettings _settings = new();

    public SettingsForm(IConfigurationService configurationService)
    {
        _configurationService = configurationService;
        InitializeComponent();
        SetupUI();
        LoadSettingsAsync();
    }

    private void SetupUI()
    {
        this.Text = "Settings";
        this.Size = new Size(500, 400);
        this.StartPosition = FormStartPosition.CenterParent;
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.MinimizeBox = false;

        var mainPanel = new TableLayoutPanel
        {
            RowCount = 7,
            ColumnCount = 2,
            Dock = DockStyle.Fill,
            Padding = new Padding(10)
        };

        mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));

        mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
        mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        // AI Model Path
        var modelPathLabel = new Label
        {
            Text = "AI Model Path:",
            TextAlign = ContentAlignment.MiddleRight,
            Dock = DockStyle.Fill
        };

        var modelPathPanel = new Panel { Dock = DockStyle.Fill };
        var modelPathTextBox = new TextBox { Dock = DockStyle.Left, Width = 300 };
        var browseButton = new Button { Text = "Browse...", Dock = DockStyle.Right };

        browseButton.Click += (s, e) =>
        {
            using var openFileDialog = new OpenFileDialog
            {
                Filter = "ONNX files (*.onnx)|*.onnx|All files (*.*)|*.*",
                Title = "Select AI Model File"
            };

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                modelPathTextBox.Text = openFileDialog.FileName;
            }
        };

        modelPathPanel.Controls.Add(browseButton);
        modelPathPanel.Controls.Add(modelPathTextBox);

        // DAM API Base URL
        var damUrlLabel = new Label
        {
            Text = "DAM API URL:",
            TextAlign = ContentAlignment.MiddleRight,
            Dock = DockStyle.Fill
        };

        var damUrlTextBox = new TextBox
        {
            Dock = DockStyle.Fill,
            PlaceholderText = "http://localhost:8080"
        };

        // DAM API Key
        var damKeyLabel = new Label
        {
            Text = "DAM API Key:",
            TextAlign = ContentAlignment.MiddleRight,
            Dock = DockStyle.Fill
        };

        var damKeyTextBox = new TextBox
        {
            Dock = DockStyle.Fill,
            PasswordChar = '*'
        };

        // Batch Size
        var batchSizeLabel = new Label
        {
            Text = "Batch Size:",
            TextAlign = ContentAlignment.MiddleRight,
            Dock = DockStyle.Fill
        };

        var batchSizeNumeric = new NumericUpDown
        {
            Minimum = 1,
            Maximum = 100,
            Value = 10,
            Dock = DockStyle.Fill
        };

        // Max Concurrent Processing
        var maxConcurrentLabel = new Label
        {
            Text = "Max Concurrent:",
            TextAlign = ContentAlignment.MiddleRight,
            Dock = DockStyle.Fill
        };

        var maxConcurrentNumeric = new NumericUpDown
        {
            Minimum = 1,
            Maximum = 10,
            Value = 3,
            Dock = DockStyle.Fill
        };

        // Buttons
        var buttonPanel = new Panel { Dock = DockStyle.Fill };
        var saveButton = new Button { Text = "Save", Dock = DockStyle.Right };
        var cancelButton = new Button { Text = "Cancel", Dock = DockStyle.Right };

        saveButton.Click += async (s, e) => await SaveSettingsAsync();
        cancelButton.Click += (s, e) => this.DialogResult = DialogResult.Cancel;

        buttonPanel.Controls.Add(cancelButton);
        buttonPanel.Controls.Add(saveButton);

        // Add controls to panel
        mainPanel.Controls.Add(modelPathLabel, 0, 0);
        mainPanel.Controls.Add(modelPathPanel, 1, 0);
        mainPanel.Controls.Add(damUrlLabel, 0, 1);
        mainPanel.Controls.Add(damUrlTextBox, 1, 1);
        mainPanel.Controls.Add(damKeyLabel, 0, 2);
        mainPanel.Controls.Add(damKeyTextBox, 1, 2);
        mainPanel.Controls.Add(batchSizeLabel, 0, 3);
        mainPanel.Controls.Add(batchSizeNumeric, 1, 3);
        mainPanel.Controls.Add(maxConcurrentLabel, 0, 4);
        mainPanel.Controls.Add(maxConcurrentNumeric, 1, 4);
        mainPanel.Controls.Add(buttonPanel, 1, 6);

        this.Controls.Add(mainPanel);

        // Store references
        this.Tag = new SettingsComponents
        {
            ModelPathTextBox = modelPathTextBox,
            DamUrlTextBox = damUrlTextBox,
            DamKeyTextBox = damKeyTextBox,
            BatchSizeNumeric = batchSizeNumeric,
            MaxConcurrentNumeric = maxConcurrentNumeric,
            SaveButton = saveButton,
            CancelButton = cancelButton
        };
    }

    private async void LoadSettingsAsync()
    {
        try
        {
            _settings = await _configurationService.GetSettingsAsync();
            var components = (SettingsComponents)this.Tag;

            components.ModelPathTextBox.Text = _settings.AIModelPath;
            components.DamUrlTextBox.Text = _settings.DamApiBaseUrl;
            components.DamKeyTextBox.Text = _settings.DamApiKey;
            components.BatchSizeNumeric.Value = _settings.BatchSize;
            components.MaxConcurrentNumeric.Value = _settings.MaxConcurrentProcessing;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading settings: {ex.Message}");
        }
    }

    private async Task SaveSettingsAsync()
    {
        try
        {
            var components = (SettingsComponents)this.Tag;

            _settings.AIModelPath = components.ModelPathTextBox.Text;
            _settings.DamApiBaseUrl = components.DamUrlTextBox.Text;
            _settings.DamApiKey = components.DamKeyTextBox.Text;
            _settings.BatchSize = (int)components.BatchSizeNumeric.Value;
            _settings.MaxConcurrentProcessing = (int)components.MaxConcurrentNumeric.Value;

            await _configurationService.SaveSettingsAsync(_settings);

            this.DialogResult = DialogResult.OK;
            MessageBox.Show("Settings saved successfully!");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error saving settings: {ex.Message}");
        }
    }

    private class SettingsComponents
    {
        public required TextBox ModelPathTextBox { get; set; }
        public required TextBox DamUrlTextBox { get; set; }
        public required TextBox DamKeyTextBox { get; set; }
        public required NumericUpDown BatchSizeNumeric { get; set; }
        public required NumericUpDown MaxConcurrentNumeric { get; set; }
        public required Button SaveButton { get; set; }
        public required Button CancelButton { get; set; }
    }
}