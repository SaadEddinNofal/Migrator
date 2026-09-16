namespace Migrator.UI.Forms;

using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Extensions.Logging;
using Migrator.Application.Interfaces;
using Migrator.Application.Services;
using Migrator.Domain.Interfaces;
using Migrator.Infrastructure.Configuration;
using Migrator.UI.Theme;

public sealed class MainForm : Form
{
    private readonly IConnectionService _connectionService;
    private readonly IMigrationHistoryService _historyService;
    private readonly IMigrationDiscoveryService _discoveryService;
    private readonly IMigrationValidationService _validationService;
    private readonly IMigrationWorkflowService _workflowService;
    private readonly MigrationExecutorService _executorService;
    private readonly UserSettingsService _settingsService;
    private readonly ThemeManager _themeManager;
    private readonly ILogger<MainForm> _logger;
    private readonly IBackupService _backupService;

    private readonly Panel _sidebarPanel = new();
    private readonly Panel _topBar = new();
    private readonly Panel _contentPanel = new();
    private readonly Panel _statusBar = new();
    private readonly Panel _connectionIndicator = new();

    private readonly Label _titleLabel = new();
    private readonly Label _connectionLabel = new();
    private readonly Label _connectionDot = new();
    private readonly Label _statusProvider = new();
    private readonly Label _statusDatabase = new();
    private readonly Label _statusEnvironment = new();
    private readonly Label _statusVersion = new();

    private readonly Dictionary<string, UserControl> _pages = new();
    private readonly Dictionary<string, Panel> _navItems = new();
    private string _activePage = "Dashboard";

    private const int SidebarWidth = 220;
    private const int TopBarHeight = 50;
    private const int StatusBarHeight = 30;

    private static readonly Font TitleFont = new("Segoe UI", 14f, FontStyle.Bold);
    private static readonly Font NavFont = new("Segoe UI", 9.5f);
    private static readonly Font StatusFont = new("Segoe UI", 8f);
    private static readonly Font LabelFont = new("Segoe UI", 9f);

    private readonly (string Name, string Text, char Icon)[] _navEntries =
    [
        ("Dashboard", "Dashboard", '\u2302'),
        ("Migration", "Migration", '\u2192'),
        ("History", "History", '\u2261'),
        ("Backups", "Backups", '\u229E'),
        ("Logs", "Logs", '\u2263'),
        ("Settings", "Settings", '\u2699'),
        ("About", "About", '\u2139')
    ];

    public MainForm(
        IConnectionService connectionService,
        IMigrationDiscoveryService discoveryService,
        IMigrationValidationService validationService,
        IMigrationWorkflowService workflowService,
        MigrationExecutorService executorService,
        UserSettingsService settingsService,
        ThemeManager themeManager,
        ILogger<MainForm> logger,
        IMigrationHistoryService historyService,
        IBackupService backupService)
    {
        _connectionService = connectionService;
        _historyService = historyService;
        _discoveryService = discoveryService;
        _validationService = validationService;
        _workflowService = workflowService;
        _executorService = executorService;
        _settingsService = settingsService;
        _themeManager = themeManager;
        _logger = logger;
        _backupService = backupService;

        InitializeComponent();
        BuildPages();
        ApplyTheme(_themeManager.CurrentTheme.ToString());
        NavigateTo("Dashboard");

        _logger.LogInformation(
            "Migrator v1.0.0 started on machine '{MachineName}' by user '{UserName}'.",
            Environment.MachineName,
            Environment.UserName);
    }

