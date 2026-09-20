using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace MarauderStudio.Core.Flashing;

/// <summary>
/// Запуск esptool.exe и парсинг его вывода.
/// Поддерживает прямой бинарник и запуск через python (prefixArgs = ["-m", "esptool"]).
/// </summary>
public sealed class EsptoolRunner
{
    private readonly string _esptoolPath;
    private readonly string[] _prefixArgs;

    public EsptoolRunner(string esptoolPath, string[]? prefixArgs = null)
    {
        _esptoolPath = esptoolPath;
        _prefixArgs = prefixArgs ?? Array.Empty<string>();
    }

    public string EsptoolPath => _esptoolPath;

    /// <summary>Запустить с произвольными аргументами. Возвращает (exitCode, combinedOutput).</summary>
    public async Task<(int ExitCode, string Output)> RunAsync(
        IEnumerable<string> args,
        IProgress<EsptoolProgress>? progress = null,
        CancellationToken ct = default,
        IDictionary<string, string?>? environmentOverrides = null)
    {
        var psi = new ProcessStartInfo
        {
            FileName = _esptoolPath,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
        };
        foreach (var a in _prefixArgs)
            psi.ArgumentList.Add(a);
        foreach (var a in args)
            psi.ArgumentList.Add(a);
        if (environmentOverrides is not null)
            foreach (var (k, v) in environmentOverrides)
                psi.Environment[k] = v;

        using var proc = new Process { StartInfo = psi, EnableRaisingEvents = true };
        var stdout = new StringBuilder();
        var stderr = new StringBuilder();
        var stdoutLock = new object();
        var stderrLock = new object();

        proc.OutputDataReceived += (_, e) =>
        {
            if (e.Data is null) return;
            lock (stdoutLock) stdout.AppendLine(e.Data);
            progress?.Report(ParseProgressLine(e.Data));
        };
        proc.ErrorDataReceived += (_, e) =>
        {
            if (e.Data is null) return;
            lock (stderrLock) stderr.AppendLine(e.Data);
            progress?.Report(ParseProgressLine(e.Data));
        };

        proc.Start();
        proc.BeginOutputReadLine();
        proc.BeginErrorReadLine();

        try
        {
            await proc.WaitForExitAsync(ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            try { if (!proc.HasExited) proc.Kill(true); } catch { /* ignore */ }
            throw;
        }

        var combined = stdout.ToString() + stderr.ToString();
        return (proc.ExitCode, combined);
    }

    /// <summary>
    /// Парсит строку вида "Writing at 0x00001000... (1 %)" → EsptoolProgress(percent, stage).
    /// </summary>
    private static readonly Regex RxProgress =
        new(@"\(\s*(\d{1,3})\s*%\s*\)", RegexOptions.Compiled);

    public static EsptoolProgress ParseProgressLine(string line)
    {
        if (string.IsNullOrEmpty(line)) return EsptoolProgress.Empty;

        var lower = line.ToLowerInvariant();
        string stage;
        if (lower.Contains("chip is") || lower.Contains("chip_id"))
            stage = "detect";
        else if (lower.Contains("eras"))               // erasing / erase flash
            stage = "erase";
        else if (lower.Contains("writ"))               // writing / write at
            stage = "write";
        else if (lower.Contains("verif"))              // verifying / verified
            stage = "verify";
        else if (lower.Contains("compressed"))         // compressed 1048576 bytes to...
            stage = "write";
        else if (lower.Contains("hash of data"))       // hash of data verified
            stage = "verify";
        else if (lower.Contains("connecting"))
            stage = "connect";
        else
            stage = "log";

        var m = RxProgress.Match(line);
        int percent = m.Success ? Math.Clamp(int.Parse(m.Groups[1].Value), 0, 100) : 0;

        return new EsptoolProgress(percent, stage, line);
    }
}

/// <summary>
/// Прогресс выполнения esptool-команды.
/// </summary>
public sealed record EsptoolProgress(int Percent, string Stage, string Line)
{
    public static EsptoolProgress Empty { get; } = new(0, "idle", string.Empty);
}
