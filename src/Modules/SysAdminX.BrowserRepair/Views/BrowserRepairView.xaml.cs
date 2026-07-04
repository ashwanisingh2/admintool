using System.Windows.Controls;
using SysAdminX.BrowserRepair.ViewModels;

namespace SysAdminX.BrowserRepair.Views;

public partial class BrowserRepairView : Page
{
    private readonly BrowserRepairViewModel _viewModel;

    public BrowserRepairView(BrowserRepairViewModel viewModel)
    {
        _viewModel = viewModel;
        DataContext = _viewModel;
        InitializeComponent();

        Loaded += (s, e) =>
        {
            if (_viewModel.LoadBrowsersCommand.CanExecute(null))
            {
                _viewModel.LoadBrowsersCommand.Execute(null);
            }
        };
    }
}
