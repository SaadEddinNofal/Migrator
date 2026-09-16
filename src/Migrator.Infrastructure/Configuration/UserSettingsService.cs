using System.Text.Json;
using System.Text.Json.Serialization;

namespace Migrator.Infrastructure.Configuration;

public sealed class UserSettingsService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    public UserSettings Current { get; private set; }

    public UserSettingsService()
    {
        Current = Load();
    }

    public UserSettings Load()
    {
        var defaults = new UserSettings();

        try
        {
            if (!File.Exists(defaults.SettingsFilePath))
                return defaults;

            var json = File.ReadAllText(defaults.SettingsFilePath);
            var loaded = JsonSerializer.Deserialize<UserSettings>(json, JsonOptions);

            if (loaded is null)
                return defaults;

            // Preserve the resolved path so subsequent saves land in the same place.
            loaded.SettingsFilePath = defaults.SettingsFilePath;
            return loaded;
        }
        catch (Exception)
        {
            // Corrupt or unreadable settings file; fall back to defaults.
            return defaults;
        }
    }

    public void Save()
    {
        Save(Current);
    }

    public void Save(UserSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        try
        {
            string filePath = settings.SettingsFilePath;
            string? directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var json = JsonSerializer.Serialize(settings, JsonOptions);
            File.WriteAllText(filePath, json);

            Current = settings;
        }
        catch (Exception)
        {
            // Persisting user settings must never crash the application.
        }
    }
}