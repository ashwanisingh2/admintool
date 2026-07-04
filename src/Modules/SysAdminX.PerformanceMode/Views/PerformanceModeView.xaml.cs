using System.Windows.Controls;
using SysAdminX.PerformanceMode.ViewModels;

namespace SysAdminX.PerformanceMode.Views;

public partial class PerformanceModeView : Page
{
    private readonly PerformanceModeViewModel _viewModel;

    public PerformanceModeView(PerformanceModeViewModel viewModel)
    {
        _viewModel = viewModel;
        DataContext = _viewModel;
        InitializeComponent();
    }
}
