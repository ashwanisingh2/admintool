// -----------------------------------------------------------------------
// <copyright file="SoftwareManagerViewModel.cs" company="SysAdminX">
//     Copyright (c) SysAdminX. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using SysAdminX.Core.Interfaces;
using SysAdminX.Core.Models;

namespace SysAdminX.SoftwareManager.ViewModels;

public partial class SoftwareManagerViewModel : ObservableObject
{
    private readonly ILogger<SoftwareManagerViewModel> _logger;
    private readonly ISoftwareManagerService _softwareService;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    private string _searchQuery = string.Empty;

    [ObservableProperty]
    private SoftwareItemModel? _selectedSoftware;

    public ObservableCollection<SoftwareItemModel> SoftwareList { get; } = new();
    public ObservableCollection<SoftwareItemModel> FilteredSoftwareList { get; } = new();

    public SoftwareManagerViewModel(
        ILogger<SoftwareManagerViewModel> logger,
        ISoftwareManagerService softwareService)
    {
        _logger = logger;
        _softwareService = softwareService;
    }

    [RelayCommand]
    private async Task LoadSoftwareAsync()
    {
        IsLoading = true;
        ErrorMessage = string.Empty;
        SoftwareList.Clear();
        FilteredSoftwareList.Clear();

        var result = await _softwareService.GetInstalledSoftwareAsync();
        if (result.IsSuccess && result.Value != null)
        {
            foreach (var item in result.Value.OrderBy(s => s.DisplayName))
            {
                SoftwareList.Add(item);
            }
            ApplyFilter();
        }
        else
        {
            ErrorMessage = result.ErrorMessage ?? "Failed to load software.";
        }

        IsLoading = false;
    }

    [RelayCommand]
    private async Task UninstallAsync()
    {
        if (SelectedSoftware == null) return;
        
        if (string.IsNullOrEmpty(SelectedSoftware.UninstallString))
        {
            ErrorMessage = "Uninstall string is missing for this application.";
            return;
        }

        IsLoading = true;
        ErrorMessage = string.Empty;

        var result = await _softwareService.UninstallSoftwareAsync(SelectedSoftware.UninstallString);
        if (!result.IsSuccess)
        {
            ErrorMessage = result.ErrorMessage ?? "Uninstall failed.";
        }
        else
        {
            // Usually uninstallation is asynchronous or launches a wizard. We wait briefly then reload.
            await Task.Delay(2000);
            await LoadSoftwareAsync();
        }

        IsLoading = false;
    }

    partial void OnSearchQueryChanged(string value)
    {
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        FilteredSoftwareList.Clear();
        var query = SearchQuery.ToLowerInvariant();
        
        var filtered = string.IsNullOrWhiteSpace(query)
            ? SoftwareList
            : SoftwareList.Where(s => s.DisplayName.ToLowerInvariant().Contains(query) || s.Publisher.ToLowerInvariant().Contains(query));

        foreach (var item in filtered)
        {
            FilteredSoftwareList.Add(item);
        }
    }
}
