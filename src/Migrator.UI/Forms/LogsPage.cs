namespace Migrator.UI.Forms;

using System.Drawing;
using System.Windows.Forms;
using Migrator.UI.Theme;

public sealed class LogsPage : UserControl
{
    private readonly ThemeManager _themeManager;

    private readonly Label _titleLabel = new();
    private readonly RichTextBox _logTextBox = new();
    private readonly Button _refreshButton = new();
    private readonly Button _clearButton = new();
    private readonly Button _exportButton = new();
    private readonly Label _fileLabel = new();

    private static readonly Font HeadingFont = new("Segoe UI", 14f, FontStyle.Bold);
    private static readonly Font BodyFont = new("Segoe UI", 9f);
    private static readonly Font LogFont = new("Consolas", 9.25f);

    private static string LogDirectory => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Migrator",
        "Logs");

    public LogsPage(ThemeManager themeManager)
    {
        _themeManager = themeManager;

        InitializeComponent();
        ApplyTheme(_themeManager.CurrentTheme.ToString());
        _themeManager.ThemeChanged += OnThemeChanged;
    }

    private void OnThemeChanged()
    {
        ApplyTheme(_themeManager.CurrentTheme.ToString());
    }

    private void InitializeComponent()
    {
        SuspendLayout();
        Dock = DockStyle.Fill;
        Padding = new Padding(24);

        _titleLabel.Text = "Logs";
        _titleLabel.Font = HeadingFont;
        _titleLabel.AutoSize = true;
        _titleLabel.Location = new Point(24, 16);

        _fileLabel.Text = "Log folder: " + LogDirectory;
        _fileLabel.Font = BodyFont;
        _fileLabel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        _fileLabel.Location = new Point(24, 50);
        _fileLabel.Size = new Size(Width - 48, 20);
        _fileLabel.TextAlign = ContentAlignment.MiddleLeft;
        _fileLabel.AutoEllipsis = true;

        _refreshButton.Text = "Refresh";
        _refreshButton.Font = BodyFont;
        _refreshButton.FlatStyle = FlatStyle.Flat;
        _refreshButton.Size = new Size(90, 30);
        _refreshButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        _refreshButton.Location = new Point(Width - 300, 46);
        _refreshButton.Click += (_, _) => LoadLatestLog();

        _clearButton.Text = "Clear";
        _clearButton.Font = BodyFont;
        _clearButton.FlatStyle = FlatStyle.Flat;
        _clearButton.Size = new Size(80, 30);
        _clearButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        _clearButton.Location = new Point(Width - 200, 46);
        _clearButton.Click += (_, _) => _logTextBox.Clear();

        _exportButton.Text = "Export";
        _exportButton.Font = BodyFont;
        _exportButton.FlatStyle = FlatStyle.Flat;
        _exportButton.Size = new Size(80, 30);
        _exportButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        _exportButton.Location = new Point(Width - 110, 46);
        _exportButton.Click += (_, _) => ExportLogs();

        _logTextBox.Dock = DockStyle.None;
        _logTextBox.Location = new Point(24, 86);
        _logTextBox.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        _logTextBox.ReadOnly = true;
        _logTextBox.BorderStyle = BorderStyle.None;
        _logTextBox.Font = LogFont;
        _logTextBox.DetectUrls = false;
        _logTextBox.HideSelection = true;
        _logTextBox.WordWrap = false;

        Controls.Add(_titleLabel);
        Controls.Add(_fileLabel);
        Controls.Add(_refreshButton);
        Controls.Add(_clearButton);
        Controls.Add(_exportButton);
        Controls.Add(_logTextBox);

        Resize += (_, _) =>
        {
            _logTextBox.Width = Width - 48;
            _logTextBox.Height = Height - 110;
            _fileLabel.Width = Width - 360;
            _refreshButton.Location = new Point(Width - 300, 46);
            _clearButton.Location = new Point(Width - 200, 46);
            _exportButton.Location = new Point(Width - 110, 46);
        };

        VisibleChanged += (_, _) =>
        {
            if (Visible)
            {
                LoadLatestLog();
            }
        };

        ResumeLayout(false);
    }

    private static string? FindLatestLogFile()
    {
        if (!Directory.Exists(LogDirectory))
            return null;

        try
        {
            return Directory.EnumerateFiles(LogDirectory, "*.log", SearchOption.TopDirectoryOnly)
                .OrderByDescending(f => File.GetLastWriteTimeUtc(f))
                .FirstOrDefault();
        }
        catch
        {
            return null;
        }
    }

    private void LoadLatestLog()
    {
        string? logFile = FindLatestLogFile();
        if (logFile is null)
        {
            _logTextBox.Text = "No log files found. Logs are written to: " + LogDirectory;
            return;
        }

        try
        {
            string content = File.ReadAllText(logFile);
            _logTextBox.Text = content;
            _logTextBox.SelectionStart = _logTextBox.TextLength;
            _logTextBox.ScrollToCaret();
        }
        catch (Exception ex)
        {
            _logTextBox.Text = $"Failed to read log file '{logFile}': {ex.Message}";
        }
    }

    private void ExportLogs()
    {
        using var dialog = new SaveFileDialog
        {
            Title = "Export Logs",
            Filter = "Log Files (*.log)|*.log|Text Files (*.txt)|*.txt|All Files (*.*)|*.*",
            FileName = $"Migrator-{DateTime.Now:yyyyMMdd-HHmmss}.log"
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        try
        {
            File.WriteAllText(dialog.FileName, _logTextBox.Text);
            MessageBox.Show(this, "Logs exported successfully.", "Migrator",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Failed to export logs: {ex.Message}", "Migrator",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ApplyTheme(string themeName)
    {
        BackColor = _themeManager.BackgroundSecondary;
        _titleLabel.ForeColor = _themeManager.TextPrimary;
        _fileLabel.ForeColor = _themeManager.TextMuted;

        _logTextBox.BackColor = _themeManager.GridBackground;
        _logTextBox.ForeColor = _themeManager.TextPrimary;

        foreach (var button in new[] { _refreshButton, _clearButton, _exportButton })
        {
            button.BackColor = _themeManager.AccentPrimary;
            button.ForeColor = Color.White;
            button.FlatAppearance.BorderColor = _themeManager.AccentPrimary;
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _themeManager.ThemeChanged -= OnThemeChanged;
        }
        base.Dispose(disposing);
    }
}