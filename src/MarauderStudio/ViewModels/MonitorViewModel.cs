using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MarauderStudio.Core.Serial;

namespace MarauderStudio.ViewModels;

/// <summary>
/// ViewModel Serial-монитора. Подключается к Marauder и отображает/отправляет команды.
/// </summary>
public sealed partial class MonitorViewModel : ObservableObject, IDisposable
{
    private readonly SerialPortService _serial = new();
    private readonly List<string> _history = new();
    private int _historyIndex = -1;

    public ObservableCollection<MonitorLine> Lines { get; } = new();

    [ObservableProperty] private string _input = string.Empty;
    [ObservableProperty] private bool _isConnected;
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string _status = "Отключено";
    [ObservableProperty] private bool _hexMode;
    [ObservableProperty] private bool _autoScroll = true;

    public MonitorViewModel()
    {
        _serial.LineReceived += line =>
        {
            System.Windows.Application.Current?.Dispatcher.Invoke(() =>
                Lines.Add(new MonitorLine(DateTime.Now, "RX", line)));
        };
        _serial.Error += ex =>
            System.Windows.Application.Current?.Dispatcher.Invoke(() =>
                Lines.Add(new MonitorLine(DateTime.Now, "ERR", ex.Message)));
    }

    [RelayCommand]
    private async Task ConnectAsync()
    {
        IsBusy = true;
        try
        {
            // Полная интеграция с Dashboard — на следующих этапах. Сейчас открываем COM9.
            await _serial.OpenAsync("COM9");
            IsConnected = true;
            Status = "Подключено к COM9";
            Lines.Add(new MonitorLine(DateTime.Now, "SYS", "Подключено (115200)"));
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
        await _serial.CloseAsync();
        IsConnected = false;
        Status = "Отключено";
        Lines.Add(new MonitorLine(DateTime.Now, "SYS", "Отключено"));
    }

    [RelayCommand]
    private void Send()
    {
        if (string.IsNullOrWhiteSpace(Input)) return;
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

    public void Dispose()
    {
        _serial.DisposeAsync().AsTask().GetAwaiter().GetResult();
    }
}

public sealed record MonitorLine(DateTime Timestamp, string Direction, string Text)
{
    public string Display => $"[{Timestamp:HH:mm:ss}] {Direction} {Text}";
}
