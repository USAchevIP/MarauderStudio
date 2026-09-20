using System.Windows;
using System.Windows.Controls;
using MarauderStudio.ViewModels;

namespace MarauderStudio.Views;

public partial class DeviceView : Page
{
    public DeviceView()
    {
        InitializeComponent();
        DataContext = AppHost.Get<DeviceViewModel>();
    }

    private void Preset_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.DataContext is CommandPreset preset)
        {
            ((DeviceViewModel)DataContext).ExecuteCommand.Execute(preset);
        }
    }
}
