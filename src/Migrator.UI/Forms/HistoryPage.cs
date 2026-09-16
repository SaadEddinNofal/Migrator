namespace Migrator.UI.Forms;

using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using Migrator.Application.Interfaces;
using Migrator.Domain.Entities;
using Migrator.Domain.Enums;
using Migrator.Domain.Interfaces;
using Migrator.Infrastructure.Configuration;
using Migrator.UI.Theme;

public sealed class HistoryPage : UserControl
{
    private readonly IMigrationHistoryService _historyService;
    private readonly ThemeManager _themeManager;
    private readonly Func<string?> _connectionStringProvider;

    private readonly Label _titleLabel = new();
    private readonly TextBox _searchTextBox = new();
    private readonly ComboBox _statusFilter = new();
    private readonly Button _refreshButton = new();
    private readonly DataGridView _grid = new();
    private readonly ContextMenuStrip _contextMenu = new();
    private readonly Label _statusLabel = new();

    private IReadOnlyList<MigrationHistoryEntry> _entries = [];

    private static readonly Font HeadingFont = new("Segoe UI", 14f, FontStyle.Bold);
    private static readonly Font BodyFont = new("Segoe UI", 9f);
    private static readonly Font GridHeaderFont = new("Segoe UI", 9f, FontStyle.Bold);

    public HistoryPage(
        IConnectionService connectionService,
        IMigrationHistoryService historyService,
        ThemeManager themeManager,
        UserSettingsService settingsService,
        Func<string?> connectionStringProvider)
    {
        _historyService = historyService;
        _themeManager = themeManager;
        _connectionStringProvider = connectionStringProvider;

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

        _titleLabel.Text = "Migration History";
        _titleLabel.Font = HeadingFont;
        _titleLabel.AutoSize = true;
        _titleLabel.Location = new Point(24, 16);

        _searchTextBox.PlaceholderText = "Search by migration ID or name...";
        _searchTextBox.Font = BodyFont;
        _searchTextBox.Size = new Size(300, 28);
        _searchTextBox.Location = new Point(24, 56);
        _searchTextBox.TextChanged += (_, _) => ApplyFilter();

        _statusFilter.Font = BodyFont;
        _statusFilter.DropDownStyle = ComboBoxStyle.DropDownList;
        _statusFilter.Items.AddRange(new object[]
        {
            "All",
            nameof(MigrationStatus.Applied),
            nameof(MigrationStatus.Failed),
            nameof(MigrationStatus.Pending),
            nameof(MigrationStatus.Modified),
            nameof(MigrationStatus.Skipped)
        });
        _statusFilter.SelectedIndex = 0;
        _statusFilter.Size = new Size(140, 28);
        _statusFilter.Location = new Point(340, 56);
        _statusFilter.SelectedIndexChanged += (_, _) => ApplyFilter();

        _refreshButton.Text = "Refresh";
        _refreshButton.Font = BodyFont;
        _refreshButton.FlatStyle = FlatStyle.Flat;
        _refreshButton.Size = new Size(90, 28);
        _refreshButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        _refreshButton.Location = new Point(Width - 240, 56);
        _refreshButton.Click += async (_, _) => await LoadHistoryAsync();

        _statusLabel.Text = "";
        _statusLabel.Font = BodyFont;
        _statusLabel.AutoSize = true;
        _statusLabel.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        _statusLabel.Location = new Point(Width - 160, 62);

        _grid.Dock = DockStyle.None;
        _grid.Location = new Point(24, 96);
        _grid.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
        _grid.ReadOnly = true;
        _grid.AllowUserToAddRows = false;
        _grid.AllowUserToDeleteRows = false;
        _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _grid.MultiSelect = false;
        _grid.RowHeadersVisible = false;
        _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        _grid.ColumnHeadersDefaultCellStyle.Font = GridHeaderFont;
        _grid.ColumnHeadersHeight = 36;
        _grid.RowTemplate.Height = 30;
        _grid.BorderStyle = BorderStyle.None;
        _grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
        _grid.EnableHeadersVisualStyles = false;

        _grid.Columns.Add("MigrationId", "Migration ID");
        _grid.Columns.Add("Name", "Name");
        _grid.Columns.Add("Type", "Type");
        _grid.Columns.Add("Status", "Status");
        _grid.Columns.Add("Checksum", "Checksum");
        _grid.Columns.Add("ExecutedAt", "Executed At");
        _grid.Columns.Add("Duration", "Duration");
        _grid.Columns.Add("Machine", "Machine");

        _contextMenu.Items.Add("Copy Migration ID", null, (_, _) => CopyMigrationId());
        _contextMenu.Items.Add("View Details", null, (_, _) => ViewDetails());

        _grid.ContextMenuStrip = _contextMenu;

        Controls.Add(_titleLabel);
        Controls.Add(_searchTextBox);
        Controls.Add(_statusFilter);
        Controls.Add(_refreshButton);
        Controls.Add(_statusLabel);
        Controls.Add(_grid);

        Resize += (_, _) =>
        {
            _grid.Width = Width - 48;
            _grid.Height = Height - 124;
            _refreshButton.Location = new Point(Width - 240, 56);
            _statusLabel.Location = new Point(Width - 170, 62);
        };

        VisibleChanged += (_, _) =>
        {
            if (Visible)
            {
                _ = LoadHistoryAsync();
            }
        };

        ResumeLayout(false);
    }

