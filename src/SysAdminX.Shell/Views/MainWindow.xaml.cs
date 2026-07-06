// -----------------------------------------------------------------------
// <copyright file="MainWindow.xaml.cs" company="SysAdminX">
//     Copyright (c) SysAdminX. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using SysAdminX.Core.Interfaces;
using SysAdminX.Shell.Services;
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
    /// <param name="serviceProvider">DI service provider, used to attach the snackbar service on first load.</param>
    public MainWindow(MainWindowViewModel viewModel, IServiceProvider serviceProvider)
    {
        _viewModel = viewModel;
        DataContext = _viewModel;

        InitializeComponent();

        Loaded += (s, e) => OnLoaded(s, e, serviceProvider);
    }

    private void OnLoaded(object sender, RoutedEventArgs e, IServiceProvider serviceProvider)
    {
        // Apply the Mica backdrop for Windows 11 style
        Wpf.Ui.Appearance.SystemThemeWatcher.Watch(this);

        // Wire the WPF-UI snackbar host to the ISnackbarService singleton so
        // that any ViewModel injecting ISnackbarService can show toasts. Also
        // attach the same instance to our cross-module IToastNotificationService.
        try
        {
            var snackbarService = serviceProvider.GetService(typeof(ISnackbarService)) as ISnackbarService;
            if (snackbarService is SnackbarService concrete)
            {
                concrete.SetSnackbarPresenter(SnackbarPresenter);
            }

            if (serviceProvider.GetService(typeof(IToastNotificationService)) is ToastNotificationService toastService)
            {
                toastService.AttachSnackbarService(snackbarService ?? new SnackbarService());
            }
        }
        catch
        {
            // SnackbarPresenter may not be available in design-time data contexts
            // or older WPF-UI versions — fail silently.
        }
    }

    /// <summary>
    /// Gets the navigation view for page registration.
    /// </summary>
    public Wpf.Ui.Controls.NavigationView GetNavigationView() => NavigationView;

    private void Window_StateChanged(object sender, System.EventArgs e)
    {
        if (WindowState == WindowState.Minimized)
        {
            Hide();
            TrayIcon.Visibility = Visibility.Visible;
        }
    }

    private void TrayIcon_TrayMouseDoubleClick(object sender, RoutedEventArgs e)
    {
        RestoreWindow();
    }

    private void TrayIconOpen_Click(object sender, RoutedEventArgs e)
    {
        RestoreWindow();
    }

    private void TrayIconExit_Click(object sender, RoutedEventArgs e)
    {
        Application.Current.Shutdown();
    }

    private void RestoreWindow()
    {
        Show();
        WindowState = WindowState.Normal;
        TrayIcon.Visibility = Visibility.Collapsed;
        Activate();
    }
}
