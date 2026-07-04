// -----------------------------------------------------------------------
// <copyright file="SettingsView.xaml.cs" company="SysAdminX">
//     Copyright (c) SysAdminX. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System.Windows.Controls;
using SysAdminX.Settings.ViewModels;

namespace SysAdminX.Settings.Views;

/// <summary>
/// Interaction logic for SettingsView.xaml
/// </summary>
public partial class SettingsView : Page
{
    public SettingsViewModel ViewModel { get; }

    public SettingsView(SettingsViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = ViewModel;

        InitializeComponent();
        Loaded += (s, e) => ViewModel.LoadSettingsCommand.Execute(null);
    }
}
