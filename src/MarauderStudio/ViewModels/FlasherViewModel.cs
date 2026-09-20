using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MarauderStudio.Core.Common;
using MarauderStudio.Core.Devices;
using MarauderStudio.Core.Firmware;
using MarauderStudio.Core.Flashing;
using MarauderStudio.Core.Serial;

namespace MarauderStudio.ViewModels;

/// <summary>
/// ViewModel экрана прошивки. Поддерживает три источника .bin (файл, GitHub, профиль)
/// и три метода (esptool USB, Web OTA, SD).
/// </summary>
public sealed partial class FlasherViewModel : ObservableObject
{
    private readonly GitHubReleasesClient _gh;
    private readonly SettingsService _settings;
    private readonly SerialPortService _serial;
    private CancellationTokenSource? _cts;

    public ObservableCollection<MarauderRelease> Releases { get; } = new();
    public ObservableCollection<MarauderAsset> AvailableAssets { get; } = new();
    public ObservableCollection<BoardProfile> BoardProfiles { get; } = new(BoardDatabase.All);
    public ObservableCollection<LogLine> Log { get; } = new();

    [ObservableProperty] private int _sourceTabIndex; // 0=File, 1=GitHub, 2=Profile
    [ObservableProperty] private string? _filePath;
    [ObservableProperty] private long _fileSize;

    // GitHub tab
    [ObservableProperty] private MarauderRelease? _selectedRelease;
    [ObservableProperty] private MarauderAsset? _selectedAsset;
    [ObservableProperty] private bool _isLoadingReleases;
    [ObservableProperty] private double _downloadProgress;

    // Profile tab
    [ObservableProperty] private BoardProfile? _selectedProfile;

    // Flash options
    [ObservableProperty] private FlashMethod _selectedMethod = FlashMethod.Usb;
    [ObservableProperty] private FlashMode _selectedMode = FlashMode.ApplicationOnly;
    [ObservableProperty] private FlashEraseMode _selectedErase = FlashEraseMode.None;

    // Live state
    [ObservableProperty] private bool _isFlashing;
    [ObservableProperty] private int _progressPercent;
    [ObservableProperty] private string _stage = "idle";
    [ObservableProperty] private string _statusMessage = "Готов";

    public FlasherViewModel(GitHubReleasesClient gh, SettingsService settings, SerialPortService serial)
    {
        _gh = gh;
        _settings = settings;
        _serial = serial;
    }

    [RelayCommand]
    private async Task LoadReleasesAsync()
    {
        if (IsLoadingReleases) return;
        IsLoadingReleases = true;
        StatusMessage = "Загрузка списка релизов…";
        try
        {
            var releases = await _gh.GetReleasesAsync(10);
            Releases.Clear();
            foreach (var r in releases) Releases.Add(r);
            if (Releases.Count > 0) SelectedRelease = Releases[0];
            StatusMessage = $"Загружено релизов: {Releases.Count}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Ошибка: {ex.Message}";
        }
        finally { IsLoadingReleases = false; }
    }

    partial void OnSelectedReleaseChanged(MarauderRelease? value)
    {
        AvailableAssets.Clear();
        if (value is null) return;
        foreach (var a in value.Assets.Where(a => a.Name.EndsWith(".bin")))
            AvailableAssets.Add(a);

        // Пытаемся предвыбрать .bin под выбранный профиль.
        if (SelectedProfile is not null)
            SelectedAsset = AvailableAssets.FirstOrDefault(a =>
                a.Name.Contains(SelectedProfile.BinarySuffix.Replace(".bin", "")));
    }

    partial void OnSelectedProfileChanged(BoardProfile? value)
    {
        if (value is null || SelectedRelease is null) return;
        SelectedAsset = AvailableAssets.FirstOrDefault(a =>
            a.Name.Contains(value.BinarySuffix.Replace(".bin", "")));
    }

