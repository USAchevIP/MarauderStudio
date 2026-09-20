namespace MarauderStudio.Core.Devices;

/// <summary>
/// Семейство чипа ESP32. Соответствует флагам --chip в esptool.
/// </summary>
public enum ChipFamily
{
    Unknown = 0,
    ESP32,
    ESP32_S2,
    ESP32_S3,
    ESP32_C3,
    ESP32_C5,
    ESP32_C6,
    ESP32_H2
}

public static class ChipFamilyExtensions
{
    public static string ToEsptoolArg(this ChipFamily family) => family switch
    {
        ChipFamily.ESP32    => "esp32",
        ChipFamily.ESP32_S2 => "esp32s2",
        ChipFamily.ESP32_S3 => "esp32s3",
        ChipFamily.ESP32_C3 => "esp32c3",
        ChipFamily.ESP32_C5 => "esp32c5",
        ChipFamily.ESP32_C6 => "esp32c6",
        ChipFamily.ESP32_H2 => "esp32h2",
        _ => "auto"
    };

    public static string DisplayName(this ChipFamily family) => family switch
    {
        ChipFamily.ESP32    => "ESP32",
        ChipFamily.ESP32_S2 => "ESP32-S2",
        ChipFamily.ESP32_S3 => "ESP32-S3",
        ChipFamily.ESP32_C3 => "ESP32-C3",
        ChipFamily.ESP32_C5 => "ESP32-C5",
        ChipFamily.ESP32_C6 => "ESP32-C6",
        ChipFamily.ESP32_H2 => "ESP32-H2",
        _ => "Unknown"
    };
}
