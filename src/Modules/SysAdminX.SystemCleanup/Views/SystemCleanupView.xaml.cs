// -----------------------------------------------------------------------
// <copyright file="SystemCleanupView.xaml.cs" company="SysAdminX">
//     Copyright (c) SysAdminX. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System.Windows;
using System.Windows.Controls;
using SysAdminX.SystemCleanup.ViewModels;

namespace SysAdminX.SystemCleanup.Views;

public partial class SystemCleanupView : Page
{
    private readonly SystemCleanupViewModel _viewModel;

    public SystemCleanupView(SystemCleanupViewModel viewModel)
    {
        _viewModel = viewModel;
        DataContext = _viewModel;
        InitializeComponent();
    }

    private void OnPageLoaded(object sender, RoutedEventArgs e)
    {
        if (_viewModel.CleanupItems.Count == 0 && _viewModel.LoadItemsCommand.CanExecute(null))
        {
            _viewModel.LoadItemsCommand.Execute(null);
        }
    }
}
