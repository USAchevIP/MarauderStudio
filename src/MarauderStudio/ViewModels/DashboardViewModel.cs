using System.Collections.ObjectModel;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MarauderStudio.Core.Devices;
using MarauderStudio.Core.Firmware;
using MarauderStudio.Core.Serial;

namespace MarauderStudio.ViewModels;

/// <summary>
/// ViewModel главной страницы. Управляет списком портов, подключением и live-данными Marauder.
/// </summary>
public sealed partial class DashboardViewModel : ObservableObject, IDisposable
{
    private readonly SerialPortService _serial = new();
    private readonly DispatcherTimer _portScanner;
    private bool _disposed;

    public ObservableCollection<PortDescriptor> AvailablePorts { get; } = new();

    [ObservableProperty] private PortDescriptor? _selectedPort;
    [ObservableProperty] private bool _isConnected;
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string _statusMessage = "—";
    [ObservableProperty] private string _firmwareVersion = "—";
    [ObservableProperty] private string _hardware = "—";
    [ObservableProperty] private string _espIdfVersion = "—";
    [ObservableProperty] private string _stationMac = "—";
    [ObservableProperty] private string _apMac = "—";
    [ObservableProperty] private string _sdCard = "—";
    [ObservableProperty] private string _chipName = "—";
    [ObservableProperty] private string _battery = "—";
    [ObservableProperty] private bool _hasDeviceInfo;

    public DashboardViewModel()
    {
        _portScanner = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromSeconds(2)
        };
        _portScanner.Tick += (_, _) => RefreshPorts();
        _portScanner.Start();

        _serial.LineReceived    += OnSerialLine;
        _serial.PromptReceived  += OnSerialPrompt;
        _serial.Error           += OnSerialError;

        RefreshPorts();
    }

    public MarauderInfo? LastInfo { get; private set; }

    [RelayCommand]
    private void RefreshPorts()
    {
        var current = PortEnumerator.GetPortsSortedByUsb();
        var previouslySelected = SelectedPort?.Port;

        // Обновляем коллекцию без мерцания: добавляем новые, удаляем отсутствующие.
        for (int i = AvailablePorts.Count - 1; i >= 0; i--)
        {
            if (!current.Any(p => p.Port == AvailablePorts[i].Port))
                AvailablePorts.RemoveAt(i);
        }
        foreach (var p in current)
        {
            if (!AvailablePorts.Any(x => x.Port == p.Port))
                AvailablePorts.Add(p);
        }

        // Восстанавливаем выбор.
        if (!string.IsNullOrEmpty(previouslySelected))
        {
            SelectedPort = AvailablePorts.FirstOrDefault(p => p.Port == previouslySelected);
        }
        else if (SelectedPort is null && AvailablePorts.Count > 0)
        {
            SelectedPort = AvailablePorts[0];
        }
    }

    [RelayCommand]
    private async Task ConnectAsync()
    {
        if (SelectedPort is null)
        {
            StatusMessage = "Выберите COM-порт";
            return;
        }
        if (IsBusy) return;

        IsBusy = true;
        StatusMessage = $"Подключение к {SelectedPort.Port}…";
        try
        {
            await _serial.OpenAsync(SelectedPort.Port, 115200);
            IsConnected = true;
            StatusMessage = "Подключено";
            // Запрашиваем info.
            await RefreshInfoAsync();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Ошибка: {ex.Message}";
            IsConnected = false;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task DisconnectAsync()
    {
        if (!IsConnected) return;
        IsBusy = true;
        try
        {
            await _serial.CloseAsync();
            IsConnected = false;
            HasDeviceInfo = false;
            FirmwareVersion = "—";
            Hardware = "—";
            StationMac = "—";
            ApMac = "—";
            SdCard = "—";
            StatusMessage = "Отключено";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task RefreshInfoAsync()
    {
        if (!IsConnected) return;
        IsBusy = true;
        StatusMessage = "Запрос info…";
        try
        {
            var raw = await _serial.RequestInfoAsync(TimeSpan.FromSeconds(3));
            var info = MarauderProtocol.ParseInfo(raw);
            LastInfo = info;
            ApplyInfo(info);
            StatusMessage = HasDeviceInfo ? "OK" : "info: пустой ответ";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Ошибка info: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void ApplyInfo(MarauderInfo info)
    {
        HasDeviceInfo    = !info.IsEmpty(MarauderInfo.Empty);
        FirmwareVersion  = info.FirmwareVersion;
        Hardware         = info.Hardware;
        EspIdfVersion    = info.EspIdfVersion;
        StationMac       = info.StationMac;
        ApMac            = info.ApMac;
        SdCard           = info.SdConnected
            ? $"Подключена ({info.SdSizeMb} МБ)"
            : "Не подключена";
        Battery          = info.BatteryMonitor
            ? $"{info.BatteryPercent}%"
            : "—";
    }

    private void OnSerialLine(string line)
        => Console.WriteLine($"[serial] {line}");

    private void OnSerialPrompt()
    {
        // Можно использовать для индикации готовности устройства.
    }

    private void OnSerialError(Exception ex)
        => StatusMessage = $"Ошибка порта: {ex.Message}";

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _portScanner.Stop();
        _serial.LineReceived   -= OnSerialLine;
        _serial.PromptReceived -= OnSerialPrompt;
        _serial.Error          -= OnSerialError;
        _serial.DisposeAsync().AsTask().GetAwaiter().GetResult();
    }
}

internal static class MarauderInfoEmptyExtensions
{
    public static bool IsEmpty(this MarauderInfo info, MarauderInfo empty)
        => string.IsNullOrEmpty(info.FirmwareVersion) &&
           string.IsNullOrEmpty(info.Hardware);
}
