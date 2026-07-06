// -----------------------------------------------------------------------
// <copyright file="SoftwareManagerViewModel.cs" company="SysAdminX">
//     Copyright (c) SysAdminX. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using SysAdminX.Core.Interfaces;
using SysAdminX.Core.Models;
using SysAdminX.SoftwareManager.Models;

namespace SysAdminX.SoftwareManager.ViewModels;

/// <summary>
/// ViewModel for the Software Manager module.
///
/// Improvements applied:
///   - Real <see cref="CancellationToken"/> propagation (was None everywhere).
///   - <see cref="IsLoading"/> is now reset in <c>finally</c> so an exception
///     can no longer leave the spinner stuck on forever.
///   - Toast notifications on install / uninstall success or failure.
///   - Cancellation token source field so a long-running install can be
///     cancelled by navigating away or by an explicit "Cancel" button.
/// </summary>
public partial class SoftwareManagerViewModel : ObservableObject
{
    private readonly ILogger<SoftwareManagerViewModel> _logger;
    private readonly ISoftwareManagerService _softwareService;
    private readonly IToastNotificationService _toastService;
    private CancellationTokenSource? _installCts;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    private string _searchQuery = string.Empty;

    [ObservableProperty]
    private SoftwareItemModel? _selectedSoftware;

    /// <summary>True when an install or uninstall is in progress.</summary>
    [ObservableProperty]
    private bool _isInstalling;

    /// <summary>Human-readable label of the app currently being installed / uninstalled.</summary>
    [ObservableProperty]
    private string _installProgressText = string.Empty;

    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    public ObservableCollection<SoftwareItemModel> SoftwareList { get; } = new();
    public ObservableCollection<SoftwareItemModel> FilteredSoftwareList { get; } = new();
    public ObservableCollection<PopularAppModel> PopularApps { get; } = new();

    public SoftwareManagerViewModel(
        ILogger<SoftwareManagerViewModel> logger,
        ISoftwareManagerService softwareService,
        IToastNotificationService toastService)
    {
        _logger = logger;
        _softwareService = softwareService;
        _toastService = toastService;
        InitializePopularApps();
    }

    private void InitializePopularApps()
    {
        PopularApps.Add(new PopularAppModel { Name = "Google Chrome", WingetId = "Google.Chrome", Icon = "Web" });
        PopularApps.Add(new PopularAppModel { Name = "VLC Media Player", WingetId = "VideoLAN.VLC", Icon = "Play" });
        PopularApps.Add(new PopularAppModel { Name = "7-Zip", WingetId = "7zip.7zip", Icon = "FolderZip" });
        PopularApps.Add(new PopularAppModel { Name = "Notepad++", WingetId = "Notepad++.Notepad++", Icon = "FileDocument" });
        PopularApps.Add(new PopularAppModel { Name = "Visual Studio Code", WingetId = "Microsoft.VisualStudioCode", Icon = "CodeBraces" });
    }

