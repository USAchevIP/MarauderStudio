using System.IO;
using System.Windows;
using System.Windows.Threading;
using MarauderStudio.Core.Common;
using MarauderStudio.Localization;
using ModernWpf;

namespace MarauderStudio;

public partial class App : Application
{
    private static readonly string LogPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "MarauderStudio", "startup.log");

    public App()
    {
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnDomainUnhandledException;
        Exit += (_, _) => AppHost.Shutdown();
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        WriteLog($"=== Marauder Studio startup at {DateTime.Now} ===");
        WriteLog($".NET version: {Environment.Version}");
        WriteLog($"OS: {Environment.OSVersion}");
        WriteLog($"BaseDirectory: {AppContext.BaseDirectory}");
        WriteLog($"Args: {string.Join(' ', e.Args)}");

        try
        {
            base.OnStartup(e);

            var settings = AppHost.Get<SettingsService>().Load();
            LocalizationService.CurrentLanguage = settings.Language;
            ThemeManager.Current.ApplicationTheme = settings.Theme switch
            {
                "light"  => ApplicationTheme.Light,
                "dark"   => ApplicationTheme.Dark,
                _        => null
            };
            WriteLog($"Settings loaded. Language={settings.Language}, Theme={settings.Theme}");

            var window = new MainWindow();
            window.Show();
            WriteLog("MainWindow shown.");
        }
        catch (Exception ex)
        {
            WriteLog($"FATAL OnStartup: {ex}");
            throw;
        }
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        WriteLog($"UI exception: {e.Exception}");
        MessageBox.Show(
            $"Необработанная ошибка:\n\n{e.Exception.Message}\n\nПодробности в файле:\n{LogPath}",
            "Marauder Studio",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
        e.Handled = true;
    }

    private void OnDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
            WriteLog($"Domain exception (fatal={e.IsTerminating}): {ex}");
        else
            WriteLog($"Domain exception (non-Exception): {e.ExceptionObject}");
    }

    private static void WriteLog(string line)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(LogPath)!);
            File.AppendAllText(LogPath, $"[{DateTime.Now:HH:mm:ss.fff}] {line}{Environment.NewLine}");
        }
        catch { /* логирование не должно ронять приложение */ }
    }
}
