using System.ComponentModel;
using System.Windows.Forms;
using Migrator.UI.Theme;

namespace Migrator.UI.Controls;

[DesignerCategory("")]
public sealed class ThemeablePanel : Panel
{
    private readonly ThemeManager _themeManager;

    public ThemeablePanel(ThemeManager themeManager)
    {
        _themeManager = themeManager ?? throw new ArgumentNullException(nameof(themeManager));

        SetStyle(ControlStyles.OptimizedDoubleBuffer
            | ControlStyles.AllPaintingInWmPaint
            | ControlStyles.UserPaint
            | ControlStyles.ResizeRedraw, true);

        DoubleBuffered = true;

        ApplyTheme();
        _themeManager.ThemeChanged += OnThemeChanged;
    }

    public ThemeManager ThemeManager => _themeManager;

    public Color SurfaceColor => _themeManager.SurfaceBackground;

    public Color CardColor => _themeManager.CardBackground;

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            _themeManager.ThemeChanged -= OnThemeChanged;
        base.Dispose(disposing);
    }

    public void ApplyTheme()
    {
        BackColor = _themeManager.BackgroundPrimary;
    }

    private void OnThemeChanged()
    {
        if (InvokeRequired)
        {
            if (IsHandleCreated)
                BeginInvoke((Action)ApplyTheme);
            return;
        }

        ApplyTheme();
    }
}