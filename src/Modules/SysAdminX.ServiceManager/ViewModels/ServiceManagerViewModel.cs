// -----------------------------------------------------------------------
// <copyright file="ServiceManagerViewModel.cs" company="SysAdminX">
//     Copyright (c) SysAdminX. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using SysAdminX.Core.Interfaces;
using SysAdminX.Core.Models;

namespace SysAdminX.ServiceManager.ViewModels;

/// <summary>
/// ViewModel for managing Windows services.
/// </summary>
public partial class ServiceManagerViewModel : ObservableObject
{
    private readonly ILogger<ServiceManagerViewModel> _logger;
    private readonly IServiceManagerService _serviceManagerService;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelection))]
    private WindowsServiceModel? _selectedService;

    [ObservableProperty]
    private string _searchQuery = string.Empty;

    public ObservableCollection<WindowsServiceModel> Services { get; } = new();
    public ObservableCollection<WindowsServiceModel> FilteredServices { get; } = new();

    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);
    public bool HasSelection => SelectedService != null;

    public ServiceManagerViewModel(
        ILogger<ServiceManagerViewModel> logger,
        IServiceManagerService serviceManagerService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _serviceManagerService = serviceManagerService ?? throw new ArgumentNullException(nameof(serviceManagerService));
    }

    [RelayCommand]
    private async Task LoadServicesAsync()
    {
        IsLoading = true;
        ErrorMessage = string.Empty;
        Services.Clear();
        FilteredServices.Clear();

        try
        {
            var result = await _serviceManagerService.GetServicesAsync();
            if (result.IsSuccess && result.Value != null)
            {
                foreach (var service in result.Value)
                {
                    Services.Add(service);
                }
                ApplyFilter();
            }
            else
            {
                ErrorMessage = result.ErrorMessage ?? "Unknown error loading services.";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception loading services");
            ErrorMessage = "An unexpected error occurred.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task StartServiceAsync()
    {
        if (SelectedService == null) return;

        IsLoading = true;
        var result = await _serviceManagerService.StartServiceAsync(SelectedService.Name);
        if (!result.IsSuccess)
        {
            ErrorMessage = result.ErrorMessage ?? "Failed to start service.";
        }
        await LoadServicesAsync();
    }

    [RelayCommand]
    private async Task StopServiceAsync()
    {
        if (SelectedService == null) return;

        IsLoading = true;
        var result = await _serviceManagerService.StopServiceAsync(SelectedService.Name);
        if (!result.IsSuccess)
        {
            ErrorMessage = result.ErrorMessage ?? "Failed to stop service.";
        }
        await LoadServicesAsync();
    }

    [RelayCommand]
    private async Task RestartServiceAsync()
    {
        if (SelectedService == null) return;

        IsLoading = true;
        var result = await _serviceManagerService.RestartServiceAsync(SelectedService.Name);
        if (!result.IsSuccess)
        {
            ErrorMessage = result.ErrorMessage ?? "Failed to restart service.";
        }
        await LoadServicesAsync();
    }

    [RelayCommand]
    private async Task ChangeStartupTypeAsync(string startMode)
    {
        if (SelectedService == null || string.IsNullOrEmpty(startMode)) return;

        IsLoading = true;
        var result = await _serviceManagerService.ChangeStartupTypeAsync(SelectedService.Name, startMode);
        if (!result.IsSuccess)
        {
            ErrorMessage = result.ErrorMessage ?? "Failed to change startup type.";
        }
        await LoadServicesAsync();
    }

    partial void OnSearchQueryChanged(string value)
    {
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        FilteredServices.Clear();
        
        var query = SearchQuery?.ToLowerInvariant() ?? string.Empty;
        var filtered = string.IsNullOrWhiteSpace(query)
            ? Services
            : Services.Where(s => 
                (s.Name?.ToLowerInvariant().Contains(query) == true) || 
                (s.DisplayName?.ToLowerInvariant().Contains(query) == true) ||
                (s.Description?.ToLowerInvariant().Contains(query) == true));

        foreach (var item in filtered)
        {
            FilteredServices.Add(item);
        }
    }
}
