// -----------------------------------------------------------------------
// <copyright file="DeviceDetailsViewModel.cs" company="SysAdminX">
//     Copyright (c) SysAdminX. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using SysAdminX.Core.Models;
using SysAdminX.DeviceDetails.Services;

namespace SysAdminX.DeviceDetails.ViewModels;

/// <summary>
/// ViewModel for the Device Details view.
/// </summary>
public partial class DeviceDetailsViewModel : ObservableObject
{
    private readonly ILogger<DeviceDetailsViewModel> _logger;
    private readonly IDeviceDetailsService _deviceService;

    [ObservableProperty]
    private DeviceDetailsModel _deviceDetails = new();

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    private bool _hasError;

    public DeviceDetailsViewModel(
        ILogger<DeviceDetailsViewModel> logger,
        IDeviceDetailsService deviceService)
    {
        _logger = logger;
        _deviceService = deviceService;
    }

    [RelayCommand]
    public async Task InitializeAsync(CancellationToken ct)
    {
        if (IsLoading) return;

        IsLoading = true;
        HasError = false;
        ErrorMessage = string.Empty;

        _logger.LogInformation("Loading device details...");

        var result = await _deviceService.GetDeviceDetailsAsync(ct);

        if (result.IsSuccess && result.Value != null)
        {
            DeviceDetails = result.Value;
            _logger.LogInformation("Device details loaded successfully.");
        }
        else
        {
            HasError = true;
            ErrorMessage = result.ErrorMessage ?? "Unknown error occurred while loading device details.";
            _logger.LogError("Failed to load device details: {Error}", ErrorMessage);
        }

        IsLoading = false;
    }

    [RelayCommand]
    public Task RefreshAsync(CancellationToken ct)
    {
        return InitializeAsync(ct);
    }
}
