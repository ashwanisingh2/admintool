using System.Windows.Controls;
using SysAdminX.AutoPilot.ViewModels;

namespace SysAdminX.AutoPilot.Views;

public partial class AutoPilotView : Page
{
    private readonly AutoPilotViewModel _viewModel;

    public AutoPilotView(AutoPilotViewModel viewModel)
    {
        _viewModel = viewModel;
        DataContext = _viewModel;
        InitializeComponent();
    }
}
