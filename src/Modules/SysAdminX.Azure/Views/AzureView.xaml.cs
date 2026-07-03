// -----------------------------------------------------------------------
// <copyright file="AzureView.xaml.cs" company="SysAdminX">
//     Copyright (c) SysAdminX. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System.Windows.Controls;
using SysAdminX.Azure.ViewModels;

namespace SysAdminX.Azure.Views;

/// <summary>
/// Interaction logic for AzureView.xaml
/// </summary>
public partial class AzureView : Page
{
    public AzureViewModel ViewModel { get; }

    public AzureView(AzureViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = ViewModel;

        InitializeComponent();
    }
}
