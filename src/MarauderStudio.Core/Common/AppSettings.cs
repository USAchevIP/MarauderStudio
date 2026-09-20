namespace MarauderStudio.Core.Common;

/// <summary>
/// Настройки приложения (хранятся в %APPDATA%/MarauderStudio/settings.json).
/// </summary>
public sealed class AppSettings
{
    public string Language { get; set; } = "ru";
    public string Theme { get; set; } = "dark"; // dark | light | system

    /// <summary>Путь к esptool.exe. Пусто → автодетект.</summary>
    public string EsptoolPath { get; set; } = string.Empty;

    /// <summary>SSID WiFi для OTA-прошивки.</summary>
    public string OtaSsid { get; set; } = "MarauderOTA";

    /// <summary>Пароль OTA-сети.</summary>
    public string OtaPassword { get; set; } = "justcallmekoko";

    /// <summary>Папка для скачанных прошивок.</summary>
    public string DownloadDir { get; set; } = string.Empty;

    public bool AutoCheckUpdates { get; set; } = true;
}
