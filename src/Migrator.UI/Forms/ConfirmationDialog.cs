namespace Migrator.UI.Forms;

using System.Drawing;
using System.Windows.Forms;
using Migrator.Domain.Enums;
using Migrator.UI.Theme;

public sealed class ConfirmationDialog : Form
{
    private readonly ThemeManager _themeManager;

    private readonly string _migrationId;
    private readonly string _migrationName;
    private readonly MigrationType _migrationType;
    private readonly string _connectionString;
    private readonly bool _createBackup;
    private readonly EnvironmentLevel _environment;

    private readonly Label _titleLabel = new();
    private readonly Panel _detailsPanel = new();
    private readonly TextBox _detailsTextBox = new();
    private readonly Label _confirmLabel = new();
    private readonly TextBox _confirmTextBox = new();
    private readonly Button _executeButton = new();
    private readonly Button _cancelButton = new();

    private static readonly Font TitleFont = new("Segoe UI", 13f, FontStyle.Bold);
    private static readonly Font BodyFont = new("Segoe UI", 9f);
    private static readonly Font DetailsFont = new("Consolas", 9.5f);

    private const string ConfirmText = "EXECUTE";

    private bool IsProduction => _environment == EnvironmentLevel.Production;

    public ConfirmationDialog(
        ThemeManager themeManager,
        string migrationId,
        string migrationName,
        MigrationType migrationType,
        string connectionString,
        bool createBackup,
        EnvironmentLevel environment)
    {
        _themeManager = themeManager;
        _migrationId = migrationId;
        _migrationName = migrationName;
        _migrationType = migrationType;
        _connectionString = connectionString;
        _createBackup = createBackup;
        _environment = environment;

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

        Text = "Confirm Migration Execution";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        ClientSize = new Size(560, 490);
        Font = BodyFont;
        AutoScaleMode = AutoScaleMode.Font;

        _titleLabel.Text = "Confirm Migration Execution";
        _titleLabel.Font = TitleFont;
        _titleLabel.AutoSize = true;
        _titleLabel.Location = new Point(24, 16);

        _detailsPanel.Location = new Point(24, 52);
        _detailsPanel.Size = new Size(512, 230);
        _detailsPanel.Padding = new Padding(12);
        _detailsPanel.TabStop = false;

        _detailsTextBox.Multiline = true;
        _detailsTextBox.ReadOnly = true;
        _detailsTextBox.Dock = DockStyle.Fill;
        _detailsTextBox.BorderStyle = BorderStyle.None;
        _detailsTextBox.Font = DetailsFont;
        _detailsTextBox.ScrollBars = ScrollBars.Vertical;
        _detailsTextBox.Text = BuildDetailsText();

        _detailsPanel.Controls.Add(_detailsTextBox);

        _cancelButton.Text = "Cancel";
        _cancelButton.Font = BodyFont;
        _cancelButton.FlatStyle = FlatStyle.Flat;
        _cancelButton.Size = new Size(110, 34);
        _cancelButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        _cancelButton.Location = new Point(426, 440);
        _cancelButton.DialogResult = DialogResult.Cancel;

        _executeButton.Text = "Execute";
        _executeButton.Font = BodyFont;
        _executeButton.FlatStyle = FlatStyle.Flat;
        _executeButton.Size = new Size(110, 34);
        _executeButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        _executeButton.Location = new Point(306, 440);
        _executeButton.Click += (_, _) => DialogResult = DialogResult.OK;
        _executeButton.Enabled = !IsProduction;

        AcceptButton = _executeButton;
        CancelButton = _cancelButton;

        Controls.Add(_titleLabel);
        Controls.Add(_detailsPanel);
        Controls.Add(_cancelButton);
        Controls.Add(_executeButton);

        if (IsProduction)
        {
            Panel warningPanel = new()
            {
                Location = new Point(24, 296),
                Size = new Size(512, 128),
                Padding = new Padding(10)
            };

            _confirmLabel.Text = $"This is a PRODUCTION environment. Type '{ConfirmText}' to enable execution:";
            _confirmLabel.Font = BodyFont;
            _confirmLabel.Location = new Point(10, 12);
            _confirmLabel.Size = new Size(490, 40);
            _confirmLabel.TextAlign = ContentAlignment.MiddleLeft;

            _confirmTextBox.Font = new Font("Consolas", 11f, FontStyle.Bold);
            _confirmTextBox.Location = new Point(10, 56);
            _confirmTextBox.Size = new Size(180, 28);
            _confirmTextBox.CharacterCasing = CharacterCasing.Upper;
            _confirmTextBox.TextChanged += (_, _) =>
            {
                _executeButton.Enabled =
                    string.Equals(_confirmTextBox.Text.Trim(), ConfirmText, StringComparison.OrdinalIgnoreCase);
            };

            warningPanel.Controls.Add(_confirmLabel);
            warningPanel.Controls.Add(_confirmTextBox);

            Controls.Add(warningPanel);

            _warningPanel = warningPanel;
        }
        else
        {
            _warningPanel = null;
            _confirmLabel.Text = string.Empty;
            _confirmTextBox.Enabled = false;
        }

        ResumeLayout(false);
        PerformLayout();
    }

