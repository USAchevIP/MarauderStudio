using System.Windows.Controls;

namespace MarauderStudio.Views;

public partial class DashboardView : Page
{
    public DashboardView()
    {
        InitializeComponent();
        DataContext = AppHost.Get<ViewModels.DashboardViewModel>();
    }
}
