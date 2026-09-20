using MarauderStudio.Core.Devices;
using MarauderStudio.Core.Firmware;
using Xunit;

namespace MarauderStudio.Tests;

public class BoardDatabaseTests
{
    [Theory]
    [InlineData("Marauder v4",       "_old_hardware.bin",           ChipFamily.ESP32,    0x1000)]
    [InlineData("Marauder v6.1",     "_v6_1.bin",                   ChipFamily.ESP32,    0x1000)]
    [InlineData("Marauder v7",       "_marauder_v7.bin",            ChipFamily.ESP32,    0x1000)]
    [InlineData("Marauder Mini",     "_mini.bin",                   ChipFamily.ESP32,    0x1000)]
    [InlineData("Marauder v8",       "_v8.bin",                     ChipFamily.ESP32_C5, 0x2000)]
    [InlineData("Marauder Mini v3",  "_mini_v3.bin",                ChipFamily.ESP32_C5, 0x2000)]
    [InlineData("LilyGo T-Dongle C5","_t_dongle_c5.bin",             ChipFamily.ESP32_C5, 0x2000)]
    [InlineData("Flipper Zero Dev Board", "_flipper.bin",            ChipFamily.ESP32_S2, 0x1000)]
    [InlineData("M5 Cardputer",      "_m5cardputer.bin",             ChipFamily.ESP32_S3, 0x1000)]
    [InlineData("M5 Nano C6",        "_m5nanoc6.bin",                ChipFamily.ESP32_C6, 0x2000)]
    public void FindByHardware_ReturnsExpected(string hardware, string expectedSuffix, ChipFamily expectedChip, int expectedBootOffset)
    {
        var profile = BoardDatabase.FindByHardware(hardware);
        Assert.NotNull(profile);
        Assert.Equal(expectedSuffix, profile!.BinarySuffix);
        Assert.Equal(expectedChip, profile.Chip);
        Assert.Equal(expectedBootOffset, profile.BootloaderOffset);
    }

    [Fact]
    public void FindByHardware_CaseInsensitive()
    {
        var profile = BoardDatabase.FindByHardware("marauder V6.1");
        Assert.NotNull(profile);
        Assert.Equal("_v6_1.bin", profile!.BinarySuffix);
    }

    [Fact]
    public void FindByHardware_Unknown_ReturnsNull()
    {
        Assert.Null(BoardDatabase.FindByHardware("Totally Made Up Board"));
        Assert.Null(BoardDatabase.FindByHardware(""));
        Assert.Null(BoardDatabase.FindByHardware(null));
    }

    [Fact]
    public void ForChip_FiltersCorrectly()
    {
        var c5 = BoardDatabase.ForChip(ChipFamily.ESP32_C5);
        Assert.NotEmpty(c5);
        Assert.All(c5, p => Assert.Equal(ChipFamily.ESP32_C5, p.Chip));
        Assert.Contains(c5, p => p.HardwareName == "Marauder v8");
        Assert.Contains(c5, p => p.HardwareName == "LilyGo T-Dongle C5");
    }

    [Fact]
    public void All_CoversAtLeastTwentyBoards()
    {
        Assert.True(BoardDatabase.All.Count >= 20,
            $"Expected ≥20 board profiles, got {BoardDatabase.All.Count}");
    }

    [Fact]
    public void FindBySuffix_LooksUpCorrectly()
    {
        var profile = BoardDatabase.FindBySuffix("_v6_1.bin");
        Assert.NotNull(profile);
        Assert.Equal("Marauder v6.1", profile!.HardwareName);
    }

    [Theory]
    [InlineData("Chip is ESP32-D0WD-V3 (revision v3.1)", ChipFamily.ESP32)]
    [InlineData("Chip is ESP32-S2 (revision v0.0)",       ChipFamily.ESP32_S2)]
    [InlineData("Chip is ESP32-S3 (revision v0.2)",       ChipFamily.ESP32_S3)]
    [InlineData("Chip is ESP32-C3 (revision v0.4)",       ChipFamily.ESP32_C3)]
    [InlineData("Chip is ESP32-C5 (revision v0.1)",       ChipFamily.ESP32_C5)]
    [InlineData("Chip is ESP32-C6 (revision v0.0)",       ChipFamily.ESP32_C6)]
    [InlineData("Crystal is 40MHz\nChip is ESP32-D0WD-V3", ChipFamily.ESP32)]
    [InlineData("random text no chip",                     ChipFamily.Unknown)]
    public void ParseChipFromOutput_RecognisesFamily(string stdout, ChipFamily expected)
    {
        Assert.Equal(expected, ChipDetector.ParseChipFromOutput(stdout));
    }
}
