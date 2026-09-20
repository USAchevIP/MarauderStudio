using System.IO.Ports;
using System.Text;
using System.Threading.Channels;

namespace MarauderStudio.Core.Serial;

/// <summary>
/// Сервис работы с COM-портом Marauder. Работает в фоне (Task-цикл чтения),
/// отдаёт строки через события LineReceived / RawReceived. Не блокирует UI.
/// </summary>
public sealed class SerialPortService : IAsyncDisposable
{
    private readonly object _gate = new();
    private SerialPort? _port;
    private CancellationTokenSource? _cts;
    private Task? _readTask;
    private readonly StringBuilder _lineBuffer = new();

    /// <summary>Открыт ли порт в данный момент.</summary>
    public bool IsOpen
    {
        get { lock (_gate) { return _port?.IsOpen ?? false; } }
    }

    /// <summary>Текущий порт (или null).</summary>
    public string? CurrentPort
    {
        get { lock (_gate) { return _port?.PortName; } }
    }

    /// <summary>Бод-рейт текущего соединения.</summary>
    public int CurrentBaud { get; private set; } = 115200;

    /// <summary>Полная строка получена из порта (включая \r\n).</summary>
    public event Action<string>? LineReceived;

    /// <summary>Приглашение командной строки обнаружено ("> " в конце потока).</summary>
    public event Action? PromptReceived;

    /// <summary>Сырые байты получены.</summary>
    public event Action<byte[]>? RawReceived;

    /// <summary>Ошибка порта (открытие, чтение).</summary>
    public event Action<Exception>? Error;

    /// <summary>
    /// Открыть порт. Если уже открыт — закрывает и открывает заново.
    /// </summary>
    public async Task OpenAsync(string portName, int baud = 115200, CancellationToken ct = default)
    {
        await CloseAsync().ConfigureAwait(false);

        var newPort = new SerialPort(portName, baud, Parity.None, 8, StopBits.One)
        {
            NewLine = "\n",
            ReadTimeout = 50,
            WriteTimeout = 1000,
            DtrEnable = true,
            RtsEnable = false
        };

        try
        {
            newPort.Open();
        }
        catch (Exception ex)
        {
            Error?.Invoke(ex);
            throw;
        }

        lock (_gate)
        {
            _port = newPort;
            CurrentBaud = baud;
        }

        _cts = new CancellationTokenSource();
        _readTask = Task.Run(() => ReadLoopAsync(_cts.Token));
        // Дать устройству проснуться.
        await Task.Delay(150, ct).ConfigureAwait(false);
    }

    public async Task CloseAsync()
    {
        Task? task;
        CancellationTokenSource? cts;
        SerialPort? port;
        lock (_gate)
        {
            task = _readTask;
            cts = _cts;
            port = _port;
            _readTask = null;
            _cts = null;
            _port = null;
        }

        try { cts?.Cancel(); } catch { /* ignore */ }
        if (task is not null)
        {
            try { await task.ConfigureAwait(false); } catch { /* ignore */ }
        }
        try { cts?.Dispose(); } catch { /* ignore */ }
        try { port?.Close(); port?.Dispose(); } catch { /* ignore */ }
    }

    /// <summary>
    /// Отправить команду Marauder (добавляет \r\n, если нет).
    /// </summary>
    public void SendCommand(string command)
    {
        SerialPort? port;
        lock (_gate) { port = _port; }
        if (port?.IsOpen != true) return;

        var text = command.EndsWith("\r\n", StringComparison.Ordinal) ? command : command + "\r\n";
        try
        {
            port.Write(text);
        }
        catch (Exception ex)
        {
            Error?.Invoke(ex);
        }
    }

    /// <summary>
    /// Запросить `info` и вернуть накопленный вывод до приглашения `>`.
    /// </summary>
    public async Task<string> RequestInfoAsync(TimeSpan? timeout = null, CancellationToken ct = default)
    {
        var captured = new StringBuilder();
        var done = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        void OnLine(string line)
        {
            captured.AppendLine(line);
            if (line.TrimEnd().EndsWith(">"))
            {
                done.TrySetResult(true);
            }
        }
        void OnPrompt()
        {
            done.TrySetResult(true);
        }

        LineReceived += OnLine;
        PromptReceived += OnPrompt;
        try
        {
            SendCommand("info");
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(timeout ?? TimeSpan.FromSeconds(3));
            await using var reg = cts.Token.Register(() => done.TrySetResult(false));
            await done.Task.ConfigureAwait(false);
        }
        finally
        {
            LineReceived -= OnLine;
            PromptReceived -= OnPrompt;
        }
        return captured.ToString();
    }

    private async Task ReadLoopAsync(CancellationToken ct)
    {
        var buffer = new byte[1024];
        try
        {
            while (!ct.IsCancellationRequested)
            {
                SerialPort? port;
                lock (_gate) { port = _port; }
                if (port is null || !port.IsOpen) break;

                int read;
                try
                {
                    read = await Task.Run(() =>
                    {
                        try { return port.Read(buffer, 0, buffer.Length); }
                        catch (TimeoutException) { return 0; }
                    }, ct).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Error?.Invoke(ex);
                    await Task.Delay(50, ct).ConfigureAwait(false);
                    continue;
                }

                if (read <= 0) continue;

                var data = new byte[read];
                Buffer.BlockCopy(buffer, 0, data, 0, read);
                RawReceived?.Invoke(data);
                ProcessBytes(data);
            }
        }
        catch (OperationCanceledException)
        {
            // нормальный выход
        }
        catch (Exception ex)
        {
            Error?.Invoke(ex);
        }
    }

    private void ProcessBytes(byte[] data)
    {
        lock (_lineBuffer)
        {
            foreach (var b in data)
            {
                if (b == '\r') continue;
                if (b == '\n')
                {
                    var line = _lineBuffer.ToString();
                    _lineBuffer.Clear();
                    LineReceived?.Invoke(line);
                    // Если строка заканчивается на "> " (приглашение Marauder)
                    if (line.EndsWith("> ", StringComparison.Ordinal) || line.EndsWith(">", StringComparison.Ordinal))
                    {
                        PromptReceived?.Invoke();
                    }
                }
                else
                {
                    _lineBuffer.Append((char)b);
                }
            }
        }
    }

    public async ValueTask DisposeAsync() => await CloseAsync().ConfigureAwait(false);
}
