namespace Migrator.UI.Forms;

using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using Migrator.Application.DTOs;
using Migrator.Application.Interfaces;
using Migrator.Domain.Entities;
using Migrator.Domain.Enums;
using Migrator.Domain.Models;
using Migrator.UI.Theme;

public sealed class DashboardPage : UserControl
{
    private readonly IMigrationWorkflowService _workflowService;
    private readonly ThemeManager _themeManager;

    private readonly Label _titleLabel = new();
    private readonly Panel _statsPanel = new();
    private readonly Label _dbInfoLabel = new();
    private readonly Label _lastMigrationLabel = new();
    private readonly Label _recentLabel = new();
    private readonly DataGridView _activityGrid = new();
    private readonly Button _refreshButton = new();

    private bool _loaded;

    private static readonly Font HeadingFont = new("Segoe UI", 14f, FontStyle.Bold);
    private static readonly Font StatNumberFont = new("Segoe UI", 20f, FontStyle.Bold);
    private static readonly Font StatLabelFont = new("Segoe UI", 8.5f);
    private static readonly Font BodyFont = new("Segoe UI", 9f);
    private static readonly Font GridHeaderFont = new("Segoe UI", 9f, FontStyle.Bold);
    private static readonly Font SectionFont = new("Segoe UI", 11f, FontStyle.Bold);

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Func<string?>? ConnectionStringProvider { get; set; }

    public DashboardPage(
        IConnectionService connectionService,
        IMigrationWorkflowService workflowService,
        ThemeManager themeManager)
    {
        _workflowService = workflowService;
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

        _titleLabel.Text = "Dashboard";
        _titleLabel.Font = HeadingFont;
        _titleLabel.AutoSize = true;
        _titleLabel.Location = new Point(24, 16);

        _refreshButton.Text = "Refresh";
        _refreshButton.Font = BodyFont;
        _refreshButton.FlatStyle = FlatStyle.Flat;
        _refreshButton.FlatAppearance.BorderSize = 1;
        _refreshButton.Size = new Size(80, 30);
        _refreshButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        _refreshButton.Location = new Point(Width - 104, 16);
        _refreshButton.Click += async (_, _) => await LoadDashboardDataAsync();

        _statsPanel.Location = new Point(24, 60);
        _statsPanel.Size = new Size(Width - 48, 100);
        _statsPanel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

        _dbInfoLabel.Font = BodyFont;
        _dbInfoLabel.AutoSize = true;
        _dbInfoLabel.Location = new Point(24, 176);

        _lastMigrationLabel.Font = BodyFont;
        _lastMigrationLabel.AutoSize = true;
        _lastMigrationLabel.Location = new Point(24, 202);

        _recentLabel.Text = "Recent Activity";
        _recentLabel.Font = SectionFont;
        _recentLabel.AutoSize = true;
        _recentLabel.Location = new Point(24, 240);

        _activityGrid.Location = new Point(24, 272);
        _activityGrid.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
        _activityGrid.ReadOnly = true;
        _activityGrid.AllowUserToAddRows = false;
        _activityGrid.AllowUserToDeleteRows = false;
        _activityGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _activityGrid.MultiSelect = false;
        _activityGrid.RowHeadersVisible = false;
        _activityGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        _activityGrid.ColumnHeadersDefaultCellStyle.Font = GridHeaderFont;
        _activityGrid.ColumnHeadersHeight = 36;
        _activityGrid.RowTemplate.Height = 30;
        _activityGrid.BorderStyle = BorderStyle.None;
        _activityGrid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
        _activityGrid.EnableHeadersVisualStyles = false;

        _activityGrid.Columns.Add("MigrationId", "Migration ID");
        _activityGrid.Columns.Add("Name", "Name");
        _activityGrid.Columns.Add("Type", "Type");
        _activityGrid.Columns.Add("Status", "Status");
        _activityGrid.Columns.Add("ExecutedAt", "Executed At");
        _activityGrid.Columns.Add("Duration", "Duration");

        Controls.Add(_titleLabel);
        Controls.Add(_refreshButton);
        Controls.Add(_statsPanel);
        Controls.Add(_dbInfoLabel);
        Controls.Add(_lastMigrationLabel);
        Controls.Add(_recentLabel);
        Controls.Add(_activityGrid);

        Resize += (_, _) =>
        {
            int w = Width - 48;
            _refreshButton.Location = new Point(Width - 104, 16);
            _statsPanel.Width = w;
            _activityGrid.Width = w;
            _activityGrid.Height = Height - 304;
            LayoutStatCards();
        };

        VisibleChanged += (_, _) =>
        {
            if (Visible && !_loaded)
            {
                _loaded = true;
                _ = LoadDashboardDataAsync();
            }
        };

        ResumeLayout(false);
    }

    private void LayoutStatCards()
    {
        if (_statsPanel.Width < 100)
            return;

        int spacing = 16;
        int cardWidth = (_statsPanel.Width - (3 * spacing)) / 4;
        int x = 0;
        foreach (Control c in _statsPanel.Controls)
        {
            c.Location = new Point(x, 0);
            c.Size = new Size(cardWidth, _statsPanel.Height);
            x += cardWidth + spacing;
        }
    }

