// -----------------------------------------------------------------------
// <copyright file="TroubleshootingViewModel.cs" company="SysAdminX">
//     Copyright (c) SysAdminX. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using SysAdminX.Core.Models;
using SysAdminX.Troubleshooting.Services;

namespace SysAdminX.Troubleshooting.ViewModels;

/// <summary>
/// ViewModel for the Troubleshooting module.
/// </summary>
public partial class TroubleshootingViewModel : ObservableObject
{
    private readonly ILogger<TroubleshootingViewModel> _logger;
    private readonly ITroubleshootingService _troubleshootingService;

    [ObservableProperty]
    private string _ramTestResult = "No results found";

    [ObservableProperty]
    private ObservableCollection<TroubleshootingActionModel> _actionHistory = new();

    public TroubleshootingViewModel(ILogger<TroubleshootingViewModel> logger, ITroubleshootingService troubleshootingService)
    {
        _logger = logger;
        _troubleshootingService = troubleshootingService;
    }

    [RelayCommand]
    public async Task RunSfcAsync(CancellationToken ct)
    {
        var result = await _troubleshootingService.RunSfcScanAsync(ct);
        if (result.IsSuccess && result.Value != null)
        {
            ActionHistory.Insert(0, result.Value);
        }
    }

    [RelayCommand]
    public async Task RunDismCheckAsync(CancellationToken ct)
    {
        var result = await _troubleshootingService.RunDismCheckHealthAsync(ct);
        if (result.IsSuccess && result.Value != null)
        {
            ActionHistory.Insert(0, result.Value);
        }
    }

    [RelayCommand]
    public async Task RunDismRestoreAsync(CancellationToken ct)
    {
        var result = await _troubleshootingService.RunDismRestoreHealthAsync(ct);
        if (result.IsSuccess && result.Value != null)
        {
            ActionHistory.Insert(0, result.Value);
        }
    }

    [RelayCommand]
    public async Task ClearTempAsync(CancellationToken ct)
    {
        var result = await _troubleshootingService.ClearTempFilesAsync(ct);
        if (result.IsSuccess && result.Value != null)
        {
            ActionHistory.Insert(0, result.Value);
        }
    }

    [RelayCommand]
    public async Task ToggleFastStartupAsync(string enableStr)
    {
        bool enable = enableStr?.ToLowerInvariant() == "true";
        var result = await _troubleshootingService.ToggleFastStartupAsync(enable, CancellationToken.None);
        if (result.IsSuccess && result.Value != null)
        {
            ActionHistory.Insert(0, result.Value);
        }
    }

    [RelayCommand]
    public async Task RunChkdskAsync(CancellationToken ct)
    {
        var result = await _troubleshootingService.RunChkdskAsync(ct);
        if (result.IsSuccess && result.Value != null)
        {
            ActionHistory.Insert(0, result.Value);
        }
    }

    [RelayCommand]
    public async Task ResetWindowsUpdateAsync(CancellationToken ct)
    {
        var result = await _troubleshootingService.ResetWindowsUpdateAsync(ct);
        if (result.IsSuccess && result.Value != null)
        {
            ActionHistory.Insert(0, result.Value);
        }
    }

    [RelayCommand]
    public async Task FixPrintSpoolerAsync(CancellationToken ct)
    {
        var result = await _troubleshootingService.FixPrintSpoolerAsync(ct);
        if (result.IsSuccess && result.Value != null)
        {
            ActionHistory.Insert(0, result.Value);
        }
    }

    [RelayCommand]
    public async Task FlushDnsAsync(CancellationToken ct)
    {
        var result = await _troubleshootingService.FlushDnsAsync(ct);
        if (result.IsSuccess && result.Value != null)
        {
            ActionHistory.Insert(0, result.Value);
        }
    }

    [RelayCommand]
    public async Task ResetWinsockAsync(CancellationToken ct)
    {
        var result = await _troubleshootingService.ResetWinsockAsync(ct);
        if (result.IsSuccess && result.Value != null)
        {
            ActionHistory.Insert(0, result.Value);
        }
    }

    [RelayCommand]
    public async Task ResetTcpIpAsync(CancellationToken ct)
    {
        var result = await _troubleshootingService.ResetTcpIpAsync(ct);
        if (result.IsSuccess && result.Value != null)
        {
            ActionHistory.Insert(0, result.Value);
        }
    }

    [RelayCommand]
    public async Task RebuildIconCacheAsync(CancellationToken ct)
    {
        var result = await _troubleshootingService.RebuildIconCacheAsync(ct);
        if (result.IsSuccess && result.Value != null)
        {
            ActionHistory.Insert(0, result.Value);
        }
    }

    [RelayCommand]
    public async Task ResetWindowsSearchAsync(CancellationToken ct)
    {
        var result = await _troubleshootingService.ResetWindowsSearchAsync(ct);
        if (result.IsSuccess && result.Value != null)
        {
            ActionHistory.Insert(0, result.Value);
        }
    }

    [RelayCommand]
    public async Task ScheduleRamTestAsync(CancellationToken ct)
    {
        var result = await _troubleshootingService.ScheduleRamTestAsync(ct);
        if (result.IsSuccess && result.Value != null)
        {
            ActionHistory.Insert(0, result.Value);
        }
    }

    [RelayCommand]
    public async Task CheckRamResultAsync(CancellationToken ct)
    {
        var result = await _troubleshootingService.CheckRamTestResultAsync(ct);
        if (result.IsSuccess)
        {
            RamTestResult = result.Value ?? "No results found";
        }
        else
        {
            RamTestResult = "Failed to retrieve results: " + result.ErrorMessage;
        }
    }
}
