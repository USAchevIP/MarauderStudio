using System.IO;
using System.Text.Json;
using System.Windows;

namespace MarauderStudio.Localization;

/// <summary>
/// Сервис локализации. Читает ru.json/en.json из ресурсов приложения,
/// позволяет менять язык на лету и оповещает подписчиков.
/// </summary>
public static class LocalizationService
{
    private const string DefaultLanguage = "ru";

    private static readonly Dictionary<string, Dictionary<string, string>> _dictionaries = new();
    private static string _currentLanguage = DefaultLanguage;

    public static event EventHandler? LanguageChanged;

    public static string CurrentLanguage
    {
        get => _currentLanguage;
        set
        {
            if (_currentLanguage == value) return;
            _currentLanguage = value;
            LanguageChanged?.Invoke(null, EventArgs.Empty);
            RefreshRequested?.Invoke(null, EventArgs.Empty);
        }
    }

    /// <summary>
    /// Сигнал, что UI должен перерисоваться (например, перезагрузить текущую страницу).
    /// </summary>
    public static event EventHandler? RefreshRequested;

    public static IReadOnlyCollection<string> AvailableLanguages => _dictionaries.Keys.ToList();

    static LocalizationService()
    {
        LoadLanguage("ru");
        LoadLanguage("en");
    }

    private static void LoadLanguage(string lang)
    {
        try
        {
            var uri = new Uri($"pack://application:,,,/Resources/Localization/{lang}.json", UriKind.Absolute);
            var streamInfo = Application.GetResourceStream(uri);
            if (streamInfo?.Stream is null) return;

            using var reader = new StreamReader(streamInfo.Stream);
            var json = reader.ReadToEnd();
            var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
            if (dict is not null)
            {
                _dictionaries[lang] = dict;
            }
        }
        catch
        {
            // Файл локализации недоступен — оставляем текущий словарь без изменений.
        }
    }

    public static string Get(string key)
    {
        if (_dictionaries.TryGetValue(_currentLanguage, out var dict) &&
            dict.TryGetValue(key, out var value))
        {
            return value;
        }
        if (_dictionaries.TryGetValue(DefaultLanguage, out var fallback) &&
            fallback.TryGetValue(key, out var fallbackValue))
        {
            return fallbackValue;
        }
        return key;
    }
}
