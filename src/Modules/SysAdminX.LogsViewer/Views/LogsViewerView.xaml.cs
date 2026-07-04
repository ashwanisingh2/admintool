// -----------------------------------------------------------------------
// <copyright file="LogsViewerView.xaml.cs" company="SysAdminX">
//     Copyright (c) SysAdminX. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System.Windows.Controls;
using SysAdminX.LogsViewer.ViewModels;

namespace SysAdminX.LogsViewer.Views;

/// <summary>
/// Interaction logic for LogsViewerView.xaml
/// </summary>
public partial class LogsViewerView : Page
{
    public LogsViewerViewModel ViewModel { get; }

    public LogsViewerView(LogsViewerViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = ViewModel;

        InitializeComponent();
        Loaded += (s, e) => ViewModel.RefreshLogsCommand.Execute(null);
    }
}
