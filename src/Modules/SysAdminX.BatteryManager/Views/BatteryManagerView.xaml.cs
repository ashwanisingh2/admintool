// -----------------------------------------------------------------------
// <copyright file="BatteryManagerView.xaml.cs" company="SysAdminX">
//     Copyright (c) SysAdminX. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Windows;
using System.Windows.Controls;
using SysAdminX.BatteryManager.ViewModels;

namespace SysAdminX.BatteryManager.Views;

public partial class BatteryManagerView : Page
{
    public BatteryManagerView(BatteryManagerViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    private async void OnPageLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is BatteryManagerViewModel vm)
        {
            await vm.InitializeAsync(System.Threading.CancellationToken.None);
        }
    }
}
