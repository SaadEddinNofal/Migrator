namespace Migrator.UI.Forms;

using System.Drawing;
using System.Windows.Forms;
using Migrator.Infrastructure.Configuration;
using Migrator.UI.Theme;

public sealed class SettingsPage : UserControl
{
    private readonly UserSettingsService _settingsService;
    private readonly ThemeManager _themeManager;

    private readonly Label _titleLabel = new();
    private readonly Label _themeLabel = new();
    private readonly Panel _themePanel = new();
    private readonly RadioButton _darkRadio = new();
    private readonly RadioButton _lightRadio = new();
    private readonly Label _generalLabel = new();
    private readonly CheckBox _backupCheckBox = new();
    private readonly CheckBox _stopOnFailureCheckBox = new();
    private readonly Label _environmentLabel = new();
    private readonly ComboBox _environmentCombo = new();
    private readonly Button _resetButton = new();
    private readonly Label _settingsFileLabel = new();

    private static readonly Font HeadingFont = new("Segoe UI", 14f, FontStyle.Bold);
    private static readonly Font SectionFont = new("Segoe UI", 11f, FontStyle.Bold);
    private static readonly Font BodyFont = new("Segoe UI", 9f);
    private static readonly Font MutedFont = new("Segoe UI", 8f);

    public SettingsPage(UserSettingsService settingsService, ThemeManager themeManager)
    {
        _settingsService = settingsService;
        _themeManager = themeManager;

        InitializeComponent();
        LoadSettingsIntoUi();
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

        _titleLabel.Text = "Settings";
        _titleLabel.Font = HeadingFont;
        _titleLabel.AutoSize = true;
        _titleLabel.Location = new Point(24, 16);

        _themeLabel.Text = "Theme";
        _themeLabel.Font = SectionFont;
        _themeLabel.AutoSize = true;
        _themeLabel.Location = new Point(24, 60);

        _themePanel.Location = new Point(24, 86);
        _themePanel.Size = new Size(Width - 48, 54);
        _themePanel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        _themePanel.Padding = new Padding(16);

        _darkRadio.Text = "Dark";
        _darkRadio.Font = BodyFont;
        _darkRadio.AutoSize = true;
        _darkRadio.Location = new Point(16, 16);
        _darkRadio.Checked = true;
        _darkRadio.CheckedChanged += (_, _) =>
        {
            if (_darkRadio.Checked)
            {
                SaveTheme("Dark");
            }
        };

        _lightRadio.Text = "Light";
        _lightRadio.Font = BodyFont;
        _lightRadio.AutoSize = true;
        _lightRadio.Location = new Point(110, 16);
        _lightRadio.CheckedChanged += (_, _) =>
        {
            if (_lightRadio.Checked)
            {
                SaveTheme("Light");
            }
        };

        _themePanel.Controls.Add(_darkRadio);
        _themePanel.Controls.Add(_lightRadio);

        _generalLabel.Text = "Migration Defaults";
        _generalLabel.Font = SectionFont;
        _generalLabel.AutoSize = true;
        _generalLabel.Location = new Point(24, 160);

        _backupCheckBox.Text = "Create backup before execution by default";
        _backupCheckBox.Font = BodyFont;
        _backupCheckBox.AutoSize = true;
        _backupCheckBox.Location = new Point(40, 186);
        _backupCheckBox.CheckedChanged += (_, _) =>
        {
            _settingsService.Current.CreateBackupByDefault = _backupCheckBox.Checked;
            _settingsService.Save();
        };

        _stopOnFailureCheckBox.Text = "Stop on first failure by default";
        _stopOnFailureCheckBox.Font = BodyFont;
        _stopOnFailureCheckBox.AutoSize = true;
        _stopOnFailureCheckBox.Location = new Point(40, 212);
        _stopOnFailureCheckBox.CheckedChanged += (_, _) =>
        {
            _settingsService.Current.StopOnFirstFailure = _stopOnFailureCheckBox.Checked;
            _settingsService.Save();
        };

        _environmentLabel.Text = "Default Environment:";
        _environmentLabel.Font = BodyFont;
        _environmentLabel.AutoSize = true;
        _environmentLabel.Location = new Point(40, 242);

        _environmentCombo.Font = BodyFont;
        _environmentCombo.DropDownStyle = ComboBoxStyle.DropDownList;
        _environmentCombo.Items.AddRange(["Development", "Staging", "Production"]);
        _environmentCombo.Location = new Point(220, 240);
        _environmentCombo.Size = new Size(140, 26);
        _environmentCombo.SelectedIndexChanged += (_, _) =>
        {
            if (_environmentCombo.SelectedItem is string env)
            {
                _settingsService.Current.DefaultEnvironment = env;
                _settingsService.Save();
            }
        };

        _resetButton.Text = "Reset Settings";
        _resetButton.Font = BodyFont;
        _resetButton.FlatStyle = FlatStyle.Flat;
        _resetButton.Size = new Size(120, 32);
        _resetButton.Location = new Point(24, 290);
        _resetButton.Click += (_, _) => ResetSettings();

        _settingsFileLabel.Text = $"Settings file: {_settingsService.Current.SettingsFilePath}";
        _settingsFileLabel.Font = MutedFont;
        _settingsFileLabel.AutoSize = true;
        _settingsFileLabel.Location = new Point(24, 336);

        Controls.Add(_titleLabel);
        Controls.Add(_themeLabel);
        Controls.Add(_themePanel);
        Controls.Add(_generalLabel);
        Controls.Add(_backupCheckBox);
        Controls.Add(_stopOnFailureCheckBox);
        Controls.Add(_environmentLabel);
        Controls.Add(_environmentCombo);
        Controls.Add(_resetButton);
        Controls.Add(_settingsFileLabel);

        Resize += (_, _) =>
        {
            _themePanel.Width = Width - 48;
        };

        ResumeLayout(false);
    }

