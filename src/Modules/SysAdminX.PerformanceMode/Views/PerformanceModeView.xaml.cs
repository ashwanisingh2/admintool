using System.Windows;
using System.Windows.Controls;
using SysAdminX.PerformanceMode.ViewModels;

namespace SysAdminX.PerformanceMode.Views;

public partial class PerformanceModeView : Page
{
    private readonly PerformanceModeViewModel _viewModel;
    private bool _loadedOnce;

    public PerformanceModeView(PerformanceModeViewModel viewModel)
    {
        _viewModel = viewModel;
        DataContext = _viewModel;
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (_loadedOnce) return;
        _loadedOnce = true;

        if (_viewModel.LoadCurrentProfileCommand.CanExecute(null))
        {
            _viewModel.LoadCurrentProfileCommand.Execute(null);
        }
    }
}
