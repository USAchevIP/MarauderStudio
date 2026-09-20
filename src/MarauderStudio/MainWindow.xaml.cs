using System.Windows;
using System.Windows.Controls;
using ModernWpf.Controls;
using MarauderStudio.Views;

namespace MarauderStudio;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        NavView.SelectedItem = NavView.MenuItems[0];
        NavigateTo("dashboard");
    }

    private void NavView_OnSelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.SelectedItem is NavigationViewItem item && item.Tag is string tag)
        {
            NavigateTo(tag);
        }
    }

    private void NavigateTo(string tag)
    {
        System.Windows.Controls.Page page = tag switch
        {
            "dashboard" => new DashboardView(),
            "flasher"   => new FlasherView(),
            "monitor"   => new MonitorView(),
            "device"    => new DeviceView(),
            "settings"  => new SettingsView(),
            "about"     => new AboutView(),
            _           => new DashboardView()
        };
        ContentFrame.Navigate(page);
    }
}
