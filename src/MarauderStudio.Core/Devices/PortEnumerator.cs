using System.IO.Ports;

namespace MarauderStudio.Core.Devices;

/// <summary>
/// Перечисление доступных COM-портов с дополнительной информацией (дружественное имя, manufacturer).
/// </summary>
public sealed record PortDescriptor(
    string Port,
    string? FriendlyName,
    string? Manufacturer,
    string? Description)
{
    public string DisplayLabel => string.IsNullOrEmpty(FriendlyName)
        ? Port
        : $"{Port} — {FriendlyName}";
}

public static class PortEnumerator
{
    /// <summary>
    /// Возвращает список COM-портов с базовой информацией.
    /// </summary>
    public static IReadOnlyList<PortDescriptor> GetPorts()
    {
        var ports = SerialPort.GetPortNames().Distinct().OrderBy(p => p, StringComparer.OrdinalIgnoreCase);
        return ports.Select(p => new PortDescriptor(p, null, null, null)).ToList();
    }

    /// <summary>
    /// Возвращает список портов, отсортированный с приоритетом ESP32-устройств.
    /// Заглушка — реальная фильтрация возможна после получения chip_id.
    /// </summary>
    public static IReadOnlyList<PortDescriptor> GetPortsSortedByUsb()
        => GetPorts();
}
