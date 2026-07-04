using System.Windows.Controls;
using SysAdminX.OneClickCare.ViewModels;

namespace SysAdminX.OneClickCare.Views;

public partial class OneClickCareView : Page
{
    private readonly OneClickCareViewModel _viewModel;

    public OneClickCareView(OneClickCareViewModel viewModel)
    {
        _viewModel = viewModel;
        DataContext = _viewModel;
        InitializeComponent();
    }
}
