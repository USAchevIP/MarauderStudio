using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MarauderStudio.ViewModels;

namespace MarauderStudio.Views;

public partial class MonitorView : Page
{
    public MonitorView()
    {
        InitializeComponent();
        DataContext = AppHost.Get<MonitorViewModel>();
        Loaded += (_, _) => InputBox.Focus();
    }

    private void InputBox_OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && DataContext is MonitorViewModel vm)
        {
            vm.SendCommand.Execute(null);
            e.Handled = true;
        }
        else if (e.Key == Key.Up && DataContext is MonitorViewModel vmUp)
        {
            vmUp.HistoryPrevCommand.Execute(null);
            e.Handled = true;
        }
        else if (e.Key == Key.Down && DataContext is MonitorViewModel vmDown)
        {
            vmDown.HistoryNextCommand.Execute(null);
            e.Handled = true;
        }
        else if (e.Key == Key.L && Keyboard.Modifiers == ModifierKeys.Control)
        {
            ((MonitorViewModel)DataContext).ClearCommand.Execute(null);
            e.Handled = true;
        }
    }
}
