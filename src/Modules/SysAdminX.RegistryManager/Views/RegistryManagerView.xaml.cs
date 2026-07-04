using System.Windows.Controls;
using SysAdminX.RegistryManager.ViewModels;

namespace SysAdminX.RegistryManager.Views;

public partial class RegistryManagerView : Page
{
    private readonly RegistryManagerViewModel _viewModel;

    public RegistryManagerView(RegistryManagerViewModel viewModel)
    {
        _viewModel = viewModel;
        DataContext = _viewModel;
        InitializeComponent();
    }
}
