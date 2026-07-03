// -----------------------------------------------------------------------
// <copyright file="TroubleshootingView.xaml.cs" company="SysAdminX">
//     Copyright (c) SysAdminX. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System.Windows.Controls;
using SysAdminX.Troubleshooting.ViewModels;

namespace SysAdminX.Troubleshooting.Views;

/// <summary>
/// Interaction logic for TroubleshootingView.xaml
/// </summary>
public partial class TroubleshootingView : Page
{
    public TroubleshootingViewModel ViewModel { get; }

    public TroubleshootingView(TroubleshootingViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = ViewModel;

        InitializeComponent();
    }
}
