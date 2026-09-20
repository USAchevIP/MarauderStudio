using MarauderStudio.Core.Serial;
using Xunit;

namespace MarauderStudio.Tests;

public class MarauderProtocolTests
{
    private const string SampleInfo = """
        Firmware: Marauder
        Version: v1.17.0
        Hardware: Marauder v6.1
        ESP-IDF: v5.5.1-710-g8410210c9a
        WSL Bypass: enabled
        Station MAC: AA:BB:CC:DD:EE:FF
        AP MAC: 06:07:0D:09:0E:0D
        SD Card: Connected
        SD Card Size: 4096MB
        Battery Monitor: supported
        Battery Lvl: 87%
        """;

    [Fact]
    public void ParseInfo_ExtractsAllFields()
    {
        var info = MarauderProtocol.ParseInfo(SampleInfo);

        Assert.Equal("v1.17.0", info.FirmwareVersion);
        Assert.Equal("Marauder v6.1", info.Hardware);
        Assert.Equal("v5.5.1-710-g8410210c9a", info.EspIdfVersion);
        Assert.Equal("AA:BB:CC:DD:EE:FF", info.StationMac);
        Assert.Equal("06:07:0D:09:0E:0D", info.ApMac);
        Assert.True(info.SdConnected);
        Assert.Equal(4096, info.SdSizeMb);
        Assert.True(info.WslBypass);
        Assert.True(info.BatteryMonitor);
        Assert.Equal(87, info.BatteryPercent);
    }

    [Fact]
    public void ParseInfo_HandlesMissingSdAndBattery()
    {
        const string sample = """
            Firmware: Marauder
            Version: v1.16.0
            Hardware: Marauder Mini
            ESP-IDF: v5.5.1
            WSL Bypass: disabled
            Station MAC: 11:22:33:44:55:66
            AP MAC: 06:07:0D:09:0E:0D
            SD Card: Not Connected
            SD Card Size: 0MB
            Battery Monitor: not supported
            """;
        var info = MarauderProtocol.ParseInfo(sample);

        Assert.Equal("v1.16.0", info.FirmwareVersion);
        Assert.Equal("Marauder Mini", info.Hardware);
        Assert.False(info.SdConnected);
        Assert.Equal(0, info.SdSizeMb);
        Assert.False(info.WslBypass);
        Assert.False(info.BatteryMonitor);
        Assert.Equal(0, info.BatteryPercent);
    }

    [Fact]
    public void ParseInfo_OnEmpty_ReturnsEmpty()
    {
        var info = MarauderProtocol.ParseInfo(string.Empty);
        Assert.Equal(MarauderInfo.Empty, info);
        Assert.False(info.SdConnected);
    }

    [Fact]
    public void ParseInfo_OnGarbage_ReturnsEmpty()
    {
        var info = MarauderProtocol.ParseInfo("some random text\r\nno fields here\r\n");
        Assert.Equal(string.Empty, info.FirmwareVersion);
        Assert.Equal(string.Empty, info.Hardware);
        Assert.False(info.SdConnected);
    }

    [Fact]
    public void ParseInfo_TrimsWhitespace()
    {
        const string sample = """
              Version:   v1.17.0
              Hardware:    Marauder v7
            """;
        var info = MarauderProtocol.ParseInfo(sample);
        Assert.Equal("v1.17.0", info.FirmwareVersion);
        Assert.Equal("Marauder v7", info.Hardware);
    }
}
