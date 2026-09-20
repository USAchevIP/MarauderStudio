using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

namespace MarauderStudio.Views;

public partial class AboutView : Page
{
    public AboutView()
    {
        InitializeComponent();
    }

    private void RepoButton_OnClick(object sender, RoutedEventArgs e)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = "https://github.com/justcallmekoko/ESP32Marauder",
            UseShellExecute = true
        });
    }
}
