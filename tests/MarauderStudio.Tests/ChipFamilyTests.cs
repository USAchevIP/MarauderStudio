using MarauderStudio.Core.Devices;
using Xunit;

namespace MarauderStudio.Tests;

public class ChipFamilyTests
{
    [Theory]
    [InlineData(ChipFamily.ESP32,    "esp32")]
    [InlineData(ChipFamily.ESP32_S2, "esp32s2")]
    [InlineData(ChipFamily.ESP32_S3, "esp32s3")]
    [InlineData(ChipFamily.ESP32_C3, "esp32c3")]
    [InlineData(ChipFamily.ESP32_C5, "esp32c5")]
    [InlineData(ChipFamily.ESP32_C6, "esp32c6")]
    public void EsptoolArg_MatchesExpected(ChipFamily family, string expected)
    {
        Assert.Equal(expected, family.ToEsptoolArg());
    }

    [Theory]
    [InlineData(ChipFamily.ESP32_S3, "ESP32-S3")]
    [InlineData(ChipFamily.ESP32_C5, "ESP32-C5")]
    [InlineData(ChipFamily.Unknown,  "Unknown")]
    public void DisplayName_IsHumanReadable(ChipFamily family, string expected)
    {
        Assert.Equal(expected, family.DisplayName());
    }
}
