using MarauderStudio.Core.Flashing;
using Xunit;

namespace MarauderStudio.Tests;

public class EsptoolProgressParseTests
{
    [Theory]
    [InlineData("Writing at 0x00001000... (1 %)", 1, "write")]
    [InlineData("Writing at 0x00010000... (50 %)", 50, "write")]
    [InlineData("Writing at 0x00001000... (100 %)", 100, "write")]
    [InlineData("Erasing flash...", 0, "erase")]
    [InlineData("Compressed 1048576 bytes to 421048...", 0, "write")]
    [InlineData("Verifying... (33 %)", 33, "verify")]
    [InlineData("Chip is ESP32-D0WD-V3 (revision v3.1)", 0, "detect")]
    [InlineData("random output", 0, "log")]
    public void ParseLine_ExtractsPercentAndStage(string line, int expectedPct, string expectedStage)
    {
        var p = EsptoolRunner.ParseProgressLine(line);
        Assert.Equal(expectedPct, p.Percent);
        Assert.Equal(expectedStage, p.Stage);
    }

    [Fact]
    public void ParseLine_EmptyInput_ReturnsIdle()
    {
        var p = EsptoolRunner.ParseProgressLine(string.Empty);
        Assert.Equal(0, p.Percent);
        Assert.Equal("idle", p.Stage);
    }
}
