using MarauderStudio.Core.Devices;

namespace MarauderStudio.Core.Firmware;

/// <summary>
/// Профиль платы Marauder: имя → чип → суффикс бинарника → смещения для esptool.
/// </summary>
public sealed record BoardProfile(
    string HardwareName,
    ChipFamily Chip,
    string BinarySuffix,
    int BootloaderOffset,
    int PartitionOffset,
    int OtaDataOffset,
    int AppOffset,
    int FlashSizeMb,
    string BuildFlag)
{
    /// <summary>Полный URL бинарника для конкретного релиза.</summary>
    public string AssetName(string version, string date) =>
        $"esp32_marauder_{version}_{date}{BinarySuffix}.bin";

    public string BootloaderBinName(string version, string date) =>
        $"esp32_marauder_{version}_{date}_bootloader{BinarySuffix}.bin";

    public string PartitionsBinName(string version, string date) =>
        $"esp32_marauder_{version}_{date}_partitions{BinarySuffix}.bin";
}
