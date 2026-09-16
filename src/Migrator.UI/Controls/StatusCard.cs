using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using Migrator.UI.Theme;

namespace Migrator.UI.Controls;

[DesignerCategory("")]
public sealed class StatusCard : UserControl
{
    private readonly ThemeManager _themeManager;

    private string _cardTitle = string.Empty;
    private string _cardValue = string.Empty;
    private string _cardSubtitle = string.Empty;
    private Color _accentColor;

    public StatusCard(ThemeManager themeManager)
    {
        _themeManager = themeManager ?? throw new ArgumentNullException(nameof(themeManager));
        _accentColor = themeManager.AccentPrimary;

        SetStyle(ControlStyles.OptimizedDoubleBuffer
            | ControlStyles.AllPaintingInWmPaint
            | ControlStyles.UserPaint
            | ControlStyles.ResizeRedraw
            | ControlStyles.SupportsTransparentBackColor, true);

        DoubleBuffered = true;
        TabStop = false;
        BackColor = Color.Transparent;
        Font = new Font("Segoe UI", 9.5f);

        _themeManager.ThemeChanged += OnThemeChanged;
    }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string CardTitle
    {
        get => _cardTitle;
        set
        {
            _cardTitle = value ?? string.Empty;
            Invalidate();
        }
    }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string CardValue
    {
        get => _cardValue;
        set
        {
            _cardValue = value ?? string.Empty;
            Invalidate();
        }
    }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string CardSubtitle
    {
        get => _cardSubtitle;
        set
        {
            _cardSubtitle = value ?? string.Empty;
            Invalidate();
        }
    }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color AccentColor
    {
        get => _accentColor;
        set
        {
            _accentColor = value;
            Invalidate();
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            _themeManager.ThemeChanged -= OnThemeChanged;
        base.Dispose(disposing);
    }

    private void OnThemeChanged()
    {
        if (_accentColor == _themeManager.AccentPrimary)
            _accentColor = _themeManager.AccentPrimary;
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        Graphics g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        Rectangle bounds = new(0, 0, Width, Height);
        using (GraphicsPath path = CreateRoundedRectangle(bounds, 10))
        using (SolidBrush surfaceBrush = new(_themeManager.CardBackground))
        using (Pen borderPen = new(_themeManager.BorderColor))
        {
            g.FillPath(surfaceBrush, path);
            g.DrawPath(borderPen, path);
        }

        Rectangle accentBar = new(8, 12, 4, Height - 24);
        using (GraphicsPath path = CreateRoundedRectangle(accentBar, 2))
        using (SolidBrush accentBrush = new(_accentColor))
        {
            g.FillPath(accentBrush, path);
        }

        Rectangle textBounds = new(20, 10, Width - 34, Height - 20);

            using (var titleFont = new Font("Segoe UI", 9f, FontStyle.Bold))
            {
                TextRenderer.DrawText(g, _cardTitle, titleFont, textBounds, _themeManager.TextSecondary,
                    TextFormatFlags.Left | TextFormatFlags.Top | TextFormatFlags.EndEllipsis);
            }

            Rectangle valueBounds = new(textBounds.X, textBounds.Y + 22, textBounds.Width, textBounds.Height - 22);
            using (var valueFont = new Font("Segoe UI", 24f, FontStyle.Bold))
            {
                TextRenderer.DrawText(g, _cardValue, valueFont, valueBounds, _themeManager.TextPrimary,
                    TextFormatFlags.Left | TextFormatFlags.Top | TextFormatFlags.EndEllipsis);
            }

            Rectangle subtitleBounds = new(textBounds.X, Height - 24, textBounds.Width, 18);
            using (var subtitleFont = new Font("Segoe UI", 8f, FontStyle.Regular))
            {
                TextRenderer.DrawText(g, _cardSubtitle, subtitleFont, subtitleBounds, _themeManager.TextMuted,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
            }
    }

    private static GraphicsPath CreateRoundedRectangle(Rectangle bounds, int radius)
    {
        int diameter = radius * 2;
        GraphicsPath path = new();

        if (diameter <= 0)
        {
            path.AddRectangle(bounds);
            path.CloseFigure();
            return path;
        }

        path.AddArc(bounds.X, bounds.Y, diameter, diameter, 180, 90);
        path.AddArc(bounds.Right - diameter, bounds.Y, diameter, diameter, 270, 90);
        path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(bounds.X, bounds.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        return path;
    }
}