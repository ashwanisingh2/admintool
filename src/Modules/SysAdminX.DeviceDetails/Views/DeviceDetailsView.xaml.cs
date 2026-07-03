// -----------------------------------------------------------------------
// <copyright file="DeviceDetailsView.xaml.cs" company="SysAdminX">
//     Copyright (c) SysAdminX. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System.Windows;
using System.Windows.Controls;
using SysAdminX.DeviceDetails.ViewModels;

namespace SysAdminX.DeviceDetails.Views;

/// <summary>
/// Interaction logic for DeviceDetailsView.xaml
/// </summary>
public partial class DeviceDetailsView : Page
{
    public DeviceDetailsViewModel ViewModel { get; }

    public DeviceDetailsView(DeviceDetailsViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = ViewModel;

        InitializeComponent();
    }

    private void OnPageLoaded(object sender, RoutedEventArgs e)
    {
        if (ViewModel.InitializeCommand.CanExecute(null))
        {
            ViewModel.InitializeCommand.Execute(null);
        }
    }
}