    private void LoadSettingsIntoUi()
    {
        _darkRadio.Checked = _settingsService.Current.Theme != "Light";
        _lightRadio.Checked = _settingsService.Current.Theme == "Light";

        _backupCheckBox.Checked = _settingsService.Current.CreateBackupByDefault;
        _stopOnFailureCheckBox.Checked = _settingsService.Current.StopOnFirstFailure;

        _environmentCombo.SelectedIndex = _settingsService.Current.DefaultEnvironment switch
        {
            "Staging" => 1,
            "Production" => 2,
            _ => 0
        };
    }

    private void SaveTheme(string theme)
    {
        _settingsService.Current.Theme = theme;
        _settingsService.Save();
        _themeManager.ApplyTheme(theme);
    }

    private void ResetSettings()
    {
        var result = MessageBox.Show(
            this,
            "Reset all settings to their default values?",
            "Reset Settings",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (result != DialogResult.Yes)
            return;

        var defaults = new UserSettings();
        _settingsService.Save(defaults);
        LoadSettingsIntoUi();
        _themeManager.ApplyTheme(defaults.Theme);
        _settingsFileLabel.Text = $"Settings file: {_settingsService.Current.SettingsFilePath}";
    }

    private void ApplyTheme(string themeName)
    {
        BackColor = _themeManager.BackgroundSecondary;
        _titleLabel.ForeColor = _themeManager.TextPrimary;
        _themeLabel.ForeColor = _themeManager.TextPrimary;
        _generalLabel.ForeColor = _themeManager.TextPrimary;

        _themePanel.BackColor = _themeManager.SurfaceBackground;
        _darkRadio.ForeColor = _themeManager.TextSecondary;
        _lightRadio.ForeColor = _themeManager.TextSecondary;
        _backupCheckBox.ForeColor = _themeManager.TextSecondary;
        _stopOnFailureCheckBox.ForeColor = _themeManager.TextSecondary;
        _environmentLabel.ForeColor = _themeManager.TextSecondary;

        _environmentCombo.BackColor = _themeManager.InputBackground;
        _environmentCombo.ForeColor = _themeManager.TextPrimary;
        _environmentCombo.FlatStyle = FlatStyle.Flat;

        _resetButton.BackColor = _themeManager.AccentDanger;
        _resetButton.ForeColor = Color.White;
        _resetButton.FlatAppearance.BorderColor = _themeManager.AccentDanger;

        _settingsFileLabel.ForeColor = _themeManager.TextMuted;
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