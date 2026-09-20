using MarauderStudio.Core.Devices;
using MarauderStudio.Core.Firmware;

namespace MarauderStudio.Core.Flashing;

/// <summary>
/// Параметры прошивки.
/// </summary>
public sealed record FlashRequest(
    string Port,
    ChipFamily Chip,
    string AppBinPath,
    IReadOnlyList<FlashSegment>? FullFlashSegments,
    FlashEraseMode EraseMode = FlashEraseMode.None,
    int Baud = 921_600)
{
    public bool IsFullFlash => FullFlashSegments is { Count: > 0 };
}

public sealed record FlashSegment(int Offset, string FilePath);

public enum FlashEraseMode
{
    None = 0,
    AppOnly,
    Full
}

/// <summary>
/// Прогресс прошивки.
/// </summary>
public sealed record FlashProgress(int Percent, string Stage, string LogLine)
{
    public static FlashProgress Idle { get; } = new(0, "idle", string.Empty);
}

/// <summary>
/// Координатор прошивки: проверка → erase → write → reboot.
/// </summary>
public sealed class FlashOrchestrator
{
    private readonly EsptoolRunner _runner;

    public FlashOrchestrator(EsptoolRunner runner)
    {
        _runner = runner;
    }

    public async Task<FlashOutcome> FlashAsync(FlashRequest request, IProgress<FlashProgress> progress, CancellationToken ct = default)
    {
        var log = new List<string>();

        try
        {
            // 1. Validate file
            progress.Report(new FlashProgress(0, "validate", $"Validating {request.AppBinPath}…"));
            if (!File.Exists(request.AppBinPath))
                return new FlashOutcome(false, "App bin not found", log);

            var meta = FirmwareMetadata.TryParse(request.AppBinPath);
            log.Add($"Firmware metadata: {meta?.Hardware ?? "(none)"} / {meta?.Chip ?? "(none)"}");

            // 2. Erase (optional)
            if (request.EraseMode != FlashEraseMode.None)
            {
                progress.Report(new FlashProgress(0, "erase", $"Erasing flash ({request.EraseMode})…"));
                var eraseArgs = new List<string> { "--port", request.Port, "--baud", request.Baud.ToString(), "--chip", request.Chip.ToEsptoolArg() };
                if (request.EraseMode == FlashEraseMode.Full) eraseArgs.AddRange(new[] { "erase_flash" });
                else eraseArgs.AddRange(new[] { "erase_region", "0x10000", "0x300000" });
                var (exitErase, outputErase) = await _runner.RunAsync(eraseArgs, ToRunnerProgress(progress, "erase"), ct).ConfigureAwait(false);
                log.Add(outputErase);
                if (exitErase != 0)
                    return new FlashOutcome(false, $"Erase failed (exit {exitErase})", log);
            }

            // 3. Write
            progress.Report(new FlashProgress(0, "write", "Writing flash…"));
            var writeArgs = new List<string>
            {
                "--port", request.Port,
                "--baud", request.Baud.ToString(),
                "--chip", request.Chip.ToEsptoolArg(),
                "--before", "default_reset",
                "--after", "hard_reset",
                "write_flash",
                "-z",
                "--flash_mode", "dio",
                "--flash_freq", "80m",
                "--flash_size", "detect"
            };

            if (request.IsFullFlash)
            {
                foreach (var seg in request.FullFlashSegments!)
                    writeArgs.Add(seg.Offset.ToString("X"));
                foreach (var seg in request.FullFlashSegments!)
                    writeArgs.Add(seg.FilePath);
            }
            else
            {
                // Application-only OTA update.
                writeArgs.Add("0x10000");
                writeArgs.Add(request.AppBinPath);
            }

            var (exitWrite, outputWrite) = await _runner.RunAsync(writeArgs, ToRunnerProgress(progress, "write"), ct).ConfigureAwait(false);
            log.Add(outputWrite);
            if (exitWrite != 0)
                return new FlashOutcome(false, $"Write failed (exit {exitWrite})", log);

            progress.Report(new FlashProgress(100, "done", "Flash completed"));
            return new FlashOutcome(true, "OK", log);
        }
        catch (OperationCanceledException)
        {
            return new FlashOutcome(false, "Cancelled", log);
        }
        catch (Exception ex)
        {
            log.Add($"Exception: {ex}");
            return new FlashOutcome(false, ex.Message, log);
        }
    }

    private static IProgress<EsptoolProgress> ToRunnerProgress(IProgress<FlashProgress> flashProgress, string stage)
        => new Progress<EsptoolProgress>(p => flashProgress.Report(new FlashProgress(p.Percent, stage, p.Line)));
}

/// <summary>
/// Результат прошивки.
/// </summary>
public sealed record FlashOutcome(bool Success, string Message, IReadOnlyList<string> Log);
