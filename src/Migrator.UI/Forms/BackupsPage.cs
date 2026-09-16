namespace Migrator.UI.Forms;

using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using Migrator.Domain.Interfaces;
using Migrator.Domain.Models;
using Migrator.UI.Theme;

public sealed class BackupsPage : UserControl
{
    private readonly ThemeManager _themeManager;
    private readonly IBackupService _backupService;
    private readonly Func<string?> _connectionStringProvider;

    private readonly Label _titleLabel = new();
    private readonly Button _refreshButton = new();
    private readonly Button _openFolderButton = new();
    private readonly Label _emptyLabel = new();
    private readonly DataGridView _grid = new();
    private readonly Label _statusLabel = new();

    private IReadOnlyList<BackupRecord> _records = [];

    private static readonly Font HeadingFont = new("Segoe UI", 14f, FontStyle.Bold);
    private static readonly Font BodyFont = new("Segoe UI", 9f);
    private static readonly Font StatusFont = new("Segoe UI", 8f);

    public BackupsPage(
        ThemeManager themeManager,
        IBackupService backupService,
        Func<string?> connectionStringProvider)
    {
        _themeManager = themeManager;
        _backupService = backupService;
        _connectionStringProvider = connectionStringProvider;

        InitializeComponent();
        ApplyTheme(_themeManager.CurrentTheme.ToString());
        _themeManager.ThemeChanged += OnThemeChanged;

        VisibleChanged += async (_, _) =>
        {
            if (Visible)
            {
                await RefreshBackupsAsync();
            }
        };
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

        _titleLabel.Text = "Backups";
        _titleLabel.Font = HeadingFont;
        _titleLabel.AutoSize = true;
        _titleLabel.Location = new Point(24, 16);

        _refreshButton.Text = "Refresh";
        _refreshButton.Font = BodyFont;
        _refreshButton.FlatStyle = FlatStyle.Flat;
        _refreshButton.Cursor = Cursors.Hand;
        _refreshButton.AutoSize = true;
        _refreshButton.Location = new Point(24, 52);
        _refreshButton.Click += async (_, _) => await RefreshBackupsAsync();

        _openFolderButton.Text = "Open Folder";
        _openFolderButton.Font = BodyFont;
        _openFolderButton.FlatStyle = FlatStyle.Flat;
        _openFolderButton.Cursor = Cursors.Hand;
        _openFolderButton.AutoSize = true;
        _openFolderButton.Location = new Point(102, 52);
        _openFolderButton.Click += OpenBackupFolder;

        _statusLabel.Font = StatusFont;
        _statusLabel.AutoSize = true;
        _statusLabel.Location = new Point(24, 86);

        _grid.Dock = DockStyle.Bottom;
        _grid.Height = 540;
        _grid.ReadOnly = true;
        _grid.AllowUserToAddRows = false;
        _grid.AllowUserToDeleteRows = false;
        _grid.RowHeadersVisible = false;
        _grid.AutoGenerateColumns = true;
        _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _grid.MultiSelect = false;
        _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

        ConfigureGridColumns();

        _emptyLabel.Font = BodyFont;
        _emptyLabel.TextAlign = ContentAlignment.MiddleCenter;
        _emptyLabel.Dock = DockStyle.Fill;
        _emptyLabel.Text = "No backups found. Enable 'Create backup before execution' in the Migration screen and run a migration.";

        Controls.Add(_grid);
        Controls.Add(_emptyLabel);
        Controls.Add(_statusLabel);
        Controls.Add(_openFolderButton);
        Controls.Add(_refreshButton);
        Controls.Add(_titleLabel);

        Resize += (_, _) => { _grid.Height = Math.Max(200, Height - 220); };

        ResumeLayout(false);
    }

