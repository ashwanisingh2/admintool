using System.Windows;
using System.Windows.Controls;
using SysAdminX.AutoPilot.ViewModels;

namespace SysAdminX.AutoPilot.Views;

public partial class AutoPilotView : Page
{
    private readonly AutoPilotViewModel _viewModel;
    private bool _loadedOnce;

    public AutoPilotView(AutoPilotViewModel viewModel)
    {
        _viewModel = viewModel;
        DataContext = _viewModel;
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        // Trigger the initial status refresh only once per page instance.
        // The view's own 30-second poll timer keeps things fresh afterwards.
        if (_loadedOnce) return;
        _loadedOnce = true;

        if (_viewModel.RefreshStatusCommand.CanExecute(null))
        {
            _viewModel.RefreshStatusCommand.Execute(null);
        }
    }
}
