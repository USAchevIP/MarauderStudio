using System.Text.RegularExpressions;
using MarauderStudio.Core.Devices;

namespace MarauderStudio.Core.Serial;

/// <summary>
/// Структура ответа `info` Marauder.
/// </summary>
public sealed record MarauderInfo(
    string FirmwareVersion,
    string Hardware,
    string EspIdfVersion,
    string StationMac,
    string ApMac,
    bool SdConnected,
    int SdSizeMb,
    bool WslBypass,
    bool BatteryMonitor,
    int BatteryPercent,
    string RawOutput)
{
    public static MarauderInfo Empty { get; } = new(
        FirmwareVersion: string.Empty,
        Hardware: string.Empty,
        EspIdfVersion: string.Empty,
        StationMac: string.Empty,
        ApMac: string.Empty,
        SdConnected: false,
        SdSizeMb: 0,
        WslBypass: false,
        BatteryMonitor: false,
        BatteryPercent: 0,
        RawOutput: string.Empty);
}

/// <summary>
/// Парсер вывода команды `info` Marauder. Источник: WiFiScan.cpp::RunInfo().
/// </summary>
public static class MarauderProtocol
{
    // Примеры строк (см. исследование):
    // "Firmware: Marauder"
    // "Version: v1.17.0"
    // "Hardware: Marauder v6.1"
    // "ESP-IDF: v5.5.1-710-g8410210c9a"
    // "WSL Bypass: enabled"
    // "Station MAC: AA:BB:CC:DD:EE:FF"
    // "AP MAC: 06:07:0D:09:0E:0D"
    // "SD Card: Connected"
    // "SD Card Size: 4096MB"
    // "Battery Monitor: supported"
    // "Battery Lvl: 87%"

    private static readonly Regex RxVersion = new(@"^\s*Version:\s*(.+?)\s*$", RegexOptions.Compiled | RegexOptions.Multiline);
    private static readonly Regex RxHardware = new(@"^\s*Hardware:\s*(.+?)\s*$", RegexOptions.Compiled | RegexOptions.Multiline);
    private static readonly Regex RxIdf = new(@"^\s*ESP-IDF:\s*(.+?)\s*$", RegexOptions.Compiled | RegexOptions.Multiline);
    private static readonly Regex RxStaMac = new(@"^\s*Station MAC:\s*([0-9A-Fa-f:]{17})\s*$", RegexOptions.Compiled | RegexOptions.Multiline);
    private static readonly Regex RxApMac = new(@"^\s*AP MAC:\s*([0-9A-Fa-f:]{17})\s*$", RegexOptions.Compiled | RegexOptions.Multiline);
    private static readonly Regex RxSd = new(@"^\s*SD Card:\s*(Connected|Not Connected)\s*$", RegexOptions.Compiled | RegexOptions.Multiline);
    private static readonly Regex RxSdSize = new(@"^\s*SD Card Size:\s*(\d+)\s*MB\s*$", RegexOptions.Compiled | RegexOptions.Multiline);
    private static readonly Regex RxWsl = new(@"^\s*WSL Bypass:\s*(enabled|disabled)\s*$", RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.IgnoreCase);
    private static readonly Regex RxBat = new(@"^\s*Battery Monitor:\s*(supported|not supported)\s*$", RegexOptions.Compiled | RegexOptions.Multiline);
    private static readonly Regex RxBatLvl = new(@"^\s*Battery Lvl:\s*(\d{1,3})\s*%\s*$", RegexOptions.Compiled | RegexOptions.Multiline);

    /// <summary>
    /// Парсит многострочный вывод `info`. Возвращает Empty при отсутствии распознаваемых полей.
    /// </summary>
    public static MarauderInfo ParseInfo(string output)
    {
        if (string.IsNullOrWhiteSpace(output)) return MarauderInfo.Empty;

        var version = MatchGroup(RxVersion, output);
        var hardware = MatchGroup(RxHardware, output);
        var idf = MatchGroup(RxIdf, output);
        var sta = MatchGroup(RxStaMac, output);
        var ap = MatchGroup(RxApMac, output);
        var sdRaw = MatchGroup(RxSd, output);
        var sdSizeRaw = MatchGroup(RxSdSize, output);
        var wslRaw = MatchGroup(RxWsl, output);
        var batRaw = MatchGroup(RxBat, output);
        var batLvlRaw = MatchGroup(RxBatLvl, output);

        return new MarauderInfo(
            FirmwareVersion: version ?? string.Empty,
            Hardware: hardware ?? string.Empty,
            EspIdfVersion: idf ?? string.Empty,
            StationMac: sta ?? string.Empty,
            ApMac: ap ?? string.Empty,
            SdConnected: string.Equals(sdRaw, "Connected", StringComparison.OrdinalIgnoreCase),
            SdSizeMb: int.TryParse(sdSizeRaw, out var sz) ? sz : 0,
            WslBypass: string.Equals(wslRaw, "enabled", StringComparison.OrdinalIgnoreCase),
            BatteryMonitor: string.Equals(batRaw, "supported", StringComparison.OrdinalIgnoreCase),
            BatteryPercent: int.TryParse(batLvlRaw, out var bp) ? Math.Clamp(bp, 0, 100) : 0,
            RawOutput: output);
    }

    private static string? MatchGroup(Regex rx, string input)
    {
        var m = rx.Match(input);
        return m.Success ? m.Groups[1].Value.Trim() : null;
    }
}
