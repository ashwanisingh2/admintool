// -----------------------------------------------------------------------
// <copyright file="ActiveDirectoryView.xaml.cs" company="SysAdminX">
//     Copyright (c) SysAdminX. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System.Windows.Controls;
using SysAdminX.ActiveDirectory.ViewModels;

namespace SysAdminX.ActiveDirectory.Views;

/// <summary>
/// Interaction logic for ActiveDirectoryView.xaml
/// </summary>
public partial class ActiveDirectoryView : Page
{
    public ActiveDirectoryViewModel ViewModel { get; }

    public ActiveDirectoryView(ActiveDirectoryViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = ViewModel;

        InitializeComponent();
    }
}
