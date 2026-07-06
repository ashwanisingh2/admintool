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
using SysAdminX.Core.Interfaces;
using SysAdminX.Core.Models;
using SysAdminX.DeviceDetails.Services;

namespace SysAdminX.DeviceDetails.ViewModels;

/// <summary>
/// ViewModel for the Device Details view.
///
/// Improvements applied:
///   - <see cref="IsLoading"/> now resets in <c>finally</c> so an exception
///     can no longer leave the spinner stuck on.
///   - TRIM operation is wrapped in try/finally with explicit cancellation
///     and toast feedback.
/// </summary>
public partial class DeviceDetailsViewModel : ObservableObject
{
    private readonly ILogger<DeviceDetailsViewModel> _logger;
    private readonly IDeviceDetailsService _deviceService;
    private readonly ITrimService _trimService;
    private readonly IToastNotificationService _toastService;

    [ObservableProperty]
    private DeviceDetailsModel _deviceDetails = new();

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    private bool _isSuccess;

    [ObservableProperty]
    private string _successMessage = string.Empty;

    /// <summary>True while a TRIM operation is in flight (drives a per-drive spinner).</summary>
    [ObservableProperty]
    private bool _isTrimRunning;

    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    public DeviceDetailsViewModel(
        ILogger<DeviceDetailsViewModel> logger,
        IDeviceDetailsService deviceService,
        ITrimService trimService,
        IToastNotificationService toastService)
    {
        _logger = logger;
        _deviceService = deviceService;
        _trimService = trimService;
        _toastService = toastService;
    }

    [RelayCommand]
    public async Task InitializeAsync(CancellationToken ct)
    {
        if (IsLoading) return;

        IsLoading = true;
        ErrorMessage = string.Empty;

        _logger.LogInformation("Loading device details...");

        try
        {
            var result = await _deviceService.GetDeviceDetailsAsync(ct);

            if (result.IsSuccess && result.Value != null)
            {
                DeviceDetails = result.Value;
                _logger.LogInformation("Device details loaded successfully.");
            }
            else
            {
                ErrorMessage = result.ErrorMessage ?? "Unknown error occurred while loading device details.";
                _logger.LogError("Failed to load device details: {Error}", ErrorMessage);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Device details load cancelled.");
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Unexpected exception loading device details.");
            ErrorMessage = ex.Message;
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

    [RelayCommand]
    public async Task RunTrimAsync(string driveLetter, CancellationToken ct = default)
    {
        if (IsTrimRunning) return;

        IsSuccess = false;
        IsTrimRunning = true;

        _logger.LogInformation("Running TRIM on {Drive}", driveLetter);

        try
        {
            var result = await _trimService.RunTrimAsync(driveLetter, ct);

            if (result.IsSuccess)
            {
                _logger.LogInformation("TRIM successful for {Drive}", driveLetter);
                SuccessMessage = $"TRIM optimization completed successfully on {driveLetter}";
                IsSuccess = true;
                _toastService.ShowSuccess($"TRIM complete on {driveLetter}",
                    "SSD optimization finished.");
            }
            else
            {
                _logger.LogError("TRIM failed for {Drive}: {Error}", driveLetter, result.ErrorMessage);
                ErrorMessage = result.ErrorMessage ?? "Unknown error occurred while running TRIM.";
                _toastService.ShowError($"TRIM failed on {driveLetter}", ErrorMessage);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("TRIM cancelled for {Drive}", driveLetter);
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "TRIM threw an exception for {Drive}", driveLetter);
            ErrorMessage = ex.Message;
            _toastService.ShowError($"TRIM failed on {driveLetter}", ex.Message);
        }
        finally
        {
            IsTrimRunning = false;
        }
    }
}
