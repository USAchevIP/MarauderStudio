using System.Text;
using MarauderStudio.Core.Firmware;
using Xunit;

namespace MarauderStudio.Tests;

public class FirmwareMetadataTests
{
    private static byte[] BuildFakeBin(string hardware, string chip, int fillerSize = 1024)
    {
        const int StructSize = 80;
        var buffer = new byte[fillerSize + StructSize];
        new Random(42).NextBytes(buffer);

        int structOffset = fillerSize;
        // Очищаем область структуры и пишем заново.
        Array.Clear(buffer, structOffset, StructSize);

        // magic
        var magic = Encoding.ASCII.GetBytes("MRDRFWID");
        Array.Copy(magic, 0, buffer, structOffset, 8);
        buffer[structOffset + 8] = 0x01; // schema_version = 1
        // reserved 3 bytes: zero (уже)
        // hardware[48]
        var hw = Encoding.ASCII.GetBytes(hardware);
        int hwLen = Math.Min(hw.Length, 48);
        Array.Copy(hw, 0, buffer, structOffset + 12, hwLen);
        // chip[12]
        var ch = Encoding.ASCII.GetBytes(chip);
        int chLen = Math.Min(ch.Length, 12);
        Array.Copy(ch, 0, buffer, structOffset + 60, chLen);
        // end_magic at structOffset + 72
        var end = Encoding.ASCII.GetBytes("DIWFRDRM");
        Array.Copy(end, 0, buffer, structOffset + 72, 8);

        return buffer;
    }

    [Fact]
    public void TryParse_FindsMetadataAtEnd()
    {
        var path = Path.GetTempFileName();
        try
        {
            File.WriteAllBytes(path, BuildFakeBin("Marauder v6.1", "esp32"));
            var meta = FirmwareMetadata.TryParse(path);
            Assert.NotNull(meta);
            Assert.Equal("Marauder v6.1", meta!.Hardware);
            Assert.Equal("esp32", meta.Chip);
            Assert.Equal(1, meta.SchemaVersion);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void TryParse_FindsMetadataInMiddle()
    {
        var path = Path.GetTempFileName();
        try
        {
            var bytes = BuildFakeBin("Marauder v8", "esp32c5", fillerSize: 4096);
            File.WriteAllBytes(path, bytes);
            var meta = FirmwareMetadata.TryParse(path);
            Assert.NotNull(meta);
            Assert.Equal("Marauder v8", meta!.Hardware);
            Assert.Equal("esp32c5", meta.Chip);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void TryParse_ReturnsNull_OnMissingSignature()
    {
        var path = Path.GetTempFileName();
        try
        {
            File.WriteAllBytes(path, new byte[2048]); // пустой файл
            Assert.Null(FirmwareMetadata.TryParse(path));
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void TryParse_ReturnsNull_OnCorruptedEndMagic()
    {
        var path = Path.GetTempFileName();
        try
        {
            var bytes = BuildFakeBin("Marauder Mini", "esp32");
            // Портим end magic.
            bytes[bytes.Length - 8] = 0x00;
            File.WriteAllBytes(path, bytes);
            Assert.Null(FirmwareMetadata.TryParse(path));
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void TryParse_ReturnsNull_OnMissingFile()
    {
        Assert.Null(FirmwareMetadata.TryParse(@"C:\nonexistent\nope.bin"));
    }
}
