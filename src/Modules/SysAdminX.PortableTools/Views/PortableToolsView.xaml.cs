// -----------------------------------------------------------------------
// <copyright file="PortableToolsView.xaml.cs" company="SysAdminX">
//     Copyright (c) SysAdminX. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System.Windows;
using System.Windows.Controls;
using SysAdminX.PortableTools.ViewModels;

namespace SysAdminX.PortableTools.Views;

public partial class PortableToolsView : Page
{
    private readonly PortableToolsViewModel _viewModel;

    public PortableToolsView(PortableToolsViewModel viewModel)
    {
        _viewModel = viewModel;
        DataContext = _viewModel;
        InitializeComponent();
    }

    private void OnPageLoaded(object sender, RoutedEventArgs e)
    {
        if (_viewModel.Tools.Count == 0 && _viewModel.LoadToolsCommand.CanExecute(null))
        {
            _viewModel.LoadToolsCommand.Execute(null);
        }
    }
}
