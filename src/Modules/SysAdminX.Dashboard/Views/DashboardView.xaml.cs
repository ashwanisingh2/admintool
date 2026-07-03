// -----------------------------------------------------------------------
// <copyright file="DashboardView.xaml.cs" company="SysAdminX">
//     Copyright (c) SysAdminX. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System.Windows;
using System.Windows.Controls;
using SysAdminX.Dashboard.ViewModels;

namespace SysAdminX.Dashboard.Views;

/// <summary>
/// Dashboard page view.
/// Code-behind is minimal — only page lifecycle management.
/// </summary>
public partial class DashboardView : Page
{
    private readonly DashboardViewModel _viewModel;

    /// <summary>
    /// Initializes a new instance of the <see cref="DashboardView"/> class.
    /// </summary>
    /// <param name="viewModel">The dashboard ViewModel.</param>
    public DashboardView(DashboardViewModel viewModel)
    {
        _viewModel = viewModel;
        DataContext = _viewModel;
        InitializeComponent();
    }

    private async void OnPageLoaded(object sender, RoutedEventArgs e)
    {
        if (_viewModel.InitializeCommand.CanExecute(null))
        {
            await _viewModel.InitializeAsync();
        }
    }
}
