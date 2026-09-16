using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using Migrator.UI.Theme;

namespace Migrator.UI.Controls;

[DesignerCategory("")]
public sealed class SideNavigation : UserControl
{
    private static readonly string[] PageNames = ["Dashboard", "Migration", "History", "Backups", "Logs", "Settings", "About"];

    private static readonly Color SidebarBackground = Color.FromArgb(25, 25, 25);
    private static readonly Color SidebarBorder = Color.FromArgb(45, 45, 45);
    private static readonly Color HoverBackground = Color.FromArgb(55, 55, 55);
    private static readonly Color ItemForeground = Color.FromArgb(190, 190, 190);
    private static readonly Color TitleForeground = Color.White;
    private static readonly Color SubtitleForeground = Color.FromArgb(130, 130, 130);
    private static readonly Color StatusConnected = Color.FromArgb(40, 167, 69);
    private static readonly Color StatusDisconnected = Color.FromArgb(220, 53, 69);

    private const int SidebarWidth = 220;
    private const int HeaderHeight = 72;
    private const int ItemHeight = 44;
    private const int FooterHeight = 64;
    private const int ItemHorizontalPadding = 24;

    private readonly ThemeManager _themeManager;
    private readonly Font _titleFont;
    private readonly Font _subtitleFont;
    private readonly Font _itemFont;
    private readonly Font _itemFontActive;
    private readonly Font _statusFont;
    private readonly Font _statusValueFont;

    private int _hoveredIndex = -1;
    private int _selectedIndex = 0;
    private bool _mouseDownOnItem;
    private int _pressedIndex = -1;

    private bool _isConnected;
    private string _databaseName = "Not connected";
    private string _serverName = string.Empty;

    public event EventHandler<string>? NavigationChanged;

    public SideNavigation(ThemeManager themeManager)
    {
        _themeManager = themeManager ?? throw new ArgumentNullException(nameof(themeManager));

        SetStyle(ControlStyles.OptimizedDoubleBuffer
            | ControlStyles.AllPaintingInWmPaint
            | ControlStyles.UserPaint
            | ControlStyles.ResizeRedraw, true);

        DoubleBuffered = true;
        TabStop = false;
        BackColor = SidebarBackground;
        Width = SidebarWidth;
        Height = 600;
        MinimumSize = new Size(SidebarWidth, 0);
        MaximumSize = new Size(SidebarWidth, 0);
        Font = new Font("Segoe UI", 9.5f);

        _titleFont = new Font("Segoe UI", 15f, FontStyle.Bold);
        _subtitleFont = new Font("Segoe UI", 7.5f, FontStyle.Regular);
        _itemFont = new Font("Segoe UI", 10f, FontStyle.Regular);
        _itemFontActive = new Font("Segoe UI", 10f, FontStyle.Bold);
        _statusFont = new Font("Segoe UI", 8f, FontStyle.Regular);
        _statusValueFont = new Font("Segoe UI", 9f, FontStyle.Bold);

        _themeManager.ThemeChanged += OnThemeChanged;
    }

    public string SelectedPage => PageNames[_selectedIndex];

    public void SelectPage(string pageName)
    {
        int index = Array.IndexOf(PageNames, pageName);
        if (index < 0)
            return;

        _selectedIndex = index;
        _hoveredIndex = -1;
        Invalidate();
    }

    public void SetConnectionStatus(bool isConnected, string databaseName, string serverName)
    {
        _isConnected = isConnected;
        _databaseName = string.IsNullOrWhiteSpace(databaseName) ? "Not connected" : databaseName;
        _serverName = serverName ?? string.Empty;
        Invalidate();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _themeManager.ThemeChanged -= OnThemeChanged;
            _titleFont.Dispose();
            _subtitleFont.Dispose();
            _itemFont.Dispose();
            _itemFontActive.Dispose();
            _statusFont.Dispose();
            _statusValueFont.Dispose();
        }