    [RelayCommand]
    private async Task LoadSoftwareAsync(CancellationToken ct)
    {
        IsLoading = true;
        ErrorMessage = string.Empty;
        SoftwareList.Clear();
        FilteredSoftwareList.Clear();

        try
        {
            var result = await _softwareService.GetInstalledSoftwareAsync(ct);
            if (result.IsSuccess && result.Value != null)
            {
                foreach (var item in result.Value.OrderBy(s => s.DisplayName))
                {
                    SoftwareList.Add(item);
                }
                ApplyFilter();
                _logger.LogInformation("Loaded {Count} installed applications.", SoftwareList.Count);
            }
            else
            {
                ErrorMessage = result.ErrorMessage ?? "Failed to load software.";
                _toastService.ShowError("Software list failed to load", ErrorMessage);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Software list load cancelled.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error loading software list");
            ErrorMessage = ex.Message;
            _toastService.ShowError("Software list failed to load", ex.Message);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task UninstallAsync(CancellationToken ct)
    {
        if (SelectedSoftware == null) return;

        if (string.IsNullOrEmpty(SelectedSoftware.UninstallString))
        {
            ErrorMessage = "Uninstall string is missing for this application.";
            _toastService.ShowWarning("Cannot uninstall", "Uninstall string is missing for this application.");
            return;
        }

        IsInstalling = true;
        InstallProgressText = $"Uninstalling {SelectedSoftware.DisplayName}...";
        ErrorMessage = string.Empty;
        _installCts = CancellationTokenSource.CreateLinkedTokenSource(ct);

        try
        {
            var displayName = SelectedSoftware.DisplayName;
            var result = await _softwareService.UninstallSoftwareAsync(
                SelectedSoftware.UninstallString, displayName, _installCts.Token);

            if (!result.IsSuccess)
            {
                ErrorMessage = result.ErrorMessage ?? "Uninstall failed.";
                _toastService.ShowError($"Uninstall failed: {displayName}", ErrorMessage);
            }
            else
            {
                _toastService.ShowSuccess($"Uninstalled {displayName}",
                    "The uninstaller has completed. Reloading software list.");
                // Uninstallation launches a wizard which the user must close.
                // Brief wait so the registry entry is gone before we rescan.
                await Task.Delay(2000, _installCts.Token);
                await LoadSoftwareAsync(ct);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Uninstall cancelled for {App}", SelectedSoftware.DisplayName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Uninstall threw an exception");
            _toastService.ShowError($"Uninstall failed: {SelectedSoftware.DisplayName}", ex.Message);
        }
        finally
        {
            _installCts?.Dispose();
            _installCts = null;
            IsInstalling = false;
            InstallProgressText = string.Empty;
        }
    }

    [RelayCommand]
    private async Task InstallAppAsync(PopularAppModel app, CancellationToken ct)
    {
        if (app == null) return;

        IsInstalling = true;
        InstallProgressText = $"Installing {app.Name}...";
        ErrorMessage = string.Empty;
        _installCts = CancellationTokenSource.CreateLinkedTokenSource(ct);

        try
        {
            var result = await _softwareService.InstallAppViaWingetAsync(app.WingetId, _installCts.Token);
            if (!result.IsSuccess)
            {
                ErrorMessage = result.ErrorMessage ?? $"Failed to install {app.Name}.";
                _toastService.ShowError($"Install failed: {app.Name}", ErrorMessage);
            }
            else
            {
                _toastService.ShowSuccess($"Installed {app.Name}",
                    "winget reported success. Reloading software list.");
                await Task.Delay(2000, _installCts.Token);
                await LoadSoftwareAsync(ct);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Install cancelled for {App}", app.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Install threw an exception for {App}", app.Name);
            _toastService.ShowError($"Install failed: {app.Name}", ex.Message);
        }
        finally
        {
            _installCts?.Dispose();
            _installCts = null;
            IsInstalling = false;
            InstallProgressText = string.Empty;
        }
    }

    /// <summary>
    /// Cancels any in-flight install / uninstall. Bound to a "Cancel" button
    /// in the UI that becomes visible when <see cref="IsInstalling"/> is true.
    /// </summary>
    [RelayCommand]
    private void CancelInstall()
    {
        try { _installCts?.Cancel(); } catch (ObjectDisposedException) { /* already disposed */ }
    }

    partial void OnSearchQueryChanged(string value)
    {
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        FilteredSoftwareList.Clear();
        var query = SearchQuery?.ToLowerInvariant() ?? string.Empty;

        var filtered = string.IsNullOrWhiteSpace(query)
            ? SoftwareList
            : SoftwareList.Where(s =>
                (s.DisplayName?.ToLowerInvariant().Contains(query) == true) ||
                (s.Publisher?.ToLowerInvariant().Contains(query) == true));

        foreach (var item in filtered)
        {
            FilteredSoftwareList.Add(item);
        }
    }
}
