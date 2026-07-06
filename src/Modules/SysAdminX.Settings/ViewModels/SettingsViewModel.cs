// -----------------------------------------------------------------------
// <copyright file="SettingsViewModel.cs" company="SysAdminX">
//     Copyright (c) SysAdminX. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using SysAdminX.Core.Interfaces;
using SysAdminX.Core.Models;
using SysAdminX.Settings.Services;
using Wpf.Ui.Appearance;

namespace SysAdminX.Settings.ViewModels;

/// <summary>
/// One row in the Visible Modules list in Settings.
/// </summary>
public partial class ModuleToggleViewModel : ObservableObject
{
    /// <summary>The page-type key written to <c>AppConfigModel.HiddenModules</c>.</summary>
    public string PageTypeKey { get; set; } = string.Empty;

    /// <summary>Human-readable name shown in the UI.</summary>
    public string DisplayName { get; set; } = string.Empty;

    [ObservableProperty]
    private bool _isVisible = true;

    partial void OnIsVisibleChanged(bool value)
    {
        VisibleChanged?.Invoke(this, value);
    }

    /// <summary>Raised when the user toggles a module. Used to update HiddenModules set.</summary>
    public event EventHandler<bool>? VisibleChanged;
}

/// <summary>
/// ViewModel for the Settings module.
///
/// Improvements applied:
///   - LoadSettingsAsync and SaveSettingsAsync wrapped in try/catch/finally.
///   - Real cancellation token propagation.
///   - Toast notifications on save outcome.
///   - New "Visible Modules" section lets the user hide modules from the
///     sidebar. Hidden modules' page types are written to
///     <c>AppConfigModel.HiddenModules</c> and the sidebar is rebuilt on
///     next startup.
///   - New "Check for Updates" button pings GitHub releases and shows the
///     latest version info as a toast + inline status text.
/// </summary>
public partial class SettingsViewModel : ObservableObject
{
    private readonly ILogger<SettingsViewModel> _logger;
    private readonly ISettingsService _settingsService;
    private readonly IToastNotificationService _toastService;
    private readonly IUpdateCheckService? _updateCheckService;

    [ObservableProperty]
    private AppConfigModel _config = new();

    [ObservableProperty]
    private bool _isSaved;

    [ObservableProperty]
    private bool _isSaving;

    [ObservableProperty]
    private bool _isCheckingUpdates;

    [ObservableProperty]
    private string _updateStatusText = "Click to check GitHub for a newer release.";

    /// <summary>Rows in the Visible Modules list.</summary>
    public ObservableCollection<ModuleToggleViewModel> ModuleToggles { get; } = new();

    /// <summary>
    /// All known module page types and their display names. Used to populate
    /// the Visible Modules list. The order matches the sidebar order in
    /// <c>MainWindow.xaml</c> so toggling matches what the user sees.
    /// </summary>
    private static readonly (string Key, string DisplayName)[] KnownModules = new[]
    {
        ("DashboardView",       "Dashboard"),
        ("DeviceDetailsView",   "Device Details"),
        ("DriverManagerView",   "Driver Manager"),
        ("PatchManagerView",    "Patch Manager"),
        ("BatteryManagerView",  "Battery Manager"),
        ("NetworkToolkitView",  "Network Toolkit"),
        ("TroubleshootingView", "Troubleshooting"),
        ("ServiceManagerView",  "Service Manager"),
        ("SystemCleanupView",   "System Cleanup"),
        ("SoftwareManagerView", "Software Manager"),
        ("PortableToolsView",   "Portable Tools"),
        ("OneClickCareView",    "One Click Care"),
        ("AutoPilotView",       "Auto Pilot"),
        ("SystemRestoreView",   "System Restore"),
        ("PrivacyCleanerView",  "Privacy Cleaner"),
        ("BrowserRepairView",   "Browser Repair"),
        ("PerformanceModeView", "Performance Mode"),
        ("StartupManagerView",  "Startup Manager"),
        ("LargeFileFinderView", "Large File Finder"),
        ("RegistryManagerView", "Registry Manager"),
        ("AIAssistantView",     "AI Assistant"),
        ("RemoteSupportView",   "Remote Support"),
        ("ReportsView",         "Reports"),
        ("LogsViewerView",      "Logs Viewer"),
        ("SecurityCenterView",  "Security Center"),
        ("SettingsView",        "Settings"),
    };

    public SettingsViewModel(
        ILogger<SettingsViewModel> logger,
        ISettingsService settingsService,
        IToastNotificationService toastService,
        IUpdateCheckService? updateCheckService = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        _toastService = toastService ?? throw new ArgumentNullException(nameof(toastService));
        _updateCheckService = updateCheckService;
    }

