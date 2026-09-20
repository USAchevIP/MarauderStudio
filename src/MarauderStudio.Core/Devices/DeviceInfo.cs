namespace MarauderStudio.Core.Devices;

/// <summary>
/// Снимок информации об устройстве, полученный через `info` или esptool.
/// </summary>
public sealed record DeviceInfo(
    string Port,
    ChipFamily Chip,
    string FirmwareVersion,
    string Hardware,
    string EspIdfVersion,
    string StationMac,
    string ApMac,
    bool SdConnected,
    int SdSizeMb,
    DateTimeOffset CapturedAt
)
{
    public static DeviceInfo Empty(string port) => new(
        Port: port,
        Chip: ChipFamily.Unknown,
        FirmwareVersion: string.Empty,
        Hardware: string.Empty,
        EspIdfVersion: string.Empty,
        StationMac: string.Empty,
        ApMac: string.Empty,
        SdConnected: false,
        SdSizeMb: 0,
        CapturedAt: DateTimeOffset.Now);

    public bool IsEmpty =>
        string.IsNullOrEmpty(FirmwareVersion) &&
        string.IsNullOrEmpty(Hardware);
}
