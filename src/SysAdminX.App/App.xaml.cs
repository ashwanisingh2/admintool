// -----------------------------------------------------------------------
// <copyright file="App.xaml.cs" company="SysAdminX">
//     Copyright (c) SysAdminX. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.IO;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using SysAdminX.Core.Interfaces;
using SysAdminX.Dashboard.ViewModels;
using SysAdminX.Dashboard.Views;
using SysAdminX.Infrastructure;
using SysAdminX.Shell.Services;
using SysAdminX.Shell.ViewModels;
using SysAdminX.Shell.Views;
using Wpf.Ui;

namespace SysAdminX.App;

/// <summary>
/// Application entry point with Dependency Injection container setup.
/// Configures all services, ViewModels, and Views.
/// </summary>
public partial class App : Application
{
    private ServiceProvider? _serviceProvider;

    /// <summary>
    /// Gets the DI service provider.
    /// </summary>
    public static IServiceProvider? Services { get; private set; }

    /// <summary>
    /// Handles application startup — configures DI and shows main window.
    /// </summary>
    private async void OnStartup(object sender, StartupEventArgs e)
    {
        // Configure Serilog
        var logPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "SysAdminX", "Logs", "sysadminx-.log");

        var logDir = Path.GetDirectoryName(logPath);
        if (!string.IsNullOrEmpty(logDir) && !Directory.Exists(logDir))
        {
            Directory.CreateDirectory(logDir);
        }

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.File(logPath,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7,
                outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();

        // Configure DI container
        var services = new ServiceCollection();
        ConfigureServices(services);
        _serviceProvider = services.BuildServiceProvider();
        Services = _serviceProvider;

        // Show main window
        var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
        
        // Apply saved theme
        var settingsService = _serviceProvider.GetRequiredService<SysAdminX.Settings.Services.ISettingsService>();
        var config = await settingsService.LoadSettingsAsync();
        
        if (config.Theme.Equals("Dark", System.StringComparison.OrdinalIgnoreCase))
        {
            Wpf.Ui.Appearance.ApplicationThemeManager.Apply(Wpf.Ui.Appearance.ApplicationTheme.Dark);
        }
        else
        {
            Wpf.Ui.Appearance.ApplicationThemeManager.Apply(Wpf.Ui.Appearance.ApplicationTheme.Light);
        }

        mainWindow.Show();

        // Set up NavigationView page provider
        var navView = mainWindow.GetNavigationView();
        navView.SetPageService(_serviceProvider.GetRequiredService<IPageService>());

        var navigationService = _serviceProvider.GetRequiredService<SysAdminX.Core.Interfaces.INavigationService>() as SysAdminX.Shell.Services.NavigationService;
        navigationService?.SetNavigationView(navView);

        // Navigate to dashboard
        navigationService?.NavigateTo(typeof(DashboardView));

        Log.Information("SysAdminX started successfully");
    }

