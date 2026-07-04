// -----------------------------------------------------------------------
// <copyright file="ReportsView.xaml.cs" company="SysAdminX">
//     Copyright (c) SysAdminX. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System.Windows.Controls;
using SysAdminX.Reports.ViewModels;

namespace SysAdminX.Reports.Views;

/// <summary>
/// Interaction logic for ReportsView.xaml
/// </summary>
public partial class ReportsView : Page
{
    public ReportsViewModel ViewModel { get; }

    public ReportsView(ReportsViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = ViewModel;

        InitializeComponent();
        Loaded += (s, e) => ViewModel.LoadHistoryCommand.Execute(null);
    }
}