        base.Dispose(disposing);
    }

    private void OnThemeChanged() => Invalidate();

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        Graphics g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(SidebarBackground);

        DrawHeader(g);
        DrawItems(g);
        DrawFooter(g);
        DrawRightBorder(g);
    }

    private void DrawHeader(Graphics g)
    {
        Color accent = _themeManager.AccentPrimary;

        Rectangle logoRect = new(18, (HeaderHeight - 30) / 2, 30, 30);
        using (GraphicsPath logoPath = CreateRoundedRectangle(logoRect, 8))
        using (SolidBrush logoBrush = new(accent))
        {
            g.FillPath(logoBrush, logoPath);
        }

        using (var logoFont = new Font("Segoe UI", 13f, FontStyle.Bold))
        {
            TextRenderer.DrawText(g, "M", logoFont, logoRect, Color.White,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
        }

        TextRenderer.DrawText(g, "Migrator", _titleFont,
            new Rectangle(56, 10, Width - 56, 32), TitleForeground,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);

        TextRenderer.DrawText(g, "DATABASE MIGRATION TOOL", _subtitleFont,
            new Rectangle(56, 42, Width - 60, 20), SubtitleForeground,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);

        using (Pen separator = new(SidebarBorder))
        {
            g.DrawLine(separator, 0, HeaderHeight, Width, HeaderHeight);
        }
    }

    private void DrawItems(Graphics g)
    {
        int contentTop = HeaderHeight + 8;
        for (int i = 0; i < PageNames.Length; i++)
        {
            Rectangle itemRect = new(8, contentTop + i * (ItemHeight + 2), Width - 16, ItemHeight);
            DrawItem(g, i, itemRect);
        }
    }

    private void DrawItem(Graphics g, int index, Rectangle itemRect)
    {
        bool isActive = index == _selectedIndex;
        bool isHovered = index == _hoveredIndex;
        bool isPressed = index == _pressedIndex;

        Color accent = _themeManager.AccentPrimary;

        if (isActive)
        {
            using (SolidBrush brush = new(Color.FromArgb(38, accent)))
            using (GraphicsPath path = CreateRoundedRectangle(itemRect, 8))
            {
                g.FillPath(brush, path);
            }

            Rectangle indicator = new(6, itemRect.Y + (itemRect.Height - 20) / 2, 4, 20);
            using (SolidBrush brush = new(accent))
            using (GraphicsPath path = CreateRoundedRectangle(indicator, 2))
            {
                g.FillPath(brush, path);
            }
        }
        else if (isPressed && isHovered)
        {
            using (SolidBrush brush = new(ControlPaint.Dark(HoverBackground)))
            using (GraphicsPath path = CreateRoundedRectangle(itemRect, 8))
            {
                g.FillPath(brush, path);
            }
        }
        else if (isHovered)
        {
            using (SolidBrush brush = new(HoverBackground))
            using (GraphicsPath path = CreateRoundedRectangle(itemRect, 8))
            {
                g.FillPath(brush, path);
            }
        }

        Color textColor = isActive ? _themeManager.SidebarActiveText : isHovered ? Color.White : ItemForeground;
        Font itemFont = isActive ? _itemFontActive : _itemFont;

        Rectangle textRect = new(itemRect.Left + ItemHorizontalPadding, itemRect.Y, itemRect.Width - ItemHorizontalPadding, itemRect.Height);
        TextRenderer.DrawText(g, PageNames[index], itemFont, textRect, textColor,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
    }

    private void DrawFooter(Graphics g)
    {
        int footerTop = Height - FooterHeight;
        using (Pen separator = new(SidebarBorder))
        {
            g.DrawLine(separator, 0, footerTop, Width, footerTop);
        }

        Color statusColor = _isConnected ? StatusConnected : StatusDisconnected;

        Rectangle dotRect = new(18, footerTop + 14, 10, 10);
        using (SolidBrush dotBrush = new(statusColor))
        {
            g.FillEllipse(dotBrush, dotRect);
        }

        using (Pen glowPen = new(Color.FromArgb(70, statusColor)))
        {
            g.DrawEllipse(glowPen, new Rectangle(dotRect.X - 2, dotRect.Y - 2, dotRect.Width + 4, dotRect.Height + 4));
        }

        Color titleColor = _isConnected ? Color.FromArgb(220, 220, 220) : Color.FromArgb(150, 150, 150);
        TextRenderer.DrawText(g, _databaseName, _statusValueFont,
            new Rectangle(38, footerTop + 6, Width - 48, 22), titleColor,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);

        TextRenderer.DrawText(g, _serverName, _statusFont,
            new Rectangle(38, footerTop + 30, Width - 48, 18), SubtitleForeground,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
    }

    private void DrawRightBorder(Graphics g)
    {
        using (Pen borderPen = new(_themeManager.BorderColor))
        {
            g.DrawLine(borderPen, Width - 1, 0, Width - 1, Height);
        }
    }

    private int HitTest(Point point)
    {
        int contentTop = HeaderHeight + 8;
        for (int i = 0; i < PageNames.Length; i++)
        {
            Rectangle itemRect = new(8, contentTop + i * (ItemHeight + 2), Width - 16, ItemHeight);
            if (itemRect.Contains(point))
                return i;
        }

        return -1;
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);

        int index = HitTest(e.Location);
        Cursor = index >= 0 ? Cursors.Hand : Cursors.Default;

        if (index != _hoveredIndex)
        {
            _hoveredIndex = index;
            Invalidate();
        }
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);

        if (e.Button == MouseButtons.Left)
        {
            _pressedIndex = HitTest(e.Location);
            _mouseDownOnItem = _pressedIndex >= 0;
            if (_mouseDownOnItem)
                Invalidate();
        }
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);

        if (e.Button == MouseButtons.Left && _mouseDownOnItem)
        {
            int index = HitTest(e.Location);
            if (index >= 0)
            {
                _selectedIndex = index;
                _hoveredIndex = -1;
                Invalidate();
                NavigationChanged?.Invoke(this, PageNames[index]);
            }
        }

        _mouseDownOnItem = false;
        _pressedIndex = -1;
        Invalidate();
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        _hoveredIndex = -1;
        if (_mouseDownOnItem)
            _pressedIndex = -1;
        Invalidate();
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