// -----------------------------------------------------------------------
// <copyright file="RemoteSupportView.xaml.cs" company="SysAdminX">
//     Copyright (c) SysAdminX. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System.Windows.Controls;
using SysAdminX.RemoteSupport.ViewModels;

namespace SysAdminX.RemoteSupport.Views;

/// <summary>
/// Interaction logic for RemoteSupportView.xaml
/// </summary>
public partial class RemoteSupportView : Page
{
    public RemoteSupportViewModel ViewModel { get; }

    public RemoteSupportView(RemoteSupportViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = ViewModel;

        InitializeComponent();
    }
}
