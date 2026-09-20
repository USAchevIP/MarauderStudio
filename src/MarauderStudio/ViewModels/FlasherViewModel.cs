using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MarauderStudio.Core.Common;
using MarauderStudio.Core.Devices;
using MarauderStudio.Core.Firmware;
using MarauderStudio.Core.Flashing;

namespace MarauderStudio.ViewModels;

/// <summary>
/// ViewModel экрана прошивки. Поддерживает три источника .bin (файл, GitHub, профиль)
/// и три метода (esptool USB, Web OTA, SD).
/// </summary>
public sealed partial class FlasherViewModel : ObservableObject, IDisposable
{
    private readonly GitHubReleasesClient _gh;
    private readonly SettingsService _settings;
    private CancellationTokenSource? _cts;

    public ObservableCollection<MarauderRelease> Releases { get; } = new();
    public ObservableCollection<MarauderAsset> AvailableAssets { get; } = new();
    public ObservableCollection<BoardProfile> BoardProfiles { get; } = new(BoardDatabase.All);
    public ObservableCollection<LogLine> Log { get; } = new();

    // Selected firmware source
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
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private bool _isFlashing;
    [ObservableProperty] private int _progressPercent;
    [ObservableProperty] private string _stage = "idle";
    [ObservableProperty] private string _statusMessage = "Готов";

    public FlasherViewModel(GitHubReleasesClient gh, SettingsService settings)
    {
        _gh = gh;
        _settings = settings;
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
                new Progress<double>(p => DownloadProgress = p * 100), default);
            FilePath = dest;
            var fi = new FileInfo(dest);
            FileSize = fi.Length;
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
        StatusMessage = $"Файл: {Path.GetFileName(path)}";
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

        var settings = _settings.Load();
        var esptool = EsptoolResolver.Resolve(settings.EsptoolPath);
        if (!esptool.Available)
        {
            StatusMessage = "esptool не найден. Укажите путь в Настройках или установите esptool в Python.";
            return;
        }

        IsFlashing = true;
        ProgressPercent = 0;
        Stage = "init";
        Log.Clear();
        _cts = new CancellationTokenSource();

        try
        {
            // Для простоты: методы Usb / Ota / Sd выбирают orchestrator.
            if (SelectedMethod == FlashMethod.Usb)
            {
                await FlashUsbAsync(esptool, settings, _cts.Token);
            }
            else if (SelectedMethod == FlashMethod.Ota)
            {
                StatusMessage = "Web OTA — реализация в следующих этапах";
                // Здесь будет WebOtaUploader (Этап 11).
            }
            else
            {
                StatusMessage = "SD Update: скопируйте файл как update.bin в корень SD, выполните 'update -s'";
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
        }
    }

    private async Task FlashUsbAsync(EsptoolResolution esptool, AppSettings settings, CancellationToken ct)
    {
        // Сейчас нет подключённого устройства из DashboardViewModel — оставим заглушку с инструкцией.
        Log.Add(new LogLine(DateTime.Now, "INFO", $"Используем {esptool.Description}"));
        Log.Add(new LogLine(DateTime.Now, "INFO", $"Файл: {FilePath}"));

        var meta = FirmwareMetadata.TryParse(FilePath!);
        if (meta is not null)
        {
            Log.Add(new LogLine(DateTime.Now, "INFO",
                $"Метаданные: {meta.Hardware} / {meta.Chip}"));
        }
        else
        {
            Log.Add(new LogLine(DateTime.Now, "WARN",
                "Метаданные MRDRFWID не найдены — файл может быть нестандартным"));
        }

        StatusMessage = "Подключите устройство и выберите его на вкладке Главная";
        Log.Add(new LogLine(DateTime.Now, "INFO",
            "Используйте Главную для подключения по USB — после подключения нажмите FLASH здесь"));
    }

    [RelayCommand]
    private void Cancel()
    {
        _cts?.Cancel();
    }

    public void Dispose()
    {
        _cts?.Cancel();
        _cts?.Dispose();
    }
}

public enum FlashMethod { Usb, Ota, Sd }
public enum FlashMode   { ApplicationOnly, Full }

public sealed record LogLine(DateTime Timestamp, string Level, string Message)
{
    public string Display => $"[{Timestamp:HH:mm:ss}] {Level,-5} {Message}";
}
