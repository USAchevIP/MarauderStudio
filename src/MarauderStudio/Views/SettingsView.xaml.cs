using System.Windows;
using System.Windows.Controls;
using ModernWpf;
using MarauderStudio.Localization;
using MarauderStudio.ViewModels;

namespace MarauderStudio.Views;

public partial class SettingsView : Page
{
    public SettingsView()
    {
        InitializeComponent();
        DataContext = AppHost.Get<SettingsViewModel>();

        // Подтянуть текущие значения
        foreach (var item in LanguageCombo.Items.OfType<ComboBoxItem>())
        {
            if (item.Tag is string tag && tag == LocalizationService.CurrentLanguage)
            {
                LanguageCombo.SelectedItem = item;
                break;
            }
        }
        var themeName = ThemeManager.Current.ApplicationTheme == ApplicationTheme.Dark ? "dark"
            : ThemeManager.Current.ApplicationTheme == ApplicationTheme.Light ? "light"
            : "system";
        foreach (var item in ThemeCombo.Items.OfType<ComboBoxItem>())
        {
            if (item.Tag is string tag && tag == themeName)
            {
                ThemeCombo.SelectedItem = item;
                break;
            }
        }
    }

    private void LanguageCombo_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (LanguageCombo.SelectedItem is ComboBoxItem item && item.Tag is string lang)
        {
            LocalizationService.CurrentLanguage = lang;
            Persist();
        }
    }

    private void ThemeCombo_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ThemeCombo.SelectedItem is not ComboBoxItem item || item.Tag is not string theme) return;
        ThemeManager.Current.ApplicationTheme = theme switch
        {
            "dark"   => ApplicationTheme.Dark,
            "light"  => ApplicationTheme.Light,
            _        => null
        };
        Persist();
    }

    private void Persist()
    {
        if (DataContext is SettingsViewModel vm)
        {
            var lang = LocalizationService.CurrentLanguage;
            var theme = ThemeManager.Current.ApplicationTheme == ApplicationTheme.Dark ? "dark"
                : ThemeManager.Current.ApplicationTheme == ApplicationTheme.Light ? "light"
                : "system";
            vm.SaveWith(lang, theme);
        }
    }
}
