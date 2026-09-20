using System.Diagnostics;
using System.IO;

namespace MarauderStudio.Core.Flashing;

/// <summary>
/// Резолвер esptool.exe: bundled → системный python -m esptool → путь из настроек.
/// </summary>
public static class EsptoolResolver
{
    /// <summary>
    /// Находит путь к esptool.exe или к python, через который можно запустить esptool.
    /// Возвращает описание источника для UI.
    /// </summary>
    public static EsptoolResolution Resolve(string? userPath = null)
    {
        // 1. Явный путь пользователя
        if (!string.IsNullOrWhiteSpace(userPath) && File.Exists(userPath))
            return new EsptoolResolution(EsptoolSource.UserPath, userPath);

        // 2. Bundled рядом с .exe (MarauderStudio/Resources/esptool/esptool.exe)
        var bundledPath = FindBundled();
        if (bundledPath is not null)
            return new EsptoolResolution(EsptoolSource.Bundled, bundledPath);

        // 3. Системный python с установленным esptool
        var pythonPath = FindPython();
        if (pythonPath is not null)
            return new EsptoolResolution(EsptoolSource.SystemPython, pythonPath);

        return new EsptoolResolution(EsptoolSource.NotFound, string.Empty);
    }

    private static string? FindBundled()
    {
        // Поиск от текущей директории вверх (для случая single-file publish).
        var dir = AppContext.BaseDirectory;
        for (int i = 0; i < 6 && dir is not null; i++)
        {
            var candidate = Path.Combine(dir, "Resources", "esptool", "esptool.exe");
            if (File.Exists(candidate)) return candidate;

            var altCandidate = Path.Combine(dir, "esptool.exe");
            if (File.Exists(altCandidate)) return altCandidate;

            dir = Path.GetDirectoryName(dir);
        }
        return null;
    }

    private static string? FindPython()
    {
        foreach (var cmd in new[] { "python", "python3", "py" })
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = cmd,
                    Arguments = "-c \"import esptool; print(esptool.__file__)\"",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                };
                using var p = Process.Start(psi);
                if (p is null) continue;
                if (!p.WaitForExit(2000)) { try { p.Kill(true); } catch { } continue; }
                if (p.ExitCode != 0) continue;
                var stdout = p.StandardOutput.ReadToEnd().Trim();
                if (!string.IsNullOrEmpty(stdout)) return cmd; // python.exe, esptool доступен через модуль
            }
            catch
            {
                // ignore — пробуем следующий
            }
        }
        return null;
    }
}

public enum EsptoolSource
{
    NotFound = 0,
    Bundled,
    UserPath,
    SystemPython
}

public sealed record EsptoolResolution(EsptoolSource Source, string Path)
{
    public bool Available => Source != EsptoolSource.NotFound;

    /// <summary>Команда для запуска (если python — отдельная логика).</summary>
    public string LaunchCommand => Source switch
    {
        EsptoolSource.SystemPython => Path, // python.exe
        _ => Path
    };

    public string Description => Source switch
    {
        EsptoolSource.Bundled       => "Bundled (рядом с .exe)",
        EsptoolSource.UserPath      => "Указан пользователем",
        EsptoolSource.SystemPython  => $"Системный {Path} (модуль esptool)",
        _ => "Не найден — укажите путь или установите esptool"
    };
}
