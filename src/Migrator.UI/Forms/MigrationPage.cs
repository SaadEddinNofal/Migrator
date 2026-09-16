namespace Migrator.UI.Forms;

using System.Drawing;
using System.Windows.Forms;
using Migrator.Application.DTOs;
using Migrator.Application.Interfaces;
using Migrator.Application.Services;
using Migrator.Domain.Enums;
using Migrator.Domain.Models;
using Migrator.Infrastructure.Configuration;
using Migrator.UI.Theme;

public sealed class MigrationPage : UserControl
{
    private readonly IConnectionService _connectionService;
    private readonly IMigrationDiscoveryService _discoveryService;
    private readonly IMigrationValidationService _validationService;
    private readonly IMigrationWorkflowService _workflowService;
    private readonly MigrationExecutorService _executorService;
    private readonly UserSettingsService _settingsService;
    private readonly ThemeManager _themeManager;

    private readonly Label _titleLabel = new();
    private readonly Label _sourceLabel = new();
    private readonly Label _connectionLabel = new();
    private readonly Label _optionsLabel = new();
    private readonly Panel _sourcePanel = new();
    private readonly Panel _connectionPanel = new();
    private readonly Panel _optionsPanel = new();
    private readonly Panel _actionsPanel = new();
    private readonly Panel _resultsPanel = new();
    private readonly RichTextBox _resultsTextBox = new();
    private readonly Button _selectSqlButton = new();
    private readonly Button _selectDllButton = new();
    private readonly Label _fileLabel = new();
    private readonly Label _typeLabel = new();
    private readonly Label _sizeLabel = new();
    private readonly Label _batchLabel = new();
    private readonly TextBox _serverTextBox = new();
    private readonly TextBox _databaseTextBox = new();
    private readonly TextBox _usernameTextBox = new();
    private readonly TextBox _passwordTextBox = new();
    private readonly ComboBox _authCombo = new();
    private readonly CheckBox _rawCheckBox = new();
    private readonly TextBox _rawConnectionTextBox = new();
    private readonly Label _connectionStatusLabel = new();
    private readonly Label _connectionDot = new();
    private readonly CheckBox _backupCheckBox = new();
    private readonly CheckBox _stopOnFailureCheckBox = new();
    private readonly Label _environmentLabel = new();
    private readonly ComboBox _environmentCombo = new();
    private readonly Button _testConnectionButton = new();
    private readonly Button _validateButton = new();
    private readonly Button _previewButton = new();
    private readonly Button _executeButton = new();
    private readonly Label _fileInfoLabel = new();

    private SqlMigrationInfo? _sqlMigration;
    private FluentMigrationInfo? _fluentMigration;
    private MigrationType _selectedType;
    private string? _currentConnectionString;
    private bool _isValid;
    private CancellationTokenSource _cts = new();

    private static readonly Font HeadingFont = new("Segoe UI", 14f, FontStyle.Bold);
    private static readonly Font SectionFont = new("Segoe UI", 11f, FontStyle.Bold);
    private static readonly Font BodyFont = new("Segoe UI", 9f);
    private static readonly Font LabelFont = new("Segoe UI", 9f);
    private static readonly Font StatusFont = new("Segoe UI", 9f, FontStyle.Bold);
    private static readonly Font FileInfoFont = new("Segoe UI", 8.5f);

