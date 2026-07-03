// -----------------------------------------------------------------------
// <copyright file="DriverManagerView.xaml.cs" company="SysAdminX">
//     Copyright (c) SysAdminX. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System.Windows;
using System.Windows.Controls;
using SysAdminX.DriverManager.ViewModels;

namespace SysAdminX.DriverManager.Views;

/// <summary>
/// Interaction logic for DriverManagerView.xaml
/// </summary>
public partial class DriverManagerView : Page
{
    public DriverManagerViewModel ViewModel { get; }

    public DriverManagerView(DriverManagerViewModel viewModel)
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
