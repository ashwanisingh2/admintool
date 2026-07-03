// -----------------------------------------------------------------------
// <copyright file="PatchManagerView.xaml.cs" company="SysAdminX">
//     Copyright (c) SysAdminX. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System.Windows;
using System.Windows.Controls;
using SysAdminX.PatchManager.ViewModels;

namespace SysAdminX.PatchManager.Views;

/// <summary>
/// Interaction logic for PatchManagerView.xaml
/// </summary>
public partial class PatchManagerView : Page
{
    public PatchManagerViewModel ViewModel { get; }

    public PatchManagerView(PatchManagerViewModel viewModel)
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