    public MigrationPage(
        IConnectionService connectionService,
        IMigrationDiscoveryService discoveryService,
        IMigrationValidationService validationService,
        IMigrationWorkflowService workflowService,
        MigrationExecutorService executorService,
        UserSettingsService settingsService,
        ThemeManager themeManager)
    {
        _connectionService = connectionService;
        _discoveryService = discoveryService;
        _validationService = validationService;
        _workflowService = workflowService;
        _executorService = executorService;
        _settingsService = settingsService;
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
        AutoScroll = true;
        Padding = new Padding(24);

        _titleLabel.Text = "Migration";
        _titleLabel.Font = HeadingFont;
        _titleLabel.AutoSize = true;
        _titleLabel.Location = new Point(24, 16);

        _sourceLabel.Text = "Migration Source";
        _sourceLabel.Font = SectionFont;
        _sourceLabel.AutoSize = true;
        _sourceLabel.Location = new Point(24, 58);

        _sourcePanel.Location = new Point(24, 84);
        _sourcePanel.Size = new Size(Width - 48, 110);
        _sourcePanel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        _sourcePanel.Padding = new Padding(16);

        _selectSqlButton.Text = "Select SQL File";
        _selectSqlButton.Font = BodyFont;
        _selectSqlButton.FlatStyle = FlatStyle.Flat;
        _selectSqlButton.Size = new Size(150, 30);
        _selectSqlButton.Location = new Point(16, 16);
        _selectSqlButton.Click += async (_, _) => await SelectSqlFileAsync();

        _selectDllButton.Text = "Select Migration DLL";
        _selectDllButton.Font = BodyFont;
        _selectDllButton.FlatStyle = FlatStyle.Flat;
        _selectDllButton.Size = new Size(160, 30);
        _selectDllButton.Location = new Point(180, 16);
        _selectDllButton.Click += async (_, _) => await SelectDllFileAsync();

        _fileLabel.Font = FileInfoFont;
        _fileLabel.AutoSize = true;
        _fileLabel.Location = new Point(16, 56);
        _fileLabel.Text = "File: (none selected)";

        _typeLabel.Font = FileInfoFont;
        _typeLabel.AutoSize = true;
        _typeLabel.Location = new Point(16, 72);
        _typeLabel.Text = "Type: -";

        _sizeLabel.Font = FileInfoFont;
        _sizeLabel.AutoSize = true;
        _sizeLabel.Location = new Point(16, 88);
        _sizeLabel.Text = "Size: - | Checksum: -";

        _batchLabel.Font = FileInfoFont;
        _batchLabel.AutoSize = true;
        _batchLabel.Location = new Point(16, 104);
        _batchLabel.Text = "Batches: -";

        _sourcePanel.Controls.Add(_selectSqlButton);
        _sourcePanel.Controls.Add(_selectDllButton);
        _sourcePanel.Controls.Add(_fileLabel);
        _sourcePanel.Controls.Add(_typeLabel);
        _sourcePanel.Controls.Add(_sizeLabel);
        _sourcePanel.Controls.Add(_batchLabel);

        _connectionLabel.Text = "Database Connection";
        _connectionLabel.Font = SectionFont;
        _connectionLabel.AutoSize = true;
        _connectionLabel.Location = new Point(24, 210);

        _connectionPanel.Location = new Point(24, 236);
        _connectionPanel.Size = new Size(Width - 48, 190);
        _connectionPanel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        _connectionPanel.Padding = new Padding(16);

        _serverTextBox.PlaceholderText = "Server";
        _serverTextBox.Font = BodyFont;
        _serverTextBox.Size = new Size(300, 26);
        _serverTextBox.Location = new Point(16, 16);

        _databaseTextBox.PlaceholderText = "Database";
        _databaseTextBox.Font = BodyFont;
        _databaseTextBox.Size = new Size(200, 26);
        _databaseTextBox.Location = new Point(330, 16);

        _authCombo.Font = BodyFont;
        _authCombo.DropDownStyle = ComboBoxStyle.DropDownList;
        _authCombo.Items.AddRange(["Windows", "SQL Server", "Azure Active Directory"]);
        _authCombo.SelectedIndex = 0;
        _authCombo.Size = new Size(160, 26);
        _authCombo.Location = new Point(544, 16);
        _authCombo.SelectedIndexChanged += (_, _) => UpdateAuthFieldsVisibility();

        _usernameTextBox.PlaceholderText = "Username";
        _usernameTextBox.Font = BodyFont;
        _usernameTextBox.Size = new Size(240, 26);
        _usernameTextBox.Location = new Point(16, 52);

        _passwordTextBox.PlaceholderText = "Password";
        _passwordTextBox.Font = BodyFont;
        _passwordTextBox.UseSystemPasswordChar = true;
        _passwordTextBox.Size = new Size(240, 26);
        _passwordTextBox.Location = new Point(270, 52);

        _testConnectionButton.Text = "Test Connection";
        _testConnectionButton.Font = BodyFont;
        _testConnectionButton.FlatStyle = FlatStyle.Flat;
        _testConnectionButton.Size = new Size(130, 30);
        _testConnectionButton.Location = new Point(544, 52);
        _testConnectionButton.Click += async (_, _) => await TestConnectionAsync();

        _connectionDot.Text = "\u25CF";
        _connectionDot.Font = StatusFont;
        _connectionDot.AutoSize = true;
        _connectionDot.Location = new Point(690, 58);
        _connectionDot.TextAlign = ContentAlignment.MiddleLeft;

        _connectionStatusLabel.Font = FileInfoFont;
        _connectionStatusLabel.AutoSize = true;
        _connectionStatusLabel.Location = new Point(706, 60);
        _connectionStatusLabel.Text = "Not tested";

        _rawCheckBox.Text = "Use raw connection string";
        _rawCheckBox.Font = BodyFont;
        _rawCheckBox.AutoSize = true;
        _rawCheckBox.Location = new Point(16, 92);
        _rawCheckBox.CheckedChanged += (_, _) =>
        {
            _rawConnectionTextBox.Enabled = _rawCheckBox.Checked;
            _serverTextBox.Enabled = !_rawCheckBox.Checked;
            _databaseTextBox.Enabled = !_rawCheckBox.Checked;
            _authCombo.Enabled = !_rawCheckBox.Checked;
            _usernameTextBox.Enabled = !_rawCheckBox.Checked;
            _passwordTextBox.Enabled = !_rawCheckBox.Checked;
        };

        _rawConnectionTextBox.Font = BodyFont;
        _rawConnectionTextBox.Size = new Size(Width - 300, 26);
        _rawConnectionTextBox.Location = new Point(16, 120);
        _rawConnectionTextBox.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        _rawConnectionTextBox.Enabled = false;

        _connectionPanel.Controls.Add(_serverTextBox);
        _connectionPanel.Controls.Add(_databaseTextBox);
        _connectionPanel.Controls.Add(_authCombo);
        _connectionPanel.Controls.Add(_usernameTextBox);
        _connectionPanel.Controls.Add(_passwordTextBox);
        _connectionPanel.Controls.Add(_testConnectionButton);
        _connectionPanel.Controls.Add(_connectionDot);
        _connectionPanel.Controls.Add(_connectionStatusLabel);
        _connectionPanel.Controls.Add(_rawCheckBox);
        _connectionPanel.Controls.Add(_rawConnectionTextBox);

        _optionsLabel.Text = "Options";
        _optionsLabel.Font = SectionFont;
        _optionsLabel.AutoSize = true;
        _optionsLabel.Location = new Point(24, 446);

        _optionsPanel.Location = new Point(24, 472);
        _optionsPanel.Size = new Size(Width - 48, 84);
        _optionsPanel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        _optionsPanel.Padding = new Padding(16);

        _backupCheckBox.Text = "Create backup before execution";
        _backupCheckBox.Font = BodyFont;
        _backupCheckBox.AutoSize = true;
        _backupCheckBox.Location = new Point(16, 22);
        _backupCheckBox.Checked = _settingsService.Current.CreateBackupByDefault;

        _stopOnFailureCheckBox.Text = "Stop on first failure";
        _stopOnFailureCheckBox.Font = BodyFont;
        _stopOnFailureCheckBox.AutoSize = true;
        _stopOnFailureCheckBox.Location = new Point(16, 48);
        _stopOnFailureCheckBox.Checked = _settingsService.Current.StopOnFirstFailure;

        _environmentLabel.Text = "Environment:";
        _environmentLabel.Font = BodyFont;
        _environmentLabel.AutoSize = true;
        _environmentLabel.Location = new Point(430, 24);

        _environmentCombo.Font = BodyFont;
        _environmentCombo.DropDownStyle = ComboBoxStyle.DropDownList;
        _environmentCombo.Items.AddRange(["Development", "Staging", "Production"]);
        _environmentCombo.Location = new Point(540, 22);
        _environmentCombo.Size = new Size(130, 26);
        _environmentCombo.SelectedItem = NormalizeEnvironment(_settingsService.Current.DefaultEnvironment);

        _optionsPanel.Controls.Add(_backupCheckBox);
        _optionsPanel.Controls.Add(_stopOnFailureCheckBox);
        _optionsPanel.Controls.Add(_environmentLabel);
        _optionsPanel.Controls.Add(_environmentCombo);

        _actionsPanel.Location = new Point(24, 572);
        _actionsPanel.Size = new Size(Width - 48, 54);
        _actionsPanel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        _actionsPanel.Padding = new Padding(16, 8, 16, 8);

        _validateButton.Text = "Validate";
        _validateButton.Font = BodyFont;
        _validateButton.FlatStyle = FlatStyle.Flat;
        _validateButton.Size = new Size(110, 34);
        _validateButton.Location = new Point(16, 8);
        _validateButton.Click += async (_, _) => await ValidateAsync();

        _previewButton.Text = "Preview";
        _previewButton.Font = BodyFont;
        _previewButton.FlatStyle = FlatStyle.Flat;
        _previewButton.Size = new Size(110, 34);
        _previewButton.Location = new Point(140, 8);
        _previewButton.Click += async (_, _) => await PreviewAsync();

        _executeButton.Text = "Execute Migration";
        _executeButton.Font = BodyFont;
        _executeButton.FlatStyle = FlatStyle.Flat;
        _executeButton.Size = new Size(170, 34);
        _executeButton.Location = new Point(Width - 220, 8);
        _executeButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        _executeButton.Enabled = false;
        _executeButton.Click += async (_, _) => await ExecuteAsync();

        _actionsPanel.Controls.Add(_validateButton);
        _actionsPanel.Controls.Add(_previewButton);
        _actionsPanel.Controls.Add(_executeButton);

        _resultsPanel.Location = new Point(24, 636);
        _resultsPanel.Size = new Size(Width - 48, Height - 660);
        _resultsPanel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
        _resultsPanel.Padding = new Padding(0);

        _resultsTextBox.Dock = DockStyle.Fill;
        _resultsTextBox.ReadOnly = true;
        _resultsTextBox.Dock = DockStyle.Fill;
        _resultsTextBox.ScrollBars = RichTextBoxScrollBars.Vertical;
        _resultsTextBox.BorderStyle = BorderStyle.None;
        _resultsTextBox.Font = new Font("Consolas", 9.25f);
        _resultsTextBox.BackColor = Color.FromArgb(18, 18, 18);
        _resultsTextBox.ForeColor = Color.FromArgb(230, 230, 230);

        _resultsPanel.Controls.Add(_resultsTextBox);

        Controls.Add(_titleLabel);
        Controls.Add(_sourceLabel);
        Controls.Add(_sourcePanel);
        Controls.Add(_connectionLabel);
        Controls.Add(_connectionPanel);
        Controls.Add(_optionsLabel);
        Controls.Add(_optionsPanel);
        Controls.Add(_actionsPanel);
        Controls.Add(_resultsPanel);

        Resize += (_, _) =>
        {
            int w = Width - 48;
            _sourcePanel.Width = w;
            _connectionPanel.Width = w;
            _optionsPanel.Width = w;
            _actionsPanel.Width = w;
            _resultsPanel.Width = w;
            _resultsPanel.Height = Height - 664;
            _executeButton.Location = new Point(w - 186, 8);
            _rawConnectionTextBox.Width = w - 40;
        };

        ResumeLayout(false);
    }

