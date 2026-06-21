using System.Text.Json;

namespace TrayMotors;

public static class SettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public static string SettingsDirectory => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "TrayMotors");

    public static string SettingsPath => Path.Combine(SettingsDirectory, "settings.json");

    public static AppSettings Load()
    {
        try
        {
            if (!File.Exists(SettingsPath))
            {
                return AppSettings.Default;
            }

            var settings = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(SettingsPath), JsonOptions);
            return (settings ?? AppSettings.Default).Normalized();
        }
        catch (Exception exception)
        {
            AppLog.Error("Settings load failed; using defaults", exception);
            return AppSettings.Default;
        }
    }

    public static void Save(AppSettings settings)
    {
        try
        {
            Directory.CreateDirectory(SettingsDirectory);
            File.WriteAllText(SettingsPath, JsonSerializer.Serialize(settings.Normalized(), JsonOptions));
        }
        catch (Exception exception)
        {
            AppLog.Error("Settings save failed", exception);
        }
    }
}
