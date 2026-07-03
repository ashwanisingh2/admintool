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
using SysAdminX.Core.Models;
using SysAdminX.PatchManager.Services;

namespace SysAdminX.PatchManager.ViewModels;

/// <summary>
/// ViewModel for managing Windows Updates.
/// </summary>
public partial class PatchManagerViewModel : ObservableObject
{
    private readonly ILogger<PatchManagerViewModel> _logger;
    private readonly IPatchManagerService _patchService;
    
    private List<UpdateInfoModel> _allUpdates = new();

    [ObservableProperty]
    private ObservableCollection<UpdateInfoModel> _updates = new();

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    private bool _hasError;

    [ObservableProperty]
    private string _searchQuery = string.Empty;

    [ObservableProperty]
    private ObservableCollection<SoftwarePackageModel> _softwareUpdates = new();

    [ObservableProperty]
    private bool _isScanningSoftware;

    public PatchManagerViewModel(ILogger<PatchManagerViewModel> logger, IPatchManagerService patchService)
    {
        _logger = logger;
        _patchService = patchService;
    }

    [RelayCommand]
    public async Task InitializeAsync(CancellationToken ct)
    {
        if (IsLoading) return;

        IsLoading = true;
        HasError = false;
        ErrorMessage = string.Empty;
        Updates.Clear();
        _allUpdates.Clear();

        var result = await _patchService.GetInstalledUpdatesAsync(ct);

        if (result.IsSuccess && result.Value != null)
        {
            _allUpdates = result.Value;
            FilterUpdates();
        }
        else
        {
            HasError = true;
            ErrorMessage = result.ErrorMessage ?? "Unknown error occurred.";
        }

        IsLoading = false;
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
        }
    }

    private void FilterUpdates()
    {
        IEnumerable<UpdateInfoModel> filtered = _allUpdates;

        if (!string.IsNullOrWhiteSpace(SearchQuery))
        {
            var q = SearchQuery.ToLowerInvariant();
            filtered = filtered.Where(u => 
                u.HotFixId.ToLowerInvariant().Contains(q) || 
                u.Description.ToLowerInvariant().Contains(q));
        }

        Updates = new ObservableCollection<UpdateInfoModel>(filtered.OrderByDescending(u => u.InstalledOn));
    }

    [RelayCommand]
    public async Task ScanSoftwareUpdatesAsync(CancellationToken ct)
    {
        if (IsScanningSoftware) return;

        IsScanningSoftware = true;
        HasError = false;
        ErrorMessage = string.Empty;
        SoftwareUpdates.Clear();

        _logger.LogInformation("Scanning for software updates...");

        var result = await _patchService.GetSoftwareUpdatesAsync(ct);

        if (result.IsSuccess && result.Value != null)
        {
            SoftwareUpdates = new ObservableCollection<SoftwarePackageModel>(result.Value);
            _logger.LogInformation("Found {Count} software updates.", SoftwareUpdates.Count);
        }
        else
        {
            HasError = true;
            ErrorMessage = result.ErrorMessage ?? "Failed to scan for software updates using winget.";
            _logger.LogError("Software update scan failed: {Error}", ErrorMessage);
        }

        IsScanningSoftware = false;
    }

    [RelayCommand]
    public async Task UpgradeAllSoftwareAsync(CancellationToken ct)
    {
        if (IsScanningSoftware) return;
        
        IsScanningSoftware = true;
        
        var result = await _patchService.UpgradeAllSoftwareAsync(ct);
        
        if (result.IsSuccess)
        {
            // Rescan after update
            await ScanSoftwareUpdatesAsync(ct);
        }
        else
        {
            HasError = true;
            ErrorMessage = result.ErrorMessage ?? "Failed to run upgrade all command.";
        }
        
        IsScanningSoftware = false;
    }
}
