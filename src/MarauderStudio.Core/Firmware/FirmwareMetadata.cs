using System.Text;

namespace MarauderStudio.Core.Firmware;

/// <summary>
/// Метаданные бинарника Marauder. Источник: FirmwareMetadata.cpp в исходниках Marauder.
/// Структура 80 байт:
///   uint8_t  magic[8]            = "MRDRFWID"
///   uint8_t  schema_version      = 0x01
///   uint8_t  reserved[3]
///   char     hardware[48]
///   char     chip[12]
///   uint8_t  end_magic[8]        = "DIWFRDRM"
/// </summary>
public sealed record FirmwareMetadata(
    int SchemaVersion,
    string Hardware,
    string Chip,
    long SizeBytes)
{
    /// <summary>
    /// Сигнатура поиска. Бинарник может иметь метаданные в любом месте файла
    /// (встроены после компиляции), поэтому сканируем потоком.
    /// </summary>
    private static readonly byte[] MagicStart  = Encoding.ASCII.GetBytes("MRDRFWID");
    private static readonly byte[] MagicEnd    = Encoding.ASCII.GetBytes("DIWFRDRM");
    private const int StructureSize = 80;

    /// <summary>
    /// Прочитать метаданные из .bin файла. Возвращает null, если сигнатура не найдена.
    /// </summary>
    public static FirmwareMetadata? TryParse(string binPath)
    {
        if (!File.Exists(binPath)) return null;
        var info = new FileInfo(binPath);
        if (info.Length < StructureSize + MagicStart.Length + MagicEnd.Length) return null;

        using var fs = File.OpenRead(binPath);
        return TryParse(fs, info.Length);
    }

    public static FirmwareMetadata? TryParse(Stream stream, long length)
    {
        if (length < StructureSize) return null;

        const int window = 64 * 1024;
        var buffer = new byte[window];
        long position = 0;
        int leftover = 0;

        while (position < length)
        {
            int toRead = (int)Math.Min(window - leftover, length - position);
            int read = stream.Read(buffer, leftover, toRead);
            if (read <= 0) break;

            int totalAvailable = leftover + read;
            int searchEnd = totalAvailable - MagicStart.Length;
            for (int i = 0; i <= searchEnd; i++)
            {
                if (!StartsWith(buffer, i, MagicStart)) continue;

                // Проверяем, что в файле достаточно байт после этой позиции.
                long absStart = position - leftover + i;
                if (absStart + StructureSize > length) continue;

                int schemaVersion = buffer[i + 8];
                if (schemaVersion != 1) continue;

                var hardware = ReadAscii(buffer, i + 12, 48);
                var chip     = ReadAscii(buffer, i + 60, 12);

                // Проверяем end_magic — он находится по абсолютному смещению absStart + 72.
                long endAbs = absStart + StructureSize - MagicEnd.Length;
                if (!VerifyEndMagic(stream, endAbs, length)) continue;

                return new FirmwareMetadata(schemaVersion, hardware, chip, length);
            }

            leftover = Math.Min(MagicStart.Length - 1, totalAvailable);
            if (leftover > 0)
            {
                Buffer.BlockCopy(buffer, totalAvailable - leftover, buffer, 0, leftover);
            }
            position += read;
        }
        return null;
    }

    private static bool VerifyEndMagic(Stream stream, long absolutePosition, long length)
    {
        if (absolutePosition < 0 || absolutePosition + MagicEnd.Length > length) return false;
        Span<byte> tail = stackalloc byte[MagicEnd.Length];
        stream.Position = absolutePosition;
        int read = stream.Read(tail);
        if (read != MagicEnd.Length) return false;
        for (int i = 0; i < MagicEnd.Length; i++)
            if (tail[i] != MagicEnd[i]) return false;
        return true;
    }

    private static bool StartsWith(byte[] buffer, int offset, byte[] needle)
    {
        if (offset + needle.Length > buffer.Length) return false;
        for (int i = 0; i < needle.Length; i++)
            if (buffer[offset + i] != needle[i]) return false;
        return true;
    }

    private static string ReadAscii(byte[] buffer, int offset, int length)
    {
        // Структура packed, читаем до первого NUL.
        var sb = new StringBuilder(length);
        for (int i = 0; i < length; i++)
        {
            byte b = buffer[offset + i];
            if (b == 0) break;
            sb.Append((char)b);
        }
        return sb.ToString();
    }
}
