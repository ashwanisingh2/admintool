// -----------------------------------------------------------------------
// <copyright file="SecurityCenterView.xaml.cs" company="SysAdminX">
//     Copyright (c) SysAdminX. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System.Windows.Controls;
using SysAdminX.SecurityCenter.ViewModels;

namespace SysAdminX.SecurityCenter.Views;

/// <summary>
/// Interaction logic for SecurityCenterView.xaml
/// </summary>
public partial class SecurityCenterView : Page
{
    public SecurityCenterViewModel ViewModel { get; }

    public SecurityCenterView(SecurityCenterViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = ViewModel;

        InitializeComponent();
        Loaded += (s, e) => ViewModel.LoadDataCommand.Execute(null);
    }
}