    private Panel? _warningPanel;

    private string BuildDetailsText()
    {
        string masked = MaskConnectionString(_connectionString);

        string details =
            $"Migration ID:       {_migrationId}{Environment.NewLine}" +
            $"Migration Name:     {_migrationName}{Environment.NewLine}" +
            $"Migration Type:     {_migrationType}{Environment.NewLine}" +
            $"{Environment.NewLine}" +
            $"Database:           {masked}{Environment.NewLine}" +
            $"{Environment.NewLine}" +
            $"Create Backup:      {(_createBackup ? "Yes" : "No")}{Environment.NewLine}" +
            $"Environment:        {_environment}{Environment.NewLine}" +
            $"{Environment.NewLine}";

        if (IsProduction)
        {
            details += "!!! PRODUCTION ENVIRONMENT !!!" + Environment.NewLine;
            details += "Executing a migration against a production database is a significant" + Environment.NewLine;
            details += "operation. Please review the details and confirm before proceeding." + Environment.NewLine;
        }

        return details;
    }

    private static string MaskConnectionString(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            return "(empty)";

        string[] parts = connectionString.Split(';');
        for (int i = 0; i < parts.Length; i++)
        {
            string part = parts[i].Trim();
            int eqIndex = part.IndexOf('=');
            if (eqIndex <= 0)
                continue;

            string key = part[..eqIndex].Trim();
            if (key.Equals("Password", StringComparison.OrdinalIgnoreCase) ||
                key.Equals("pwd", StringComparison.OrdinalIgnoreCase))
            {
                parts[i] = $"{key}=*****";
            }
        }

        return string.Join("; ", parts);
    }

    private void ApplyTheme(string themeName)
    {
        BackColor = _themeManager.BackgroundSecondary;
        _titleLabel.ForeColor = _themeManager.TextPrimary;

        _detailsPanel.BackColor = _themeManager.SurfaceBackground;
        _detailsTextBox.BackColor = _themeManager.GridBackground;
        _detailsTextBox.ForeColor = _themeManager.TextPrimary;

        _cancelButton.BackColor = _themeManager.BackgroundTertiary;
        _cancelButton.ForeColor = _themeManager.TextPrimary;
        _cancelButton.FlatAppearance.BorderColor = _themeManager.InputBorder;

        _executeButton.BackColor = IsProduction ? _themeManager.AccentDanger : _themeManager.AccentSuccess;
        _executeButton.ForeColor = Color.White;
        _executeButton.FlatAppearance.BorderColor = _executeButton.BackColor;

        if (_warningPanel is not null)
        {
            _warningPanel.BackColor = Color.FromArgb(80, _themeManager.AccentDanger);
            _confirmLabel.ForeColor = Color.White;
            _confirmTextBox.BackColor = Color.White;
            _confirmTextBox.ForeColor = Color.FromArgb(30, 30, 30);
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