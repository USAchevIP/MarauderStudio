using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using MarauderStudio.ViewModels;

namespace MarauderStudio.Views;

public partial class FlasherView : Page
{
    public FlasherView()
    {
        InitializeComponent();
        DataContext = AppHost.Get<FlasherViewModel>();
    }

    private void BrowseButton_OnClick(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog
        {
            Title = "Выберите прошивку Marauder (.bin)",
            Filter = "Бинарники прошивки (*.bin)|*.bin|Все файлы (*.*)|*.*",
            CheckFileExists = true
        };
        if (dlg.ShowDialog() == true)
        {
            ((FlasherViewModel)DataContext).SetFileCommand.Execute(dlg.FileName);
        }
    }

    private void DropZone_OnDragOver(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop)
            ? DragDropEffects.Copy
            : DragDropEffects.None;
        e.Handled = true;
    }

    private void DropZone_OnDrop(object sender, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;
        var files = (string[])e.Data.GetData(DataFormats.FileDrop);
        var bin = files.FirstOrDefault(f => f.EndsWith(".bin", StringComparison.OrdinalIgnoreCase));
        if (bin is not null)
        {
            ((FlasherViewModel)DataContext).SetFileCommand.Execute(bin);
        }
    }
}