    private static string NormalizeEnvironment(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return "Development";

        return name.Trim() switch
        {
            "Development" => "Development",
            "Staging" => "Staging",
            "Production" => "Production",
            _ => "Development"
        };
    }

    private void UpdateAuthFieldsVisibility()
    {
        bool showCredentials = _authCombo.SelectedIndex == 1;
        _usernameTextBox.Visible = showCredentials;
        _passwordTextBox.Visible = showCredentials;
    }

    private string FormatFileSize(long bytes)
    {
        return bytes switch
        {
            >= 1024 * 1024 => $"{bytes / (1024.0 * 1024.0):0.00} MB",
            >= 1024 => $"{bytes / 1024.0:0.0} KB",
            _ => $"{bytes} B"
        };
    }

    private async Task SelectSqlFileAsync()
    {
        using var dialog = new OpenFileDialog
        {
            Title = "Select SQL Migration File",
            Filter = "SQL Files (*.sql)|*.sql|All Files (*.*)|*.*",
            CheckFileExists = true,
            Multiselect = false,
            InitialDirectory = _settingsService.Current.LastMigrationDirectory ?? Environment.CurrentDirectory
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        try
        {
            ShowMessage($"Loading SQL migration from '{dialog.FileName}'...");
            SetBusy(true);

            IReadOnlyList<SqlMigrationInfo> migrations =
                await _discoveryService.DiscoverSqlMigrationsAsync(dialog.FileName, _cts.Token);

            if (migrations.Count == 0)
            {
                ShowMessage("No SQL migrations found in the selected file.");
                return;
            }

            _sqlMigration = migrations[0];
            _fluentMigration = null;
            _selectedType = MigrationType.SqlFile;
            _isValid = false;
            _executeButton.Enabled = false;

            _settingsService.Current.LastMigrationDirectory = Path.GetDirectoryName(dialog.FileName);
            _settingsService.Save();

            _fileLabel.Text = $"File: {_sqlMigration.FileName}";
            _typeLabel.Text = "Type: SQL Migration";
            _sizeLabel.Text = $"Size: {FormatFileSize(_sqlMigration.FileSizeBytes)} | Checksum: {ShortChecksum(_sqlMigration.Checksum)}";
            _batchLabel.Text = $"Batches: {_sqlMigration.BatchCount}";

            ShowMessage($"Loaded SQL migration '{_sqlMigration.MigrationId}_{_sqlMigration.MigrationName}'.");
            ShowMessage($"File size: {FormatFileSize(_sqlMigration.FileSizeBytes)}");
            ShowMessage($"Checksum: {_sqlMigration.Checksum}");
            ShowMessage($"Batches: {_sqlMigration.BatchCount}");
            ShowMessage("Select a database connection and click 'Validate' to continue.");
        }
        catch (Exception ex)
        {
            ShowError($"Error loading SQL migration: {ex.Message}");
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async Task SelectDllFileAsync()
    {
        using var dialog = new OpenFileDialog
        {
            Title = "Select FluentMigrator Migration Assembly",
            Filter = "Assembly Files (*.dll)|*.dll|All Files (*.*)|*.*",
            CheckFileExists = true,
            Multiselect = false,
            InitialDirectory = _settingsService.Current.LastMigrationDirectory ?? Environment.CurrentDirectory
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        try
        {
            ShowMessage($"Discovering FluentMigrator migrations in '{Path.GetFileName(dialog.FileName)}'...");
            SetBusy(true);

            IReadOnlyList<FluentMigrationInfo> migrations =
                await _discoveryService.DiscoverFluentMigrationsAsync(dialog.FileName, _cts.Token);

            if (migrations.Count == 0)
            {
                ShowMessage("No FluentMigrator migrations found in the selected assembly.");
                return;
            }

            _fluentMigration = migrations[0];
            _sqlMigration = null;
            _selectedType = MigrationType.FluentMigrator;
            _isValid = false;
            _executeButton.Enabled = false;

            _settingsService.Current.LastMigrationDirectory = Path.GetDirectoryName(dialog.FileName);
            _settingsService.Save();

            _fileLabel.Text = $"File: {Path.GetFileName(dialog.FileName)}";
            _typeLabel.Text = "Type: FluentMigrator Assembly";
            _sizeLabel.Text = $"Assembly: {_fluentMigration.AssemblyName}";
            _batchLabel.Text = $"Migration: v{_fluentMigration.Version} {_fluentMigration.MigrationName}";

            ShowMessage($"Discovered {migrations.Count} FluentMigrator migration(s) in '{_fluentMigration.AssemblyName}'.");
            ShowMessage($"First migration: v{_fluentMigration.Version} - {_fluentMigration.MigrationName} ({_fluentMigration.Namespace})");
            ShowMessage("Select a database connection and click 'Validate' to continue.");
        }
        catch (Exception ex)
        {
            ShowError($"Error loading FluentMigrator assembly: {ex.Message}");
        }
        finally
        {
            SetBusy(false);
        }
    }

    private static string ShortChecksum(string checksum)
    {
        return string.IsNullOrWhiteSpace(checksum)
            ? "-"
            : (checksum.Length <= 8 ? checksum : checksum[..8]);
    }

    private ConnectionConfigurationDto BuildConnectionConfig()
    {
        if (_rawCheckBox.Checked)
        {
            return new ConnectionConfigurationDto
            {
                UseRawConnectionString = true,
                RawConnectionString = _rawConnectionTextBox.Text,
                Provider = DatabaseProviderType.SqlServer
            };
        }

        return new ConnectionConfigurationDto
        {
            Server = _serverTextBox.Text.Trim(),
            Database = _databaseTextBox.Text.Trim(),
            AuthenticationType = _authCombo.SelectedIndex switch
            {
                1 => AuthenticationType.SqlServer,
                2 => AuthenticationType.AzureActiveDirectory,
                _ => AuthenticationType.Windows
            },
            Username = _usernameTextBox.Text.Trim(),
            Password = _passwordTextBox.Text,
            UseRawConnectionString = false,
            Provider = DatabaseProviderType.SqlServer
        };
    }

    private async Task TestConnectionAsync()
    {
        try
        {
            ConnectionConfigurationDto config = BuildConnectionConfig();

            _testConnectionButton.Enabled = false;
            _testConnectionButton.Text = "...";
            _connectionStatusLabel.Text = "Testing...";
            _connectionDot.ForeColor = _themeManager.AccentWarning;

            _currentConnectionString = _connectionService.BuildConnectionString(config);

            ConnectionTestResult result = await _connectionService.TestConnectionAsync(config, _cts.Token);

            if (result.IsSuccess)
            {
                _connectionDot.ForeColor = _themeManager.AccentSuccess;
                _connectionStatusLabel.Text = $"Connected to {result.DatabaseName} ({result.DurationMs} ms)";
                _settingsService.Current.LastConnectionString = _currentConnectionString;
                _settingsService.Save();
                ShowMessage($"Connection test succeeded in {result.DurationMs} ms.");
                ShowMessage($"Server: {result.ServerName} | Database: {result.DatabaseName} | Version: {result.ServerVersion}");
            }
            else
            {
                _connectionDot.ForeColor = _themeManager.AccentDanger;
                _connectionStatusLabel.Text = "Connection failed";
                ShowError($"Connection test failed: {result.ErrorMessage ?? "Unknown error"}");
            }
        }
        catch (OperationCanceledException)
        {
            _connectionStatusLabel.Text = "Test cancelled";
        }
        catch (Exception ex)
        {
            _connectionDot.ForeColor = _themeManager.AccentDanger;
            _connectionStatusLabel.Text = "Connection failed";
            ShowError($"Connection test failed: {ex.Message}");
        }
        finally
        {
            _testConnectionButton.Enabled = true;
            _testConnectionButton.Text = "Test Connection";
        }
    }

    private string? TryGetConnectionString()
    {
        if (!string.IsNullOrWhiteSpace(_currentConnectionString))
            return _currentConnectionString;

        ConnectionConfigurationDto config = BuildConnectionConfig();
        try
        {
            return _connectionService.BuildConnectionString(config);
        }
        catch
        {
            return null;
        }
    }

    private async Task ValidateAsync()
    {
        if (!HasMigrationSelected())
        {
            ShowError("Select a migration file first.");
            return;
        }

        string? connectionString = TryGetConnectionString();
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            ShowError("Provide a database connection first.");
            return;
        }

        _currentConnectionString = connectionString;

        try
        {
            SetBusy(true);
            _validateButton.Text = "Validating...";
            ShowMessage("Validating migration...");

            MigrationValidationResult? result = _selectedType switch
            {
                MigrationType.SqlFile => await _validationService.ValidateAsync(connectionString, _sqlMigration!, _cts.Token),
                _ => await _validationService.ValidateAsync(connectionString, _fluentMigration!, _cts.Token)
            };

            if (result is null)
            {
                ShowError("Validation returned no result.");
                return;
            }

            if (result.IsValid)
            {
                _isValid = true;
                _executeButton.Enabled = true;
                ShowMessage("VALIDATION PASSED");
                ShowMessage("The migration is safe to execute against this database.");

                foreach (string warning in result.Warnings)
                {
                    ShowWarning($"Warning: {warning}");
                }
            }
            else
            {
                _isValid = false;
                _executeButton.Enabled = false;
                ShowError($"VALIDATION FAILED: {result.ErrorMessage ?? "The migration is not valid for execution."}");

                if (result.IsChecksumMismatch)
                {
                    ShowError($"Checksum mismatch detected.");
                    ShowError($"Stored checksum:   {result.StoredChecksum ?? "-"}");
                    ShowError($"Current checksum:  {result.CurrentChecksum ?? "-"}");
                }

                foreach (string warning in result.Warnings)
                {
                    ShowWarning($"Warning: {warning}");
                }
            }
        }
        catch (OperationCanceledException)
        {
            ShowMessage("Validation cancelled.");
        }
        catch (Exception ex)
        {
            ShowError($"Validation error: {ex.Message}");
        }
        finally
        {
            SetBusy(false);
            _validateButton.Text = "Validate";
        }
    }

    private async Task PreviewAsync()
    {
        if (!HasMigrationSelected())
        {
            ShowError("Select a migration file first.");
            return;
        }

        string? connectionString = TryGetConnectionString();
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            ShowError("Provide a database connection first.");
            return;
        }

        _currentConnectionString = connectionString;

        try
        {
            SetBusy(true);
            _previewButton.Text = "Loading...";
            ShowMessage("Loading preview information...");

            MigrationPreviewInfo? preview = _selectedType switch
            {
                MigrationType.SqlFile => await _workflowService.GetPreviewInfoAsync(_sqlMigration!, connectionString, _cts.Token),
                _ => await _workflowService.GetPreviewInfoAsync(_fluentMigration!, connectionString, _cts.Token)
            };

            if (preview is null)
            {
                ShowError("Preview returned no result.");
                return;
            }

            ShowMessage("---------------- PREVIEW ----------------");
            ShowMessage($"Migration ID:      {preview.MigrationId}");
            ShowMessage($"Migration Name:    {preview.MigrationName}");
            ShowMessage($"Migration Type:    {preview.MigrationType}");
            ShowMessage($"Current Status:    {preview.CurrentStatus}");
            ShowMessage($"Would Be Applied:  {preview.WouldBeApplied}");

            if (preview.StoredChecksum is not null)
                ShowMessage($"Stored Checksum:   {preview.StoredChecksum}");
            ShowMessage($"Current Checksum:  {preview.CurrentChecksum}");

            if (preview.IsChecksumMismatch)
                ShowWarning("CHECKSUM MISMATCH: The migration content differs from what was previously applied.");

            if (!string.IsNullOrWhiteSpace(preview.WarningMessage))
                ShowWarning($"Warning: {preview.WarningMessage}");

            ShowMessage("------------------------------------------");
        }
        catch (OperationCanceledException)
        {
            ShowMessage("Preview cancelled.");
        }
        catch (Exception ex)
        {
            ShowError($"Preview error: {ex.Message}");
        }
        finally
        {
            SetBusy(false);
            _previewButton.Text = "Preview";
        }
    }

    private async Task ExecuteAsync()
    {
        if (!_isValid)
        {
            ShowError("Validation must pass before executing.");
            return;
        }

        if (!HasMigrationSelected())
        {
            ShowError("No migration selected.");
            return;
        }

        string? connectionString = _currentConnectionString;
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            ShowError("Establish a database connection first.");
            return;
        }

        EnvironmentLevel environment = _environmentCombo.SelectedIndex switch
        {
            1 => EnvironmentLevel.Staging,
            2 => EnvironmentLevel.Production,
            _ => EnvironmentLevel.Development
        };

        bool createBackup = _backupCheckBox.Checked;
        bool stopOnFailure = _stopOnFailureCheckBox.Checked || environment == EnvironmentLevel.Production;

        string migrationId = _selectedType == MigrationType.SqlFile
            ? _sqlMigration!.MigrationId
            : _fluentMigration!.Version.ToString();
        string migrationName = _selectedType == MigrationType.SqlFile
            ? _sqlMigration!.MigrationName
            : _fluentMigration!.MigrationName;

        using var dialog = new ConfirmationDialog(
            _themeManager,
            migrationId,
            migrationName,
            _selectedType,
            connectionString,
            createBackup,
            environment);

        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            ShowMessage("Execution cancelled by user.");
            return;
        }

        var request = new MigrationExecutionRequest
        {
            ConnectionString = connectionString,
            CreateBackup = createBackup,
            StopOnFirstFailure = stopOnFailure,
            Environment = environment
        };

        try
        {
            SetBusy(true);
            _executeButton.Text = "Executing...";
            ShowMessage("Executing migration...");

            MigrationExecutionResult? result = _selectedType switch
            {
                MigrationType.SqlFile => await _executorService.ExecuteSqlMigrationAsync(_sqlMigration!, request, _cts.Token),
                _ => await _executorService.ExecuteFluentMigrationAsync(_fluentMigration!, request, _cts.Token)
            };

            if (result is null)
            {
                ShowError("Execution returned no result.");
                return;
            }

            _settingsService.Current.LastConnectionString = connectionString;
            _settingsService.Current.CreateBackupByDefault = createBackup;
            _settingsService.Current.StopOnFirstFailure = stopOnFailure;
            _settingsService.Save();

            if (result.IsSuccess)
            {
                ShowSuccess($"EXECUTION SUCCEEDED in {result.ExecutionDurationMs} ms");
                ShowSuccess($"Migration: {result.MigrationId}_{result.MigrationName}");
                if (result.BackupPath is not null)
                    ShowSuccess($"Backup created: {result.BackupPath}");
                else if (createBackup)
                    ShowMessage("Backup skipped: target SQL Server is not local.");
            }
            else
            {
                ShowError($"EXECUTION FAILED: {result.ErrorMessage ?? "Unknown error"}");
                if (result.BackupPath is not null)
                    ShowWarning($"A backup was created: {result.BackupPath}");
            }
        }
        catch (OperationCanceledException)
        {
            ShowMessage("Execution cancelled.");
        }
        catch (Exception ex)
        {
            ShowError($"Execution error: {ex.Message}");
        }
        finally
        {
            SetBusy(false);
            _executeButton.Text = "Execute Migration";
        }
    }

