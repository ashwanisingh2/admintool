// -----------------------------------------------------------------------
// <copyright file="BatteryManagerViewModel.cs" company="SysAdminX">
//     Copyright (c) SysAdminX. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using SysAdminX.BatteryManager.Services;
using SysAdminX.Core.Models;
using SysAdminX.Core.Interfaces;

namespace SysAdminX.BatteryManager.ViewModels;

/// <summary>
/// ViewModel for the Battery Manager module.
///
/// Improvements applied:
///   - Constructor now requires IToastNotificationService so GenerateReport
///     and GenerateDetailedReport can surface success / failure as a toast
///     instead of (or in addition to) the inline ErrorMessage.
/// </summary>
public partial class BatteryManagerViewModel : ObservableObject
{
    private readonly ILogger<BatteryManagerViewModel> _logger;
    private readonly IBatteryManagerService _batteryService;
    private readonly IProcessExecutorService _processExecutorService;
    private readonly IToastNotificationService _toastService;

    [ObservableProperty]
    private BatteryInfoModel? _batteryInfo;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _hasError;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    public BatteryManagerViewModel(
        ILogger<BatteryManagerViewModel> logger,
        IBatteryManagerService batteryService,
        IProcessExecutorService processExecutorService,
        IToastNotificationService toastService)
    {
        _logger = logger;
        _batteryService = batteryService;
        _processExecutorService = processExecutorService;
        _toastService = toastService;
    }

    [RelayCommand]
    public async Task InitializeAsync(CancellationToken ct)
    {
        if (IsLoading) return;

        IsLoading = true;
        HasError = false;
        ErrorMessage = string.Empty;

        try
        {
            var result = await _batteryService.GetBatteryInfoAsync(ct);

            if (result.IsSuccess)
            {
                BatteryInfo = result.Value;
            }
            else
            {
                HasError = true;
                ErrorMessage = result.ErrorMessage ?? "Failed to query battery information.";
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Battery init cancelled.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Battery init threw an exception.");
            HasError = true;
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public Task RefreshAsync(CancellationToken ct) => InitializeAsync(ct);

    [RelayCommand]
    public async Task GenerateReportAsync(CancellationToken ct)
    {
        try
        {
            IsLoading = true;
            string dest = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "SysAdminX_Reports");

            var result = await _batteryService.GenerateBatteryReportAsync(dest, ct);
            if (result.IsSuccess && System.IO.File.Exists(result.Value))
            {
                _toastService.ShowSuccess("Battery report generated", result.Value);
                // Open the report
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = result.Value,
                    UseShellExecute = true
                });
            }
            else
            {
                HasError = true;
                ErrorMessage = result.ErrorMessage ?? "Failed to generate report.";
                _toastService.ShowError("Battery report failed", ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = ex.Message;
            _toastService.ShowError("Battery report failed", ex.Message);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task GenerateDetailedReportAsync(CancellationToken ct)
    {
        try
        {
            IsLoading = true;
            string tempFile = Path.Combine(Path.GetTempPath(), "battery_report.html");

            var result = await _processExecutorService.ExecuteAsync("powercfg", $"/batteryreport /output \"{tempFile}\"", requireElevation: false, ct);
            if (result.IsSuccess && System.IO.File.Exists(tempFile))
            {
                _toastService.ShowSuccess("Detailed battery report generated", tempFile);
                // Open the report
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = tempFile,
                    UseShellExecute = true
                });
            }
            else
            {
                HasError = true;
                ErrorMessage = result.ErrorMessage ?? "Failed to generate detailed report.";
                _toastService.ShowError("Detailed battery report failed", ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = ex.Message;
            _toastService.ShowError("Detailed battery report failed", ex.Message);
        }
        finally
        {
            IsLoading = false;
        }
    }
}
