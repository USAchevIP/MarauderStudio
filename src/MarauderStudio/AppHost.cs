using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using MarauderStudio.Core.Common;
using MarauderStudio.Core.Firmware;
using MarauderStudio.Core.Serial;
using MarauderStudio.ViewModels;
using MarauderStudio.Views;

namespace MarauderStudio;

/// <summary>
/// Простой DI-контейнер на базе Microsoft.Extensions.DependencyInjection.
/// SerialPortService и ViewModel'ы — синглтоны: порт один на всё приложение,
/// состояние соединения живёт при переходах между страницами.
/// </summary>
public static class AppHost
{
    private static ServiceProvider? _provider;

    public static IServiceProvider Services
    {
        get
        {
            if (_provider is null)
                Build();
            return _provider!;
        }
    }

    public static object? GetService(Type t) => Services.GetService(t);

    public static T Get<T>() where T : notnull => Services.GetRequiredService<T>();

    public static void Build()
    {
        var sc = new ServiceCollection();

        // Core — синглтоны
        sc.AddSingleton<SettingsService>();
        sc.AddSingleton<GitHubReleasesClient>();
        sc.AddSingleton<SerialPortService>();

        // ViewModels — синглтоны, чтобы состояние жило между переходами
        sc.AddSingleton<DashboardViewModel>();
        sc.AddSingleton<FlasherViewModel>();
        sc.AddSingleton<MonitorViewModel>();
        sc.AddSingleton<DeviceViewModel>();
        sc.AddSingleton<SettingsViewModel>();

        // Views — transient (страницы лёгкие, VM в них синглтонные)
        sc.AddTransient<DashboardView>();
        sc.AddTransient<FlasherView>();
        sc.AddTransient<MonitorView>();
        sc.AddTransient<DeviceView>();
        sc.AddTransient<SettingsView>();
        sc.AddTransient<AboutView>();

        _provider = sc.BuildServiceProvider();
    }

    public static void Shutdown()
    {
        // Освобождаем COM-порт при выходе.
        try { Get<SerialPortService>().DisposeAsync().AsTask().Wait(2000); } catch { }
        _provider?.Dispose();
        _provider = null;
    }
}
