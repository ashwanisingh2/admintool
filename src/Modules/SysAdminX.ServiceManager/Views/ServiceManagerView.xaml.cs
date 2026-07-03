// -----------------------------------------------------------------------
// <copyright file="ServiceManagerView.xaml.cs" company="SysAdminX">
//     Copyright (c) SysAdminX. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System.Windows;
using System.Windows.Controls;
using SysAdminX.ServiceManager.ViewModels;

namespace SysAdminX.ServiceManager.Views;

/// <summary>
/// Interaction logic for ServiceManagerView.xaml
/// </summary>
public partial class ServiceManagerView : Page
{
    private readonly ServiceManagerViewModel _viewModel;

    /// <summary>
    /// Initializes a new instance of the <see cref="ServiceManagerView"/> class.
    /// </summary>
    public ServiceManagerView(ServiceManagerViewModel viewModel)
    {
        _viewModel = viewModel;
        DataContext = _viewModel;
        InitializeComponent();
    }

    private void OnPageLoaded(object sender, RoutedEventArgs e)
    {
        // Load services automatically when the page is loaded if not already loaded
        if (_viewModel.Services.Count == 0 && _viewModel.LoadServicesCommand.CanExecute(null))
        {
            _viewModel.LoadServicesCommand.Execute(null);
        }
    }
}
