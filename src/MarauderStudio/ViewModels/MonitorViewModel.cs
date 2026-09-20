using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MarauderStudio.Core.Devices;
using MarauderStudio.Core.Serial;

namespace MarauderStudio.ViewModels;

/// <summary>
/// ViewModel Serial-монитора. Использует общий с приложением SerialPortService:
/// если подключение выполнено на Dashboard — монитор сразу видит вывод.
/// </summary>
public sealed partial class MonitorViewModel : ObservableObject
{
    private readonly SerialPortService _serial;
    private readonly List<string> _history = new();
    private int _historyIndex;

    public ObservableCollection<MonitorLine> Lines { get; } = new();

    [ObservableProperty] private string _input = string.Empty;
    [ObservableProperty] private bool _isConnected;
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string _status = "Отключено";

    public MonitorViewModel(SerialPortService serial)
    {
        _serial = serial;

        _serial.LineReceived += line =>
        {
            System.Windows.Application.Current?.Dispatcher.BeginInvoke(() =>
                Lines.Add(new MonitorLine(DateTime.Now, "RX", line)));
        };
        _serial.PromptReceived += () =>
        {
            System.Windows.Application.Current?.Dispatcher.BeginInvoke(() =>
                Status = "Устройство готово (>)");
        };
        _serial.Error += ex =>
            System.Windows.Application.Current?.Dispatcher.BeginInvoke(() =>
            {
                Lines.Add(new MonitorLine(DateTime.Now, "ERR", ex.Message));
                if (!_serial.IsOpen)
                {
                    IsConnected = false;
                    Status = "Соединение потеряно";
                }
            });

        // Отражаем состояние, если подключение уже выполнено на Dashboard.
        IsConnected = _serial.IsOpen;
        if (IsConnected)
            Status = $"Подключено к {_serial.CurrentPort}";
    }

    [RelayCommand]
    private async Task ConnectAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        try
        {
            // Порт уже открыт (Dashboard) — просто отражаем состояние.
            if (_serial.IsOpen)
            {
                IsConnected = true;
                Status = $"Подключено к {_serial.CurrentPort}";
                return;
            }

            // Иначе открываем первый доступный порт.
            var ports = PortEnumerator.GetPorts();
            if (ports.Count == 0)
            {
                Status = "COM-порты не найдены. Подключите устройство.";
                return;
            }

            var port = ports[0].Port;
            Status = $"Подключение к {port}…";
            await _serial.OpenAsync(port);
            IsConnected = true;
            Status = $"Подключено к {port}";
            Lines.Add(new MonitorLine(DateTime.Now, "SYS", $"Подключено: {port} @ {_serial.CurrentBaud}"));
        }
        catch (Exception ex)
        {
            Status = $"Ошибка: {ex.Message}";
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task DisconnectAsync()
    {
        if (!_serial.IsOpen) { IsConnected = false; Status = "Отключено"; return; }
        try
        {
            await _serial.CloseAsync();
            Lines.Add(new MonitorLine(DateTime.Now, "SYS", "Отключено"));
        }
        finally
        {
            IsConnected = false;
            Status = "Отключено";
        }
    }

    [RelayCommand]
    private void Send()
    {
        if (string.IsNullOrWhiteSpace(Input)) return;
        if (!_serial.IsOpen)
        {
            Status = "Нет подключения. Нажмите «Подключить» или подключитесь на Dashboard.";
            return;
        }

        var cmd = Input.Trim();
        if (cmd != _history.LastOrDefault())
            _history.Add(cmd);
        _historyIndex = _history.Count;

        _serial.SendCommand(cmd);
        Lines.Add(new MonitorLine(DateTime.Now, "TX", cmd));
        Input = string.Empty;
    }

    [RelayCommand]
    private void SendQuick(string? command)
    {
        if (string.IsNullOrEmpty(command)) return;
        Input = command;
        Send();
    }

    [RelayCommand]
    private void Clear()
    {
        Lines.Clear();
    }

    [RelayCommand]
    private void HistoryPrev()
    {
        if (_history.Count == 0) return;
        _historyIndex = Math.Max(0, _historyIndex - 1);
        Input = _history[_historyIndex];
    }

    [RelayCommand]
    private void HistoryNext()
    {
        if (_history.Count == 0) return;
        _historyIndex = Math.Min(_history.Count, _historyIndex + 1);
        Input = _historyIndex >= _history.Count ? string.Empty : _history[_historyIndex];
    }
}

public sealed record MonitorLine(DateTime Timestamp, string Direction, string Text)
{
    public string Display => $"[{Timestamp:HH:mm:ss}] {Direction} {Text}";
}