    [RelayCommand]
    private async Task DownloadFirmwareAsync()
    {
        if (SelectedAsset is null)
        {
            StatusMessage = "Выберите бинарник";
            return;
        }
        var settings = _settings.Load();
        var dir = string.IsNullOrWhiteSpace(settings.DownloadDir)
            ? Path.Combine(Path.GetTempPath(), "MarauderStudioFirmware")
            : settings.DownloadDir;
        Directory.CreateDirectory(dir);

        var dest = Path.Combine(dir, SelectedAsset.Name);
        StatusMessage = $"Загрузка → {dest}";
        try
        {
            await _gh.DownloadAsync(SelectedAsset, dest,
                new Progress<double>(p => DownloadProgress = p * 100));
            FilePath = dest;
            var fi = new FileInfo(dest);
            FileSize = fi.Length;
            SourceTabIndex = 0; // переключаем на вкладку файла, где виден путь
            StatusMessage = $"Скачано: {Path.GetFileName(dest)} ({fi.Length / 1024} КБ)";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Ошибка загрузки: {ex.Message}";
        }
    }

    [RelayCommand]
    private void SetFile(string path)
    {
        if (string.IsNullOrEmpty(path) || !File.Exists(path)) return;
        FilePath = path;
        FileSize = new FileInfo(path).Length;
        StatusMessage = $"Файл: {Path.GetFileName(path)} ({FileSize / 1024} КБ)";
    }

    [RelayCommand]
    private async Task FlashAsync()
    {
        if (string.IsNullOrEmpty(FilePath) || !File.Exists(FilePath))
        {
            StatusMessage = "Выберите корректный .bin файл";
            return;
        }
        if (IsFlashing) return;

        switch (SelectedMethod)
        {
            case FlashMethod.Usb:
                await FlashUsbAsync();
                break;
            case FlashMethod.Ota:
                await FlashOtaAsync();
                break;
            case FlashMethod.Sd:
                StatusMessage = "SD: скопируйте файл как update.bin в корень SD, затем на устройстве выполните 'update -s'";
                Log.Add(new LogLine(DateTime.Now, "INFO", "SD Update: переименуйте .bin в update.bin и поместите в корень SD-карты."));
                Log.Add(new LogLine(DateTime.Now, "INFO", "Затем на устройстве: Device > Update Firmware > SD Update, или команда 'update -s'."));
                break;
        }
    }

