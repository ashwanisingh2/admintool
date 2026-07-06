using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SysAdminX.Core.Interfaces;
using SysAdminX.Core.Models;

namespace SysAdminX.SystemRestore.ViewModels;

public partial class SystemRestoreViewModel : ObservableObject
{
    private readonly ISystemRestoreService _systemRestoreService;

    [ObservableProperty]
    private bool _isProtectionEnabled;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _newPointDescription = string.Empty;

    public ObservableCollection<SystemRestorePoint> RestorePoints { get; } = new();

    public SystemRestoreViewModel(ISystemRestoreService systemRestoreService)
    {
        _systemRestoreService = systemRestoreService;
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
        IsLoading = true;
        try
        {
            var statusResult = await _systemRestoreService.IsProtectionEnabledAsync(CancellationToken.None);
            if (statusResult.IsSuccess)
            {
                IsProtectionEnabled = statusResult.Value;
            }

            var pointsResult = await _systemRestoreService.ListPointsAsync(CancellationToken.None);
            if (pointsResult.IsSuccess)
            {
                RestorePoints.Clear();
                foreach (var point in pointsResult.Value)
                {
                    RestorePoints.Add(point);
                }
            }
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
                MessageBox.Show("System Protection has been enabled on drive C:\\", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                await LoadDataAsync();
            }
            else
            {
                MessageBox.Show($"Failed to enable System Protection: {result.ErrorMessage}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
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
                MessageBox.Show("Restore point created successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                NewPointDescription = string.Empty;
                await LoadDataAsync();
            }
            else
            {
                MessageBox.Show($"Failed to create restore point: {result.ErrorMessage}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        finally
        {
            IsLoading = false;
            CreatePointCommand.NotifyCanExecuteChanged();
        }
    }

    private async Task RestorePointAsync()
    {
        var result = await _systemRestoreService.RestoreToPointAsync(0, CancellationToken.None);
        if (!result.IsSuccess)
        {
            MessageBox.Show($"Failed to launch System Restore: {result.ErrorMessage}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
