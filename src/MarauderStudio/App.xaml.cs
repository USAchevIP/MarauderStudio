using System.Windows;
using System.Windows.Threading;
using MarauderStudio.Core.Common;
using MarauderStudio.Localization;
using ModernWpf;

namespace MarauderStudio;

public partial class App : Application
{
    public App()
    {
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        Exit += (_, _) => AppHost.Shutdown();
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Загружаем настройки и применяем тему/язык.
        var settings = AppHost.Get<SettingsService>().Load();
        LocalizationService.CurrentLanguage = settings.Language;
        ThemeManager.Current.ApplicationTheme = settings.Theme switch
        {
            "light"  => ApplicationTheme.Light,
            "dark"   => ApplicationTheme.Dark,
            _        => null
        };

        var window = new MainWindow();
        window.Show();
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        MessageBox.Show(
            $"Необработанная ошибка:\n\n{e.Exception.Message}\n\n{e.Exception.StackTrace}",
            "Marauder Studio",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
        e.Handled = true;
    }
}
