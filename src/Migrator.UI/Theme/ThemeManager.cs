namespace Migrator.UI.Theme;

public enum AppTheme { Dark, Light }

public sealed class ThemeManager
{
    public AppTheme CurrentTheme { get; private set; } = AppTheme.Dark;

    // Colors
    public Color BackgroundPrimary { get; private set; }
    public Color BackgroundSecondary { get; private set; }
    public Color BackgroundTertiary { get; private set; }
    public Color SidebarBackground { get; private set; }
    public Color SurfaceBackground { get; private set; }
    public Color CardBackground { get; private set; }
    public Color TextPrimary { get; private set; }
    public Color TextSecondary { get; private set; }
    public Color TextMuted { get; private set; }
    public Color AccentPrimary { get; private set; }
    public Color AccentSuccess { get; private set; }
    public Color AccentWarning { get; private set; }
    public Color AccentDanger { get; private set; }
    public Color BorderColor { get; private set; }
    public Color SidebarActiveBackground { get; private set; }
    public Color SidebarHoverBackground { get; private set; }
    public Color SidebarText { get; private set; }
    public Color SidebarActiveText { get; private set; }
    public Color StatusConnected { get; private set; }
    public Color StatusDisconnected { get; private set; }
    public Color InputBackground { get; private set; }
    public Color InputBorder { get; private set; }
    public Color GridBackground { get; private set; }
    public Color GridRowAlternate { get; private set; }
    public Color GridHeaderBackground { get; private set; }
    public Color GridSelection { get; private set; }

    public event Action? ThemeChanged;

    public void ApplyTheme(string themeName)
    {
        if (Enum.TryParse<AppTheme>(themeName, true, out var theme))
            CurrentTheme = theme;

        if (CurrentTheme == AppTheme.Dark)
            ApplyDarkTheme();
        else
            ApplyLightTheme();

        ThemeChanged?.Invoke();
    }

    private void ApplyDarkTheme()
    {
        BackgroundPrimary = Color.FromArgb(30, 30, 30);
        BackgroundSecondary = Color.FromArgb(40, 40, 40);
        BackgroundTertiary = Color.FromArgb(50, 50, 50);
        SidebarBackground = Color.FromArgb(25, 25, 25);
        SurfaceBackground = Color.FromArgb(35, 35, 35);
        CardBackground = Color.FromArgb(45, 45, 45);
        TextPrimary = Color.FromArgb(240, 240, 240);
        TextSecondary = Color.FromArgb(180, 180, 180);
        TextMuted = Color.FromArgb(120, 120, 120);
        AccentPrimary = Color.FromArgb(0, 122, 204);
        AccentSuccess = Color.FromArgb(40, 167, 69);
        AccentWarning = Color.FromArgb(255, 193, 7);
        AccentDanger = Color.FromArgb(220, 53, 69);
        BorderColor = Color.FromArgb(60, 60, 60);
        SidebarActiveBackground = Color.FromArgb(0, 122, 204);
        SidebarHoverBackground = Color.FromArgb(55, 55, 55);
        SidebarText = Color.FromArgb(180, 180, 180);
        SidebarActiveText = Color.White;
        StatusConnected = Color.FromArgb(40, 167, 69);
        StatusDisconnected = Color.FromArgb(220, 53, 69);
        InputBackground = Color.FromArgb(50, 50, 50);
        InputBorder = Color.FromArgb(70, 70, 70);
        GridBackground = Color.FromArgb(35, 35, 35);
        GridRowAlternate = Color.FromArgb(40, 40, 40);
        GridHeaderBackground = Color.FromArgb(50, 50, 50);
        GridSelection = Color.FromArgb(0, 80, 140);
    }

    private void ApplyLightTheme()
    {
        BackgroundPrimary = Color.FromArgb(245, 245, 245);
        BackgroundSecondary = Color.FromArgb(240, 240, 240);
        BackgroundTertiary = Color.FromArgb(235, 235, 235);
        SidebarBackground = Color.FromArgb(35, 40, 50);
        SurfaceBackground = Color.FromArgb(250, 250, 250);
        CardBackground = Color.White;
        TextPrimary = Color.FromArgb(33, 37, 41);
        TextSecondary = Color.FromArgb(100, 100, 100);
        TextMuted = Color.FromArgb(150, 150, 150);
        AccentPrimary = Color.FromArgb(0, 102, 178);
        AccentSuccess = Color.FromArgb(36, 142, 60);
        AccentWarning = Color.FromArgb(220, 170, 0);
        AccentDanger = Color.FromArgb(200, 45, 60);
        BorderColor = Color.FromArgb(218, 220, 224);
        SidebarActiveBackground = Color.FromArgb(0, 102, 178);
        SidebarHoverBackground = Color.FromArgb(50, 55, 65);
        SidebarText = Color.FromArgb(180, 185, 195);
        SidebarActiveText = Color.White;
        StatusConnected = Color.FromArgb(36, 142, 60);
        StatusDisconnected = Color.FromArgb(200, 45, 60);
        InputBackground = Color.White;
        InputBorder = Color.FromArgb(200, 200, 200);
        GridBackground = Color.White;
        GridRowAlternate = Color.FromArgb(248, 248, 248);
        GridHeaderBackground = Color.FromArgb(240, 240, 240);
        GridSelection = Color.FromArgb(200, 230, 255);
    }
}