using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using MarauderStudio.Core.Common;
using MarauderStudio.Core.Firmware;
using MarauderStudio.Core.Flashing;
using MarauderStudio.ViewModels;
using MarauderStudio.Views;

namespace MarauderStudio;

/// <summary>
/// Простой DI-контейнер на базе Microsoft.Extensions.DependencyInjection.
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

        // Core
        sc.AddSingleton<SettingsService>();
        sc.AddSingleton<GitHubReleasesClient>();
        sc.AddSingleton<FirmwareMetadata>();

        // ViewModels
        sc.AddTransient<DashboardViewModel>();
        sc.AddTransient<FlasherViewModel>();
        sc.AddTransient<MonitorViewModel>();
        sc.AddTransient<DeviceViewModel>();
        sc.AddTransient<SettingsViewModel>();

        // Views
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
        _provider?.Dispose();
        _provider = null;
    }
}
