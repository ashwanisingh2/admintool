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
}
