namespace Migrator.UI.Forms;

using System.Drawing;
using System.Windows.Forms;
using Migrator.UI.Theme;

public sealed class AboutPage : UserControl
{
    private readonly ThemeManager _themeManager;

    private readonly Label _titleLabel = new();
    private readonly Label _versionLabel = new();
    private readonly Label _taglineLabel = new();
    private readonly Label _stackLabel = new();
    private readonly Label _featuresLabel = new();
    private readonly Label _copyrightLabel = new();

    private static readonly Font TitleFont = new("Segoe UI", 24f, FontStyle.Bold);
    private static readonly Font VersionFont = new("Segoe UI", 11f);
    private static readonly Font BodyFont = new("Segoe UI", 9f);
    private static readonly Font MutedFont = new("Segoe UI", 8.5f);

    public AboutPage(ThemeManager themeManager)
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
        AutoScroll = true;
        Padding = new Padding(60);

        _titleLabel.Text = "Migrator";
        _titleLabel.Font = TitleFont;
        _titleLabel.AutoSize = true;
        _titleLabel.Location = new Point(60, 60);

        _versionLabel.Text = "Version 1.0.0";
        _versionLabel.Font = VersionFont;
        _versionLabel.AutoSize = true;
        _versionLabel.Location = new Point(60, 116);

        _taglineLabel.Text = "Professional Database Migration Tool";
        _taglineLabel.Font = new Font("Segoe UI", 10.5f, FontStyle.Italic);
        _taglineLabel.AutoSize = true;
        _taglineLabel.Location = new Point(60, 148);

        _stackLabel.Text = ".NET 10 | Windows Forms | ReaLTaiizor";
        _stackLabel.Font = BodyFont;
        _stackLabel.AutoSize = true;
        _stackLabel.Location = new Point(60, 190);

        _featuresLabel.Text = "SQL Server support with FluentMigrator integration";
        _featuresLabel.Font = BodyFont;
        _featuresLabel.AutoSize = true;
        _featuresLabel.Location = new Point(60, 220);

        _copyrightLabel.Text = "\u00A9 " + DateTime.Now.Year + " Migrator. All rights reserved.";
        _copyrightLabel.Font = MutedFont;
        _copyrightLabel.AutoSize = true;
        _copyrightLabel.Location = new Point(60, 270);

        Controls.Add(_titleLabel);
        Controls.Add(_versionLabel);
        Controls.Add(_taglineLabel);
        Controls.Add(_stackLabel);
        Controls.Add(_featuresLabel);
        Controls.Add(_copyrightLabel);

        ResumeLayout(false);
    }

    private void ApplyTheme(string themeName)
    {
        BackColor = _themeManager.BackgroundSecondary;
        _titleLabel.ForeColor = _themeManager.TextPrimary;
        _versionLabel.ForeColor = _themeManager.AccentPrimary;
        _taglineLabel.ForeColor = _themeManager.TextSecondary;
        _stackLabel.ForeColor = _themeManager.TextSecondary;
        _featuresLabel.ForeColor = _themeManager.TextSecondary;
        _copyrightLabel.ForeColor = _themeManager.TextMuted;
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