    /// <summary>
    /// Configures all application services, ViewModels, and Views in the DI container.
    /// </summary>
    private static void ConfigureServices(IServiceCollection services)
    {
        // Logging
        services.AddLogging(builder =>
        {
            builder.ClearProviders();
            builder.AddSerilog(dispose: true);
        });

        // Infrastructure
        services.AddSingleton<ILogService, LogService>();
        services.AddSingleton<IWmiService, WmiService>();
        services.AddSingleton<IPowerShellService, PowerShellService>();
        services.AddSingleton<ISystemHealthService, SystemHealthService>();
        services.AddSingleton<IRegistryService, RegistryService>();
        services.AddSingleton<IProcessExecutorService, ProcessExecutorService>();

        // Navigation
        services.AddSingleton<SysAdminX.Shell.Services.NavigationService>();
        services.AddSingleton<SysAdminX.Core.Interfaces.INavigationService>(sp => sp.GetRequiredService<SysAdminX.Shell.Services.NavigationService>());
        services.AddSingleton<IPageService, NavigationViewPageProvider>();

        // Shell
        services.AddSingleton<MainWindowViewModel>();
        services.AddSingleton<MainWindow>();

        // Modules - Dashboard
        services.AddTransient<DashboardViewModel>();
        services.AddTransient<DashboardView>();

        // Modules - Device Details
        services.AddSingleton<SysAdminX.DeviceDetails.Services.IDeviceDetailsService, SysAdminX.DeviceDetails.Services.DeviceDetailsService>();
        services.AddTransient<SysAdminX.DeviceDetails.ViewModels.DeviceDetailsViewModel>();
        services.AddTransient<SysAdminX.DeviceDetails.Views.DeviceDetailsView>();

        // Modules - Driver Manager
        services.AddSingleton<SysAdminX.DriverManager.Services.IDriverManagerService, SysAdminX.DriverManager.Services.DriverManagerService>();
        services.AddTransient<SysAdminX.DriverManager.ViewModels.DriverManagerViewModel>();
        services.AddTransient<SysAdminX.DriverManager.Views.DriverManagerView>();

        // Modules - Patch Manager
        services.AddSingleton<SysAdminX.PatchManager.Services.IPatchManagerService, SysAdminX.PatchManager.Services.PatchManagerService>();
        services.AddTransient<SysAdminX.PatchManager.ViewModels.PatchManagerViewModel>();
        services.AddTransient<SysAdminX.PatchManager.Views.PatchManagerView>();

        // Modules - Network Toolkit
        services.AddSingleton<SysAdminX.NetworkToolkit.Services.INetworkService, SysAdminX.NetworkToolkit.Services.NetworkService>();
        services.AddTransient<SysAdminX.NetworkToolkit.ViewModels.NetworkToolkitViewModel>();
        services.AddTransient<SysAdminX.NetworkToolkit.Views.NetworkToolkitView>();

        // Modules - Troubleshooting
        services.AddSingleton<SysAdminX.Troubleshooting.Services.ITroubleshootingService, SysAdminX.Troubleshooting.Services.TroubleshootingService>();
        services.AddTransient<SysAdminX.Troubleshooting.ViewModels.TroubleshootingViewModel>();
        services.AddTransient<SysAdminX.Troubleshooting.Views.TroubleshootingView>();

        // Modules - AI Assistant
        services.AddSingleton<SysAdminX.AIAssistant.Services.IAIAssistantService, SysAdminX.AIAssistant.Services.AIAssistantService>();
        services.AddTransient<SysAdminX.AIAssistant.ViewModels.AIAssistantViewModel>();
        services.AddTransient<SysAdminX.AIAssistant.Views.AIAssistantView>();

        // Modules - Reports
        services.AddSingleton<SysAdminX.Reports.Services.IReportService, SysAdminX.Reports.Services.ReportService>();
        services.AddTransient<SysAdminX.Reports.ViewModels.ReportsViewModel>();
        services.AddTransient<SysAdminX.Reports.Views.ReportsView>();

        // Modules - Settings
        services.AddSingleton<SysAdminX.Settings.Services.ISettingsService, SysAdminX.Settings.Services.SettingsService>();
        services.AddTransient<SysAdminX.Settings.ViewModels.SettingsViewModel>();
        services.AddTransient<SysAdminX.Settings.Views.SettingsView>();

        // Modules - Logs Viewer
        services.AddSingleton<SysAdminX.LogsViewer.Services.ILogsService, SysAdminX.LogsViewer.Services.LogsService>();
        services.AddTransient<SysAdminX.LogsViewer.ViewModels.LogsViewerViewModel>();
        services.AddTransient<SysAdminX.LogsViewer.Views.LogsViewerView>();

        // Modules - Security Center
        services.AddSingleton<SysAdminX.SecurityCenter.Services.ISecurityService, SysAdminX.SecurityCenter.Services.SecurityService>();
        services.AddTransient<SysAdminX.SecurityCenter.ViewModels.SecurityCenterViewModel>();
        services.AddTransient<SysAdminX.SecurityCenter.Views.SecurityCenterView>();



        // Modules - Remote Support
        services.AddSingleton<SysAdminX.RemoteSupport.Services.IRemoteSupportService, SysAdminX.RemoteSupport.Services.RemoteSupportService>();
        services.AddTransient<SysAdminX.RemoteSupport.ViewModels.RemoteSupportViewModel>();
        services.AddTransient<SysAdminX.RemoteSupport.Views.RemoteSupportView>();

        // Modules - Battery Manager
        services.AddSingleton<SysAdminX.BatteryManager.Services.IBatteryManagerService, SysAdminX.BatteryManager.Services.BatteryManagerService>();
        services.AddTransient<SysAdminX.BatteryManager.ViewModels.BatteryManagerViewModel>();
        services.AddTransient<SysAdminX.BatteryManager.Views.BatteryManagerView>();

        // Modules - Service Manager
        services.AddSingleton<SysAdminX.Core.Interfaces.IServiceManagerService, SysAdminX.Infrastructure.Services.ServiceManagerService>();
        services.AddTransient<SysAdminX.ServiceManager.ViewModels.ServiceManagerViewModel>();
        services.AddTransient<SysAdminX.ServiceManager.Views.ServiceManagerView>();

        // Modules - System Cleanup
        services.AddSingleton<SysAdminX.Core.Interfaces.ISystemCleanupService, SysAdminX.Infrastructure.Services.SystemCleanupService>();
        services.AddTransient<SysAdminX.SystemCleanup.ViewModels.SystemCleanupViewModel>();
        services.AddTransient<SysAdminX.SystemCleanup.Views.SystemCleanupView>();

        // Modules - Software Manager
        services.AddSingleton<SysAdminX.Core.Interfaces.ISoftwareManagerService, SysAdminX.Infrastructure.Services.SoftwareManagerService>();
        services.AddTransient<SysAdminX.SoftwareManager.ViewModels.SoftwareManagerViewModel>();
        services.AddTransient<SysAdminX.SoftwareManager.Views.SoftwareManagerView>();

        // Modules - Portable Tools
        services.AddSingleton<SysAdminX.Core.Interfaces.IPortableToolsService, SysAdminX.Infrastructure.Services.PortableToolsService>();
        services.AddTransient<SysAdminX.PortableTools.ViewModels.PortableToolsViewModel>();
        services.AddTransient<SysAdminX.PortableTools.Views.PortableToolsView>();
    }

    /// <summary>
    /// Handles application exit — cleanup resources.
    /// </summary>
    private void OnExit(object sender, ExitEventArgs e)
    {
        Log.Information("SysAdminX shutting down");
        Log.CloseAndFlush();
        _serviceProvider?.Dispose();
    }

    /// <summary>
    /// Handles unhandled dispatcher exceptions to prevent crashes.
    /// </summary>
    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        Log.Fatal(e.Exception, "Unhandled exception in dispatcher");
        MessageBox.Show(
            $"An unexpected error occurred:\n\n{e.Exception.Message}\n\nThe error has been logged.",
            "SysAdminX — Error",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
        e.Handled = true;
    }
}
