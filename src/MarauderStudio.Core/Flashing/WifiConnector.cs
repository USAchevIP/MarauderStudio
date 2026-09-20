using ManagedNativeWifi;

namespace MarauderStudio.Core.Flashing;

/// <summary>
/// Подключение к WiFi AP (например, MarauderOTA) через Windows WLAN API.
/// </summary>
public static class WifiConnector
{
    /// <summary>
    /// Проверяет, доступна ли сеть с указанным SSID в радиоэфире.
    /// </summary>
    public static bool IsAvailable(string ssid)
    {
        if (string.IsNullOrWhiteSpace(ssid)) return false;
        var wanted = System.Text.Encoding.UTF8.GetBytes(ssid);
        return NativeWifi.EnumerateAvailableNetworkSsids()
            .Any(s => BytesEqual(s.ToBytes(), wanted));
    }

    /// <summary>
    /// Проверяет, подключены ли уже к указанному SSID.
    /// </summary>
    public static bool IsConnected(string ssid)
    {
        if (string.IsNullOrWhiteSpace(ssid)) return false;
        var wanted = System.Text.Encoding.UTF8.GetBytes(ssid);
        return NativeWifi.EnumerateConnectedNetworkSsids()
            .Any(s => BytesEqual(s.ToBytes(), wanted));
    }

    /// <summary>
    /// Возвращает имена сохранённых профилей с указанным SSID.
    /// </summary>
    public static IEnumerable<string> GetProfileNames(string ssid)
    {
        var wanted = System.Text.Encoding.UTF8.GetBytes(ssid);
        return NativeWifi.EnumerateProfileNames()
            .Where(p => NativeWifi.EnumerateProfiles()
                .Any(pi => string.Equals(pi.Name, p, StringComparison.OrdinalIgnoreCase)
                           && BytesEqual(pi.Ssid?.ToBytes(), wanted)));
    }

    private static bool BytesEqual(byte[]? a, byte[]? b)
    {
        if (a is null || b is null) return false;
        if (a.Length != b.Length) return false;
        for (int i = 0; i < a.Length; i++)
            if (a[i] != b[i]) return false;
        return true;
    }
}
