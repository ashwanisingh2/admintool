// -----------------------------------------------------------------------
// <copyright file="DriverManagerViewModel.cs" company="SysAdminX">
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
using SysAdminX.DriverManager.Services;

namespace SysAdminX.DriverManager.ViewModels;

/// <summary>
/// ViewModel for managing device drivers.
/// </summary>
public partial class DriverManagerViewModel : ObservableObject
{
    private readonly ILogger<DriverManagerViewModel> _logger;
    private readonly IDriverManagerService _driverService;
    
    private List<DriverInfoModel> _allDrivers = new();

    [ObservableProperty]
    private ObservableCollection<DriverInfoModel> _drivers = new();

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    private bool _hasError;

    [ObservableProperty]
    private string _searchQuery = string.Empty;

    [ObservableProperty]
    private bool _showThirdPartyOnly;

    public DriverManagerViewModel(ILogger<DriverManagerViewModel> logger, IDriverManagerService driverService)
    {
        _logger = logger;
        _driverService = driverService;
    }

    [RelayCommand]
    public async Task InitializeAsync(CancellationToken ct)
    {
        if (IsLoading) return;

        IsLoading = true;
        HasError = false;
        ErrorMessage = string.Empty;
        Drivers.Clear();
        _allDrivers.Clear();

        var result = await _driverService.ScanDriversAsync(ct);

        if (result.IsSuccess && result.Value != null)
        {
            _allDrivers = result.Value;
            FilterDrivers();
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
        FilterDrivers();
    }

    partial void OnShowThirdPartyOnlyChanged(bool value)
    {
        FilterDrivers();
    }

    [RelayCommand]
    private void LaunchDeviceManager()
    {
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "mmc.exe",
                Arguments = "devmgmt.msc",
                UseShellExecute = true
            });
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Failed to launch Device Manager");
        }
    }

    [RelayCommand]
    private void ExportDrivers()
    {
        try
        {
            string backupDir = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "SysAdminX_DriverBackup");
            if (!System.IO.Directory.Exists(backupDir))
            {
                System.IO.Directory.CreateDirectory(backupDir);
            }

            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "powershell",
                Arguments = $"-NoExit -Command \"Write-Host 'Exporting all 3rd-party drivers to {backupDir}... This requires Administrator privileges.' -ForegroundColor Cyan; pnputil.exe /export-driver * '{backupDir}'; Write-Host 'Backup Complete! You can close this window.' -ForegroundColor Green\"",
                UseShellExecute = true,
                Verb = "runas"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to launch driver backup");
        }
    }

    private void FilterDrivers()
    {
        IEnumerable<DriverInfoModel> filtered = _allDrivers;

        if (ShowThirdPartyOnly)
        {
            filtered = filtered.Where(d => !d.IsMicrosoft);
        }

        if (!string.IsNullOrWhiteSpace(SearchQuery))
        {
            var q = SearchQuery.ToLowerInvariant();
            filtered = filtered.Where(d => 
                d.DeviceName.ToLowerInvariant().Contains(q) || 
                d.Manufacturer.ToLowerInvariant().Contains(q) || 
                d.ClassName.ToLowerInvariant().Contains(q) ||
                d.Provider.ToLowerInvariant().Contains(q));
        }

        Drivers = new ObservableCollection<DriverInfoModel>(filtered.OrderBy(d => d.DeviceName));
    }

    [ObservableProperty]
    private ObservableCollection<string> _missingDriverUpdates = new();

    [ObservableProperty]
    private bool _isScanningUpdates;

    [RelayCommand]
    public async Task ScanUpdatesAsync(CancellationToken ct)
    {
        if (IsScanningUpdates) return;

        IsScanningUpdates = true;
        HasError = false;
        ErrorMessage = string.Empty;
        MissingDriverUpdates.Clear();

        _logger.LogInformation("Scanning for driver updates...");

        var result = await _driverService.ScanDriverUpdatesAsync(ct);

        if (result.IsSuccess && result.Value != null)
        {
            MissingDriverUpdates = new ObservableCollection<string>(result.Value);
            _logger.LogInformation("Found {Count} driver updates.", MissingDriverUpdates.Count);
            
            if (MissingDriverUpdates.Count == 0)
            {
                MissingDriverUpdates.Add("System is up to date. No driver updates found.");
            }
        }
        else
        {
            HasError = true;
            ErrorMessage = result.ErrorMessage ?? "Failed to scan for driver updates.";
            _logger.LogError("Driver update scan failed: {Error}", ErrorMessage);
        }

        IsScanningUpdates = false;
    }

    [RelayCommand]
    public async Task InstallUpdatesAsync(CancellationToken ct)
    {
        if (IsScanningUpdates) return;
        
        IsScanningUpdates = true;
        
        var result = await _driverService.InstallDriverUpdatesAsync(ct);
        
        if (result.IsSuccess)
        {
            await ScanUpdatesAsync(ct);
        }
        else
        {
            HasError = true;
            ErrorMessage = result.ErrorMessage ?? "Failed to run install updates command.";
        }
        
        IsScanningUpdates = false;
    }
}