    private async Task LoadDashboardDataAsync()
    {
        try
        {
            _refreshButton.Enabled = false;
            _refreshButton.Text = "...";

            string? connectionString = ConnectionStringProvider?.Invoke();
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                ShowDisconnectedState();
                return;
            }

            DashboardData data = await _workflowService.GetDashboardDataAsync(
                connectionString,
                Array.Empty<SqlMigrationInfo>(),
                CancellationToken.None);

            if (!data.IsConnected)
            {
                ShowDisconnectedState();
                return;
            }

            _dbInfoLabel.Text = $"Database: {data.ServerName ?? "unknown"} / {data.DatabaseName ?? "unknown"}";
            _lastMigrationLabel.Text = data.LastMigration is not null
                ? $"Last Migration: {data.LastMigration.MigrationId} - {data.LastMigration.MigrationName} ({data.LastMigration.ExecutedAt:yyyy-MM-dd HH:mm})"
                : "Last Migration: (none)";

            CreateStatCards(data.AppliedCount, data.PendingCount, data.FailedCount, data.ModifiedCount);
            ShowRecentActivity(data.RecentActivity);
        }
        catch (Exception ex)
        {
            ShowErrorState(ex.Message);
        }
        finally
        {
            _refreshButton.Enabled = true;
            _refreshButton.Text = "Refresh";
        }
    }

    private void ShowDisconnectedState()
    {
        CreateStatCards(0, 0, 0, 0);
        _dbInfoLabel.Text = "Database: (not connected)";
        _lastMigrationLabel.Text = "Connect to a database and execute a migration to populate this dashboard.";
        _activityGrid.Rows.Clear();
    }

    private void ShowErrorState(string message)
    {
        CreateStatCards(0, 0, 0, 0);
        _dbInfoLabel.Text = "Database: (error)";
        _lastMigrationLabel.Text = $"Error loading dashboard: {message}";
        _activityGrid.Rows.Clear();
    }

    private void CreateStatCards(int applied, int pending, int failed, int modified)
    {
        _statsPanel.Controls.Clear();

        AddStatCard("Applied", applied, _themeManager.AccentSuccess);
        AddStatCard("Pending", pending, _themeManager.AccentWarning);
        AddStatCard("Failed", failed, _themeManager.AccentDanger);
        AddStatCard("Modified", modified, _themeManager.AccentPrimary);

        LayoutStatCards();
    }

    private void AddStatCard(string label, int value, Color accentColor)
    {
        Panel card = new()
        {
            BackColor = _themeManager.CardBackground,
            Padding = new Padding(16, 12, 16, 12)
        };

        Panel accentBar = new()
        {
            Height = 3,
            Dock = DockStyle.Bottom,
            BackColor = accentColor
        };

        Label nameLabel = new()
        {
            Text = label,
            Font = StatLabelFont,
            ForeColor = _themeManager.TextMuted,
            AutoSize = true,
            Dock = DockStyle.Top,
            TextAlign = ContentAlignment.MiddleLeft
        };

        Label valueLabel = new()
        {
            Text = value.ToString(),
            Font = StatNumberFont,
            ForeColor = accentColor,
            AutoSize = true,
            Dock = DockStyle.Top,
            TextAlign = ContentAlignment.MiddleLeft
        };

        card.Controls.Add(nameLabel);
        card.Controls.Add(valueLabel);
        card.Controls.Add(accentBar);

        _statsPanel.Controls.Add(card);
    }

    private void ShowRecentActivity(IReadOnlyList<MigrationHistoryEntry> activity)
    {
        _activityGrid.Rows.Clear();

        foreach (MigrationHistoryEntry entry in activity)
        {
            int rowIndex = _activityGrid.Rows.Add(
                entry.MigrationId,
                entry.MigrationName,
                entry.MigrationType.ToString(),
                entry.Status.ToString(),
                entry.ExecutedAt.ToString("yyyy-MM-dd HH:mm:ss"),
                $"{entry.ExecutionDurationMs} ms");

            Color statusColor = entry.Status switch
            {
                MigrationStatus.Applied => _themeManager.AccentSuccess,
                MigrationStatus.Failed => _themeManager.AccentDanger,
                MigrationStatus.Pending => _themeManager.AccentWarning,
                MigrationStatus.Modified => _themeManager.AccentPrimary,
                _ => _themeManager.TextMuted
            };

            _activityGrid.Rows[rowIndex].Cells["Status"].Style.ForeColor = statusColor;
        }
    }

    private void ApplyTheme(string themeName)
    {
        BackColor = _themeManager.BackgroundSecondary;
        _titleLabel.ForeColor = _themeManager.TextPrimary;
        _dbInfoLabel.ForeColor = _themeManager.TextSecondary;
        _lastMigrationLabel.ForeColor = _themeManager.TextSecondary;
        _recentLabel.ForeColor = _themeManager.TextPrimary;

        _refreshButton.BackColor = _themeManager.AccentPrimary;
        _refreshButton.ForeColor = Color.White;
        _refreshButton.FlatAppearance.BorderColor = _themeManager.AccentPrimary;

        _activityGrid.BackgroundColor = _themeManager.GridBackground;
        _activityGrid.DefaultCellStyle.BackColor = _themeManager.GridBackground;
        _activityGrid.DefaultCellStyle.ForeColor = _themeManager.TextPrimary;
        _activityGrid.DefaultCellStyle.SelectionBackColor = _themeManager.GridSelection;
        _activityGrid.DefaultCellStyle.SelectionForeColor = _themeManager.TextPrimary;
        _activityGrid.ColumnHeadersDefaultCellStyle.BackColor = _themeManager.GridHeaderBackground;
        _activityGrid.ColumnHeadersDefaultCellStyle.ForeColor = _themeManager.TextPrimary;
        _activityGrid.GridColor = _themeManager.BorderColor;

        foreach (Control c in _statsPanel.Controls)
        {
            if (c is Panel card)
            {
                card.BackColor = _themeManager.CardBackground;
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