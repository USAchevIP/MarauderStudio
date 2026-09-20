using System.Diagnostics;
using System.Text.RegularExpressions;
using MarauderStudio.Core.Devices;

namespace MarauderStudio.Core.Firmware;

/// <summary>
/// Определяет чип ESP32 по выводу `esptool chip_id`.
/// Запускает локальный `esptool.exe` (bundled или системный) и парсит stdout.
/// </summary>
public sealed class ChipDetector
{
    private readonly Func<string, string[], Task<(int exit, string output)>> _esptoolRunner;

    /// <summary>
    /// Конструктор с dependency-injected runner. В UI создаётся с обёрткой над EsptoolRunner.
    /// </summary>
    public ChipDetector(Func<string, string[], Task<(int exit, string output)>> esptoolRunner)
    {
        _esptoolRunner = esptoolRunner;
    }

    /// <summary>
    /// Запускает chip_id и парсит результат. Не бросает исключение при ошибке — возвращает Unknown.
    /// </summary>
    public async Task<ChipDetectionResult> DetectAsync(string port, CancellationToken ct = default)
    {
        try
        {
            var (exit, output) = await _esptoolRunner(port, new[] { "--chip", "auto", "chip_id" }).ConfigureAwait(false);
            if (exit != 0) return new ChipDetectionResult(ChipFamily.Unknown, $"esptool exit {exit}: {output}", null);

            var family = ParseChipFromOutput(output);
            var chipName = ExtractChipName(output);
            var mac = ExtractMac(output);
            return new ChipDetectionResult(family, output.Trim(), chipName, mac);
        }
        catch (Exception ex)
        {
            return new ChipDetectionResult(ChipFamily.Unknown, $"error: {ex.Message}", null, null);
        }
    }

    /// <summary>
    /// Парсит строку "Chip is ESP32-D0WD-V3 (revision v3.1)" → ESP32.
    /// Поддерживает ESP32, ESP32-S2, ESP32-S3, ESP32-C3, ESP32-C5, ESP32-C6, ESP32-H2.
    /// </summary>
    public static ChipFamily ParseChipFromOutput(string output)
    {
        if (string.IsNullOrWhiteSpace(output)) return ChipFamily.Unknown;

        var m = Regex.Match(output, @"Chip is\s+ESP32[-\w]*", RegexOptions.IgnoreCase);
        if (!m.Success) return ChipFamily.Unknown;

        var chipText = m.Value.ToUpperInvariant();
        if (chipText.Contains("ESP32-S2")) return ChipFamily.ESP32_S2;
        if (chipText.Contains("ESP32-S3")) return ChipFamily.ESP32_S3;
        if (chipText.Contains("ESP32-C6")) return ChipFamily.ESP32_C6;
        if (chipText.Contains("ESP32-C5")) return ChipFamily.ESP32_C5;
        if (chipText.Contains("ESP32-C3")) return ChipFamily.ESP32_C3;
        if (chipText.Contains("ESP32-H2")) return ChipFamily.ESP32_H2;
        if (chipText.Contains("ESP32"))    return ChipFamily.ESP32;
        return ChipFamily.Unknown;
    }

    private static string? ExtractChipName(string output)
    {
        var m = Regex.Match(output, @"Chip is\s+(\S+)");
        return m.Success ? m.Groups[1].Value : null;
    }

    private static string? ExtractMac(string output)
    {
        var m = Regex.Match(output, @"MAC:\s*([0-9a-fA-F:]{17})");
        return m.Success ? m.Groups[1].Value : null;
    }
}

/// <summary>
/// Результат определения чипа.
/// </summary>
public sealed record ChipDetectionResult(
    ChipFamily Family,
    string Diagnostic,
    string? ChipName = null,
    string? Mac = null)
{
    public bool Detected => Family != ChipFamily.Unknown;
}
