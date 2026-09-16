namespace Migrator.UI.Forms;

using System.Drawing;
using System.Windows.Forms;
using Migrator.UI.Theme;

public sealed class BackupsPage : UserControl
{
    private readonly ThemeManager _themeManager;

    private readonly Label _titleLabel = new();
    private readonly Panel _placeholderPanel = new();
    private readonly Label _placeholderIcon = new();
    private readonly Label _placeholderTitle = new();
    private readonly Label _placeholderMessage = new();

    private static readonly Font HeadingFont = new("Segoe UI", 14f, FontStyle.Bold);
    private static readonly Font PlaceholderIconFont = new("Segoe UI", 30f);
    private static readonly Font PlaceholderTitleFont = new("Segoe UI", 12f);
    private static readonly Font BodyFont = new("Segoe UI", 9f);

    public BackupsPage(ThemeManager themeManager)
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

        _titleLabel.Text = "Backups";
        _titleLabel.Font = HeadingFont;
        _titleLabel.AutoSize = true;
        _titleLabel.Location = new Point(24, 16);

        _placeholderPanel.Dock = DockStyle.Fill;
        _placeholderPanel.BorderStyle = BorderStyle.None;
        _placeholderPanel.Padding = new Padding(0, 60, 0, 0);

        _placeholderIcon.Text = "\u229E";
        _placeholderIcon.Font = PlaceholderIconFont;
        _placeholderIcon.AutoSize = true;
        _placeholderIcon.Anchor = AnchorStyles.Top;
        _placeholderIcon.Location = new Point(Width / 2 - 60, 160);

        _placeholderTitle.Text = "No Backups Yet";
        _placeholderTitle.Font = PlaceholderTitleFont;
        _placeholderTitle.AutoSize = true;
        _placeholderTitle.Anchor = AnchorStyles.Top;
        _placeholderTitle.Location = new Point(Width / 2 - 70, 220);

        _placeholderMessage.Text = "Backup records will appear here after migrations with backup enabled are executed.";
        _placeholderMessage.Font = BodyFont;
        _placeholderMessage.AutoSize = true;
        _placeholderMessage.Anchor = AnchorStyles.Top;
        _placeholderMessage.Location = new Point(Width / 2 - 280, 250);

        _placeholderPanel.Controls.Add(_placeholderIcon);
        _placeholderPanel.Controls.Add(_placeholderTitle);
        _placeholderPanel.Controls.Add(_placeholderMessage);

        Controls.Add(_titleLabel);
        Controls.Add(_placeholderPanel);

        Resize += (_, _) =>
        {
            _placeholderIcon.Location = new Point(Width / 2 - 60, 160);
            _placeholderTitle.Location = new Point(Width / 2 - _placeholderTitle.Width / 2, 220);
            _placeholderMessage.Location = new Point(Width / 2 - _placeholderMessage.Width / 2, 250);
        };

        ResumeLayout(false);
    }

    private void ApplyTheme(string themeName)
    {
        BackColor = _themeManager.BackgroundSecondary;
        _titleLabel.ForeColor = _themeManager.TextPrimary;
        _placeholderPanel.BackColor = _themeManager.SurfaceBackground;
        _placeholderIcon.ForeColor = _themeManager.TextMuted;
        _placeholderTitle.ForeColor = _themeManager.TextSecondary;
        _placeholderMessage.ForeColor = _themeManager.TextMuted;
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