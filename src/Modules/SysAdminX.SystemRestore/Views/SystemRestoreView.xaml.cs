using System.Windows.Controls;
using SysAdminX.SystemRestore.ViewModels;

namespace SysAdminX.SystemRestore.Views;

public partial class SystemRestoreView : Page
{
    public SystemRestoreViewModel ViewModel { get; }

    public SystemRestoreView(SystemRestoreViewModel viewModel)
    {
        InitializeComponent();
        ViewModel = viewModel;
        DataContext = ViewModel;
        
        Loaded += (s, e) => 
        {
            if (ViewModel.LoadDataCommand.CanExecute(null))
            {
                ViewModel.LoadDataCommand.Execute(null);
            }
        };
    }
}