    private void InitializeComponent()
    {
        SuspendLayout();
        DoubleBuffered = true;

        Text = "Migrator";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(1024, 600);
        Size = new Size(_settingsService.Current.WindowWidth, _settingsService.Current.WindowHeight);
        FormBorderStyle = FormBorderStyle.Sizable;
        Font = LabelFont;

        _topBar.Dock = DockStyle.Top;
        _topBar.Height = TopBarHeight;
        _topBar.Padding = new Padding(12, 0, 12, 0);

        _titleLabel.Text = "Migrator";
        _titleLabel.Font = TitleFont;
        _titleLabel.AutoSize = true;
        _titleLabel.Dock = DockStyle.Left;
        _titleLabel.TextAlign = ContentAlignment.MiddleLeft;

        _connectionDot.Text = "\u25CF";
        _connectionDot.Font = new Font("Segoe UI", 10f);
        _connectionDot.AutoSize = true;
        _connectionDot.Dock = DockStyle.Right;
        _connectionDot.TextAlign = ContentAlignment.MiddleLeft;
        _connectionDot.Cursor = Cursors.Hand;

        _connectionLabel.Text = "Disconnected";
        _connectionLabel.Font = LabelFont;
        _connectionLabel.AutoSize = true;
        _connectionLabel.Dock = DockStyle.Right;
        _connectionLabel.TextAlign = ContentAlignment.MiddleLeft;
        _connectionLabel.Padding = new Padding(0, 0, 6, 0);

        _topBar.Controls.Add(_connectionLabel);
        _topBar.Controls.Add(_connectionDot);
        _topBar.Controls.Add(_titleLabel);

        _statusBar.Dock = DockStyle.Bottom;
        _statusBar.Height = StatusBarHeight;
        _statusBar.Padding = new Padding(12, 0, 12, 0);

        _statusVersion.Text = "v1.0.0";
        _statusVersion.Font = StatusFont;
        _statusVersion.AutoSize = true;
        _statusVersion.Dock = DockStyle.Right;
        _statusVersion.TextAlign = ContentAlignment.MiddleLeft;
        _statusVersion.Padding = new Padding(8, 0, 0, 0);

        _statusEnvironment.Text = "Environment: Development";
        _statusEnvironment.Font = StatusFont;
        _statusEnvironment.AutoSize = true;
        _statusEnvironment.Dock = DockStyle.Right;
        _statusEnvironment.TextAlign = ContentAlignment.MiddleLeft;
        _statusEnvironment.Padding = new Padding(8, 0, 0, 0);

        _statusDatabase.Text = "Database: (none)";
        _statusDatabase.Font = StatusFont;
        _statusDatabase.AutoSize = true;
        _statusDatabase.Dock = DockStyle.Right;
        _statusDatabase.TextAlign = ContentAlignment.MiddleLeft;
        _statusDatabase.Padding = new Padding(8, 0, 0, 0);

        _statusProvider.Text = "SQL Server";
        _statusProvider.Font = StatusFont;
        _statusProvider.AutoSize = true;
        _statusProvider.Dock = DockStyle.Left;
        _statusProvider.TextAlign = ContentAlignment.MiddleLeft;

        _statusBar.Controls.Add(_statusVersion);
        _statusBar.Controls.Add(_statusEnvironment);
        _statusBar.Controls.Add(_statusDatabase);
        _statusBar.Controls.Add(_statusProvider);

        _sidebarPanel.Dock = DockStyle.Left;
        _sidebarPanel.Width = SidebarWidth;
        _sidebarPanel.Padding = new Padding(0);

        BuildSidebar();

        _contentPanel.Dock = DockStyle.Fill;
        _contentPanel.Padding = new Padding(0);

        Controls.Add(_contentPanel);
        Controls.Add(_sidebarPanel);
        Controls.Add(_topBar);
        Controls.Add(_statusBar);

        _themeManager.ThemeChanged += OnThemeChanged;
        FormClosing += OnFormClosing;

        ResumeLayout(false);
    }

    private void BuildSidebar()
    {
        Panel sidebarHeader = new()
        {
            Dock = DockStyle.Top,
            Height = TopBarHeight,
            Padding = new Padding(16, 0, 16, 0)
        };

        Label menuTitle = new()
        {
            Text = "Navigation",
            Font = new Font("Segoe UI", 8f, FontStyle.Bold),
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft
        };
        sidebarHeader.Controls.Add(menuTitle);
        _sidebarPanel.Controls.Add(sidebarHeader);

        Panel navContainer = new()
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(0, 4, 0, 0)
        };

        foreach (var entry in _navEntries)
        {
            Panel navItem = new()
            {
                Dock = DockStyle.Top,
                Height = 40,
                Padding = new Padding(16, 0, 8, 0),
                Cursor = Cursors.Hand,
                Tag = entry.Name
            };

            Label navLabel = new()
            {
                Text = $"{entry.Icon}  {entry.Text}",
                Font = NavFont,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(4, 0, 0, 0),
                Tag = entry.Name
            };

            navItem.Controls.Add(navLabel);
            navItem.Click += NavItem_Click;
            navLabel.Click += NavItem_Click;

            _navItems[entry.Name] = navItem;
            navContainer.Controls.Add(navItem);
        }

