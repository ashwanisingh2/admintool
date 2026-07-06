using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using SysAdminX.Core.Interfaces;
using SysAdminX.Core.Models;

namespace SysAdminX.SystemRestore.ViewModels;

/// <summary>
/// ViewModel for the System Restore module.
///
/// Improvements applied:
///   - All MessageBox.Show calls (except the restore-point launcher) replaced
///     with toast notifications so the UI thread is no longer blocked.
///   - Top-level try/catch added to LoadDataAsync so a thrown exception can
///     no longer crash the page.
///   - IToastNotificationService injected.
/// </summary>
public partial class SystemRestoreViewModel : ObservableObject
{
    private readonly ISystemRestoreService _systemRestoreService;
    private readonly IToastNotificationService _toastService;
    private readonly ILogger<SystemRestoreViewModel> _logger;

    [ObservableProperty]
    private bool _isProtectionEnabled;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _newPointDescription = string.Empty;

    public ObservableCollection<SystemRestorePoint> RestorePoints { get; } = new();

    public SystemRestoreViewModel(
        ISystemRestoreService systemRestoreService,
        IToastNotificationService toastService,
        ILogger<SystemRestoreViewModel> logger)
    {
        _systemRestoreService = systemRestoreService ?? throw new ArgumentNullException(nameof(systemRestoreService));
        _toastService = toastService ?? throw new ArgumentNullException(nameof(toastService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        LoadDataCommand = new AsyncRelayCommand(LoadDataAsync);
        EnableProtectionCommand = new AsyncRelayCommand(EnableProtectionAsync);
        CreatePointCommand = new AsyncRelayCommand(CreatePointAsync, () => !string.IsNullOrWhiteSpace(NewPointDescription) && !IsLoading);
        RestorePointCommand = new AsyncRelayCommand(RestorePointAsync);
    }

    public IAsyncRelayCommand LoadDataCommand { get; }
    public IAsyncRelayCommand EnableProtectionCommand { get; }
    public IAsyncRelayCommand CreatePointCommand { get; }
    public IAsyncRelayCommand RestorePointCommand { get; }

    partial void OnNewPointDescriptionChanged(string value)
    {
        CreatePointCommand.NotifyCanExecuteChanged();
    }

    private async Task LoadDataAsync()
    {
        if (IsLoading) return;
        IsLoading = true;
        try
        {
            var statusResult = await _systemRestoreService.IsProtectionEnabledAsync(CancellationToken.None);
            if (statusResult.IsSuccess)
            {
                IsProtectionEnabled = statusResult.Value;
            }

            var pointsResult = await _systemRestoreService.ListPointsAsync(CancellationToken.None);
            if (pointsResult.IsSuccess && pointsResult.Value != null)
            {
                RestorePoints.Clear();
                foreach (var point in pointsResult.Value)
                {
                    RestorePoints.Add(point);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load System Restore data.");
            _toastService.ShowError("Failed to load restore points", ex.Message);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task EnableProtectionAsync()
    {
        IsLoading = true;
        try
        {
            var result = await _systemRestoreService.EnableProtectionAsync("C:\\", CancellationToken.None);
            if (result.IsSuccess)
            {
                _toastService.ShowSuccess("System Protection enabled", "Drive C:\\ is now protected.");
                await LoadDataAsync();
            }
            else
            {
                _toastService.ShowError("Failed to enable System Protection", result.ErrorMessage ?? "Unknown error.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Enable protection threw an exception.");
            _toastService.ShowError("Failed to enable System Protection", ex.Message);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task CreatePointAsync()
    {
        IsLoading = true;
        try
        {
            var result = await _systemRestoreService.CreatePointAsync(NewPointDescription, CancellationToken.None);
            if (result.IsSuccess)
            {
                _toastService.ShowSuccess("Restore point created", NewPointDescription);
                NewPointDescription = string.Empty;
                await LoadDataAsync();
            }
            else
            {
                _toastService.ShowError("Failed to create restore point", result.ErrorMessage ?? "Unknown error.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Create restore point threw an exception.");
            _toastService.ShowError("Failed to create restore point", ex.Message);
        }
        finally
        {
            IsLoading = false;
            CreatePointCommand.NotifyCanExecuteChanged();
        }
    }

    private async Task RestorePointAsync()
    {
        try
        {
            var result = await _systemRestoreService.RestoreToPointAsync(0, CancellationToken.None);
            if (!result.IsSuccess)
            {
                _toastService.ShowError("Failed to launch System Restore", result.ErrorMessage ?? "Unknown error.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Restore point launch threw an exception.");
            _toastService.ShowError("Failed to launch System Restore", ex.Message);
        }
    }
}
