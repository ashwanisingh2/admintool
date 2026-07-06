// -----------------------------------------------------------------------
// <copyright file="MainWindow.xaml.cs" company="SysAdminX">
//     Copyright (c) SysAdminX. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
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
    private readonly INavigationService _navigationService;

    /// <summary>
    /// Maps the user-visible name of each sidebar item to its target page
    /// type. Used by the search box to jump to the right page.
    /// </summary>
    private readonly Dictionary<string, Type> _pageNameMap = new(StringComparer.OrdinalIgnoreCase);

    public MainWindow(
        MainWindowViewModel viewModel,
        IServiceProvider serviceProvider,
        INavigationService navigationService)
    {
        _viewModel = viewModel;
        _navigationService = navigationService;
        DataContext = _viewModel;

        InitializeComponent();

        Loaded += (s, e) => OnLoaded(s, e, serviceProvider);

        // Build the page-name map from the NavigationView items so the search
        // box can resolve "disk" -> DriverManagerView, "wifi" -> NetworkToolkitView, etc.
        BuildPageNameMap();
    }

    private void BuildPageNameMap()
    {
        try
        {
            foreach (var item in NavigationView.MenuItems.OfType<NavigationViewItem>())
            {
                if (item.TargetPageType != null && item.Content is string name)
                {
                    _pageNameMap[name] = item.TargetPageType;
                    // Also map common synonyms so "wifi" finds Network Toolkit,
                    // "ram" finds Device Details, "network" finds Network Toolkit, etc.
                    foreach (var syn in GetSynonyms(name, item.TargetPageType.Name))
                    {
                        _pageNameMap[syn] = item.TargetPageType;
                    }
                }
            }
            foreach (var item in NavigationView.FooterMenuItems.OfType<NavigationViewItem>())
            {
                if (item.TargetPageType != null && item.Content is string name)
                {
                    _pageNameMap[name] = item.TargetPageType;
                    foreach (var syn in GetSynonyms(name, item.TargetPageType.Name))
                    {
                        _pageNameMap[syn] = item.TargetPageType;
                    }
                }
            }
        }
        catch (Exception)
        {
            // Map building is best-effort — search box will just be less useful.
        }
    }

    /// <summary>
    /// Returns common synonyms / keywords for a given module so the sidebar
    /// search box accepts natural-language queries like "wifi", "ram", "dns".
    /// </summary>
    private static IEnumerable<string> GetSynonyms(string displayName, string pageTypeName)
    {
        // Always include the type name without the "View" suffix.
        if (pageTypeName.EndsWith("View"))
            yield return pageTypeName.Substring(0, pageTypeName.Length - 4);

        // Per-module synonyms. Keep them lowercase so the (case-insensitive)
        // lookup in HandleSidebarSearch just works.
        switch (pageTypeName)
        {
            case "DashboardView":       yield return "home"; break;
            case "DeviceDetailsView":    yield return "ram"; yield return "cpu"; yield return "hardware"; yield return "specs"; break;
            case "DriverManagerView":    yield return "driver"; yield return "drivers"; break;
            case "PatchManagerView":     yield return "update"; yield return "updates"; yield return "windows update"; yield return "winget"; break;
            case "BatteryManagerView":   yield return "battery"; yield return "power"; break;
            case "NetworkToolkitView":   yield return "wifi"; yield return "network"; yield return "ping"; yield return "port"; yield return "lan"; break;
            case "TroubleshootingView":  yield return "sfc"; yield return "dism"; yield return "chkdsk"; yield return "dns"; yield return "fix"; break;
            case "ServiceManagerView":   yield return "service"; yield return "services"; break;
            case "SystemCleanupView":    yield return "cleanup"; yield return "clean"; yield return "temp"; yield return "cache"; break;
            case "SoftwareManagerView":  yield return "software"; yield return "uninstall"; yield return "install"; break;
            case "PortableToolsView":    yield return "tools"; yield return "portable"; yield return "sysinternals"; break;
            case "OneClickCareView":     yield return "care"; yield return "oneclick"; yield return "optimize"; break;
            case "AutoPilotView":        yield return "autopilot"; yield return "schedule"; yield return "automatic"; break;
            case "SystemRestoreView":    yield return "restore"; yield return "restore point"; break;
            case "PrivacyCleanerView":   yield return "privacy"; yield return "cookies"; yield return "history"; break;
            case "BrowserRepairView":    yield return "browser"; yield return "chrome"; yield return "edge"; yield return "firefox"; break;
            case "PerformanceModeView":  yield return "performance"; yield return "gaming"; yield return "power plan"; break;
            case "StartupManagerView":   yield return "startup"; yield return "boot"; break;
            case "LargeFileFinderView":  yield return "large files"; yield return "disk space"; yield return "files"; break;
            case "RegistryManagerView":  yield return "registry"; yield return "reg"; break;
            case "AIAssistantView":      yield return "ai"; yield return "chat"; yield return "assistant"; break;
            case "RemoteSupportView":    yield return "remote"; yield return "rdp"; yield return "ssh"; break;
            case "ReportsView":          yield return "report"; yield return "pdf"; yield return "export"; break;
            case "LogsViewerView":       yield return "logs"; yield return "log"; yield return "bsod"; break;
            case "SecurityCenterView":   yield return "security"; yield return "defender"; yield return "firewall"; yield return "antivirus"; break;
            case "SettingsView":         yield return "settings"; yield return "config"; yield return "preferences"; break;
        }
    }

    /// <summary>
    /// Bound to the KeyDown event of the sidebar search box. Enter triggers
    /// navigation to the first matching page.
    /// </summary>
    private void SidebarSearchBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;

        if (sender is Wpf.Ui.Controls.TextBox tb)
        {
            var query = tb.Text?.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(query)) return;

            if (TryResolvePage(query, out var pageType))
            {
                _navigationService.NavigateTo(pageType);
                tb.Text = string.Empty;
                e.Handled = true;
            }
        }
    }

    /// <summary>
    /// Resolves a search query to a page type. Matches against display names,
    /// type names (without "View" suffix), and the synonym list above.
    /// </summary>
    private bool TryResolvePage(string query, out Type pageType)
    {
        // Exact match
        if (_pageNameMap.TryGetValue(query, out pageType))
            return true;

        // Prefix match (e.g. "drive" -> DriverManagerView)
        var hit = _pageNameMap.Keys.FirstOrDefault(k => k.StartsWith(query, StringComparison.OrdinalIgnoreCase));
        if (hit != null)
        {
            pageType = _pageNameMap[hit];
            return true;
        }

        // Substring match (e.g. "manage" -> DriverManagerView, ServiceManagerView, SoftwareManagerView)
        var subHit = _pageNameMap.Keys.FirstOrDefault(k => k.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0);
        if (subHit != null)
        {
            pageType = _pageNameMap[subHit];
            return true;
        }

        pageType = null!;
        return false;
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

        // Apply compact sidebar setting from config
        ApplyCompactSidebarSetting(serviceProvider);

        // Hide any modules the user has disabled in Settings.
        ApplyHiddenModulesSetting(serviceProvider);
    }

    /// <summary>
    /// Reads <c>AppConfigModel.CompactSidebar</c> from the loaded settings and
    /// sets the NavigationView pane mode to LeftFluent (icons only) when true.
    /// </summary>
    private void ApplyCompactSidebarSetting(IServiceProvider serviceProvider)
    {
        try
        {
            var settingsSvc = serviceProvider.GetService(typeof(SysAdminX.Settings.Services.ISettingsService))
                as SysAdminX.Settings.Services.ISettingsService;
            if (settingsSvc == null) return;
            // SettingsService.LoadSettingsAsync is async but we're already on
            // the UI thread after main window load — fire and forget is fine
            // here because we're only reading a flag, not mutating.
            _ = System.Threading.Tasks.Task.Run(async () =>
            {
                try
                {
                    var config = await settingsSvc.LoadSettingsAsync();
                    Dispatcher.Invoke(() =>
                    {
                        NavigationView.PaneDisplayMode = config.CompactSidebar
                            ? NavigationViewPaneDisplayMode.LeftFluent
                            : NavigationViewPaneDisplayMode.Left;
                    });
                }
                catch { /* best-effort */ }
            });
        }
        catch { /* best-effort */ }
    }

    /// <summary>
    /// Reads <c>AppConfigModel.HiddenModules</c> and removes the matching
    /// NavigationViewItems from the sidebar so the user sees a cleaner menu.
    /// </summary>
    private void ApplyHiddenModulesSetting(IServiceProvider serviceProvider)
    {
        try
        {
            var settingsSvc = serviceProvider.GetService(typeof(SysAdminX.Settings.Services.ISettingsService))
                as SysAdminX.Settings.Services.ISettingsService;
            if (settingsSvc == null) return;
            _ = System.Threading.Tasks.Task.Run(async () =>
            {
                try
                {
                    var config = await settingsSvc.LoadSettingsAsync();
                    Dispatcher.Invoke(() =>
                    {
                        HideMatchingItems(NavigationView.MenuItems, config.HiddenModules);
                        HideMatchingItems(NavigationView.FooterMenuItems, config.HiddenModules);
                    });
                }
                catch { /* best-effort */ }
            });
        }
        catch { /* best-effort */ }
    }

    private static void HideMatchingItems(System.Collections.IList items, System.Collections.Generic.HashSet<string> hidden)
    {
        if (hidden == null || hidden.Count == 0) return;
        var toRemove = new List<NavigationViewItem>();
        foreach (var item in items.OfType<NavigationViewItem>())
        {
            if (item.TargetPageType != null && hidden.Contains(item.TargetPageType.Name))
            {
                toRemove.Add(item);
            }
        }
        foreach (var r in toRemove)
        {
            items.Remove(r);
        }
    }

    /// <summary>
    /// Handles global keyboard shortcuts for the whole window.
    ///   Ctrl+1..9  -> jump to the Nth menu item
    ///   Ctrl+R     -> refresh the current page (calls RefreshAsync if VM exposes it)
    ///   Ctrl+,     -> jump to Settings (matches VS Code)
    ///   Ctrl+L     -> jump to Logs Viewer
    ///   Ctrl+F     -> focus the sidebar search box
    /// </summary>
    private void MainWindow_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        // Only handle Ctrl+ shortcuts
        if ((Keyboard.Modifiers & ModifierKeys.Control) != ModifierKeys.Control)
            return;

        try
        {
            switch (e.Key)
            {
                case Key.D1: case Key.D2: case Key.D3: case Key.D4: case Key.D5:
                case Key.D6: case Key.D7: case Key.D8: case Key.D9:
                    if (int.TryParse(e.Key.ToString().Substring(1), out var idx))
                    {
                        var items = NavigationView.MenuItems.OfType<NavigationViewItem>().ToList();
                        if (idx >= 1 && idx <= items.Count)
                        {
                            var item = items[idx - 1];
                            if (item.TargetPageType != null)
                            {
                                _navigationService.NavigateTo(item.TargetPageType);
                                e.Handled = true;
                            }
                        }
                    }
                    break;

                case Key.R:
                    // Try to invoke RefreshAsync on the current page's VM.
                    TryRefreshCurrentPage();
                    e.Handled = true;
                    break;

                case Key.OemComma: // Ctrl+,
                    if (_pageNameMap.TryGetValue("Settings", out var settingsType))
                    {
                        _navigationService.NavigateTo(settingsType);
                        e.Handled = true;
                    }
                    break;

                case Key.L:
                    if (_pageNameMap.TryGetValue("Logs Viewer", out var logsType))
                    {
                        _navigationService.NavigateTo(logsType);
                        e.Handled = true;
                    }
                    break;

                case Key.F:
                    // Focus the sidebar search box
                    if (SidebarSearchBox != null)
                    {
                        SidebarSearchBox.Focus();
                        Keyboard.Focus(SidebarSearchBox);
                        e.Handled = true;
                    }
                    break;
            }
        }
        catch (Exception)
        {
            // Shortcuts are best-effort — don't crash the app if one fails.
        }
    }

    /// <summary>
    /// Best-effort refresh of the current page. Looks for a RefreshCommand or
    /// RefreshAsync method on the current page's DataContext and invokes it.
    /// </summary>
    private void TryRefreshCurrentPage()
    {
        try
        {
            var page = NavigationView.Content as FrameworkElement;
            var dc = page?.DataContext;
            if (dc == null) return;

            // Use reflection to find a RefreshCommand or RefreshAsyncCommand
            var cmdProp = dc.GetType().GetProperty("RefreshCommand");
            if (cmdProp?.GetValue(dc) is ICommand cmd && cmd.CanExecute(null))
            {
                cmd.Execute(null);
            }
        }
        catch { /* best-effort */ }
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