        _sidebarPanel.Controls.Add(navContainer);
    }

    private void NavItem_Click(object? sender, EventArgs e)
    {
        Control? control = sender as Control;
        string? pageName = control?.Tag as string;
        if (!string.IsNullOrEmpty(pageName))
        {
            NavigateTo(pageName);
        }
    }

    private void NavigateTo(string pageName)
    {
        if (!_pages.ContainsKey(pageName))
            return;

        _activePage = pageName;

        foreach (var kvp in _navItems)
        {
            Panel item = kvp.Value;
            Label? label = item.Controls.OfType<Label>().FirstOrDefault();
            if (kvp.Key == pageName)
            {
                item.BackColor = _themeManager.AccentPrimary;
                if (label is not null)
                {
                    label.ForeColor = Color.White;
                }
            }
            else
            {
                item.BackColor = _themeManager.SidebarBackground;
                if (label is not null)
                {
                    label.ForeColor = _themeManager.SidebarText;
                }
            }
        }

        foreach (var kvp in _pages)
        {
            kvp.Value.Visible = kvp.Key == pageName;
        }

        if (_pages.TryGetValue(pageName, out UserControl? page))
        {
            page.BringToFront();
        }
    }

    private void BuildPages()
    {
        var dashboard = new DashboardPage(_connectionService, _workflowService, _themeManager)
        {
            ConnectionStringProvider = GetCurrentConnectionString
        };
        var migration = new MigrationPage(
            _connectionService,
            _discoveryService,
            _validationService,
            _workflowService,
            _executorService,
            _settingsService,
            _themeManager);
        var history = new HistoryPage(
            _connectionService,
            _historyService,
            _themeManager,
            _settingsService,
            GetCurrentConnectionString);
        var backups = new BackupsPage(_themeManager, _backupService, GetCurrentConnectionString);
        var logs = new LogsPage(_themeManager);
        var settings = new SettingsPage(_settingsService, _themeManager);
        var about = new AboutPage(_themeManager);

        _pages["Dashboard"] = dashboard;
        _pages["Migration"] = migration;
        _pages["History"] = history;
        _pages["Backups"] = backups;
        _pages["Logs"] = logs;
        _pages["Settings"] = settings;
        _pages["About"] = about;

        foreach (var page in _pages.Values)
        {
            page.Dock = DockStyle.Fill;
            page.Visible = false;
            _contentPanel.Controls.Add(page);
        }
    }

    private string? GetCurrentConnectionString()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(_settingsService.Current.LastConnectionString))
                return null;
            return _settingsService.Current.LastConnectionString;
        }
        catch
        {
            return null;
        }
    }

    public void UpdateConnectionStatus(bool connected)
    {
        if (InvokeRequired)
        {
            BeginInvoke(() => UpdateConnectionStatus(connected));
            return;
        }

        _connectionDot.ForeColor = connected ? _themeManager.AccentSuccess : _themeManager.AccentDanger;
        _connectionLabel.Text = connected ? "Connected" : "Disconnected";
        _connectionLabel.ForeColor = connected ? _themeManager.AccentSuccess : _themeManager.AccentDanger;
    }

    public void UpdateStatusBar(string? server, string? database, string? environment)
    {
        if (InvokeRequired)
        {
            BeginInvoke(() => UpdateStatusBar(server, database, environment));
            return;
        }

        _statusDatabase.Text = string.IsNullOrEmpty(database)
            ? "Database: (none)"
            : $"Database: {database}";
        _statusEnvironment.Text = string.IsNullOrEmpty(environment)
            ? "Environment: Development"
            : $"Environment: {environment}";
    }

    private void ApplyTheme(string themeName)
    {
        _ = themeName;

        BackColor = _themeManager.BackgroundPrimary;
        _topBar.BackColor = _themeManager.BackgroundPrimary;
        _sidebarPanel.BackColor = _themeManager.SidebarBackground;
        _contentPanel.BackColor = _themeManager.BackgroundSecondary;
        _statusBar.BackColor = _themeManager.BackgroundTertiary;

        _titleLabel.ForeColor = _themeManager.TextPrimary;
        _connectionLabel.ForeColor = _themeManager.TextSecondary;
        _statusProvider.ForeColor = _themeManager.TextMuted;
        _statusDatabase.ForeColor = _themeManager.TextMuted;
        _statusEnvironment.ForeColor = _themeManager.TextMuted;
        _statusVersion.ForeColor = _themeManager.TextMuted;

        foreach (var navItem in _navItems.Values)
        {
            navItem.BackColor = _themeManager.SidebarBackground;
            Label? label = navItem.Controls.OfType<Label>().FirstOrDefault();
            if (label is not null)
            {
                label.ForeColor = _themeManager.SidebarText;
            }
        }

        NavigateTo(_activePage);
    }

    private void OnThemeChanged()
    {
        string themeName = _themeManager.CurrentTheme.ToString();
        if (InvokeRequired)
        {
            BeginInvoke(() => ApplyTheme(themeName));
            return;
        }

        ApplyTheme(themeName);
    }

    private void OnFormClosing(object? sender, FormClosingEventArgs e)
    {
        try
        {
            _logger.LogInformation("Migrator is shutting down.");

            _settingsService.Current.WindowWidth = Width;
            _settingsService.Current.WindowHeight = Height;
            _settingsService.Save();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save window settings.");
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
