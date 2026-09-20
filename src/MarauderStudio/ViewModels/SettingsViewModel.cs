using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MarauderStudio.Core.Common;

namespace MarauderStudio.ViewModels;

public sealed partial class SettingsViewModel : ObservableObject
{
    private readonly SettingsService _settings;

    [ObservableProperty] private string _esptoolPath = string.Empty;
    [ObservableProperty] private string _otaSsid = "MarauderOTA";
    [ObservableProperty] private string _otaPassword = "justcallmekoko";
    [ObservableProperty] private string _downloadDir = string.Empty;
    [ObservableProperty] private bool _autoCheckUpdates = true;
    [ObservableProperty] private string _esptoolDescription = "Поиск…";

    public SettingsViewModel(SettingsService settings)
    {
        _settings = settings;
        var s = settings.Load();
        EsptoolPath = s.EsptoolPath;
        OtaSsid = s.OtaSsid;
        OtaPassword = s.OtaPassword;
        DownloadDir = s.DownloadDir;
        AutoCheckUpdates = s.AutoCheckUpdates;
        RefreshEsptoolDescription();
    }

    public void RefreshEsptoolDescription()
    {
        var res = Core.Flashing.EsptoolResolver.Resolve(EsptoolPath);
        EsptoolDescription = res.Description;
    }

    [RelayCommand]
    private void Save()
    {
        var s = new AppSettings
        {
            EsptoolPath = EsptoolPath,
            OtaSsid = OtaSsid,
            OtaPassword = OtaPassword,
            DownloadDir = DownloadDir,
            AutoCheckUpdates = AutoCheckUpdates
        };
        _settings.Save(s);
    }

    [RelayCommand]
    private void BrowseEsptool()
    {
        var dlg = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Выберите esptool.exe",
            Filter = "esptool.exe|esptool.exe;*.exe|Все файлы (*.*)|*.*"
        };
        if (dlg.ShowDialog() == true)
        {
            EsptoolPath = dlg.FileName;
            RefreshEsptoolDescription();
        }
    }

    [RelayCommand]
    private void BrowseDownloadDir()
    {
        var dlg = new Microsoft.Win32.OpenFolderDialog
        {
            Title = "Выберите папку для загрузок"
        };
        if (dlg.ShowDialog() == true)
        {
            DownloadDir = dlg.FolderName;
        }
    }
}
