using System.Linq;
using System.Windows.Controls;
using SysAdminX.StartupManager.ViewModels;
using Wpf.Ui.Controls;

namespace SysAdminX.StartupManager.Views;

public partial class StartupManagerView : Page, INavigationAware
{
    private readonly StartupManagerViewModel _viewModel;

    public StartupManagerView(StartupManagerViewModel viewModel)
    {
        _viewModel = viewModel;
        DataContext = _viewModel;
        InitializeComponent();
    }

    public void OnNavigatedTo()
    {
        if (!_viewModel.StartupApps.Any())
        {
            _viewModel.LoadStartupAppsCommand.Execute(null);
        }
    }

    public void OnNavigatedFrom()
    {
    }
}
