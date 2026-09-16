using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using Migrator.UI.Theme;

namespace Migrator.UI.Controls;

[DesignerCategory("")]
public sealed class ConnectionIndicator : UserControl
{
    private readonly ThemeManager _themeManager;

    private bool _isConnected;
    private string _databaseName = string.Empty;
    private string _serverName = string.Empty;

    public ConnectionIndicator(ThemeManager themeManager)
    {
        _themeManager = themeManager ?? throw new ArgumentNullException(nameof(themeManager));

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
    public bool IsConnected
    {
        get => _isConnected;
        set
        {
            _isConnected = value;
            Invalidate();
        }
    }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string DatabaseName
    {
        get => _databaseName;
        set
        {
            _databaseName = value ?? string.Empty;
            Invalidate();
        }
    }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string ServerName
    {
        get => _serverName;
        set
        {
            _serverName = value ?? string.Empty;
            Invalidate();
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            _themeManager.ThemeChanged -= OnThemeChanged;
        base.Dispose(disposing);
    }

    private void OnThemeChanged() => Invalidate();

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        Graphics g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        Color statusColor = _isConnected
            ? _themeManager.StatusConnected
            : _themeManager.StatusDisconnected;

        Rectangle dotRect = new(4, (Height - 10) / 2, 10, 10);
        using (SolidBrush dotBrush = new(statusColor))
        {
            g.FillEllipse(dotBrush, dotRect);
        }

        using (Pen glowPen = new(Color.FromArgb(70, statusColor), 1f))
        {
            g.DrawEllipse(glowPen, new RectangleF(dotRect.X - 2.5f, dotRect.Y - 2.5f, 15f, 15f));
        }

        string primaryText;
        string secondaryText;
        Color primaryColor;
        Color secondaryColor;

        if (_isConnected)
        {
            primaryText = string.IsNullOrWhiteSpace(_databaseName) ? "Connected" : _databaseName;
            primaryColor = _themeManager.TextPrimary;
            secondaryText = _serverName;
            secondaryColor = _themeManager.TextSecondary;
        }
        else
        {
            primaryText = "Not connected";
            primaryColor = _themeManager.TextMuted;
            secondaryText = string.Empty;
            secondaryColor = _themeManager.TextMuted;
        }

        using var primaryFont = new Font("Segoe UI", 9.5f, FontStyle.Bold);
        Rectangle primaryRect = new(22, 2, Width - 24, 22);
        TextRenderer.DrawText(g, primaryText, primaryFont, primaryRect, primaryColor,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);

        if (!string.IsNullOrWhiteSpace(secondaryText))
        {
            using var secondaryFont = new Font("Segoe UI", 8f, FontStyle.Regular);
            Rectangle secondaryRect = new(22, Height - 20, Width - 24, 18);
            TextRenderer.DrawText(g, secondaryText, secondaryFont, secondaryRect, secondaryColor,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        }
    }
}