    private bool HasMigrationSelected()
    {
        return _selectedType switch
        {
            MigrationType.SqlFile => _sqlMigration is not null,
            MigrationType.FluentMigrator => _fluentMigration is not null,
            _ => false
        };
    }

    private void SetBusy(bool busy)
    {
        _selectSqlButton.Enabled = !busy;
        _selectDllButton.Enabled = !busy;
        _testConnectionButton.Enabled = !busy;
        if (!_isValid)
        {
            _executeButton.Enabled = false;
        }
    }

    private void ShowMessage(string message)
    {
        AppendResult(message, _themeManager.TextPrimary);
    }

    private void ShowSuccess(string message)
    {
        AppendResult(message, Color.FromArgb(46, 200, 113));
    }

    private void ShowWarning(string message)
    {
        AppendResult(message, Color.FromArgb(240, 165, 65));
    }

    private void ShowError(string message)
    {
        AppendResult(message, Color.FromArgb(230, 80, 80));
    }

    private void AppendResult(string message, Color color)
    {
        _resultsTextBox.SelectionStart = _resultsTextBox.TextLength;
        _resultsTextBox.SelectionLength = 0;
        _resultsTextBox.SelectionColor = color;
        _resultsTextBox.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}");
        _resultsTextBox.SelectionColor = _themeManager.TextPrimary;
        _resultsTextBox.ScrollToCaret();
    }

    private void ApplyTheme(string themeName)
    {
        BackColor = _themeManager.BackgroundSecondary;

        _titleLabel.ForeColor = _themeManager.TextPrimary;
        _sourceLabel.ForeColor = _themeManager.TextPrimary;
        _connectionLabel.ForeColor = _themeManager.TextPrimary;
        _optionsLabel.ForeColor = _themeManager.TextPrimary;

        foreach (var panel in new[] { _sourcePanel, _connectionPanel, _optionsPanel, _actionsPanel, _resultsPanel })
        {
            panel.BackColor = _themeManager.SurfaceBackground;
        }

        _fileLabel.ForeColor = _themeManager.TextSecondary;
        _typeLabel.ForeColor = _themeManager.TextSecondary;
        _sizeLabel.ForeColor = _themeManager.TextSecondary;
        _batchLabel.ForeColor = _themeManager.TextSecondary;
        _connectionStatusLabel.ForeColor = _themeManager.TextSecondary;
        _environmentLabel.ForeColor = _themeManager.TextSecondary;

        foreach (var textBox in new[] { _serverTextBox, _databaseTextBox, _usernameTextBox, _passwordTextBox, _rawConnectionTextBox })
        {
            textBox.BackColor = _themeManager.InputBackground;
            textBox.ForeColor = _themeManager.TextPrimary;
            textBox.BorderStyle = BorderStyle.FixedSingle;
        }

        foreach (var combo in new[] { _authCombo, _environmentCombo })
        {
            combo.BackColor = _themeManager.InputBackground;
            combo.ForeColor = _themeManager.TextPrimary;
            combo.FlatStyle = FlatStyle.Flat;
        }

        _rawCheckBox.ForeColor = _themeManager.TextSecondary;
        _backupCheckBox.ForeColor = _themeManager.TextSecondary;
        _stopOnFailureCheckBox.ForeColor = _themeManager.TextSecondary;

        foreach (var button in new[] { _selectSqlButton, _selectDllButton, _testConnectionButton, _validateButton, _previewButton, _executeButton })
        {
            button.BackColor = _themeManager.AccentPrimary;
            button.ForeColor = Color.White;
            button.FlatAppearance.BorderColor = _themeManager.AccentPrimary;
        }

        _connectionDot.ForeColor = _themeManager.TextMuted;
        _resultsTextBox.BackColor = _themeManager.GridBackground;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _cts.Dispose();
            _themeManager.ThemeChanged -= OnThemeChanged;
        }
        base.Dispose(disposing);
    }
}