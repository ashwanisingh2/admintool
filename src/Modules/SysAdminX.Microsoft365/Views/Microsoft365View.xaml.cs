// -----------------------------------------------------------------------
// <copyright file="Microsoft365View.xaml.cs" company="SysAdminX">
//     Copyright (c) SysAdminX. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System.Windows.Controls;
using SysAdminX.Microsoft365.ViewModels;

namespace SysAdminX.Microsoft365.Views;

/// <summary>
/// Interaction logic for Microsoft365View.xaml
/// </summary>
public partial class Microsoft365View : Page
{
    public Microsoft365ViewModel ViewModel { get; }

    public Microsoft365View(Microsoft365ViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = ViewModel;

        InitializeComponent();
    }
}
