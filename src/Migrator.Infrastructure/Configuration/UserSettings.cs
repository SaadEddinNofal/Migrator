namespace Migrator.Infrastructure.Configuration;

public sealed class UserSettings
{
    public string Theme { get; set; } = "Dark";
    public string? LastMigrationDirectory { get; set; }
    public string? LastConnectionString { get; set; }
    public bool CreateBackupByDefault { get; set; }
    public bool StopOnFirstFailure { get; set; } = true;
    public string DefaultEnvironment { get; set; } = "Development";
    public int WindowWidth { get; set; } = 1400;
    public int WindowHeight { get; set; } = 850;
    public string SettingsFilePath { get; set; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Migrator",
        "settings.json");
}