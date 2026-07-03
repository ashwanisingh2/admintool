// -----------------------------------------------------------------------
// <copyright file="SoftwareManagerView.xaml.cs" company="SysAdminX">
//     Copyright (c) SysAdminX. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System.Windows;
using System.Windows.Controls;
using SysAdminX.SoftwareManager.ViewModels;

namespace SysAdminX.SoftwareManager.Views;

public partial class SoftwareManagerView : Page
{
    private readonly SoftwareManagerViewModel _viewModel;

    public SoftwareManagerView(SoftwareManagerViewModel viewModel)
    {
        _viewModel = viewModel;
        DataContext = _viewModel;
        InitializeComponent();
    }

    private void OnPageLoaded(object sender, RoutedEventArgs e)
    {
        if (_viewModel.SoftwareList.Count == 0 && _viewModel.LoadSoftwareCommand.CanExecute(null))
        {
            _viewModel.LoadSoftwareCommand.Execute(null);
        }
    }
}
