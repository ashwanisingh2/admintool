using System.Windows;
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
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        // Trigger the initial load only once per page instance — the
        // NavigationView in WPF-UI re-uses page instances when the user
        // navigates back, so this is the right hook for first-time loads.
        if (_viewModel.Backups.Count == 0 && _viewModel.LoadBackupsCommand.CanExecute(null))
        {
            _viewModel.LoadBackupsCommand.Execute(null);
        }
    }
}
