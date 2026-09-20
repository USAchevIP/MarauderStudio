using System.IO;
using System.Text.Json;

namespace MarauderStudio.Core.Common;

/// <summary>
/// Загрузка/сохранение настроек приложения в %APPDATA%/MarauderStudio/settings.json.
/// </summary>
public sealed class SettingsService
{
    public string FilePath { get; }

    public SettingsService(string? overridePath = null)
    {
        FilePath = overridePath ?? GetDefaultPath();
    }

    public AppSettings Load()
    {
        try
        {
            if (!File.Exists(FilePath)) return new AppSettings();
            var json = File.ReadAllText(FilePath);
            return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
        }
        catch
        {
            return new AppSettings();
        }
    }

    public void Save(AppSettings settings)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(FilePath, json);
        }
        catch
        {
            // Тихо проглатываем — UI покажет своё уведомление.
        }
    }

    public static string GetDefaultPath()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(appData, "MarauderStudio", "settings.json");
    }
}
