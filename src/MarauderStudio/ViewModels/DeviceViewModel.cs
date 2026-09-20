using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MarauderStudio.Core.Serial;

namespace MarauderStudio.ViewModels;

/// <summary>
/// Панель команд Marauder: пресеты WiFi/Attacks/Sniff/BT/GPS.
/// </summary>
public sealed partial class DeviceViewModel : ObservableObject
{
    private readonly SerialPortService _serial = new();

    public ObservableCollection<CommandPreset> WifiPresets { get; } = new();
    public ObservableCollection<CommandPreset> AttackPresets { get; } = new();
    public ObservableCollection<CommandPreset> SniffPresets { get; } = new();
    public ObservableCollection<CommandPreset> BtPresets { get; } = new();
    public ObservableCollection<CommandPreset> GpsPresets { get; } = new();

    [ObservableProperty] private string _lastCommand = "—";
    [ObservableProperty] private string _status = "Отключено";

    public DeviceViewModel()
    {
        WifiPresets.Add(new CommandPreset("Сканировать AP", "scanap", "Wifi"));
        WifiPresets.Add(new CommandPreset("Сканировать станции", "scansta", "Wifi"));
        WifiPresets.Add(new CommandPreset("Выбрать AP", "select -a", "Wifi"));
        WifiPresets.Add(new CommandPreset("Очистить список", "clearlist", "Wifi"));
        WifiPresets.Add(new CommandPreset("Список AP", "list -a", "Wifi"));

        AttackPresets.Add(new CommandPreset("Beacon spam", "attack -t beacon -l", "Attack"));
        AttackPresets.Add(new CommandPreset("Deauth", "attack -t deauth -l", "Attack"));
        AttackPresets.Add(new CommandPreset("Probe", "attack -t probe -l", "Attack"));
        AttackPresets.Add(new CommandPreset("Evil portal", "evilportal -c start", "Attack"));
        AttackPresets.Add(new CommandPreset("Стоп атаки", "stopscan", "Attack"));

        SniffPresets.Add(new CommandPreset("Beacon", "sniffbeacon", "Sniff"));
        SniffPresets.Add(new CommandPreset("Deauth", "sniffdeauth", "Sniff"));
        SniffPresets.Add(new CommandPreset("PMKID", "sniffpmkid", "Sniff"));
        SniffPresets.Add(new CommandPreset("SAE", "sniffsae", "Sniff"));
        SniffPresets.Add(new CommandPreset("BT", "sniffbt", "Sniff"));

        BtPresets.Add(new CommandPreset("Sour Apple", "sourapple", "BT"));
        BtPresets.Add(new CommandPreset("Samsung spam", "samsungblespam", "BT"));
        BtPresets.Add(new CommandPreset("Swift Pair", "swiftpair", "BT"));
        BtPresets.Add(new CommandPreset("AirTag spoof", "spoofat", "BT"));

        GpsPresets.Add(new CommandPreset("GPS data", "gpsdata", "GPS"));
        GpsPresets.Add(new CommandPreset("Wardrive старт", "wardrive -s", "GPS"));
        GpsPresets.Add(new CommandPreset("Wardrive стоп", "wardrive -x", "GPS"));

        _serial.LineReceived += line =>
            System.Windows.Application.Current?.Dispatcher.Invoke(() =>
            {
                LastCommand = line;
            });
    }

    [RelayCommand]
    private void Execute(CommandPreset preset)
    {
        if (preset is null) return;
        _serial.SendCommand(preset.Command);
        LastCommand = preset.Command;
    }

    public void ConnectPort(string port) => _ = _serial.OpenAsync(port);
}

public sealed record CommandPreset(string Title, string Command, string Category)
{
    public string Display => $"{Title}  →  {Command}";
}
