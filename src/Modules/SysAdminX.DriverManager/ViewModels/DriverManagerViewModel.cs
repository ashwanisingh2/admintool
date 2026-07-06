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

    [ObservableProperty]
    private bool _isRegistrySafeModeEnabled = true;

    public DriverManagerViewModel(
        ILogger<DriverManagerViewModel> logger,
        IDriverManagerService driverService,
        SysAdminX.Core.Interfaces.IToastNotificationService toastService)
    {
        _logger = logger;
        _driverService = driverService;
        _toastService = toastService;
    }

    private readonly SysAdminX.Core.Interfaces.IToastNotificationService _toastService;

    [RelayCommand]
    private async Task DisableDriverAsync(DriverInfoModel? driver, CancellationToken ct = default)
    {
        if (driver == null || string.IsNullOrWhiteSpace(driver.HardwareId)) return;
        try
        {
            var result = await _driverService.DisableDriverWithBackupAsync(driver.HardwareId, IsRegistrySafeModeEnabled, ct);
            if (result.IsSuccess)
            {
                _toastService.ShowSuccess($"Driver disabled: {driver.DeviceName}",
                    $"Backup saved to:\n{result.Value}");
                await RefreshAsync(ct);
            }
            else
            {
                _toastService.ShowError("Failed to disable driver", result.ErrorMessage ?? "Unknown error.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Disable driver threw an exception.");
            _toastService.ShowError("Failed to disable driver", ex.Message);
        }
    }

    [RelayCommand]
    private async Task EnableDriverAsync(DriverInfoModel? driver, CancellationToken ct = default)
    {
        if (driver == null || string.IsNullOrWhiteSpace(driver.HardwareId)) return;
        try
        {
            var result = await _driverService.EnableDriverAsync(driver.HardwareId, ct);
            if (result.IsSuccess)
            {
                _toastService.ShowSuccess($"Driver enabled: {driver.DeviceName}",
                    "The driver was re-enabled.");
                await RefreshAsync(ct);
            }
            else
            {
                _toastService.ShowError("Failed to enable driver", result.ErrorMessage ?? "Unknown error.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Enable driver threw an exception.");
            _toastService.ShowError("Failed to enable driver", ex.Message);
        }
    }

    [RelayCommand]
    private async Task RollbackDriverAsync(DriverInfoModel? driver, CancellationToken ct = default)
    {
        if (driver == null || string.IsNullOrWhiteSpace(driver.HardwareId)) return;
        try
        {
            var result = await _driverService.RollbackDriverAsync(driver.HardwareId, ct);
            if (result.IsSuccess)
            {
                _toastService.ShowSuccess($"Driver rolled back: {driver.DeviceName}",
                    "The previous driver version was restored.");
                await RefreshAsync(ct);
            }
            else
            {
                _toastService.ShowError("Failed to roll back driver", result.ErrorMessage ?? "Unknown error.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Rollback driver threw an exception.");
            _toastService.ShowError("Failed to roll back driver", ex.Message);
        }
    }

    [RelayCommand]
    private async Task RestoreBackupAsync(CancellationToken ct = default)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "Registry Files (*.reg)|*.reg|All Files (*.*)|*.*",
            Title = "Select Driver Backup File"
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                var result = await _driverService.RestoreFromBackupAsync(dialog.FileName, ct);
                if (result.IsSuccess)
                {
                    _toastService.ShowSuccess("Backup restored", dialog.FileName);
                }
                else
                {
                    _toastService.ShowError("Failed to restore backup", result.ErrorMessage ?? "Unknown error.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Restore backup threw an exception.");
                _toastService.ShowError("Failed to restore backup", ex.Message);
            }
        }
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

        try
        {
            var result = await _driverService.ScanDriversAsync(ct);

            await CheckOemUpdaterAsync(ct);

            if (result.IsSuccess && result.Value != null)
            {
                _allDrivers = result.Value;
                FilterDrivers();
                _logger.LogInformation("Loaded {Count} drivers.", _allDrivers.Count);
            }
            else
            {
                HasError = true;
                ErrorMessage = result.ErrorMessage ?? "Unknown error occurred.";
                _toastService.ShowError("Failed to load drivers", ErrorMessage);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Driver scan cancelled.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Driver scan threw an exception.");
            HasError = true;
            ErrorMessage = ex.Message;
            _toastService.ShowError("Failed to load drivers", ex.Message);
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
                (d.DeviceName?.ToLowerInvariant().Contains(q) == true) ||
                (d.Manufacturer?.ToLowerInvariant().Contains(q) == true) ||
                (d.ClassName?.ToLowerInvariant().Contains(q) == true) ||
                (d.Provider?.ToLowerInvariant().Contains(q) == true));
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

        try
        {
            var result = await _driverService.ScanDriverUpdatesAsync(ct);

            if (result.IsSuccess && result.Value != null)
            {
                MissingDriverUpdates = new ObservableCollection<string>(result.Value);
                _logger.LogInformation("Found {Count} driver updates.", MissingDriverUpdates.Count);

                if (MissingDriverUpdates.Count == 0)
                {
                    MissingDriverUpdates.Add("System is up to date. No driver updates found.");
                }
                _toastService.ShowSuccess("Driver update scan complete",
                    $"Found {MissingDriverUpdates.Count} driver updates.");
            }
            else
            {
                HasError = true;
                ErrorMessage = result.ErrorMessage ?? "Failed to scan for driver updates.";
                _logger.LogError("Driver update scan failed: {Error}", ErrorMessage);
                _toastService.ShowError("Driver update scan failed", ErrorMessage);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Driver update scan cancelled.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Driver update scan threw an exception.");
            HasError = true;
            ErrorMessage = ex.Message;
            _toastService.ShowError("Driver update scan failed", ex.Message);
        }
        finally
        {
            IsScanningUpdates = false;
        }
    }

    [RelayCommand]
    public async Task InstallUpdatesAsync(CancellationToken ct)
    {
        if (IsScanningUpdates) return;

        IsScanningUpdates = true;

        try
        {
            var result = await _driverService.InstallDriverUpdatesAsync(ct);

            if (result.IsSuccess)
            {
                _toastService.ShowSuccess("Driver updates installed",
                    "All available driver updates were installed.");
                await ScanUpdatesAsync(ct);
            }
            else
            {
                HasError = true;
                ErrorMessage = result.ErrorMessage ?? "Failed to run install updates command.";
                _toastService.ShowError("Driver install failed", ErrorMessage);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Driver install cancelled.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Driver install threw an exception.");
            HasError = true;
            ErrorMessage = ex.Message;
            _toastService.ShowError("Driver install failed", ex.Message);
        }
        finally
        {
            IsScanningUpdates = false;
        }
    }

    [ObservableProperty]
    private ObservableCollection<DriverInfoModel> _unsignedDrivers = new();

    [ObservableProperty]
    private bool _isScanningUnsigned;

    [ObservableProperty]
    private OemUpdaterInfoModel? _oemUpdater;

    [RelayCommand]
    public async Task ScanUnsignedDriversAsync(CancellationToken ct)
    {
        if (IsScanningUnsigned) return;

        IsScanningUnsigned = true;
        UnsignedDrivers.Clear();

        try
        {
            var result = await _driverService.ScanUnsignedDriversAsync(ct);

            if (result.IsSuccess && result.Value != null)
            {
                UnsignedDrivers = new ObservableCollection<DriverInfoModel>(result.Value);
                _toastService.ShowSuccess("Unsigned driver scan complete",
                    $"Found {UnsignedDrivers.Count} unsigned drivers.");
            }
            else
            {
                HasError = true;
                ErrorMessage = result.ErrorMessage ?? "Failed to scan unsigned drivers.";
                _toastService.ShowError("Unsigned driver scan failed", ErrorMessage);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Unsigned driver scan cancelled.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unsigned driver scan threw an exception.");
            HasError = true;
            ErrorMessage = ex.Message;
            _toastService.ShowError("Unsigned driver scan failed", ex.Message);
        }
        finally
        {
            IsScanningUnsigned = false;
        }
    }

    [RelayCommand]
    public async Task CheckOemUpdaterAsync(CancellationToken ct)
    {
        var result = await _driverService.CheckOemUpdaterAsync(ct);
        if (result.IsSuccess)
        {
            OemUpdater = result.Value;
        }
    }

    [RelayCommand]
    private void LaunchOemUpdater()
    {
        if (OemUpdater?.IsInstalled == true)
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = OemUpdater.ExecutablePath,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to launch OEM Updater");
            }
        }
    }
}
