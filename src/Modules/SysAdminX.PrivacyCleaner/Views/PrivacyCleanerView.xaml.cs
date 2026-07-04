using System.Windows.Controls;
using SysAdminX.PrivacyCleaner.ViewModels;

namespace SysAdminX.PrivacyCleaner.Views;

public partial class PrivacyCleanerView : Page
{
    private readonly PrivacyCleanerViewModel _viewModel;

    public PrivacyCleanerView(PrivacyCleanerViewModel viewModel)
    {
        _viewModel = viewModel;
        DataContext = _viewModel;
        InitializeComponent();

        Loaded += (s, e) =>
        {
            if (_viewModel.ScanCommand.CanExecute(null))
            {
                _viewModel.ScanCommand.Execute(null);
            }
        };
    }
}
