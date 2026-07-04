using System.Windows.Controls;
using SysAdminX.LargeFileFinder.ViewModels;

namespace SysAdminX.LargeFileFinder.Views;

public partial class LargeFileFinderView : Page
{
    private readonly LargeFileFinderViewModel _viewModel;

    public LargeFileFinderView(LargeFileFinderViewModel viewModel)
    {
        _viewModel = viewModel;
        DataContext = _viewModel;
        InitializeComponent();
    }
}
