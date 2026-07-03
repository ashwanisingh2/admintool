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

namespace SysAdminX.BatteryManager.ViewModels;

public partial class BatteryManagerViewModel : ObservableObject
{
    private readonly ILogger<BatteryManagerViewModel> _logger;
    private readonly IBatteryManagerService _batteryService;

    [ObservableProperty]
    private BatteryInfoModel? _batteryInfo;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _hasError;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    public BatteryManagerViewModel(ILogger<BatteryManagerViewModel> logger, IBatteryManagerService batteryService)
    {
        _logger = logger;
        _batteryService = batteryService;
    }

    [RelayCommand]
    public async Task InitializeAsync(CancellationToken ct)
    {
        if (IsLoading) return;

        IsLoading = true;
        HasError = false;
        ErrorMessage = string.Empty;

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

        IsLoading = false;
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
            if (result.IsSuccess)
            {
                // Open the report
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = result.Value ?? "",
                    UseShellExecute = true
                });
            }
            else
            {
                HasError = true;
                ErrorMessage = result.ErrorMessage ?? "Failed to generate report.";
            }
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsLoading = false;
        }
    }
}
