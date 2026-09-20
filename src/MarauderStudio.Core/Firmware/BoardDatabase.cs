using MarauderStudio.Core.Devices;

namespace MarauderStudio.Core.Firmware;

/// <summary>
/// База данных плат Marauder. Источник: configs.h + Release Bins репозитория.
/// </summary>
public static class BoardDatabase
{
    private const int BootOffsetEsp32 = 0x1000;
    private const int BootOffsetC5C6  = 0x2000;

    private static readonly BoardProfile[] _all = new[]
    {
        // ESP32 classic
        new BoardProfile("Marauder v4",          ChipFamily.ESP32,    "_old_hardware.bin",            BootOffsetEsp32, 0x8000, 0xE000, 0x10000, 4,  "MARAUDER_V4"),
        new BoardProfile("Marauder v6",          ChipFamily.ESP32,    "_v6.bin",                      BootOffsetEsp32, 0x8000, 0xE000, 0x10000, 4,  "MARAUDER_V6"),
        new BoardProfile("Marauder v6.1",        ChipFamily.ESP32,    "_v6_1.bin",                    BootOffsetEsp32, 0x8000, 0xE000, 0x10000, 4,  "MARAUDER_V6_1"),
        new BoardProfile("Marauder v7",          ChipFamily.ESP32,    "_marauder_v7.bin",             BootOffsetEsp32, 0x8000, 0xE000, 0x10000, 4,  "MARAUDER_V7"),
        new BoardProfile("Marauder v7.1",        ChipFamily.ESP32,    "_marauder_v7.bin",             BootOffsetEsp32, 0x8000, 0xE000, 0x10000, 4,  "MARAUDER_V7_1"),
        new BoardProfile("Marauder Mini",        ChipFamily.ESP32,    "_mini.bin",                    BootOffsetEsp32, 0x8000, 0xE000, 0x10000, 4,  "MARAUDER_MINI"),
        new BoardProfile("Marauder Kit",         ChipFamily.ESP32,    "_kit.bin",                     BootOffsetEsp32, 0x8000, 0xE000, 0x10000, 4,  "MARAUDER_KIT"),
        new BoardProfile("Marauder Dev Board Pro", ChipFamily.ESP32,  "_marauder_dev_board_pro.bin",  BootOffsetEsp32, 0x8000, 0xE000, 0x10000, 4,  "MARAUDER_DEV_BOARD_PRO"),
        new BoardProfile("ESP32 LDDB",           ChipFamily.ESP32,    "_esp32_lddb.bin",              BootOffsetEsp32, 0x8000, 0xE000, 0x10000, 4,  "ESP32_LDDB"),
        new BoardProfile("M5Stick-C Plus",       ChipFamily.ESP32,    "_m5stickc_plus.bin",           BootOffsetEsp32, 0x8000, 0xE000, 0x10000, 4,  "MARAUDER_M5STICKC"),
        new BoardProfile("M5Stick-C Plus2",      ChipFamily.ESP32,    "_m5stickc_plus2.bin",          BootOffsetEsp32, 0x8000, 0xE000, 0x10000, 4,  "MARAUDER_M5STICKCP2"),
        new BoardProfile("CYD 2432S028",         ChipFamily.ESP32,    "_cyd_2432S028.bin",            BootOffsetEsp32, 0x8000, 0xE000, 0x10000, 4,  "MARAUDER_CYD_MICRO"),
        new BoardProfile("CYD 2432S028 2USB",    ChipFamily.ESP32,    "_cyd_2432S028_2usb.bin",       BootOffsetEsp32, 0x8000, 0xE000, 0x10000, 4,  "MARAUDER_CYD_2USB"),
        new BoardProfile("CYD 3.5inch",          ChipFamily.ESP32,    "_cyd_3_5_inch.bin",            BootOffsetEsp32, 0x8000, 0xE000, 0x10000, 4,  "MARAUDER_CYD_3_5_INCH"),
        new BoardProfile("CYD 2432S024 GUITION", ChipFamily.ESP32,    "_cyd_2432S024_guition.bin",    BootOffsetEsp32, 0x8000, 0xE000, 0x10000, 4,  "MARAUDER_CYD_GUITION"),

        // ESP32-S2
        new BoardProfile("Adafruit Feather ESP32-S2 Reverse TFT", ChipFamily.ESP32_S2, "_rev_feather.bin", BootOffsetEsp32, 0x8000, 0xE000, 0x10000, 4, "MARAUDER_REV_FEATHER"),
        new BoardProfile("Flipper Zero Dev Board", ChipFamily.ESP32_S2, "_flipper.bin",           BootOffsetEsp32, 0x8000, 0xE000, 0x10000, 4, "MARAUDER_FLIPPER"),

        // ESP32-S3
        new BoardProfile("Flipper Zero Multi Board S3", ChipFamily.ESP32_S3, "_multiboardS3.bin",   BootOffsetEsp32, 0x8000, 0xE000, 0x10000, 8, "MARAUDER_MULTIBOARD_S3"),
        new BoardProfile("M5 Cardputer",              ChipFamily.ESP32_S3, "_m5cardputer.bin",           BootOffsetEsp32, 0x8000, 0xE000, 0x10000, 8, "MARAUDER_CARDPUTER"),
        new BoardProfile("M5 Cardputer ADV",         ChipFamily.ESP32_S3, "_m5cardputer_adv.bin",       BootOffsetEsp32, 0x8000, 0xE000, 0x10000, 8, "MARAUDER_CARDPUTER_ADV"),
        new BoardProfile("XIAO ESP32 S3",            ChipFamily.ESP32_S3, "_xiao_esp32_s3.bin",         BootOffsetEsp32, 0x8000, 0xE000, 0x10000, 8, "XIAO_ESP32_S3"),

        // ESP32-C5
        new BoardProfile("Marauder v8",          ChipFamily.ESP32_C5,  "_v8.bin",                 BootOffsetC5C6, 0x8000, 0xE000, 0x10000, 8,  "MARAUDER_V8"),
        new BoardProfile("Marauder Mini v3",     ChipFamily.ESP32_C5,  "_mini_v3.bin",            BootOffsetC5C6, 0x8000, 0xE000, 0x10000, 8,  "MARAUDER_MINI_V3"),
        new BoardProfile("Dual Mini C5",         ChipFamily.ESP32_C5,  "_dual_mini_c5.bin",       BootOffsetC5C6, 0x8000, 0xE000, 0x10000, 8,  "DUAL_MINI_C5"),
        new BoardProfile("Pancake Marauder V8",  ChipFamily.ESP32_C5,  "_pancake.bin",            BootOffsetC5C6, 0x8000, 0xE000, 0x10000, 8,  "MARAUDER_PANCAKE"),
        new BoardProfile("ESP32-C5 DevKit",      ChipFamily.ESP32_C5,  "_esp32c5devkitc1.bin",    BootOffsetC5C6, 0x8000, 0xE000, 0x10000, 8,  "MARAUDER_C5"),
        new BoardProfile("LilyGo T-Dongle C5",   ChipFamily.ESP32_C5,  "_t_dongle_c5.bin",        BootOffsetC5C6, 0x8000, 0xE000, 0x10000, 16, "MARAUDER_T_DONGLE_C5"),

        // ESP32-C6
        new BoardProfile("M5 Nano C6",           ChipFamily.ESP32_C6,  "_m5nanoc6.bin",           BootOffsetC5C6, 0x8000, 0xE000, 0x10000, 4,  "MARAUDER_M5_NANO_C6")
    };

    public static IReadOnlyList<BoardProfile> All => _all;

    /// <summary>
    /// Поиск точного соответствия HardwareName.
    /// </summary>
    public static BoardProfile? FindByHardware(string? hardwareName)
    {
        if (string.IsNullOrWhiteSpace(hardwareName)) return null;
        return _all.FirstOrDefault(p =>
            string.Equals(p.HardwareName, hardwareName, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// По суффиксу бинарника (например "_v6_1.bin").
    /// </summary>
    public static BoardProfile? FindBySuffix(string? suffix)
    {
        if (string.IsNullOrWhiteSpace(suffix)) return null;
        return _all.FirstOrDefault(p =>
            string.Equals(p.BinarySuffix, suffix, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Все профили для указанного семейства чипов.
    /// </summary>
    public static IReadOnlyList<BoardProfile> ForChip(ChipFamily family)
        => _all.Where(p => p.Chip == family).ToList();
}