    private async Task LoadHistoryAsync()
    {
        try
        {
            _refreshButton.Enabled = false;
            _refreshButton.Text = "...";

            string? connectionString = _connectionStringProvider();
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                _entries = [];
                _grid.Rows.Clear();
                _statusLabel.Text = "No connection";
                return;
            }

            _entries = await _historyService.GetAllAsync(connectionString, CancellationToken.None);
            ApplyFilter();
            _statusLabel.Text = $"{_entries.Count} record(s)";
        }
        catch (Exception ex)
        {
            _statusLabel.Text = "Error";
            MessageBox.Show(this, $"Failed to load migration history: {ex.Message}", "Migrator",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            _refreshButton.Enabled = true;
            _refreshButton.Text = "Refresh";
        }
    }

    private void ApplyFilter()
    {
        string search = _searchTextBox.Text.Trim();
        string? selectedStatus = _statusFilter.SelectedItem as string;

        IEnumerable<MigrationHistoryEntry> filtered = _entries;

        if (!string.IsNullOrEmpty(selectedStatus) && selectedStatus != "All" &&
            Enum.TryParse<MigrationStatus>(selectedStatus, out MigrationStatus status))
        {
            filtered = filtered.Where(e => e.Status == status);
        }

        if (!string.IsNullOrEmpty(search))
        {
            filtered = filtered.Where(e =>
                e.MigrationId.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                e.MigrationName.Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        DisplayRows(filtered.ToList());
    }

    private void DisplayRows(IReadOnlyList<MigrationHistoryEntry> entries)
    {
        _grid.SuspendLayout();
        _grid.Rows.Clear();

        foreach (MigrationHistoryEntry entry in entries)
        {
            int rowIndex = _grid.Rows.Add(
                entry.MigrationId,
                entry.MigrationName,
                entry.MigrationType.ToString(),
                entry.Status.ToString(),
                ShortChecksum(entry.Checksum),
                entry.ExecutedAt.ToString("yyyy-MM-dd HH:mm:ss"),
                $"{entry.ExecutionDurationMs} ms",
                entry.MachineName);

            Color statusColor = entry.Status switch
            {
                MigrationStatus.Applied => _themeManager.AccentSuccess,
                MigrationStatus.Failed => _themeManager.AccentDanger,
                MigrationStatus.Pending => _themeManager.AccentWarning,
                MigrationStatus.Modified => _themeManager.AccentPrimary,
                _ => _themeManager.TextMuted
            };

            _grid.Rows[rowIndex].Cells["Status"].Style.ForeColor = statusColor;
            _grid.Rows[rowIndex].Tag = entry;
        }

        _grid.ResumeLayout();
    }

    private static string ShortChecksum(string checksum)
    {
        if (string.IsNullOrWhiteSpace(checksum))
            return "-";
        return checksum.Length <= 12 ? checksum : checksum[..12];
    }

    private void CopyMigrationId()
    {
        if (_grid.CurrentRow?.Tag is not MigrationHistoryEntry entry)
            return;

        try
        {
            Clipboard.SetText(entry.MigrationId);
        }
        catch
        {
            // Clipboard access may fail intermittently; ignore.
        }
    }

    private void ViewDetails()
    {
        if (_grid.CurrentRow?.Tag is not MigrationHistoryEntry entry)
            return;

        string details =
            $"Migration ID:      {entry.MigrationId}{Environment.NewLine}" +
            $"Migration Name:    {entry.MigrationName}{Environment.NewLine}" +
            $"Type:              {entry.MigrationType}{Environment.NewLine}" +
            $"Status:            {entry.Status}{Environment.NewLine}" +
            $"Provider:          {entry.Provider}{Environment.NewLine}" +
            $"Checksum:          {entry.Checksum}{Environment.NewLine}" +
            $"Executed At:       {entry.ExecutedAt:yyyy-MM-dd HH:mm:ss}{Environment.NewLine}" +
            $"Duration:          {entry.ExecutionDurationMs} ms{Environment.NewLine}" +
            $"Applied By:        {entry.AppliedBy}{Environment.NewLine}" +
            $"Machine:           {entry.MachineName}{Environment.NewLine}" +
            $"Database:          {entry.DatabaseName}{Environment.NewLine}" +
            $"Server:            {entry.ServerName}{Environment.NewLine}" +
            (string.IsNullOrEmpty(entry.ErrorMessage)
                ? string.Empty
                : $"Error:             {entry.ErrorMessage}{Environment.NewLine}");

        using var dialog = new Form
        {
            Text = $"Migration Details - {entry.MigrationId}",
            Size = new Size(560, 420),
            StartPosition = FormStartPosition.CenterParent,
            BackColor = _themeManager.BackgroundSecondary,
            Font = BodyFont
        };

        TextBox textBox = new()
        {
            Multiline = true,
            ReadOnly = true,
            Dock = DockStyle.Fill,
            Font = new Font("Consolas", 9.5f),
            BackColor = _themeManager.GridBackground,
            ForeColor = _themeManager.TextPrimary,
            BorderStyle = BorderStyle.None,
            Text = details,
            ScrollBars = ScrollBars.Vertical,
            Padding = new Padding(12)
        };

        dialog.Controls.Add(textBox);
        dialog.ShowDialog(this);
    }

    private void ApplyTheme(string themeName)
    {
        BackColor = _themeManager.BackgroundSecondary;
        _titleLabel.ForeColor = _themeManager.TextPrimary;

        _searchTextBox.BackColor = _themeManager.InputBackground;
        _searchTextBox.ForeColor = _themeManager.TextPrimary;
        _searchTextBox.BorderStyle = BorderStyle.FixedSingle;

        _statusFilter.BackColor = _themeManager.InputBackground;
        _statusFilter.ForeColor = _themeManager.TextPrimary;
        _statusFilter.FlatStyle = FlatStyle.Flat;

        _refreshButton.BackColor = _themeManager.AccentPrimary;
        _refreshButton.ForeColor = Color.White;
        _refreshButton.FlatAppearance.BorderColor = _themeManager.AccentPrimary;

        _statusLabel.ForeColor = _themeManager.TextMuted;

        _grid.BackgroundColor = _themeManager.GridBackground;
        _grid.DefaultCellStyle.BackColor = _themeManager.GridBackground;
        _grid.DefaultCellStyle.ForeColor = _themeManager.TextPrimary;
        _grid.DefaultCellStyle.SelectionBackColor = _themeManager.GridSelection;
        _grid.DefaultCellStyle.SelectionForeColor = _themeManager.TextPrimary;
        _grid.ColumnHeadersDefaultCellStyle.BackColor = _themeManager.GridHeaderBackground;
        _grid.ColumnHeadersDefaultCellStyle.ForeColor = _themeManager.TextPrimary;
        _grid.GridColor = _themeManager.BorderColor;

        foreach (DataGridViewRow row in _grid.Rows)
        {
            if (row.Cells["Status"].Value is string statusText &&
                Enum.TryParse<MigrationStatus>(statusText, out MigrationStatus status))
            {
                row.Cells["Status"].Style.ForeColor = status switch
                {
                    MigrationStatus.Applied => _themeManager.AccentSuccess,
                    MigrationStatus.Failed => _themeManager.AccentDanger,
                    MigrationStatus.Pending => _themeManager.AccentWarning,
                    MigrationStatus.Modified => _themeManager.AccentPrimary,
                    _ => _themeManager.TextMuted
                };
            }
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