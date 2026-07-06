// -----------------------------------------------------------------------
// <copyright file="TroubleshootingViewModel.cs" company="SysAdminX">
//     Copyright (c) SysAdminX. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using SysAdminX.Core.Interfaces;
using SysAdminX.Core.Models;
using SysAdminX.Troubleshooting.Services;

namespace SysAdminX.Troubleshooting.ViewModels;

/// <summary>
/// ViewModel for the Troubleshooting module.
///
/// Each "Run X" command follows the same pattern:
///   1. Bail out if another troubleshooting action is already running.
///   2. Set <see cref="IsRunning"/> (drives the spinner + button disabling).
///   3. Await the service in a try/catch/finally.
///   4. Surface the result both as a history entry (UI) and as a toast
///      (so the user gets immediate feedback even if they switched tabs).
/// </summary>
public partial class TroubleshootingViewModel : ObservableObject
{
    private readonly ILogger<TroubleshootingViewModel> _logger;
    private readonly ITroubleshootingService _troubleshootingService;
    private readonly IToastNotificationService _toastService;

    [ObservableProperty]
    private string _ramTestResult = "No results found";

    [ObservableProperty]
    private ObservableCollection<TroubleshootingActionModel> _actionHistory = new();

    [ObservableProperty]
    private bool _isRunning;

    /// <summary>
    /// Human-readable label of the action currently in-flight, bound to the
    /// spinner text in the UI. Empty when nothing is running.
    /// </summary>
    [ObservableProperty]
    private string _currentAction = string.Empty;

    public TroubleshootingViewModel(
        ILogger<TroubleshootingViewModel> logger,
        ITroubleshootingService troubleshootingService,
        IToastNotificationService toastService)
    {
        _logger = logger;
        _troubleshootingService = troubleshootingService;
        _toastService = toastService;
    }

    /// <summary>
    /// Helper that wraps the common "run action, add to history, toast" pattern.
    /// The <paramref name="actionName"/> is shown in the spinner while the
    /// task runs; the underlying factory returns the service result.
    /// </summary>
    private async Task RunActionAsync(
        string actionName,
        System.Func<CancellationToken, Task<Result<TroubleshootingActionModel>>> factory,
        CancellationToken ct)
    {
        if (IsRunning) return;

        IsRunning = true;
        CurrentAction = actionName;
        try
        {
            var result = await factory(ct);
            if (result.IsSuccess && result.Value != null)
            {
                ActionHistory.Insert(0, result.Value);
                if (result.Value.IsSuccess)
                    _toastService.ShowSuccess($"{actionName} completed", result.Value.OutputMessage);
                else
                    _toastService.ShowError($"{actionName} failed", result.Value.OutputMessage);
            }
            else
            {
                var err = result.ErrorMessage ?? "Unknown error";
                _toastService.ShowError($"{actionName} failed", err);
            }
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "{Action} threw an exception", actionName);
            _toastService.ShowError($"{actionName} threw an exception", ex.Message);
        }
        finally
        {
            IsRunning = false;
            CurrentAction = string.Empty;
        }
    }

    [RelayCommand]
    public Task RunSfcAsync(CancellationToken ct)
        => RunActionAsync("SFC Scan", _troubleshootingService.RunSfcScanAsync, ct);

    [RelayCommand]
    public Task RunDismCheckAsync(CancellationToken ct)
        => RunActionAsync("DISM CheckHealth", _troubleshootingService.RunDismCheckHealthAsync, ct);

    [RelayCommand]
    public Task RunDismRestoreAsync(CancellationToken ct)
        => RunActionAsync("DISM RestoreHealth", _troubleshootingService.RunDismRestoreHealthAsync, ct);

    [RelayCommand]
    public Task ClearTempAsync(CancellationToken ct)
        => RunActionAsync("Clear Temp Files", _troubleshootingService.ClearTempFilesAsync, ct);

    [RelayCommand]
    public async Task ToggleFastStartupAsync(string enableStr)
    {
        bool enable = enableStr?.ToLowerInvariant() == "true";
        await RunActionAsync(
            enable ? "Enable Fast Startup" : "Disable Fast Startup",
            _ => _troubleshootingService.ToggleFastStartupAsync(enable, CancellationToken.None),
            CancellationToken.None);
    }

    [RelayCommand]
    public Task RunChkdskAsync(CancellationToken ct)
        => RunActionAsync("Schedule CHKDSK", _troubleshootingService.RunChkdskAsync, ct);

    [RelayCommand]
    public Task ResetWindowsUpdateAsync(CancellationToken ct)
        => RunActionAsync("Reset Windows Update", _troubleshootingService.ResetWindowsUpdateAsync, ct);

    [RelayCommand]
    public Task FixPrintSpoolerAsync(CancellationToken ct)
        => RunActionAsync("Fix Print Spooler", _troubleshootingService.FixPrintSpoolerAsync, ct);

    [RelayCommand]
    public Task FlushDnsAsync(CancellationToken ct)
        => RunActionAsync("Flush DNS", _troubleshootingService.FlushDnsAsync, ct);

    [RelayCommand]
    public Task ResetWinsockAsync(CancellationToken ct)
        => RunActionAsync("Reset Winsock", _troubleshootingService.ResetWinsockAsync, ct);

    [RelayCommand]
    public Task ResetTcpIpAsync(CancellationToken ct)
        => RunActionAsync("Reset TCP/IP", _troubleshootingService.ResetTcpIpAsync, ct);

    [RelayCommand]
    public Task RebuildIconCacheAsync(CancellationToken ct)
        => RunActionAsync("Rebuild Icon Cache", _troubleshootingService.RebuildIconCacheAsync, ct);

    [RelayCommand]
    public Task ResetWindowsSearchAsync(CancellationToken ct)
        => RunActionAsync("Reset Windows Search", _troubleshootingService.ResetWindowsSearchAsync, ct);

    [RelayCommand]
    public Task ScheduleRamTestAsync(CancellationToken ct)
        => RunActionAsync("Schedule RAM Test", _troubleshootingService.ScheduleRamTestAsync, ct);

    [RelayCommand]
    public async Task CheckRamResultAsync(CancellationToken ct)
    {
        try
        {
            var result = await _troubleshootingService.CheckRamTestResultAsync(ct);
            if (result.IsSuccess)
            {
                RamTestResult = result.Value ?? "No results found";
            }
            else
            {
                RamTestResult = "Failed to retrieve results: " + result.ErrorMessage;
                _toastService.ShowError("RAM test result", result.ErrorMessage ?? "Failed to retrieve results.");
            }
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve RAM test result");
            _toastService.ShowError("RAM test result", ex.Message);
        }
    }
}