    private async Task FlashUsbAsync()
    {
        if (_serial.IsOpen)
        {
            StatusMessage = "Порт занят монитором. Отключитесь на вкладке «Главная» и повторите.";
            Log.Add(new LogLine(DateTime.Now, "WARN", "esptool не может работать, пока COM-порт открыт приложением."));
            return;
        }

        var settings = _settings.Load();
        var esptool = EsptoolResolver.Resolve(settings.EsptoolPath);
        if (!esptool.Available)
        {
            StatusMessage = "esptool не найден. Укажите путь в Настройках или установите esptool в Python.";
            Log.Add(new LogLine(DateTime.Now, "ERROR", "esptool не найден."));
            return;
        }

        // Порт для прошивки: приоритет — выбранный на Dashboard, иначе первый доступный.
        var ports = PortEnumerator.GetPorts();
        if (ports.Count == 0)
        {
            StatusMessage = "COM-порты не найдены. Подключите устройство по USB.";
            return;
        }

        IsFlashing = true;
        ProgressPercent = 0;
        Stage = "init";
        _cts = new CancellationTokenSource();

        try
        {
            Log.Add(new LogLine(DateTime.Now, "INFO", $"esptool: {esptool.Description}"));
            Log.Add(new LogLine(DateTime.Now, "INFO", $"Файл: {FilePath}"));

            var meta = FirmwareMetadata.TryParse(FilePath!);
            if (meta is not null)
                Log.Add(new LogLine(DateTime.Now, "INFO", $"Метаданные: {meta.Hardware} / {meta.Chip}"));
            else
                Log.Add(new LogLine(DateTime.Now, "WARN", "Метаданные MRDRFWID не найдены — прошивка без валидации платы."));

            var request = new FlashRequest(
                Port: ports[0].Port,
                Chip: ChipFamily.Unknown, // esptool определит сам (--chip auto)
                AppBinPath: FilePath!,
                FullFlashSegments: null,
                EraseMode: SelectedErase);

            // Для системного Python esptool запускается как "python -m esptool …".
            var prefixArgs = esptool.Source == EsptoolSource.SystemPython
                ? new[] { "-m", "esptool" }
                : null;
            var runner = new EsptoolRunner(esptool.Path, prefixArgs);

            var orchestrator = new FlashOrchestrator(runner);
            var progress = new Progress<FlashProgress>(p =>
            {
                ProgressPercent = p.Percent;
                Stage = p.Stage;
                if (!string.IsNullOrWhiteSpace(p.LogLine))
                    Log.Add(new LogLine(DateTime.Now, "INFO", p.LogLine));
            });

            StatusMessage = "Прошивка…";
            var result = await orchestrator.FlashAsync(request, progress, _cts.Token);
            foreach (var line in result.Log.TakeLast(15))
                Log.Add(new LogLine(DateTime.Now, result.Success ? "INFO" : "ERROR", line));

            if (result.Success)
            {
                ProgressPercent = 100;
                Stage = "done";
                StatusMessage = "Готово ✓";
                Log.Add(new LogLine(DateTime.Now, "INFO", "Прошивка успешно завершена."));
            }
            else
            {
                Stage = "failed";
                StatusMessage = $"Ошибка: {result.Message}";
            }
        }
        catch (Exception ex)
        {
            Log.Add(new LogLine(DateTime.Now, "ERROR", ex.Message));
            StatusMessage = $"Ошибка: {ex.Message}";
        }
        finally
        {
            IsFlashing = false;
            _cts?.Dispose();
            _cts = null;
        }
    }

    private async Task FlashOtaAsync()
    {
        var settings = _settings.Load();
        var ssid = settings.OtaSsid;

        if (!WifiConnector.IsConnected(ssid))
        {
            StatusMessage = $"Подключитесь к WiFi «{ssid}» (пароль: {settings.OtaPassword}) и повторите.";
            Log.Add(new LogLine(DateTime.Now, "WARN", $"WiFi «{ssid}» не подключён."));
            Log.Add(new LogLine(DateTime.Now, "INFO", "На устройстве: Device > Update Firmware > Web Update, затем подключитесь к AP MarauderOTA."));
            return;
        }

        IsFlashing = true;
        ProgressPercent = 0;
        Stage = "ota";
        try
        {
            var uploader = new WebOtaUploader();
            var progress = new Progress<double>(p =>
            {
                ProgressPercent = (int)(p * 100);
            });
            StatusMessage = "Загрузка на устройство по OTA…";
            var result = await uploader.UploadAsync(FilePath!, progress);
            if (result.Success)
            {
                ProgressPercent = 100;
                Stage = "done";
                StatusMessage = "Готово ✓ (устройство перезагружается)";
                Log.Add(new LogLine(DateTime.Now, "INFO", "Web OTA: прошивка принята, устройство перезагружается."));
            }
            else
            {
                Stage = "failed";
                StatusMessage = $"OTA ошибка: {result.Message}";
                Log.Add(new LogLine(DateTime.Now, "ERROR", result.Message));
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Ошибка: {ex.Message}";
        }
        finally
        {
            IsFlashing = false;
        }
    }

    [RelayCommand]
    private void Cancel()
    {
        _cts?.Cancel();
        StatusMessage = "Отмена…";
    }
}

public enum FlashMethod { Usb, Ota, Sd }
public enum FlashMode   { ApplicationOnly, Full }

public sealed record LogLine(DateTime Timestamp, string Level, string Message)
{
    public string Display => $"[{Timestamp:HH:mm:ss}] {Level,-5} {Message}";
}