    [RelayCommand]
    public async Task LoadSettingsAsync(CancellationToken ct = default)
    {
        try
        {
            Config = await _settingsService.LoadSettingsAsync(ct);
            RebuildModuleToggles();
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Load settings cancelled.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load settings.");
            _toastService.ShowError("Failed to load settings", ex.Message);
        }
    }

    /// <summary>
    /// Rebuilds the Visible Modules list from the current Config's HiddenModules
    /// set. Subscribes to each row's VisibleChanged event so we can update the
    /// set in real time.
    /// </summary>
    private void RebuildModuleToggles()
    {
        foreach (var t in ModuleToggles)
        {
            t.VisibleChanged -= OnModuleVisibleChanged;
        }
        ModuleToggles.Clear();

        foreach (var (key, name) in KnownModules)
        {
            var row = new ModuleToggleViewModel
            {
                PageTypeKey = key,
                DisplayName = name,
                IsVisible = !Config.HiddenModules.Contains(key)
            };
            row.VisibleChanged += OnModuleVisibleChanged;
            ModuleToggles.Add(row);
        }
    }

    private void OnModuleVisibleChanged(object? sender, bool isVisible)
    {
        if (sender is not ModuleToggleViewModel row) return;
        if (isVisible)
            Config.HiddenModules.Remove(row.PageTypeKey);
        else
            Config.HiddenModules.Add(row.PageTypeKey);
    }

    [RelayCommand]
    public async Task SaveSettingsAsync(CancellationToken ct = default)
    {
        if (IsSaving) return;
        IsSaving = true;
        IsSaved = false;

        try
        {
            await _settingsService.SaveSettingsAsync(Config, ct);
            IsSaved = true;

            // Apply theme immediately
            if (Config.Theme.Equals("Dark", StringComparison.OrdinalIgnoreCase))
            {
                ApplicationThemeManager.Apply(ApplicationTheme.Dark);
            }
            else
            {
                ApplicationThemeManager.Apply(ApplicationTheme.Light);
            }

            _toastService.ShowSuccess("Settings saved", "Your preferences have been saved.");

            // Let the 'Saved' indicator show for a few seconds
            await Task.Delay(3000, ct);
            IsSaved = false;
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Save settings cancelled.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save settings.");
            _toastService.ShowError("Failed to save settings", ex.Message);
        }
        finally
        {
            IsSaving = false;
        }
    }

    /// <summary>
    /// Manually triggers an update check against GitHub. Bound to the
    /// "Check Now" button in Settings.
    /// </summary>
    [RelayCommand]
    public async Task CheckForUpdatesAsync(CancellationToken ct = default)
    {
        if (IsCheckingUpdates) return;
        if (_updateCheckService == null)
        {
            UpdateStatusText = "Update checker is not available in this build.";
            _toastService.ShowWarning("Update checker unavailable",
                "IUpdateCheckService is not registered in the DI container.");
            return;
        }

        IsCheckingUpdates = true;
        UpdateStatusText = "Checking GitHub for the latest release...";

        try
        {
            var repo = string.IsNullOrWhiteSpace(Config.UpdateRepository)
                ? "ashwanisingh2/admintool"
                : Config.UpdateRepository;

            var release = await _updateCheckService.GetLatestReleaseAsync(repo, ct);
            if (release == null)
            {
                UpdateStatusText = "Could not fetch release info. Check your internet connection.";
                _toastService.ShowWarning("Update check failed",
                    "GitHub did not return a release. You may be rate-limited.");
                return;
            }

            // Current version comes from the assembly.
            var currentVersion = System.Reflection.Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "0.0.0.0";
            var isNewer = _updateCheckService.IsNewerVersion(currentVersion, release.TagName);

            if (isNewer)
            {
                UpdateStatusText = $"New version available: {release.TagName} (you have v{currentVersion})";
                _toastService.ShowSuccess($"Update available: {release.TagName}",
                    $"You have v{currentVersion}. Click to open the release page.");

                // Offer to open the release page in the browser.
                if (!string.IsNullOrEmpty(release.HtmlUrl) &&
                    MessageBox.Show(
                        $"A new version ({release.TagName}) is available.\n\nOpen the release page in your browser?",
                        "Update Available",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Information) == MessageBoxResult.Yes)
                {
                    try
                    {
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = release.HtmlUrl,
                            UseShellExecute = true
                        });
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to open release URL.");
                    }
                }
            }
            else
            {
                UpdateStatusText = $"You're up to date. Latest on GitHub: {release.TagName}";
                _toastService.ShowSuccess("Up to date",
                    $"You have v{currentVersion}, latest on GitHub is {release.TagName}.");
            }
        }
        catch (OperationCanceledException)
        {
            UpdateStatusText = "Update check cancelled.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Update check threw an exception.");
            UpdateStatusText = "Update check failed: " + ex.Message;
            _toastService.ShowError("Update check failed", ex.Message);
        }
        finally
        {
            IsCheckingUpdates = false;
        }
    }
}
