// -----------------------------------------------------------------------
// <copyright file="PatchManagerViewModel.cs" company="SysAdminX">
//     Copyright (c) SysAdminX. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using SysAdminX.Core.Interfaces;
using SysAdminX.Core.Models;
using SysAdminX.PatchManager.Services;

namespace SysAdminX.PatchManager.ViewModels;

/// <summary>
/// ViewModel for managing Windows Updates and winget-driven software updates.
///
/// Improvements applied:
///   - All async command bodies now wrapped in try/finally so a thrown
///     exception can no longer leave <see cref="IsLoading"/> /
///     <see cref="IsScanningSoftware"/> / <see cref="IsScanningMissingUpdates"/>
///     stuck on forever.
///   - Toast notifications for every long-running operation so the user
///     gets feedback even if they switched to another tab.
///   - <see cref="RebootRequired"/> is now cleared at the start of a new
///     install so a stale "reboot required" banner does not persist.
/// </summary>
public partial class PatchManagerViewModel : ObservableObject
{
    private readonly ILogger<PatchManagerViewModel> _logger;
    private readonly IPatchManagerService _patchService;
    private readonly IToastNotificationService _toastService;

    private List<UpdateInfoModel> _allUpdates = new();

    [ObservableProperty]
    private ObservableCollection<UpdateInfoModel> _updates = new();

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    private string _searchQuery = string.Empty;

    [ObservableProperty]
    private ObservableCollection<SoftwarePackageModel> _softwareUpdates = new();

    [ObservableProperty]
    private bool _isScanningSoftware;

    [ObservableProperty]
    private ObservableCollection<MissingUpdateModel> _missingUpdates = new();

    [ObservableProperty]
    private bool _isScanningMissingUpdates;

    [ObservableProperty]
    private bool _rebootRequired;

    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    public PatchManagerViewModel(
        ILogger<PatchManagerViewModel> logger,
        IPatchManagerService patchService,
        IToastNotificationService toastService)
    {
        _logger = logger;
        _patchService = patchService;
        _toastService = toastService;
    }

