// -----------------------------------------------------------------------
// <copyright file="MainWindow.xaml.cs" company="SysAdminX">
//     Copyright (c) SysAdminX. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System.Windows;
using SysAdminX.Shell.ViewModels;
using Wpf.Ui;
using Wpf.Ui.Controls;

namespace SysAdminX.Shell.Views;

/// <summary>
/// Main application window with Fluent Design navigation sidebar.
/// Code-behind is minimal — only UI-specific initialization logic.
/// </summary>
public partial class MainWindow : FluentWindow
{
    private readonly MainWindowViewModel _viewModel;

    /// <summary>
    /// Initializes a new instance of the <see cref="MainWindow"/> class.
    /// </summary>
    /// <param name="viewModel">The main window ViewModel.</param>
    public MainWindow(MainWindowViewModel viewModel)
    {
        _viewModel = viewModel;
        DataContext = _viewModel;

        InitializeComponent();

        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        // Apply the Mica backdrop for Windows 11 style
        Wpf.Ui.Appearance.SystemThemeWatcher.Watch(this);
    }

    /// <summary>
    /// Gets the navigation view for page registration.
    /// </summary>
    public Wpf.Ui.Controls.NavigationView GetNavigationView() => NavigationView;
}
