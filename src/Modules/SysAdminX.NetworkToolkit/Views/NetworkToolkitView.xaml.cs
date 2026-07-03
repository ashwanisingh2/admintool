// -----------------------------------------------------------------------
// <copyright file="NetworkToolkitView.xaml.cs" company="SysAdminX">
//     Copyright (c) SysAdminX. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System.Windows;
using System.Windows.Controls;
using SysAdminX.NetworkToolkit.ViewModels;

namespace SysAdminX.NetworkToolkit.Views;

/// <summary>
/// Interaction logic for NetworkToolkitView.xaml
/// </summary>
public partial class NetworkToolkitView : Page
{
    public NetworkToolkitViewModel ViewModel { get; }

    public NetworkToolkitView(NetworkToolkitViewModel viewModel)
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