    [RelayCommand]
    public async Task InitializeAsync(CancellationToken ct)
    {
        if (IsLoading) return;

        IsLoading = true;
        ErrorMessage = string.Empty;
        Updates.Clear();
        _allUpdates.Clear();

        try
        {
            var result = await _patchService.GetInstalledUpdatesAsync(ct);

            if (result.IsSuccess && result.Value != null)
            {
                _allUpdates = result.Value;
                FilterUpdates();
                _logger.LogInformation("Loaded {Count} installed updates.", _allUpdates.Count);
            }
            else
            {
                ErrorMessage = result.ErrorMessage ?? "Unknown error occurred.";
                _toastService.ShowError("Failed to load installed updates", ErrorMessage);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Installed updates load cancelled.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error loading installed updates");
            ErrorMessage = ex.Message;
            _toastService.ShowError("Failed to load installed updates", ex.Message);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public Task RefreshAsync(CancellationToken ct)
    {
        return InitializeAsync(ct);
    }

    partial void OnSearchQueryChanged(string value)
    {
        FilterUpdates();
    }

    [RelayCommand]
    private void LaunchWindowsUpdate()
    {
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "ms-settings:windowsupdate",
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to launch Windows Update");
            _toastService.ShowError("Cannot open Windows Update", ex.Message);
        }
    }

    private void FilterUpdates()
    {
        IEnumerable<UpdateInfoModel> filtered = _allUpdates;

        if (!string.IsNullOrWhiteSpace(SearchQuery))
        {
            var q = SearchQuery.ToLowerInvariant();
            filtered = filtered.Where(u =>
                (u.HotFixId?.ToLowerInvariant().Contains(q) == true) ||
                (u.Description?.ToLowerInvariant().Contains(q) == true));
        }

        // Sort by InstalledOn descending. InstalledOn is a free-form string
        // (WMI returns either mm/dd/yyyy or a hex filetime), so we use
        // DateTime.TryParse as a tiebreaker and fall back to ordinal string
        // comparison for unparseable values. Null/empty strings sort last.
        Updates = new ObservableCollection<UpdateInfoModel>(
            filtered
                .OrderByDescending(u =>
                {
                    if (string.IsNullOrWhiteSpace(u.InstalledOn)) return DateTime.MinValue;
                    return DateTime.TryParse(u.InstalledOn, out var d) ? d : DateTime.MinValue;
                })
                .ThenByDescending(u => u.InstalledOn ?? string.Empty, StringComparer.OrdinalIgnoreCase)
                .ThenBy(u => u.HotFixId ?? string.Empty, StringComparer.OrdinalIgnoreCase));
    }

    [RelayCommand]
    public async Task ScanSoftwareUpdatesAsync(CancellationToken ct)
    {
        if (IsScanningSoftware) return;

        IsScanningSoftware = true;
        ErrorMessage = string.Empty;
        SoftwareUpdates.Clear();

        _logger.LogInformation("Scanning for software updates...");

        try
        {
            var result = await _patchService.GetSoftwareUpdatesAsync(ct);

            if (result.IsSuccess && result.Value != null)
            {
                SoftwareUpdates = new ObservableCollection<SoftwarePackageModel>(result.Value);
                _logger.LogInformation("Found {Count} software updates.", SoftwareUpdates.Count);
                _toastService.ShowSuccess("Software scan complete",
                    $"Found {SoftwareUpdates.Count} packages with available updates.");
            }
            else
            {
                ErrorMessage = result.ErrorMessage ?? "Failed to scan for software updates using winget.";
                _logger.LogError("Software update scan failed: {Error}", ErrorMessage);
                _toastService.ShowError("Software scan failed", ErrorMessage);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Software update scan cancelled.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error scanning software updates");
            ErrorMessage = ex.Message;
            _toastService.ShowError("Software scan failed", ex.Message);
        }
        finally
        {
            IsScanningSoftware = false;
        }
    }

    [RelayCommand]
    public async Task UpgradeAllSoftwareAsync(CancellationToken ct)
    {
        if (IsScanningSoftware) return;

        IsScanningSoftware = true;

        try
        {
            var result = await _patchService.UpgradeAllSoftwareAsync(ct);

            if (result.IsSuccess)
            {
                _toastService.ShowSuccess("Software upgrade complete",
                    "winget upgrade --all has finished. Rescanning for remaining updates.");
                // Rescan after update
                await ScanSoftwareUpdatesAsync(ct);
            }
            else
            {
                ErrorMessage = result.ErrorMessage ?? "Failed to run upgrade all command.";
                _toastService.ShowError("Software upgrade failed", ErrorMessage);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Software upgrade cancelled.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error running upgrade all");
            ErrorMessage = ex.Message;
            _toastService.ShowError("Software upgrade failed", ex.Message);
        }
        finally
        {
            IsScanningSoftware = false;
        }
    }

    [RelayCommand]
    public async Task ScanMissingUpdatesAsync(CancellationToken ct)
    {
        if (IsScanningMissingUpdates) return;

        IsScanningMissingUpdates = true;
        ErrorMessage = string.Empty;
        MissingUpdates.Clear();

        _logger.LogInformation("Scanning for missing Windows Updates...");

        try
        {
            var result = await _patchService.GetMissingUpdatesAsync(ct);

            if (result.IsSuccess && result.Value != null)
            {
                MissingUpdates = new ObservableCollection<MissingUpdateModel>(result.Value);
                _logger.LogInformation("Found {Count} missing updates.", MissingUpdates.Count);
                _toastService.ShowSuccess("Windows Update scan complete",
                    $"Found {MissingUpdates.Count} missing updates.");
            }
            else
            {
                ErrorMessage = result.ErrorMessage ?? "Failed to scan for missing Windows Updates.";
                _logger.LogError("Missing updates scan failed: {Error}", ErrorMessage);
                _toastService.ShowError("Windows Update scan failed", ErrorMessage);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Missing updates scan cancelled.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error scanning missing Windows Updates");
            ErrorMessage = ex.Message;
            _toastService.ShowError("Windows Update scan failed", ex.Message);
        }
        finally
        {
            IsScanningMissingUpdates = false;
        }
    }

    [RelayCommand]
    public async Task InstallMissingUpdatesAsync(CancellationToken ct)
    {
        if (IsScanningMissingUpdates) return;

        IsScanningMissingUpdates = true;
        ErrorMessage = string.Empty;
        // Clear any stale "reboot required" banner from a previous install
        // so the user does not get a false positive if this install also
        // ends up needing a reboot.
        RebootRequired = false;

        try
        {
            var result = await _patchService.InstallMissingUpdatesAsync(ct);

            if (result.IsSuccess && result.Value != null)
            {
                if (result.Value.RebootRequired)
                {
                    RebootRequired = true;
                    _toastService.ShowWarning("Reboot required",
                        "One or more installed updates require a restart to take effect.");
                }
                else
                {
                    _toastService.ShowSuccess("Updates installed",
                        "All missing Windows Updates were installed successfully.");
                }

                // Rescan after install
                await ScanMissingUpdatesAsync(ct);
                // Also refresh installed updates
                await InitializeAsync(ct);
            }
            else
            {
                ErrorMessage = result.ErrorMessage ?? "Failed to install missing Windows Updates.";
                _toastService.ShowError("Install failed", ErrorMessage);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Install missing updates cancelled.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error installing missing Windows Updates");
            ErrorMessage = ex.Message;
            _toastService.ShowError("Install failed", ex.Message);
        }
        finally
        {
            IsScanningMissingUpdates = false;
        }
    }
}