    private void ConfigureGridColumns()
    {
        _grid.Columns.Clear();

        _grid.Columns.Add("BackupName", "Backup Name");
        _grid.Columns.Add("DatabaseName", "Database");
        _grid.Columns.Add("CreatedAt", "Created At");
        _grid.Columns.Add("Size", "Size");
        _grid.Columns.Add("Status", "Status");
        _grid.Columns.Add("MigrationId", "Migration");
        _grid.Columns.Add("Location", "Location");

        _grid.Columns["BackupName"]!.FillWeight = 20;
        _grid.Columns["DatabaseName"]!.FillWeight = 14;
        _grid.Columns["CreatedAt"]!.FillWeight = 16;
        _grid.Columns["Size"]!.FillWeight = 10;
        _grid.Columns["Status"]!.FillWeight = 10;
        _grid.Columns["MigrationId"]!.FillWeight = 14;
        _grid.Columns["Location"]!.FillWeight = 30;
    }

    private async Task RefreshBackupsAsync()
    {
        string? connectionString = _connectionStringProvider();
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            _grid.Visible = false;
            _emptyLabel.Visible = true;
            _emptyLabel.Text = "No database connection configured.";
            _statusLabel.Text = "Connected: No";
            return;
        }

        try
        {
            _refreshButton.Enabled = false;
            _records = await _backupService.GetBackupRecordsAsync(connectionString);
            PopulateGrid();

            _statusLabel.Text = $"Connected: Yes | {_records.Count} backup record(s)";
        }
        catch (Exception ex)
        {
            _grid.Visible = false;
            _emptyLabel.Visible = true;
            _emptyLabel.Text = $"Could not load backup records: {ex.Message}";
            _statusLabel.Text = "Connected: Yes | Error loading records";
        }
        finally
        {
            _refreshButton.Enabled = true;
        }
    }

    private void PopulateGrid()
    {
        _grid.Rows.Clear();

        if (_records.Count == 0)
        {
            _grid.Visible = false;
            _emptyLabel.Visible = true;
            _emptyLabel.Text = "No backups found. Enable 'Create backup before execution' in the Migration screen and run a migration.";
            return;
        }

        _emptyLabel.Visible = false;
        _grid.Visible = true;

        foreach (BackupRecord record in _records)
        {
            string size = record.SizeBytes > 0
                ? $"{record.SizeBytes / 1024d / 1024d:F2} MB"
                : record.Status == "Failed" ? "-" : "Pending";

            _grid.Rows.Add(
                record.BackupName,
                record.DatabaseName,
                record.CreatedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss"),
                size,
                record.Status,
                record.MigrationId ?? "-",
                record.Location);
        }
    }

    private void OpenBackupFolder(object? sender, EventArgs e)
    {
        try
        {
            string folder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "Migrator",
                "Backups");

            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }

            Process.Start(new ProcessStartInfo(folder) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Could not open backup folder: {ex.Message}", "Backups",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void ApplyTheme(string themeName)
    {
        BackColor = _themeManager.BackgroundSecondary;
        _titleLabel.ForeColor = _themeManager.TextPrimary;
        _statusLabel.ForeColor = _themeManager.TextMuted;
        _emptyLabel.ForeColor = _themeManager.TextSecondary;

        _refreshButton.BackColor = _themeManager.SurfaceBackground;
        _refreshButton.ForeColor = _themeManager.TextPrimary;
        _refreshButton.FlatAppearance.BorderColor = _themeManager.BorderColor;

        _openFolderButton.BackColor = _themeManager.AccentPrimary;
        _openFolderButton.ForeColor = Color.White;
        _openFolderButton.FlatAppearance.BorderColor = _themeManager.AccentPrimary;

        _grid.BackgroundColor = _themeManager.GridBackground;
        _grid.BorderStyle = BorderStyle.None;
        _grid.ForeColor = _themeManager.TextPrimary;
        _grid.DefaultCellStyle.BackColor = _themeManager.GridBackground;
        _grid.DefaultCellStyle.ForeColor = _themeManager.TextPrimary;
        _grid.DefaultCellStyle.SelectionBackColor = _themeManager.GridSelection;
        _grid.DefaultCellStyle.SelectionForeColor = Color.White;
        _grid.AlternatingRowsDefaultCellStyle.BackColor = _themeManager.GridRowAlternate;
        _grid.ColumnHeadersDefaultCellStyle.BackColor = _themeManager.GridHeaderBackground;
        _grid.ColumnHeadersDefaultCellStyle.ForeColor = _themeManager.TextSecondary;
        _grid.EnableHeadersVisualStyles = false;
        _grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
        _grid.GridColor = _themeManager.BorderColor;
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